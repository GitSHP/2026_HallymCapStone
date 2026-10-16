using Promotion.Core;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    public static class UiBuilder  // 코드에서 UI 오브젝트를 만들 때 쓰는 도우미 함수 모음
    // -> 화면 레이아웃의 원본은 Assets/UI/Prefabs의 프리팹이고, 이 클래스는 작은 보조 요소를 코드로 만들 때만 사용함
    {
        public static RectTransform Node(string name, Transform parent)  // 이름만 있는 빈 UI 오브젝트를 만들어 부모 밑에 붙이는 함수
        {
            var go = new GameObject(name, typeof(RectTransform));  // RectTransform을 가진 새 오브젝트를 만들고
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);  // 부모 밑으로 옮김 -> false를 줘서 부모 기준 위치·크기를 그대로 유지
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color, bool rounded = true)  // 단색 패널(배경 이미지)을 만드는 함수
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();  // 빈 오브젝트에 Image 컴포넌트를 붙이고
            image.sprite = rounded ? UiArt.RoundedRect : UiArt.Square;  // 둥근 모서리 여부에 따라 사용할 스프라이트를 고름
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;  // 둥근 사각형은 Sliced로 그려서 크기가 바뀌어도 모서리가 찌그러지지 않게 함
            image.color = color;
            image.pixelsPerUnitMultiplier = 1f;  // 모서리 둥글기 배율을 기본값으로 고정
            return image;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, string text,
            float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)  // 글자(텍스트 UI)를 만드는 함수
        {
            var rect = Node(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();  // 빈 오브젝트에 TextMeshPro 텍스트를 붙이고
            label.text = text;  // 표시할 내용
            label.fontSize = size;  // 글자 크기
            label.color = color;  // 글자 색
            label.alignment = alignment;  // 정렬 방식 (기본: 가운데)
            label.fontStyle = style;  // 굵게·기울임 등 스타일
            label.raycastTarget = false;  // 글자가 마우스 클릭을 가로채지 않도록 함 -> 아래에 있는 버튼이 정상적으로 눌림
            label.textWrappingMode = TextWrappingModes.Normal;  // 영역을 넘으면 자동 줄바꿈
            return label;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color)  // 스프라이트를 받아 아이콘 이미지를 만드는 함수
        {
            var rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;  // 원본 비율을 유지 -> 아이콘이 늘어나 보이지 않게 함
            image.raycastTarget = false;  // 아이콘이 클릭을 가로채지 않도록 함
            return image;
        }

        public static Image Icon(string name, Transform parent, IconId id, Color color)  // 아이콘 종류(IconId)로 아이콘 이미지를 만드는 함수
            => Icon(name, parent, UiSprites.Get(id), color);  // IconId에 맞는 스프라이트를 찾아 위의 Icon 함수에 넘김

        public static CanvasGroup Group(GameObject go)  // 오브젝트의 CanvasGroup을 가져오는 함수 -> 없으면 새로 붙여서 돌려줌
        // -> CanvasGroup은 자식 UI 전체의 투명도를 한 번에 조절할 때 사용 (페이드 연출용)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();  // 이미 있으면 그대로, 없으면 추가
        }

        public static RectTransform Stretch(RectTransform rect, float padding = 0f)  // UI를 부모 영역 전체에 꽉 차게 늘리는 함수 (padding만큼 안쪽 여백)
        {
            rect.anchorMin = Vector2.zero;  // 앵커를 부모의 왼쪽 아래(0,0)부터
            rect.anchorMax = Vector2.one;  // 오른쪽 위(1,1)까지로 설정 -> 부모 크기를 따라감
            rect.pivot = new Vector2(0.5f, 0.5f);  // 기준점은 가운데
            rect.offsetMin = new Vector2(padding, padding);  // 왼쪽·아래 여백
            rect.offsetMax = new Vector2(-padding, -padding);  // 오른쪽·위 여백
            return rect;
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)  // UI를 부모의 한 점(anchor)에 고정하고 위치·크기를 지정하는 함수
        {
            rect.anchorMin = anchor;  // 앵커 최소·최대를 같은 점으로 두어
            rect.anchorMax = anchor;  // 부모 크기가 바뀌어도 UI 크기는 그대로 유지되게 함
            rect.pivot = pivot;  // UI 자신의 기준점
            rect.sizeDelta = size;  // 가로·세로 크기
            rect.anchoredPosition = anchoredPosition;  // 앵커로부터 떨어진 위치
            return rect;
        }

        // -----자주 쓰는 앵커 위치 (Place 함수에 넘겨 사용)-----
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);  // 왼쪽 위
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);  // 오른쪽 위
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);  // 오른쪽 아래
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);  // 정가운데
        public static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);  // 위쪽 가운데

        public static Canvas OverlayCanvas(string name, int sortingOrder)  // 화면 맨 위에 덮이는 오버레이 캔버스를 새로 만드는 함수
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;  // 카메라와 상관없이 화면 위에 바로 그리는 방식
            canvas.sortingOrder = sortingOrder;  // 다른 캔버스보다 위·아래에 그려질지 결정하는 순서

            var scaler = go.AddComponent<CanvasScaler>();  // 해상도가 달라도 UI 비율이 유지되도록 CanvasScaler를 붙이고
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;  // 화면 크기에 맞춰 UI 크기를 조절
            scaler.referenceResolution = new Vector2(UiTheme.ReferenceWidth, UiTheme.ReferenceHeight);  // 기준 해상도 1920x1080
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;  // 가로와 세로를 반반씩 기준으로 맞춤

            go.AddComponent<GraphicRaycaster>();  // 이 캔버스의 버튼이 마우스 클릭을 받을 수 있게 함
            return canvas;
        }
    }
}
