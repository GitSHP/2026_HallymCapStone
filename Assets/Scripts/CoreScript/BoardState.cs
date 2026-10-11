using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// 보드의 점유 상태다. 게임플레이 시스템은 어느 칸에 무엇이 있는지 여기에 묻는다.
    /// 이 클래스는 스프라이트, 트윈, 입력에 대해 전혀 알지 못한다.
    /// </summary>
    public class BoardState : MonoBehaviour, IBoardView
    {
        BoardGrid _grid;
        Piece[,] _pieces;

        readonly List<Piece> _all = new();

        public IReadOnlyList<Piece> AllPieces => _all;
        public BoardGrid Grid => _grid;
        public int Width => _grid.width;
        public int Height => _grid.height;

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

            // 목적지에 이미 있던 기물은 사망 연출이 잠시 이어지더라도 지금 바로 보드에서
            // 제거한다. 규칙은 보드를 즉시 읽기 때문이다.
            var occupant = _pieces[to.x, to.y];
            if (occupant != null && occupant != piece)
                _all.Remove(occupant);

            _pieces[piece.Coord.x, piece.Coord.y] = null;
            piece.Coord = to;
            piece.HasMoved = true;
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
