using UnityEngine;

namespace Promotion.UI
{
    public static class UiTheme  // UI 전체에서 같이 쓰는 색상과 크기 값을 한 곳에 모아 둔 클래스 -> 색을 바꿀 때 여기만 고치면 됨
    {
        // -----배경·패널 색-----
        public static readonly Color Backdrop = new Color(0.016f, 0.024f, 0.047f, 0.86f);  // 화면 전체를 어둡게 덮는 반투명 배경 색
        public static readonly Color Panel = new Color(0.047f, 0.071f, 0.118f, 1f);  // 패널(창) 바탕 색
        public static readonly Color PanelEdge = new Color(0.086f, 0.129f, 0.208f, 1f);  // 패널 테두리 색
        public static readonly Color Card = new Color(0.078f, 0.114f, 0.188f, 1f);  // 카드 바탕 색
        public static readonly Color CardWell = new Color(0.114f, 0.161f, 0.259f, 1f);  // 카드 안쪽 오목한 영역(아이콘 자리) 색
        public static readonly Color CardDim = new Color(0.059f, 0.086f, 0.141f, 1f);  // 구매했거나 비활성된 카드의 어두운 바탕 색

        // -----글자 색-----
        public static readonly Color Ink = new Color(0.902f, 0.937f, 0.988f, 1f);  // 기본 글자 색 (밝은 흰색 계열)
        public static readonly Color InkMuted = new Color(0.455f, 0.541f, 0.682f, 1f);  // 보조 설명용 흐린 글자 색
        public static readonly Color InkFaint = new Color(0.278f, 0.345f, 0.459f, 1f);  // 가장 흐린 글자 색 (비활성 표시 등)
        public static readonly Color InkOnAccent = new Color(0.047f, 0.071f, 0.118f, 1f);  // 밝은 강조색 위에 올라가는 어두운 글자 색

        // -----강조 색-----
        public static readonly Color Gold = new Color(0.949f, 0.737f, 0.318f, 1f);  // 골드(가격·재화) 표시 색
        public static readonly Color ArtifactAccent = new Color(0.545f, 0.494f, 1f, 1f);  // 유물 카드의 강조 색 (보라)
        public static readonly Color PieceAccent = new Color(0.259f, 0.694f, 0.949f, 1f);  // 기물 카드의 강조 색 (하늘)
        public static readonly Color Confirm = new Color(0.231f, 0.510f, 0.965f, 1f);  // 확인·진행 버튼 색 (파랑)
        public static readonly Color Deny = new Color(0.976f, 0.400f, 0.400f, 1f);  // 거절·경고 색 (빨강)
        public static readonly Color Disabled = new Color(0.192f, 0.239f, 0.333f, 1f);  // 누를 수 없는 버튼 색

        public static Color Well(Color accent) => new Color(accent.r, accent.g, accent.b, 0.13f);  // 강조색을 아주 옅게(투명도 13%) 만든 배경 색을 돌려주는 함수

        public static Color Glyph(Color accent) => Color.Lerp(accent, Color.white, 0.25f);  // 강조색을 흰색 쪽으로 25% 밝게 만든 아이콘 색을 돌려주는 함수

        // -----화면 기준 해상도-----
        public const float ReferenceWidth = 1920f;  // CanvasScaler 기준 가로 해상도
        public const float ReferenceHeight = 1080f;  // CanvasScaler 기준 세로 해상도

        // -----패널 크기-----
        public const float PanelWidth = 1320f;  // 상점 등 큰 패널의 가로 길이
        public const float PanelHeight = 960f;  // 큰 패널의 세로 길이
        public const float PanelPadding = 48f;  // 패널 안쪽 여백

        // -----카드 크기-----
        public const float CardWidth = 320f;  // 카드 한 장의 가로 길이
        public const float CardHeight = 280f;  // 카드 한 장의 세로 길이
        public const float CardGap = 40f;  // 카드 사이 간격

        public const int OverlaySortingOrder = 200;  // 오버레이 캔버스의 정렬 순서 -> 높을수록 다른 UI보다 위에 그려짐
    }
}
