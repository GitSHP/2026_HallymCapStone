using System.Collections.Generic;
using Gambonanza.Data;
using UnityEngine;

namespace Gambonanza.Core
{
    /// <summary>
    /// Occupancy of the board. Gameplay systems ask this what is where;
    /// nothing here knows about sprites, tweens or input.
    /// </summary>
    public class BoardState : MonoBehaviour
    {
        BoardGrid _grid;
        Piece[,] _pieces;

        readonly List<Piece> _all = new();

        public IReadOnlyList<Piece> AllPieces => _all;
        public BoardGrid Grid => _grid;

        void Awake()
        {
            _grid = GetComponent<BoardGrid>();
            _pieces = new Piece[_grid.width, _grid.height];
        }

        public bool IsInside(Coord c) => _grid.IsInside(c);

        public Piece GetPiece(Coord c)
            => _grid.IsInside(c) ? _pieces[c.x, c.y] : null;

        public bool IsEmpty(Coord c) => _grid.IsInside(c) && _pieces[c.x, c.y] == null;

        public Piece Place(PieceDefinition def, Team team, Coord coord)
        {
            if (!_grid.IsInside(coord) || !IsEmpty(coord))
                return null;

            var piece = new Piece(def, team, coord);
            _pieces[coord.x, coord.y] = piece;
            _all.Add(piece);
            return piece;
        }

        public void Move(Piece piece, Coord to)
        {
            if (piece == null || !_grid.IsInside(to))
                return;

            _pieces[piece.Coord.x, piece.Coord.y] = null;
            piece.Coord = to;
            _pieces[to.x, to.y] = piece;
        }

        public void Remove(Piece piece)
        {
            if (piece == null)
                return;

            if (GetPiece(piece.Coord) == piece)
                _pieces[piece.Coord.x, piece.Coord.y] = null;

            _all.Remove(piece);
        }

        public Piece FindObjective(Team team)
        {
            foreach (var p in _all)
                if (p.Team == team && p.IsObjective)
                    return p;
            return null;
        }

        public void GetPiecesOf(Team team, List<Piece> results)
        {
            results.Clear();
            foreach (var p in _all)
                if (p.Team == team)
                    results.Add(p);
        }
    }
}
