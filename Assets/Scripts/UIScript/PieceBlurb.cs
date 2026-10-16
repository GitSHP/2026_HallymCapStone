using Promotion.Data;

namespace Promotion.UI
{
    public static class PieceBlurb  // 상점 카드 등에 표시할 기물의 짧은 설명 문구를 만들어 주는 클래스
    {
        public static string For(PieceDefinition piece)  // 기물 데이터를 받아 그 기물의 설명 문구를 돌려주는 함수
        {
            if (piece == null)  // 만약 기물 데이터가 없다면 -> 기본 문구를 돌려줌
                return Fallback;

            return FromGlyph(piece.glyph);  // 기물의 글자(P, N, B ...)로 어떤 기물인지 판단해서 문구를 고름
        }

        const string Fallback = "덱에 추가할 새 기물.";  // 어떤 기물인지 알 수 없을 때 쓰는 기본 설명 문구

        static string FromGlyph(string glyph)  // 기물 글자의 첫 글자를 보고 행마법 설명 문구를 돌려주는 함수
        {
            if (string.IsNullOrEmpty(glyph))  // 만약 글자가 비어 있다면 -> 기본 문구를 돌려줌
                return Fallback;

            switch (char.ToUpperInvariant(glyph[0]))  // 첫 글자를 대문자로 바꿔서 비교 -> 소문자로 적혀 있어도 같은 기물로 인식함
            {
                case 'P': return "앞으로 한 칸 전진.\n대각선으로 잡는다.";  // 폰
                case 'N': return "L자 모양으로 도약.\n무엇이든 뛰어넘는다.";  // 나이트
                case 'B': return "대각선으로\n원하는 만큼 이동.";  // 비숍
                case 'R': return "직선으로\n원하는 만큼 이동.";  // 룩
                case 'Q': return "모든 방향으로\n원하는 만큼 이동.";  // 퀸
                case 'K': return "반드시 지켜야 할 목표.\n적이 닿으면 패배한다.";  // 킹
                default: return Fallback;  // 그 외의 글자라면 기본 문구
            }
        }
    }
}
