using Promotion.Core;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// Small constructors for the uGUI objects every screen needs. The project
    /// builds its views in code rather than from prefabs (see PieceView, TileView),
    /// and this keeps that code readable instead of a wall of RectTransform setup.
    /// </summary>
    public static class UiBuilder
    {
        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color, bool rounded = true)
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded ? PlaceholderArt.RoundedRect : PlaceholderArt.Square;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            // Sliced sprites smaller than their borders otherwise refuse to draw.
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text,
            float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)
        {
            var rect = Node(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        /// <summary>
        /// An icon image. Never a raycast target: the card or button under it owns
        /// the click, and an icon that swallowed pointer-exit would strand a hover.
        /// </summary>
        public static Image Icon(string name, Transform parent, Sprite sprite, Color color)
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Image Icon(string name, Transform parent, IconId id, Color color)
            => Icon(name, parent, UiSprites.Get(id), color);

        public static CanvasGroup Group(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }

        // ---------- Anchoring ----------

        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        /// <summary>Anchors and sizes in one call; anchoredPosition is relative to that anchor.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        // ---------- Screens ----------

        /// <summary>
        /// A full-screen overlay canvas. Scales against a 1920x1080 reference so the
        /// layout below can be written in fixed pixels and still hold at any resolution.
        /// </summary>
        public static Canvas OverlayCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiTheme.ReferenceWidth, UiTheme.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
