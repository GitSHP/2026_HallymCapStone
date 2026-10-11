using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// 인터페이스의 색상표와 치수를 한곳에 모은다.
    ///
    /// 짙은 남색에서 검정까지의 바탕에 파란 계열 강조색 하나로 빛을 준다. 아래의 보드가
    /// 어두운 플레이 화면이라 밝은 인터페이스는 따로 떠 있는 물체처럼 보였다. 패널을
    /// 보드보다 어둡게 두고 강조색만 빛나게 하면, 상점이 자신이 끼어든 게임과 같은
    /// 세계에 놓인 것처럼 보인다.
    /// </summary>
    public static class UiTheme
    {
        // ---------- 표면 (어두운 것부터 밝은 것 순) ----------

        public static readonly Color Backdrop = new Color(0.016f, 0.024f, 0.047f, 0.86f);
        public static readonly Color Panel = new Color(0.047f, 0.071f, 0.118f, 1f);
        public static readonly Color PanelEdge = new Color(0.086f, 0.129f, 0.208f, 1f);
        public static readonly Color Card = new Color(0.078f, 0.114f, 0.188f, 1f);
        public static readonly Color CardWell = new Color(0.114f, 0.161f, 0.259f, 1f);
        public static readonly Color CardDim = new Color(0.059f, 0.086f, 0.141f, 1f);

        // ---------- 글자색 ----------

        public static readonly Color Ink = new Color(0.902f, 0.937f, 0.988f, 1f);
        public static readonly Color InkMuted = new Color(0.455f, 0.541f, 0.682f, 1f);
        public static readonly Color InkFaint = new Color(0.278f, 0.345f, 0.459f, 1f);
        public static readonly Color InkOnAccent = new Color(0.047f, 0.071f, 0.118f, 1f);

        // ---------- 강조색 ----------

        public static readonly Color Gold = new Color(0.949f, 0.737f, 0.318f, 1f);
        public static readonly Color ArtifactAccent = new Color(0.545f, 0.494f, 1f, 1f);
        public static readonly Color PieceAccent = new Color(0.259f, 0.694f, 0.949f, 1f);
        public static readonly Color Confirm = new Color(0.231f, 0.510f, 0.965f, 1f);
        public static readonly Color Deny = new Color(0.976f, 0.400f, 0.400f, 1f);
        public static readonly Color Disabled = new Color(0.192f, 0.239f, 0.333f, 1f);

        /// <summary>아이콘 뒤에 까는 색 바탕이다. 강조색을 카드 위에 흐리게 녹인 색이다.</summary>
        public static Color Well(Color accent) => new Color(accent.r, accent.g, accent.b, 0.13f);

        /// <summary>아이콘 자체의 색이다. 바탕보다 밝게 띄운다.</summary>
        public static Color Glyph(Color accent) => Color.Lerp(accent, Color.white, 0.25f);

        // ---------- 치수 ----------

        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public const float PanelWidth = 1320f;
        public const float PanelHeight = 960f;
        public const float PanelPadding = 48f;

        public const float CardWidth = 320f;
        public const float CardHeight = 280f;
        public const float CardGap = 40f;

        /// <summary>보드와 스테이지 HUD 위에 올라올 만큼 높은 캔버스 정렬 순서다.</summary>
        public const int OverlaySortingOrder = 200;
    }
}
