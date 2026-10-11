using System.Collections.Generic;
using Promotion.Data;

namespace Promotion.Core
{
    /// <summary>수를 되돌리는 데 필요한 정보다.</summary>
    public struct MoveUndo
    {
        public Piece piece;
        public Coord from;
        public Coord to;
        public Piece captured;
        public int capturedIndex;
        public bool hadMoved;
        /// <summary>승격 시 <see cref="piece"/>를 대체한 기물이며, 없으면 null이다.</summary>
        public Piece promoted;
    }

    /// <summary>
    /// 규칙 검사와 AI가 수를 두고 되돌리기를 수천 번 반복할 수 있는 보드의 개별 사본이다.
    /// 실제 보드나 화면에 있는 것은 전혀 건드리지 않는다. 원본에서 복제한
    /// 자체 Piece 객체를 가진다.
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

        /// <summary>다른 보드(실제 보드 또는 탐색 보드)의 스냅샷이다.</summary>
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

        /// <summary>이 사본에서 <paramref name="original"/>과 같은 칸에 서 있는 기물이다.</summary>
        public Piece Mirror(Piece original) => original == null ? null : GetPiece(original.Coord);

        public Piece FindKing(Team team)
        {
            foreach (var p in _pieces)
                if (p.Team == team && p.IsObjective)
                    return p;
            return null;
        }

        /// <summary>
        /// 수를 둔다. <paramref name="promotion"/>은 도착 시 이동한 기물을 대체하며,
        /// 그대로 두려면 null을 넘긴다.
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
