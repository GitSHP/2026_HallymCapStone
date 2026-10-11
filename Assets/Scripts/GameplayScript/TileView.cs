using Promotion.Core;
using Promotion.Feel;
using PrimeTween;
using UnityEngine;

namespace Promotion.Gameplay
{
    public enum TileHighlight
    {
        None,
        Hover,
        Selected,
        Move,
        Attack,
        Blocked
    }

    /// <summary>
    /// 보드의 한 칸이다. 바탕 스프라이트(체크무늬 색)와 모든 강조 표시에 쓰는
    /// 오버레이 스프라이트를 따로 가진다. 그래서 강조 연출이 보드 자체의 배치를
    /// 흐트러뜨리지 않는다.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [Header("선택적 이미지 교체 (비워 두면 임시 이미지 사용)")]
        public Sprite baseSprite;
        public Sprite overlaySprite;

        public Coord Coord { get; private set; }

        SpriteRenderer _base;
        SpriteRenderer _overlay;
        Color _baseColor;
        TileHighlight _highlight = TileHighlight.None;
        bool _hovered;

        static readonly Color HoverTint = new Color(1f, 1f, 1f, 0.22f);
        static readonly Color SelectedTint = new Color(1f, 0.92f, 0.35f, 0.55f);
        static readonly Color MoveTint = new Color(0.35f, 1f, 0.45f, 0.45f);
        static readonly Color AttackTint = new Color(1f, 0.3f, 0.3f, 0.55f);
        static readonly Color BlockedTint = new Color(0.4f, 0.4f, 0.45f, 0.45f);

        public void Init(Coord coord, Color baseColor, int sortingOrder)
        {
            Coord = coord;
            _baseColor = baseColor;
            name = $"Tile_{coord.x}_{coord.y}";

            _base = gameObject.AddComponent<SpriteRenderer>();
            _base.sprite = baseSprite != null ? baseSprite : PlaceholderArt.Square;
            _base.color = baseColor;
            _base.sortingOrder = sortingOrder;
            _base.sharedMaterial = PlaceholderArt.UnlitMaterial;

            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(transform, false);
            _overlay = overlayGo.AddComponent<SpriteRenderer>();
            _overlay.sprite = overlaySprite != null ? overlaySprite : PlaceholderArt.Square;
            _overlay.sortingOrder = sortingOrder + 1;
            _overlay.sharedMaterial = PlaceholderArt.UnlitMaterial;
            _overlay.enabled = false;
        }

        /// <summary>칸 자체의 색을 바꾼다. 아래에 보드 그림이 보일 때는 투명하게 둔다.</summary>
        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            if (_base != null)
                _base.color = color;
        }

        /// <param name="popIndex">여러 강조 표시가 한꺼번에 튀어나오지 않고 물결처럼 차례로 나타나게 하는 순번이다.</param>
        public void SetHighlight(TileHighlight highlight, int popIndex = 0)
        {
            if (_highlight == highlight)
                return;

            _highlight = highlight;

            if (highlight == TileHighlight.None)
            {
                _overlay.enabled = false;
                return;
            }

            _overlay.color = TintFor(highlight);
            _overlay.enabled = true;

            // 호버는 즉각적인 피드백으로 보여야 한다. 의도된 강조 표시에만 시차를 둔 팝 연출을 준다.
            if (highlight == TileHighlight.Hover)
                _overlay.transform.localScale = Vector3.one;
            else
                Fx.PopIn(_overlay.transform, popIndex);
        }

        public void SetHovered(bool hovered)
        {
            if (_hovered == hovered)
                return;

            _hovered = hovered;

            if (hovered)
                Fx.HoverIn(transform);
            else
                Fx.HoverOut(transform);

            // 호버 색은 이 칸에 더 중요한 표시가 없을 때만 보인다.
            if (_highlight == TileHighlight.None || _highlight == TileHighlight.Hover)
                SetHighlight(hovered ? TileHighlight.Hover : TileHighlight.None);
        }

        static Color TintFor(TileHighlight highlight) => highlight switch
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
