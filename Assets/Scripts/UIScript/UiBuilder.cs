using Promotion.Core;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// 모든 화면에 필요한 uGUI 오브젝트를 만드는 작은 생성 함수 모음이다. 이 프로젝트는
    /// 뷰를 프리팹이 아니라 코드로 만든다(PieceView, TileView 참고). 이 클래스는 그 코드가
    /// RectTransform 설정의 나열로 뒤덮이지 않고 읽기 쉽게 유지되도록 한다.
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
            image.sprite = rounded ? UiArt.RoundedRect : UiArt.Square;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            // 이렇게 하지 않으면 테두리보다 작은 슬라이스 스프라이트는 그려지지 않는다.
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
        /// 아이콘 이미지를 만든다. 레이캐스트 대상으로 두지 않는다. 클릭은 아래의 카드나
        /// 버튼이 받아야 하고, 아이콘이 포인터 이탈 이벤트를 삼키면 호버 상태가 풀리지 않고 남는다.
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

        // ---------- 앵커 배치 ----------

        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        /// <summary>앵커와 크기를 한 번에 지정한다. anchoredPosition은 그 앵커를 기준으로 한다.</summary>
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

        // ---------- 화면 ----------

        /// <summary>
        /// 전체 화면 오버레이 캔버스를 만든다. 1920x1080 기준 해상도에 맞춰 크기를 조절하므로,
        /// 그 아래 레이아웃을 고정 픽셀로 작성해도 어떤 해상도에서든 유지된다.
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
