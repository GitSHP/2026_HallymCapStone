using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public class BoardState : MonoBehaviour, IBoardView   // 실제 게임 보드에서 "어느 칸에 어떤 기물이 있는지"를 관리하는 클래스
    // -> 화면(PieceView)과 분리된 데이터 원본이며, 기물 배치·이동·제거는 반드시 이 클래스를 거쳐야 함
    {
        BoardGrid _grid;   // 같은 오브젝트에 붙은 보드 격자 (보드 크기, 칸 안쪽 판정에 사용)
        Piece[,] _pieces;   // 칸 좌표 [x, y]로 그 칸의 기물을 바로 찾기 위한 2차원 배열 (빈 칸은 null)

        readonly List<Piece> _all = new();   // 보드 위의 모든 기물 목록 -> 전체 순회(적 찾기, 킹 찾기 등)에 사용

        public IReadOnlyList<Piece> AllPieces => _all;   // 외부에서 기물 목록을 읽기만 할 수 있게 공개
        public BoardGrid Grid => _grid;   // 보드 격자에 접근하기 위한 속성
        public int Width => _grid.width;   // 보드 가로 칸 수
        public int Height => _grid.height;   // 보드 세로 칸 수

        void Awake()   // 오브젝트가 생성될 때 한 번 실행
        {
            _grid = GetComponent<BoardGrid>();   // 같은 오브젝트의 BoardGrid를 가져오고
            _pieces = new Piece[_grid.width, _grid.height];   // 보드 크기만큼 빈 칸 배열을 만듦
        }

        public bool IsInside(Coord c) => _grid.IsInside(c);   // 좌표가 보드 안쪽인지 확인하는 함수 (BoardGrid에 위임)

        public Piece GetPiece(Coord c)   // 그 칸에 있는 기물을 돌려주는 함수
            => _grid.IsInside(c) ? _pieces[c.x, c.y] : null;   // 보드 밖 좌표면 배열 범위 에러 대신 null을 돌려줌

        public bool IsEmpty(Coord c) => _grid.IsInside(c) && _pieces[c.x, c.y] == null;   // 보드 안쪽이면서 기물이 없는 빈 칸인지 확인하는 함수

        public Piece Place(PieceDefinition def, Team team, Coord coord)   // 새 기물을 만들어 지정한 칸에 놓는 함수 (소환·배치·적 스폰에 사용)
        {
            if (!_grid.IsInside(coord) || !IsEmpty(coord))   // 만약 보드 밖이거나 이미 다른 기물이 있는 칸이라면
                return null;   // 놓지 않고 null을 돌려줌 -> 호출한 쪽에서 실패로 처리

            var piece = new Piece(def, team, coord);   // 기물 데이터를 만들고
            _pieces[coord.x, coord.y] = piece;   // 칸 배열에 기록하고
            _all.Add(piece);   // 전체 목록에도 추가함
            return piece;
        }

        public void Move(Piece piece, Coord to)   // 기물을 다른 칸으로 옮기는 함수 -> 도착 칸에 다른 기물이 있으면 잡음
        {
            if (piece == null || !_grid.IsInside(to))   // 만약 기물이 없거나 도착 칸이 보드 밖이라면 아무것도 하지 않음
                return;

            var occupant = _pieces[to.x, to.y];   // 도착 칸에 원래 있던 기물
            if (occupant != null && occupant != piece)   // 만약 다른 기물이 서 있다면 -> 잡기 (위치 기반: 도착하면 잡고 그 칸을 차지)
                _all.Remove(occupant);   // 잡힌 기물을 전체 목록에서 제거 (칸 배열은 아래에서 덮어씀)

            _pieces[piece.Coord.x, piece.Coord.y] = null;   // 출발 칸을 비우고
            piece.Coord = to;   // 기물의 좌표를 도착 칸으로 바꾸고
            piece.HasMoved = true;   // 한 번 움직였다고 표시 -> 첫 수 전용 이동이 더 이상 안 되게 함
            _pieces[to.x, to.y] = piece;   // 도착 칸에 기물을 기록함
        }

        public void Remove(Piece piece)   // 기물을 보드에서 없애는 함수
        {
            if (piece == null)
                return;

            if (GetPiece(piece.Coord) == piece)   // 만약 그 칸에 정말 이 기물이 있다면 칸을 비움
                _pieces[piece.Coord.x, piece.Coord.y] = null;   // -> 이미 다른 기물이 그 칸을 차지했다면 그 기물을 지우지 않도록 확인하는 것

            _all.Remove(piece);   // 전체 목록에서도 제거
        }

        public Piece FindObjective(Team team)   // 그 진영의 킹(지켜야 하는 기물)을 찾는 함수 -> 없으면 null
        {
            foreach (var p in _all)   // 모든 기물 중에서
                if (p.Team == team && p.IsObjective)   // 같은 진영이면서 킹인 기물을 찾으면
                    return p;   // 바로 돌려줌
            return null;
        }

        public void GetPiecesOf(Team team, List<Piece> results)   // 한 진영의 기물만 골라 results 리스트에 담아주는 함수
        // -> 리스트를 매번 새로 만들지 않고 호출한 쪽이 넘겨준 리스트를 재사용함 (메모리 할당 줄이기)
        {
            results.Clear();   // 이전 내용을 비우고
            foreach (var p in _all)
                if (p.Team == team)   // 같은 진영의 기물만
                    results.Add(p);   // 담음
        }
    }
}
