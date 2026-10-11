using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public enum ChessStatus
    {
        Ongoing,
        /// <summary>The side to move is in check but can get out of it.</summary>
        Check,
        Checkmate,
        Stalemate
    }

    /// <summary>One legal move for a side: which piece, where to.</summary>
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
    /// Real chess legality on top of the piece patterns: a move is legal only if
    /// it does not leave the mover's own king attacked. That single rule covers
    /// pins, getting out of check and the king never stepping into an attack.
    /// Works on a SearchBoard so trial moves never disturb the live board.
    /// </summary>
    public static class ChessRules
    {
        // Scratch lists, one per purpose and per search depth, so perft and the AI
        // reuse memory instead of allocating at every node.
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

        // ---------- Attacks ----------

        /// <summary>
        /// Whether any piece of <paramref name="byTeam"/> could capture on <paramref name="square"/>,
        /// whatever stands there. Only capturing patterns count: a pawn attacks its
        /// diagonals even when they are empty, but never the square in front of it.
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

        /// <summary>Whether this one piece could capture on <paramref name="square"/>.</summary>
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

        // ---------- Legal moves ----------

        /// <summary>The squares <paramref name="piece"/> may legally move to.</summary>
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

        /// <summary>Every legal move for <paramref name="team"/>.</summary>
        public static void GetAllLegalMoves(SearchBoard board, Team team, List<ChessMove> results, int depth = 0)
        {
            results.Clear();

            // Copy first: trying moves reorders the board's piece list.
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

        /// <summary>Where the game stands for the side about to move.</summary>
        public static ChessStatus GetStatus(SearchBoard board, Team sideToMove, int depth = 0)
        {
            bool inCheck = IsInCheck(board, sideToMove);
            bool canMove = HasAnyLegalMove(board, sideToMove, depth);

            if (!canMove)
                return inCheck ? ChessStatus.Checkmate : ChessStatus.Stalemate;
            return inCheck ? ChessStatus.Check : ChessStatus.Ongoing;
        }

        // ---------- Promotion ----------

        /// <summary>A promotable piece arriving on the far rank for its side.</summary>
        public static bool IsPromotion(IBoardView board, Piece piece, Coord to)
        {
            if (piece == null || !piece.Def.canPromote)
                return false;
            int farRank = piece.Team.ForwardY() > 0 ? board.Height - 1 : 0;
            return to.y == farRank;
        }

        // ---------- Verification ----------

        /// <summary>
        /// Counts the move sequences of a given length — the standard way to check
        /// a move generator against known totals (20, 400, 8902, 197281 from the
        /// opening position). Promotions count once, as the queen.
        /// </summary>
        public static long Perft(SearchBoard board, Team side, int depth, PieceDefinition promotion)
        {
            if (depth == 0)
                return 1;

            var moves = Borrow(MovePool, depth);
            GetAllLegalMoves(board, side, moves, depth);

            if (depth == 1)
                return moves.Count;

            // Iterate a copy: the pooled list for this depth is reused below us.
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
