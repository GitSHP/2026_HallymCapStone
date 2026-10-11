using Promotion.Data;

namespace Promotion.UI
{
    /// <summary>
    /// 기물마다 수치가 아니라 움직이는 방식을 설명하는 짧은 문장 하나를 둔다.
    /// 이 게임은 체스 행마법을 가르치려고 만든 것이고, 플레이어가 기물을 처음 만나는
    /// 곳이 상점이다. 능력치 표는 행동으로 옮길 정보를 주지 못하지만,
    /// "L자로 도약한다"는 한 줄은 그 자체로 배울 내용 전부다.
    ///
    /// PieceDefinition이 아니라 여기에 두는 이유는 기물 에셋이 게임플레이 쪽 소유이기
    /// 때문이다. 상점은 자기가 파는 물건을 설명하기만 하면 된다.
    /// </summary>
    // [아이템 담당] 기물 카드에 뜨는 행마법 설명문. 기물을 추가하면 여기에 한 줄 더한다.
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
