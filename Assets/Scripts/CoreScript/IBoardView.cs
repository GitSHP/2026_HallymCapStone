using System.Collections.Generic;

namespace Promotion.Core
{
    /// <summary>
    /// 보드의 읽기 전용 뷰로, 어느 칸에 무엇이 있는지를 제공한다. 실제 보드와 AI의
    /// 탐색용 사본이 모두 이를 구현하므로, 이동 규칙을 한 번만 작성해 공유한다.
    /// </summary>
    public interface IBoardView
    {
        int Width { get; }
        int Height { get; }
        IReadOnlyList<Piece> AllPieces { get; }
        bool IsInside(Coord c);
        Piece GetPiece(Coord c);
    }
}
