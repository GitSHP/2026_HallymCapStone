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
        }

        public float BaseScale { get; private set; } = 0.82f;   // 기물의 기본 크기 -> 연출이 끝나면 이 크기로 돌아옴

        public void SnapToCoord()   // 연출 없이 기물이 있는 칸 위치로 바로 옮기는 함수
            => transform.position = _grid.CoordToWorld(Piece.Coord);

        public void SetSortingBoost(bool lifted)    // 드래그 중에는 다른 기물보다 위에 그려지도록 그리기 순서를 올리고 내리는 함수
            => _body.sortingOrder = lifted ? PieceSortingOrder + 20 : PieceSortingOrder;

        public void Flash() => Fx.HitFlash(_body, _bodyColor);  // 맞았을 때 번쩍이며 흔들리는 연출을 재생하는 함수

        public void PlayDeath() // 잡혔을 때 사라지는 연출을 재생하고, 끝나면 오브젝트를 파괴하는 함수
            => Fx.Die(transform, () => Destroy(gameObject));
    }
}
