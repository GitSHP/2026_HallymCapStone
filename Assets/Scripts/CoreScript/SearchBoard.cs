using System.Collections.Generic;
using Promotion.Data;

namespace Promotion.Core
{
    public struct MoveUndo   // 둔 수를 되돌리기 위해 필요한 정보를 모아둔 구조체
    {
        public Piece piece;   // 움직인 기물
        public Coord from;   // 출발 칸
        public Coord to;   // 도착 칸
        public Piece captured;   // 이 수로 잡힌 기물 (없으면 null)
        public int capturedIndex;   // 잡힌 기물이 기물 목록에서 몇 번째였는지 -> 되돌릴 때 같은 자리에 다시 끼워 넣어 순서를 유지
        public bool hadMoved;   // 움직이기 전의 HasMoved 값 -> 첫 수 전용 이동 가능 여부를 원래대로 복원하기 위함
        public Piece promoted;   // 프로모션으로 원래 기물을 대신하게 된 새 기물 (프로모션이 없었으면 null)
    }

    public sealed class SearchBoard : IBoardView   // 규칙 검사와 체스 AI가 쓰는 계산용 복사 보드
    // -> "수를 둬보고 되돌리기"를 수천 번 반복해도 실제 보드(BoardState)와 화면은 전혀 건드리지 않음
    // -> 원본에서 복제한 자기만의 Piece 객체를 따로 가짐
    {
        readonly Piece[,] _grid;   // 칸 좌표 [x, y]로 기물을 바로 찾기 위한 2차원 배열
        readonly List<Piece> _pieces = new();   // 이 복사 보드 위의 모든 기물 목록

        public int Width { get; }   // 보드 가로 칸 수
        public int Height { get; }   // 보드 세로 칸 수
        public IReadOnlyList<Piece> AllPieces => _pieces;   // 기물 목록을 읽기 전용으로 공개

        public SearchBoard(int width, int height)   // 주어진 크기의 빈 보드를 만드는 생성자
        {
            Width = width;
            Height = height;
            _grid = new Piece[width, height];
        }

        public static SearchBoard CopyOf(IBoardView source)   // 다른 보드(실제 보드 또는 다른 복사 보드)의 현재 모습을 그대로 복사하는 함수
        {
            var copy = new SearchBoard(source.Width, source.Height);   // 같은 크기의 빈 보드를 만들고
            foreach (var p in source.AllPieces)   // 원본의 기물을 하나씩
                copy.Add(p.Def, p.Team, p.Coord, p.HasMoved);   // 새 Piece로 복제해서 같은 칸에 놓음 (움직인 적 있는지도 그대로 복사)
            return copy;
        }

        public Piece Add(PieceDefinition def, Team team, Coord coord, bool hasMoved = false)   // 복사 보드에 기물을 새로 놓는 함수
        {
            if (!IsInside(coord) || _grid[coord.x, coord.y] != null)   // 만약 보드 밖이거나 이미 기물이 있는 칸이라면 놓지 않음
                return null;

            var piece = new Piece(def, team, coord) { HasMoved = hasMoved };   // 기물을 만들고
            _grid[coord.x, coord.y] = piece;   // 칸 배열에 기록하고
            _pieces.Add(piece);   // 목록에도 추가
            return piece;
        }

        public bool IsInside(Coord c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;   // 좌표가 보드 안쪽인지 확인하는 함수

        public Piece GetPiece(Coord c) => IsInside(c) ? _grid[c.x, c.y] : null;   // 그 칸의 기물을 돌려주는 함수 (보드 밖이면 null)

        public Piece Mirror(Piece original) => original == null ? null : GetPiece(original.Coord);
        // 실제 보드의 기물과 같은 칸에 있는 복사 보드의 기물을 찾는 함수 -> 원본 기물과 복사본 기물을 연결할 때 사용

        public Piece FindKing(Team team)   // 그 진영의 킹을 찾는 함수 -> 없으면 null
        {
            foreach (var p in _pieces)
                if (p.Team == team && p.IsObjective)
                    return p;
            return null;
        }

        public MoveUndo Make(Piece piece, Coord to, PieceDefinition promotion = null)
        // 복사 보드에서 수를 실제로 두는 함수 -> 되돌리기용 정보(MoveUndo)를 돌려줌
        // -> promotion에 기물 데이터를 넘기면 도착 후 그 기물로 바뀜(프로모션), 그대로 두려면 null
        {
            var undo = new MoveUndo   // 두기 전 상태를 먼저 기록해 둠
            {
                piece = piece,
                from = piece.Coord,
                to = to,
                captured = _grid[to.x, to.y],   // 도착 칸에 있던 기물 = 잡힐 기물
                capturedIndex = -1,
                hadMoved = piece.HasMoved
            };

            if (undo.captured != null)   // 만약 잡히는 기물이 있다면
            {
                undo.capturedIndex = _pieces.IndexOf(undo.captured);   // 목록에서의 위치를 기억해 두고
                _pieces.RemoveAt(undo.capturedIndex);   // 목록에서 제거
            }

            _grid[undo.from.x, undo.from.y] = null;   // 출발 칸을 비우고
            piece.Coord = to;   // 기물 좌표를 도착 칸으로 바꾸고
            piece.HasMoved = true;   // 움직였다고 표시하고
            _grid[to.x, to.y] = piece;   // 도착 칸에 기물을 기록

            if (promotion != null)   // 만약 프로모션 수라면
            {
                var promoted = new Piece(promotion, piece.Team, to) { HasMoved = true };   // 바뀔 기물을 새로 만들고
                int index = _pieces.IndexOf(piece);
                _pieces[index] = promoted;   // 목록에서 원래 폰 자리를 새 기물로 교체하고
                _grid[to.x, to.y] = promoted;   // 칸에도 새 기물을 기록
                undo.promoted = promoted;   // 되돌릴 때 쓰도록 기억 (원래 폰 객체는 undo.piece에 그대로 남아 있음)
            }

            return undo;
        }

        public void Unmake(MoveUndo undo)   // Make로 둔 수를 정확히 되돌리는 함수 -> Make의 반대 순서로 복원
        {
            if (undo.promoted != null)   // 만약 프로모션이 있었다면
            {
                int index = _pieces.IndexOf(undo.promoted);
                _pieces[index] = undo.piece;   // 목록에서 새 기물을 원래 폰으로 되돌림
            }

            undo.piece.Coord = undo.from;   // 기물을 출발 칸으로 되돌리고
            undo.piece.HasMoved = undo.hadMoved;   // 움직임 여부도 원래대로 복원
            _grid[undo.from.x, undo.from.y] = undo.piece;   // 출발 칸에 다시 기물을 놓고
            _grid[undo.to.x, undo.to.y] = undo.captured;   // 도착 칸에는 잡혔던 기물을 되돌려 놓음 (없었다면 null로 비워짐)

            if (undo.captured != null)   // 잡혔던 기물이 있다면
                _pieces.Insert(undo.capturedIndex, undo.captured);   // 목록의 원래 위치에 다시 끼워 넣음 -> 순서가 그대로 유지됨
        }
    }
}
