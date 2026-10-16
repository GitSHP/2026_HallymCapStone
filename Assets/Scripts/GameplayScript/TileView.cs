using Promotion.Core;
using Promotion.Feel;
using Promotion.UI;
using PrimeTween;
using UnityEngine;

namespace Promotion.Gameplay
{
    public enum TileHighlight   // 칸 강조 표시의 종류
    {
        None,   // 표시 없음
        Hover,  // 포인터가 올라가 있음(흰색)
        Selected,   // 집어 든 기물의 출발 칸(노란색)
        Move,   // 이동/배치할 수 있는 칸(초록색)
        Attack, // 잡기를 할 수 있는 칸(빨간색)
        Blocked // 막힌 칸(회색)
    }

    public class TileView : MonoBehaviour   // 보드의 칸 하나를 화면에 그리고 강조 표시를 보여 주는 클래스
    // 칸 그림(base) 위에 반투명 덮개(overlay)를 하나 더 두고, 덮개의 색으로 강조 표시를 함
    {
        [Header("선택적 이미지 교체 (비워 두면 임시 이미지 사용)")]
        public Sprite baseSprite;   // 칸 바닥 그림(없으면 임시 사각형)
        public Sprite overlaySprite;    // 강조 표시용 덮개 그림(없으면 임시 사각형)

        public Coord Coord { get; private set; }    // 이 칸의 보드 좌표

        SpriteRenderer _base;   // 칸 바닥을 그리는 컴포넌트
        SpriteRenderer _overlay;    // 강조 표시 덮개를 그리는 컴포넌트
        Color _baseColor;   // 칸 바닥의 기본 색(체스판처럼 밝은 칸/어두운 칸)
        TileHighlight _highlight = TileHighlight.None;  // 현재 강조 표시 종류
        bool _hovered;  // 지금 포인터가 이 칸 위에 있는지 여부

        static readonly Color HoverTint = new Color(1f, 1f, 1f, 0.22f);    // 포인터 표시 색(옅은 흰색)
        static readonly Color SelectedTint = new Color(1f, 0.92f, 0.35f, 0.55f);   // 선택 표시 색(노란색)
        static readonly Color MoveTint = new Color(0.35f, 1f, 0.45f, 0.45f);   // 이동 가능 표시 색(초록색)
        static readonly Color AttackTint = new Color(1f, 0.3f, 0.3f, 0.55f);   // 잡기 가능 표시 색(빨간색)
        static readonly Color BlockedTint = new Color(0.4f, 0.4f, 0.45f, 0.45f);   // 막힘 표시 색(회색)

        public void Init(Coord coord, Color baseColor, int sortingOrder)    // 칸의 좌표, 색, 그리기 순서를 설정하는 함수 -> 보드를 만들 때 한 번 호출
        {
            Coord = coord;
            _baseColor = baseColor;
            name = $"Tile_{coord.x}_{coord.y}"; // 하이어라키에서 알아보기 쉽게 "Tile_x_y"로 이름 설정

            _base = gameObject.AddComponent<SpriteRenderer>();  // 칸 바닥 그림 컴포넌트를 추가하고 설정
            _base.sprite = baseSprite != null ? baseSprite : UiArt.Square;
            _base.color = baseColor;
            _base.sortingOrder = sortingOrder;
            _base.sharedMaterial = PlaceholderArt.UnlitMaterial;    // 조명 영향을 받지 않는 재질

            var overlayGo = new GameObject("Overlay");  // 강조 표시용 자식 오브젝트를 만들고
            overlayGo.transform.SetParent(transform, false);
            _overlay = overlayGo.AddComponent<SpriteRenderer>();
            _overlay.sprite = overlaySprite != null ? overlaySprite : UiArt.Square;
            _overlay.sortingOrder = sortingOrder + 1;   // 바닥보다 위에 그려지도록
            _overlay.sharedMaterial = PlaceholderArt.UnlitMaterial;
            _overlay.enabled = false;   // 처음에는 강조 표시 없음
        }

        public void SetBaseColor(Color color)   // 칸 바닥 색을 바꾸는 함수(보드 그림/테마 변경용)
        {
            _baseColor = color;
            if (_base != null)
                _base.color = color;
        }

        public void SetHighlight(TileHighlight highlight, int popIndex = 0) // 칸의 강조 표시를 바꾸는 함수
        // popIndex는 등장 순서 -> 여러 칸이 동시에 켜질 때 순서대로 조금씩 늦게 나타나 물결처럼 보이게 함
        {
            if (_highlight == highlight)    // 이미 같은 표시라면 아무것도 하지 않음 -> 연출이 매번 다시 재생되지 않게
                return;

            _highlight = highlight;

            if (highlight == TileHighlight.None)    // 표시 없음이라면 덮개를 끄고 종료
            {
                _overlay.enabled = false;
                return;
            }

            _overlay.color = TintFor(highlight);    // 종류에 맞는 색으로 덮개를 칠하고
            _overlay.enabled = true;    // 덮개를 켬

            if (highlight == TileHighlight.Hover)   // 포인터 표시는 자주 바뀌므로 연출 없이 바로 원래 크기로
                _overlay.transform.localScale = Vector3.one;
            else    // 나머지 표시는 톡 튀어나오는 연출로 나타남
                Fx.PopIn(_overlay.transform, popIndex);
        }

        public void SetHovered(bool hovered)    // 포인터가 이 칸에 올라오거나 벗어났을 때 호출되는 함수
        {
            if (_hovered == hovered)    // 상태가 그대로라면 아무것도 하지 않음
                return;

            _hovered = hovered;

            if (hovered)    // 올라왔다면 칸을 살짝 키우고
                Fx.HoverIn(transform);
            else    // 벗어났다면 원래 크기로 되돌림
                Fx.HoverOut(transform);

            if (_highlight == TileHighlight.None || _highlight == TileHighlight.Hover)
            // 다른 강조 표시(이동, 잡기 등)가 없을 때만 포인터 표시를 켜고 끔 -> 중요한 표시를 덮어쓰지 않게
                SetHighlight(hovered ? TileHighlight.Hover : TileHighlight.None);
        }

        static Color TintFor(TileHighlight highlight) => highlight switch   // 강조 표시 종류에 맞는 색을 돌려주는 함수
        {
            TileHighlight.Hover => HoverTint,
            TileHighlight.Selected => SelectedTint,
            TileHighlight.Move => MoveTint,
            TileHighlight.Attack => AttackTint,
            TileHighlight.Blocked => BlockedTint,
            _ => Color.clear
        };
    }
}
