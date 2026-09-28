using Gambonanza.Data;

namespace Gambonanza.UI
{
    /// <summary>
    /// One short line per piece, describing how it moves rather than what its
    /// numbers are. The game exists to teach chess movement, so the shop is the
    /// first place a player meets a piece — a stat block tells them nothing they
    /// can act on, while "leaps in an L" is the whole lesson.
    ///
    /// Kept here rather than on PieceDefinition because the piece assets belong
    /// to the gameplay side; the shop only needs to describe what it is selling.
    /// </summary>
    public static class PieceBlurb
    {
        public static string For(PieceDefinition piece)
        {
            if (piece == null)
                return Fallback;

            return FromGlyph(piece.glyph);
        }

        const string Fallback = "A new unit for your deck.";

        static string FromGlyph(string glyph)
        {
            if (string.IsNullOrEmpty(glyph))
                return Fallback;

            switch (char.ToUpperInvariant(glyph[0]))
            {
                case 'P': return "Steps forward one square.\nCaptures diagonally.";
                case 'N': return "Leaps in an L shape.\nJumps over anything.";
                case 'B': return "Slides any distance\nalong the diagonals.";
                case 'R': return "Slides any distance\nin straight lines.";
                case 'Q': return "Slides any distance\nin every direction.";
                case 'K': return "Your objective.\nProtect it at all costs.";
                default: return Fallback;
            }
        }
    }
}
