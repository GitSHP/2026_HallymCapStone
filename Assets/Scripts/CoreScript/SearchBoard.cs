using System.Collections.Generic;
using Promotion.Data;

namespace Promotion.Core
{
    /// <summary>What it takes to take a move back.</summary>
    public struct MoveUndo
    {
        public Piece piece;
        public Coord from;
        public Coord to;
        public Piece captured;
        public int capturedIndex;
        public bool hadMoved;
        /// <summary>The piece that replaced <see cref="piece"/> on promotion, or null.</summary>
        public Piece promoted;
    }

    /// <summary>
    /// A private copy of the board that rules checks and the AI can play moves
    /// on and take back thousands of times, without touching the live board or
    /// anything on screen. Holds its own Piece objects, cloned from the source.
    /// </summary>
    public sealed class SearchBoard : IBoardView
    {
        readonly Piece[,] _grid;
        readonly List<Piece> _pieces = new();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Piece> AllPieces => _pieces;

        public SearchBoard(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new Piece[width, height];
        }

        /// <summary>Snapshot of another board, live or search.</summary>
        public static SearchBoard CopyOf(IBoardView source)
        {
            var copy = new SearchBoard(source.Width, source.Height);
            foreach (var p in source.AllPieces)
                copy.Add(p.Def, p.Team, p.Coord, p.HasMoved);
            return copy;
        }

        public Piece Add(PieceDefinition def, Team team, Coord coord, bool hasMoved = false)
        {
            if (!IsInside(coord) || _grid[coord.x, coord.y] != null)
                return null;

            var piece = new Piece(def, team, coord) { HasMoved = hasMoved };
            _grid[coord.x, coord.y] = piece;
            _pieces.Add(piece);
            return piece;
        }

        public bool IsInside(Coord c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public Piece GetPiece(Coord c) => IsInside(c) ? _grid[c.x, c.y] : null;

        /// <summary>The piece standing on the same square as <paramref name="original"/> on this copy.</summary>
        public Piece Mirror(Piece original) => original == null ? null : GetPiece(original.Coord);

        public Piece FindKing(Team team)
        {
            foreach (var p in _pieces)
                if (p.Team == team && p.IsObjective)
                    return p;
            return null;
        }

        /// <summary>
        /// Plays a move. <paramref name="promotion"/> replaces the mover on arrival;
        /// pass null to keep it as it is.
        /// </summary>
        public MoveUndo Make(Piece piece, Coord to, PieceDefinition promotion = null)
        {
            var undo = new MoveUndo
            {
                piece = piece,
                from = piece.Coord,
                to = to,
                captured = _grid[to.x, to.y],
                capturedIndex = -1,
                hadMoved = piece.HasMoved
            };

            if (undo.captured != null)
            {
                undo.capturedIndex = _pieces.IndexOf(undo.captured);
                _pieces.RemoveAt(undo.capturedIndex);
            }

            _grid[undo.from.x, undo.from.y] = null;
            piece.Coord = to;
            piece.HasMoved = true;
            _grid[to.x, to.y] = piece;

            if (promotion != null)
            {
                var promoted = new Piece(promotion, piece.Team, to) { HasMoved = true };
                int index = _pieces.IndexOf(piece);
                _pieces[index] = promoted;
                _grid[to.x, to.y] = promoted;
                undo.promoted = promoted;
            }

            return undo;
        }

        public void Unmake(MoveUndo undo)
        {
            if (undo.promoted != null)
            {
                int index = _pieces.IndexOf(undo.promoted);
                _pieces[index] = undo.piece;
            }

            undo.piece.Coord = undo.from;
            undo.piece.HasMoved = undo.hadMoved;
            _grid[undo.from.x, undo.from.y] = undo.piece;
            _grid[undo.to.x, undo.to.y] = undo.captured;

            if (undo.captured != null)
                _pieces.Insert(undo.capturedIndex, undo.captured);
        }
    }
}
