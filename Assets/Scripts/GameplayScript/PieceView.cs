using Promotion.Core;
using Promotion.Feel;
using Promotion.UI;
using TMPro;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class PieceView : MonoBehaviour    // 기물 하나를 화면에 그려 주는 클래스 -> 게임 규칙은 없고 보이는 것만 담당
    {
        public const int PieceSortingOrder = 10;    // 기물 그림의 기본 그리기 순서 -> 칸(타일)보다 위에 그려지도록

        public Piece Piece { get; private set; }    // 이 오브젝트가 보여 주는 기물 데이터

        BoardGrid _grid;    // 칸 좌표 -> 월드 좌표 변환용
        SpriteRenderer _body;   // 기물 그림을 그리는 컴포넌트
        TextMeshPro _label; // 그림이 없을 때 대신 보여 줄 글자(예: ♞)
        Color _bodyColor;   // 기물의 기본 색 -> 피격 번쩍임 후 이 색으로 돌아감
        FireAura _aura; // 누적 킬로 붙은 불꽃 입자 묶음 -> 처음 불이 붙을 때 만들고, 꺼져도 지우지 않고 다음 점화에 다시 씀
        FireAura _idleAura; // 처음부터 늘 두르고 있는 오라 (중갑병의 보라·검정 오라) -> 연속 킬 불꽃(_aura)과 따로 관리
        bool _burning;  // 불을 붙이라는 명령을 받은 상태인지 여부 -> 점화 연출을 기다리는 동안에도 true (같은 점화가 두 번 겹치지 않게)

        public void Init(Piece piece, BoardGrid grid)   // 기물 데이터를 받아 그림, 색, 크기, 위치를 설정하는 함수 -> 생성 직후 한 번 호출
        {
            Piece = piece;
            _grid = grid;
            name = $"{piece.Team}_{piece.Def.itemName}";    // 하이어라키에서 알아보기 쉽게 "진영_기물이름"으로 이름 설정

            _body = gameObject.AddComponent<SpriteRenderer>();  // 그림을 그릴 컴포넌트 추가

            bool enemyArt = piece.Team == Team.Enemy && piece.Def.enemySprite != null; // 적이고 적 전용 그림이 있는지 여부
            var art = enemyArt ? piece.Def.enemySprite : piece.Def.itemImage;   // 적 전용 그림이 있으면 그것을, 아니면 기본 그림을 사용
            bool hasArt = art != null;  // 사용할 그림이 있는지 여부
            _body.sprite = hasArt ? art : UiArt.Circle;    // 그림이 없다면 임시 원 모양을 사용
            _bodyColor = enemyArt ? Color.white
                : piece.Team == Team.Player ? piece.Def.playerColor : piece.Def.enemyColor;
            // 적 전용 그림은 원래 색 그대로(흰색 = 색 변화 없음), 아니면 진영별 색을 입힘
            _body.color = _bodyColor;
            _body.sortingOrder = PieceSortingOrder;
            _body.sharedMaterial = PlaceholderArt.UnlitMaterial;    // 조명 영향을 받지 않는 재질 -> 항상 같은 밝기로 보임

            if (!hasArt)    // 만약 그림이 없다면 원 위에 기물 글자를 띄워서 어떤 기물인지 알 수 있게 함
            {
                var labelGo = new GameObject("Glyph");  // 글자용 자식 오브젝트를 만들고
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);    // 원보다 살짝 앞에 둠

                _label = labelGo.AddComponent<TextMeshPro>();
                _label.text = piece.Def.glyph;  // 기물 데이터에 지정된 글자
                _label.fontSize = 5f;
                _label.alignment = TextAlignmentOptions.Center;
                _label.color = piece.Team == Team.Player
                    ? new Color(0.1f, 0.1f, 0.12f)
                    : new Color(0.95f, 0.9f, 0.85f);    // 아군은 어두운 글자, 적은 밝은 글자
                _label.rectTransform.sizeDelta = new Vector2(1f, 1f);
                _label.GetComponent<MeshRenderer>().sortingOrder = PieceSortingOrder + 1;   // 글자가 원보다 위에 그려지도록
            }

            BaseScale = hasArt ? 1f : 0.82f;    // 그림이 있으면 원래 크기, 임시 원이면 칸보다 조금 작게
            transform.localScale = Vector3.one * BaseScale;
            SnapToCoord();  // 기물이 있는 칸 위치로 이동

            if (piece.Def.idleAura != null) // 늘 두르는 오라가 지정된 기물이라면(중갑병) 나오자마자 조용히 타오르게 함
            {
                var prefab = piece.Def.idleAura.GetComponent<FireAura>();
                if (prefab != null)
                {
                    _idleAura = Instantiate(prefab, transform);
                    _idleAura.transform.localPosition = Vector3.zero;
                    _idleAura.SetSortingOrder(_body.sortingOrder);
                    _idleAura.PlayIdle();
                }
            }
        }

        public Sprite Art   // 보드 위에서 그리고 있는 기물 그림 -> 처치 하이라이트에 같은 그림을 띄울 때 사용
        {
            get { return _body != null ? _body.sprite : null; }
        }

        public Color ArtColor   // 기물 그림의 기본 색 (피격 번쩍임·불빛 깜빡임 중이어도 원래 색)
        {
            get { return _bodyColor; }
        }

        public float BaseScale { get; private set; } = 0.82f;   // 기물의 기본 크기 -> 연출이 끝나면 이 크기로 돌아옴

        public void SnapToCoord()   // 연출 없이 기물이 있는 칸 위치로 바로 옮기는 함수
        {
            transform.position = _grid.CoordToWorld(Piece.Coord);
        }

        public void SetSortingBoost(bool lifted)    // 드래그 중에는 다른 기물보다 위에 그려지도록 그리기 순서를 올리고 내리는 함수
        {
            _body.sortingOrder = lifted ? PieceSortingOrder + 20 : PieceSortingOrder;
            if (_aura != null)  // 불이 붙어 있다면 불꽃 층도 몸체를 따라 올리고 내림 -> 드래그 중에도 불꽃은 몸체 뒤, 불티는 앞
                _aura.SetSortingOrder(_body.sortingOrder);
            if (_idleAura != null)
                _idleAura.SetSortingOrder(_body.sortingOrder);
        }

        public void SetSortingBehind()  // 잡힌 기물을 다른 기물보다 아래에 그리도록 순서를 내리는 함수
        // -> 사라지는 연출 동안 잡은 기물(가해자)이 그 위를 덮어 '찍어 누르는' 것처럼 보이게 함
        {
            _body.sortingOrder = PieceSortingOrder - 5;
            if (_label != null)
                _label.GetComponent<MeshRenderer>().sortingOrder = PieceSortingOrder - 4;
            if (_aura != null)
                _aura.SetSortingOrder(_body.sortingOrder);
            if (_idleAura != null)
                _idleAura.SetSortingOrder(_body.sortingOrder);
        }

        public void PlayBlocked()   // 잡기를 막아 냈을 때(중갑병 <- 폰) 몸체가 '팅' 하고 흔들리는 연출을 재생하는 함수
        {
            Fx.Blocked(transform);
        }

        public bool Burning // 지금 기물에 불이 타고 있는지 여부
        {
            get { return _burning; }
        }

        public void SetBurning(bool on, FireAura prefab, Transform boardRoot)
        // 누적 킬 불꽃을 켜고 끄는 함수 -> KillStreakTracker가 스테이지 누적 킬 수에 따라 호출함
        // on: true면 불을 붙이고, false면 끔
        // prefab: 처음 불이 붙을 때 복제할 FireAura 프리팹 (KillStreakTracker 인스펙터에 연결됨)
        // boardRoot: 점화 순간 흔들 체스판 (Board 오브젝트)
        {
            if (on == Burning)  // 이미 그 상태라면 아무것도 하지 않음 -> 불붙은 기물이 또 잡아도 점화 연출이 반복되지 않음
                return;

            if (!on)    // 끄는 경우 -> 입자 방출을 멈추고 몸체 색을 되돌림
            {
                _burning = false;
                if (_aura != null)
                    _aura.Extinguish();
                Fx.FireGlowStop(_body, _bodyColor);
                return;
            }

            if (_aura == null)  // 처음 불이 붙는다면 불꽃 입자 묶음을 만들어 기물 자식으로 붙임 -> 기물을 따라 움직이고, 기물이 사라지면 함께 사라짐
            {
                if (prefab == null)
                    return;
                _aura = Instantiate(prefab, transform);
                _aura.transform.localPosition = Vector3.zero;
            }
            _aura.SetSortingOrder(_body.sortingOrder);
            _burning = true;

            var aura = _aura;
            Fx.FireIgnite(transform, boardRoot, BaseScale, () =>    // 잡기 연출이 끝나면 점화 (기다리는 사이 기물이 잡히면 취소됨)
            {
                if (!_burning)  // 기다리는 사이 다시 꺼졌다면 점화하지 않음
                    return;
                aura.Ignite();
                Fx.FireGlow(_body);
            });
        }

        public void Flash()  // 맞았을 때 번쩍이며 흔들리는 연출을 재생하는 함수
        {
            Fx.HitFlash(_body, _bodyColor);
        }

        public void PlayDeath() // 잡혔을 때 사라지는 연출을 재생하고, 끝나면 오브젝트를 파괴하는 함수
        {
            Fx.Die(transform, () => Destroy(gameObject));
        }
    }
}
