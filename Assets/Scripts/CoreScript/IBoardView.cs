using System.Collections.Generic;

namespace Promotion.Core
{
    /// <summary>
    /// Read-only view of a board: what is where. Both the live board and the AI's
    /// search copy implement it, so movement rules are written once and shared.
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
