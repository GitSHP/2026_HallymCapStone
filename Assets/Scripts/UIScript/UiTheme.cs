using UnityEngine;

namespace Gambonanza.UI
{
    /// <summary>
    /// The palette and metrics of the interface in one place.
    ///
    /// Dark navy through black, lit by one blue accent family. The board below is
    /// a dark playfield, so a pale interface floated off it as a separate object;
    /// keeping the panel darker than the board and letting only the accents glow
    /// puts the shop in the same world as the game it interrupts.
    /// </summary>
    public static class UiTheme
    {
        // ---------- Surfaces (darkest to lightest) ----------

        public static readonly Color Backdrop = new Color(0.016f, 0.024f, 0.047f, 0.86f);
        public static readonly Color Panel = new Color(0.047f, 0.071f, 0.118f, 1f);
        public static readonly Color PanelEdge = new Color(0.086f, 0.129f, 0.208f, 1f);
        public static readonly Color Card = new Color(0.078f, 0.114f, 0.188f, 1f);
        public static readonly Color CardWell = new Color(0.114f, 0.161f, 0.259f, 1f);
        public static readonly Color CardDim = new Color(0.059f, 0.086f, 0.141f, 1f);

        // ---------- Ink ----------

        public static readonly Color Ink = new Color(0.902f, 0.937f, 0.988f, 1f);
        public static readonly Color InkMuted = new Color(0.455f, 0.541f, 0.682f, 1f);
        public static readonly Color InkFaint = new Color(0.278f, 0.345f, 0.459f, 1f);
        public static readonly Color InkOnAccent = new Color(0.047f, 0.071f, 0.118f, 1f);

        // ---------- Accents ----------

        public static readonly Color Gold = new Color(0.949f, 0.737f, 0.318f, 1f);
        public static readonly Color ArtifactAccent = new Color(0.545f, 0.494f, 1f, 1f);
        public static readonly Color PieceAccent = new Color(0.259f, 0.694f, 0.949f, 1f);
        public static readonly Color Confirm = new Color(0.231f, 0.510f, 0.965f, 1f);
        public static readonly Color Deny = new Color(0.976f, 0.400f, 0.400f, 1f);
        public static readonly Color Disabled = new Color(0.192f, 0.239f, 0.333f, 1f);

        /// <summary>A tinted well behind an icon: the accent, dimmed into the card.</summary>
        public static Color Well(Color accent) => new Color(accent.r, accent.g, accent.b, 0.13f);

        /// <summary>The icon itself, lifted off its well.</summary>
        public static Color Glyph(Color accent) => Color.Lerp(accent, Color.white, 0.25f);

        // ---------- Metrics ----------

        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        public const float PanelWidth = 1320f;
        public const float PanelHeight = 960f;
        public const float PanelPadding = 48f;

        public const float CardWidth = 320f;
        public const float CardHeight = 280f;
        public const float CardGap = 40f;

        /// <summary>Canvas order high enough to sit over the board and the stage HUD.</summary>
        public const int OverlaySortingOrder = 200;
    }
}
