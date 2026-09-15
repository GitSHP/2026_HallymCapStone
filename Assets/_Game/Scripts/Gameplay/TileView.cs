using Gambonanza.Core;
using Gambonanza.Feel;
using PrimeTween;
using UnityEngine;

namespace Gambonanza.Gameplay
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
    /// One board square. Owns a base sprite (the checker colour) and an overlay
    /// sprite used for every highlight, so highlight animation never disturbs
    /// the board's own layout.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [Header("Optional art overrides (leave empty to use placeholders)")]
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

        /// <param name="popIndex">Stagger index so a set of highlights ripples in rather than snapping.</param>
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

            // Hover reads as instant feedback; deliberate highlights get the staggered pop.
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

            // Hover tint only shows when nothing more important is on this tile.
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
