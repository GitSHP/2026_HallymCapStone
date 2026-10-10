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

        const string Fallback = "덱에 추가할 새 기물.";

        static string FromGlyph(string glyph)
        {
            if (string.IsNullOrEmpty(glyph))
                return Fallback;

            switch (char.ToUpperInvariant(glyph[0]))
            {
                case 'P': return "앞으로 한 칸 전진.\n대각선으로 잡는다.";
                case 'N': return "L자 모양으로 도약.\n무엇이든 뛰어넘는다.";
                case 'B': return "대각선으로\n원하는 만큼 이동.";
                case 'R': return "직선으로\n원하는 만큼 이동.";
                case 'Q': return "모든 방향으로\n원하는 만큼 이동.";
                case 'K': return "반드시 지켜야 할 목표.\n적이 닿으면 패배한다.";
                default: return Fallback;
            }
        }
    }
}
