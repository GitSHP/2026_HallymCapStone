using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public enum ChessStatus   // 최종전 체스 대결에서 "이제 둘 차례인 쪽"의 현재 상황
    {
        Ongoing,   // 평범하게 진행 중 (킹이 공격받고 있지 않음)
        Check,   // 체크 -> 킹이 공격받고 있지만 피할 수 있는 수가 있음
        Checkmate,   // 체크메이트 -> 킹이 공격받고 있고 피할 수 있는 수가 하나도 없음 (패배)
        Stalemate   // 스테일메이트 -> 킹은 안전하지만 둘 수 있는 수가 하나도 없음 (무승부)
    }

    public struct ChessMove   // 한 진영이 둘 수 있는 수 하나 -> 어떤 기물을 어디로 옮기는지 담음
    {
        public Piece piece;   // 움직일 기물
        public Coord to;   // 도착할 칸
        public bool isCapture;   // 이 수가 상대 기물을 잡는 수인지 여부

        public ChessMove(Piece piece, Coord to, bool isCapture)   // 기물·도착 칸·잡기 여부로 수를 만드는 생성자
        {
            this.piece = piece;
            this.to = to;
            this.isCapture = isCapture;
        }
    }

    public static class ChessRules   // 최종전 체스 대결에서 쓰는 진짜 체스 규칙 판정 클래스
    // -> MoveResolver가 구한 행마법 후보 위에 "그 수를 둔 뒤 자기 킹이 공격받지 않아야 한다"는 조건을 더함
    // -> 이 조건 하나로 핀(움직이면 킹이 노출되는 기물), 체크 탈출, 킹이 공격받는 칸으로 들어가지 않기까지 모두 처리됨
    // -> SearchBoard(계산용 복사 보드) 위에서 수를 시험하므로 실제 보드와 화면은 전혀 흐트러지지 않음
    {
        // 아래 리스트들은 용도별·탐색 깊이별로 하나씩 두고 재사용하는 임시 리스트 보관함
        // -> AI가 수천 번 수를 시험할 때마다 리스트를 새로 만들면 메모리 낭비가 크기 때문
        static readonly List<List<MoveOption>> CandidatePool = new();   // 행마법상 후보 수(합법성 검사 전)를 담는 리스트들
        static readonly List<List<MoveOption>> OptionPool = new();   // 합법성 검사를 통과한 수를 담는 리스트들
        static readonly List<List<Piece>> PiecePool = new();   // 움직일 기물 목록을 미리 복사해 두는 리스트들
        static readonly List<List<ChessMove>> MovePool = new();   // Perft 검증에서 깊이별 전체 수 목록을 담는 리스트들

        static List<T> Borrow<T>(List<List<T>> pool, int depth)   // 보관함에서 해당 깊이용 리스트를 빌려오는 함수 (비워서 돌려줌)
        // -> 깊이마다 다른 리스트를 써야 하위 탐색이 상위 탐색의 리스트를 덮어쓰지 않음
        {
            while (pool.Count <= depth)   // 만약 그 깊이용 리스트가 아직 없다면
                pool.Add(new List<T>());   // 필요한 만큼 새로 만들어 추가
            var list = pool[depth];   // 해당 깊이 리스트를 꺼내서
            list.Clear();   // 이전 내용을 비우고
            return list;   // 돌려줌
        }

        public static bool IsSquareAttacked(IBoardView board, Coord square, Team byTeam)
        // byTeam 진영의 기물 중 하나라도 square 칸을 공격(잡을 수 있는 상태)하고 있는지 확인하는 함수
        // -> 그 칸에 무엇이 있든 상관없이 "잡기 가능한 패턴"만 셈 (폰은 대각선만 공격하고 앞 칸은 공격하지 않음)
        {
            var pieces = board.AllPieces;
            for (int i = 0; i < pieces.Count; i++)   // 보드의 모든 기물을 검사해서
            {
                var p = pieces[i];
                if (p.Team == byTeam && Attacks(board, p, square))   // 만약 그 진영 기물이 이 칸을 공격하고 있다면
                    return true;   // 바로 공격받는 칸이라고 알림
            }
            return false;   // 아무도 공격하지 않음
        }

        public static bool Attacks(IBoardView board, Piece piece, Coord square)
        // 기물 하나가 square 칸을 공격하고 있는지 확인하는 함수
        // -> MoveResolver와 같은 방식으로 행마법을 따라가지만, 그 칸에 무엇이 있는지는 따지지 않음
        {
            var patterns = piece.Def.patterns;
            if (patterns == null)   // 만약 행마법이 없다면 아무 칸도 공격할 수 없음
                return false;

            foreach (var pattern in patterns)
            {
                if (pattern?.directions == null || !pattern.canCapture)   // 비어있거나 잡기가 불가능한 패턴(예: 폰의 전진)은 공격이 아니므로 건너뜀
                    continue;
                if (pattern.firstMoveOnly && piece.HasMoved)   // 첫 수 전용 패턴인데 이미 움직였다면 건너뜀
                    continue;

                foreach (var rawDir in pattern.directions)
                {
                    var dir = rawDir;
                    if (pattern.relativeToFacing)   // 진영별로 앞뒤가 바뀌는 패턴이면 적 쪽은 y 방향을 뒤집음
                        dir.y *= piece.Team.ForwardY();
                    if (dir == Vector2Int.zero)   // 제자리 방향은 무시
                        continue;

                    int range = pattern.sliding ? Mathf.Max(1, pattern.maxDistance) : 1;   // 연속 이동이면 최대 사거리, 아니면 한 칸
                    var cursor = piece.Coord;
                    for (int step = 0; step < range; step++)   // 한 칸씩 나아가며
                    {
                        cursor += dir;
                        if (!board.IsInside(cursor))   // 보드 밖이면 이 방향 종료
                            break;
                        if (cursor == square)   // 만약 목표 칸에 닿았다면 -> 공격 중
                            return true;
                        if (board.GetPiece(cursor) != null)   // 중간에 다른 기물이 막고 있다면 그 너머는 공격할 수 없음
                            break;
                    }
                }
            }
            return false;
        }

        public static bool IsInCheck(IBoardView board, Team team)   // team 진영의 킹이 체크(상대 기물에게 공격받는 상태)인지 확인하는 함수
        {
            Piece king = null;
            var pieces = board.AllPieces;
            for (int i = 0; i < pieces.Count; i++)   // 먼저 그 진영의 킹을 찾고
                if (pieces[i].Team == team && pieces[i].IsObjective) { king = pieces[i]; break; }

            return king != null && IsSquareAttacked(board, king.Coord, team.Opponent());   // 킹이 있고 + 킹이 선 칸을 상대가 공격하고 있으면 체크
        }

        public static void GetLegalMoves(SearchBoard board, Piece piece, List<MoveOption> results, int depth = 0)
        // 기물 하나가 체스 규칙상 실제로 둘 수 있는 수만 results에 담는 함수
        {
            results.Clear();
            var candidates = Borrow(CandidatePool, depth);   // 임시 리스트를 빌려
            MoveResolver.GetLegalMoves(board, piece, candidates);   // 행마법상 갈 수 있는 칸을 먼저 모두 구함

            foreach (var option in candidates)   // 후보 하나하나를
            {
                var undo = board.Make(piece, option.target);   // 복사 보드에서 실제로 둬보고
                bool safe = !IsInCheck(board, piece.Team);   // 둔 뒤에 자기 킹이 공격받지 않는지 확인하고
                board.Unmake(undo);   // 다시 되돌림

                if (safe)   // 만약 킹이 안전하다면 -> 합법적인 수로 인정
                    results.Add(option);
            }
        }

        public static void GetAllLegalMoves(SearchBoard board, Team team, List<ChessMove> results, int depth = 0)
        // 한 진영이 둘 수 있는 모든 합법적인 수를 results에 담는 함수
        {
            results.Clear();

            var movers = Borrow(PiecePool, depth);   // 움직일 기물 목록을 먼저 따로 복사해 둠
            foreach (var p in board.AllPieces)   // -> 수를 시험하면 보드의 기물 리스트 순서가 바뀌기 때문에 원본을 직접 돌면 안 됨
                if (p.Team == team)
                    movers.Add(p);

            var options = Borrow(OptionPool, depth);
            foreach (var p in movers)   // 기물마다
            {
                GetLegalMoves(board, p, options, depth);   // 합법적인 이동 칸을 구하고
                foreach (var o in options)
                    results.Add(new ChessMove(p, o.target, o.isCapture));   // "기물 + 도착 칸" 형태의 수로 바꿔 담음
            }
        }

        public static bool HasAnyLegalMove(SearchBoard board, Team team, int depth = 0)
        // 한 진영에게 둘 수 있는 수가 하나라도 있는지 확인하는 함수 -> 체크메이트·스테일메이트 판정에 사용
        {
            var movers = Borrow(PiecePool, depth);   // GetAllLegalMoves와 같은 이유로 기물 목록을 먼저 복사
            foreach (var p in board.AllPieces)
                if (p.Team == team)
                    movers.Add(p);

            var options = Borrow(OptionPool, depth);
            foreach (var p in movers)
            {
                GetLegalMoves(board, p, options, depth);
                if (options.Count > 0)   // 하나라도 찾으면 더 볼 필요 없이 바로 true -> 전부 구하는 것보다 빠름
                    return true;
            }
            return false;
        }

        public static ChessStatus GetStatus(SearchBoard board, Team sideToMove, int depth = 0)
        // 이제 둘 차례인 쪽(sideToMove) 기준으로 현재 게임 상태를 판정하는 함수
        {
            bool inCheck = IsInCheck(board, sideToMove);   // 킹이 공격받고 있는지
            bool canMove = HasAnyLegalMove(board, sideToMove, depth);   // 둘 수 있는 수가 있는지

            if (!canMove)   // 만약 둘 수가 하나도 없다면 -> 체크 중이면 체크메이트(패배), 아니면 스테일메이트(무승부)
                return inCheck ? ChessStatus.Checkmate : ChessStatus.Stalemate;
            return inCheck ? ChessStatus.Check : ChessStatus.Ongoing;   // 둘 수가 있다면 -> 체크 중이면 체크, 아니면 평소 진행
        }

        public static bool IsPromotion(IBoardView board, Piece piece, Coord to)
        // 이 이동이 프로모션(폰이 상대 쪽 맨 끝 줄에 도착해 다른 기물로 바뀌는 규칙)인지 확인하는 함수
        {
            if (piece == null || !piece.Def.canPromote)   // 만약 프로모션이 가능한 기물(폰)이 아니라면 해당 없음
                return false;
            int farRank = piece.Team.ForwardY() > 0 ? board.Height - 1 : 0;   // 그 진영 기준 맨 끝 줄 -> 플레이어는 맨 위, 적은 맨 아래
            return to.y == farRank;   // 도착 칸이 맨 끝 줄이면 프로모션
        }

        public static long Perft(SearchBoard board, Team side, int depth, PieceDefinition promotion)
        // 정해진 수만큼 번갈아 둘 때 나올 수 있는 모든 경우의 수를 세는 검증용 함수
        // -> 체스 시작 배치 기준으로 알려진 값(1수 20, 2수 400, 3수 8902, 4수 197281)과 비교해 규칙 코드가 정확한지 확인함
        // -> 프로모션은 promotion으로 넘긴 기물(퀸) 한 종류로만 셈
        {
            if (depth == 0)   // 더 둘 수가 없으면 지금 상태 하나로 셈
                return 1;

            var moves = Borrow(MovePool, depth);
            GetAllLegalMoves(board, side, moves, depth);   // 지금 둘 수 있는 모든 수를 구하고

            if (depth == 1)   // 마지막 한 수라면 수의 개수가 곧 경우의 수 -> 직접 두지 않아 빠름
                return moves.Count;

            var snapshot = moves.ToArray();   // 목록을 배열로 복사해서 순회 -> 이 깊이용 리스트는 하위 탐색에서 다시 쓰일 수 있기 때문
            long total = 0;
            foreach (var m in snapshot)   // 각 수에 대해
            {
                var promo = IsPromotion(board, m.piece, m.to) ? promotion : null;   // 프로모션 수라면 바뀔 기물을 정하고
                var undo = board.Make(m.piece, m.to, promo);   // 둬보고
                total += Perft(board, side.Opponent(), depth - 1, promotion);   // 상대 차례로 한 단계 더 들어가 경우의 수를 더한 뒤
                board.Unmake(undo);   // 되돌림
            }
            return total;
        }
    }
}
