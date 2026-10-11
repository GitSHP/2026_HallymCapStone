using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public enum ChessStatus
    {
        Ongoing,
        /// <summary>둘 차례인 쪽이 체크 상태지만 벗어날 수 있다.</summary>
        Check,
        Checkmate,
        Stalemate
    }

    /// <summary>한 진영의 합법적인 수 하나다. 어떤 기물을 어디로 옮기는지 담는다.</summary>
    public struct ChessMove
    {
        public Piece piece;
        public Coord to;
        public bool isCapture;

        public ChessMove(Piece piece, Coord to, bool isCapture)
        {
            this.piece = piece;
            this.to = to;
            this.isCapture = isCapture;
        }
    }

    /// <summary>
    /// 기물 패턴 위에 실제 체스의 합법성 판정을 더한다. 수를 둔 뒤 자기 킹이 공격받지
    /// 않을 때만 합법이다. 이 규칙 하나로 핀, 체크 탈출, 킹이 공격받는 칸으로
    /// 들어가지 않는 것까지 모두 처리된다. SearchBoard에서 동작하므로 시험 삼아
    /// 둔 수가 실제 보드를 흐트러뜨리지 않는다.
    /// </summary>
    public static class ChessRules
    {
        // 용도별·탐색 깊이별로 하나씩 두는 임시 리스트다. perft와 AI가 노드마다
        // 새로 할당하지 않고 메모리를 재사용하게 한다.
        static readonly List<List<MoveOption>> CandidatePool = new();
        static readonly List<List<MoveOption>> OptionPool = new();
        static readonly List<List<Piece>> PiecePool = new();
        static readonly List<List<ChessMove>> MovePool = new();

        static List<T> Borrow<T>(List<List<T>> pool, int depth)
        {
            while (pool.Count <= depth)
                pool.Add(new List<T>());
            var list = pool[depth];
            list.Clear();
            return list;
        }

        // ---------- 공격 ----------

        /// <summary>
        /// <paramref name="byTeam"/>의 기물 중 하나라도 <paramref name="square"/>에서 잡기를 할 수 있는지
        /// 판단한다. 그 칸에 무엇이 있든 상관없다. 잡기 가능한 패턴만 센다. 폰은 대각선이
        /// 비어 있어도 그 칸을 공격하지만, 앞 칸은 절대 공격하지 않는다.
        /// </summary>
        public static bool IsSquareAttacked(IBoardView board, Coord square, Team byTeam)
        {
            var pieces = board.AllPieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.Team == byTeam && Attacks(board, p, square))
                    return true;
            }
            return false;
        }

        /// <summary>이 기물 하나가 <paramref name="square"/>에서 잡기를 할 수 있는지 판단한다.</summary>
        public static bool Attacks(IBoardView board, Piece piece, Coord square)
        {
            var patterns = piece.Def.patterns;
            if (patterns == null)
                return false;

            foreach (var pattern in patterns)
            {
                if (pattern?.directions == null || !pattern.canCapture)
                    continue;
                if (pattern.firstMoveOnly && piece.HasMoved)
                    continue;

                foreach (var rawDir in pattern.directions)
                {
                    var dir = rawDir;
                    if (pattern.relativeToFacing)
                        dir.y *= piece.Team.ForwardY();
                    if (dir == Vector2Int.zero)
                        continue;

                    int range = pattern.sliding ? Mathf.Max(1, pattern.maxDistance) : 1;
                    var cursor = piece.Coord;
                    for (int step = 0; step < range; step++)
                    {
                        cursor += dir;
                        if (!board.IsInside(cursor))
                            break;
                        if (cursor == square)
                            return true;
                        if (board.GetPiece(cursor) != null)
                            break;
                    }
                }
            }
            return false;
        }

        public static bool IsInCheck(IBoardView board, Team team)
        {
            Piece king = null;
            var pieces = board.AllPieces;
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i].Team == team && pieces[i].IsObjective) { king = pieces[i]; break; }

            return king != null && IsSquareAttacked(board, king.Coord, team.Opponent());
        }

        // ---------- 합법 수 ----------

        /// <summary><paramref name="piece"/>가 합법적으로 이동할 수 있는 칸들이다.</summary>
        public static void GetLegalMoves(SearchBoard board, Piece piece, List<MoveOption> results, int depth = 0)
        {
            results.Clear();
            var candidates = Borrow(CandidatePool, depth);
            MoveResolver.GetLegalMoves(board, piece, candidates);

            foreach (var option in candidates)
            {
                var undo = board.Make(piece, option.target);
                bool safe = !IsInCheck(board, piece.Team);
                board.Unmake(undo);

                if (safe)
                    results.Add(option);
            }
        }

        /// <summary><paramref name="team"/>의 모든 합법 수이다.</summary>
        public static void GetAllLegalMoves(SearchBoard board, Team team, List<ChessMove> results, int depth = 0)
        {
            results.Clear();

            // 먼저 복사한다. 수를 시험하면 보드의 기물 리스트 순서가 바뀌기 때문이다.
            var movers = Borrow(PiecePool, depth);
            foreach (var p in board.AllPieces)
                if (p.Team == team)
                    movers.Add(p);

            var options = Borrow(OptionPool, depth);
            foreach (var p in movers)
            {
                GetLegalMoves(board, p, options, depth);
                foreach (var o in options)
                    results.Add(new ChessMove(p, o.target, o.isCapture));
            }
        }

        public static bool HasAnyLegalMove(SearchBoard board, Team team, int depth = 0)
        {
            var movers = Borrow(PiecePool, depth);
            foreach (var p in board.AllPieces)
                if (p.Team == team)
                    movers.Add(p);

            var options = Borrow(OptionPool, depth);
            foreach (var p in movers)
            {
                GetLegalMoves(board, p, options, depth);
                if (options.Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>곧 둘 차례인 쪽 기준의 현재 게임 상태이다.</summary>
        public static ChessStatus GetStatus(SearchBoard board, Team sideToMove, int depth = 0)
        {
            bool inCheck = IsInCheck(board, sideToMove);
            bool canMove = HasAnyLegalMove(board, sideToMove, depth);

            if (!canMove)
                return inCheck ? ChessStatus.Checkmate : ChessStatus.Stalemate;
            return inCheck ? ChessStatus.Check : ChessStatus.Ongoing;
        }

        // ---------- 승격 ----------

        /// <summary>승격 가능한 기물이 자기 진영 기준 맨 끝 랭크에 도착했는지 판단한다.</summary>
        public static bool IsPromotion(IBoardView board, Piece piece, Coord to)
        {
            if (piece == null || !piece.Def.canPromote)
                return false;
            int farRank = piece.Team.ForwardY() > 0 ? board.Height - 1 : 0;
            return to.y == farRank;
        }

        // ---------- 검증 ----------

        /// <summary>
        /// 주어진 길이의 수순 개수를 센다. 수 생성기를 알려진 합계(초기 배치 기준
        /// 20, 400, 8902, 197281)와 대조하는 표준 방법이다. 승격은 퀸으로 한 번만 센다.
        /// </summary>
        public static long Perft(SearchBoard board, Team side, int depth, PieceDefinition promotion)
        {
            if (depth == 0)
                return 1;

            var moves = Borrow(MovePool, depth);
            GetAllLegalMoves(board, side, moves, depth);

            if (depth == 1)
                return moves.Count;

            // 사본을 순회한다. 이 깊이의 풀 리스트는 하위 탐색에서 재사용되기 때문이다.
            var snapshot = moves.ToArray();
            long total = 0;
            foreach (var m in snapshot)
            {
                var promo = IsPromotion(board, m.piece, m.to) ? promotion : null;
                var undo = board.Make(m.piece, m.to, promo);
                total += Perft(board, side.Opponent(), depth - 1, promotion);
                board.Unmake(undo);
            }
            return total;
        }
    }
}
