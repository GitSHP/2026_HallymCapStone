using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>Why the AI picked the move it did — logged while tuning difficulty.</summary>
    public enum AIDecisionKind
    {
        Mate,
        Opening,
        Blunder,
        Considered
    }

    public struct AIDecision
    {
        public ChessMove move;
        public AIDecisionKind kind;
        /// <summary>0 = the move the AI rated best.</summary>
        public int rank;
        public float score;
        public float bestScore;
    }

    /// <summary>
    /// The chess opponent. It rates every legal move by looking a set number of
    /// moves ahead, then chooses loosely among them the way a beginner would:
    /// usually a reasonable move, sometimes a believable mistake, never a missed
    /// checkmate. Everything that sets the difficulty lives in an AIProfile.
    /// Pure logic on a SearchBoard — the live board is never touched.
    /// </summary>
    public static class ChessAI
    {
        const float MateScore = 1000f;
        // Stalemate ends the duel in the AI's favour whoever is stuck, so the AI
        // values it — but well below an actual checkmate.
        const float StalemateScore = 500f;

        struct Scored
        {
            public ChessMove move;
            public float score;
            public bool mates;
        }

        public static AIDecision ChooseMove(SearchBoard board, Team side, AIProfile profile,
            System.Random rng, int movesPlayed, PieceDefinition promotion)
        {
            var moves = new List<ChessMove>();
            ChessRules.GetAllLegalMoves(board, side, moves, 0);
            if (moves.Count == 0)
                return default;

            int depth = Mathf.Max(1, profile.maxDepth);
            var scored = new List<Scored>(moves.Count);

            foreach (var m in moves)
            {
                var promo = ChessRules.IsPromotion(board, m.piece, m.to) ? promotion : null;
                var undo = board.Make(m.piece, m.to, promo);

                float score = -Search(board, side.Opponent(), side, depth - 1, -Mathf.Infinity, Mathf.Infinity, 1, promotion);
                bool mates = score >= MateScore - 10f;

                // One move ahead cannot see the reply, so add a guard against
                // simply handing over a big piece; small losses stay invisible,
                // which is exactly the kind of mistake a beginner makes.
                if (depth == 1 && !mates)
                {
                    var moved = undo.promoted ?? m.piece;
                    float loss = HangingLoss(board, moved);
                    if (loss > profile.maxBlunderLoss)
                        score -= loss;
                }

                board.Unmake(undo);
                scored.Add(new Scored { move = m, score = score, mates = mates });
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));
            float best = scored[0].score;

            // 1. Never miss the win (subject to the profile).
            if (scored[0].mates && rng.NextDouble() < profile.mateFindRate)
                return Decide(scored, 0, AIDecisionKind.Mate);

            // 2. A sensible start: centre pawns and developing pieces.
            if (movesPlayed < profile.sensibleOpeningMoves)
            {
                int pick = PickOpening(board, scored, rng);
                if (pick >= 0)
                    return Decide(scored, pick, AIDecisionKind.Opening);
            }

            // 3. Now and then, a believable mistake — more often when well ahead.
            float lead = Material(board, side) - Material(board, side.Opponent());
            float blunderRate = lead >= profile.mercyLead ? profile.mercyBlunderRate : profile.blunderRate;
            if (rng.NextDouble() < blunderRate)
            {
                var pool = new List<int>();
                for (int i = 0; i < scored.Count; i++)
                    if (scored[i].score >= best - profile.maxBlunderLoss && scored[i].score > -StalemateScore)
                        pool.Add(i);
                if (pool.Count > 0)
                    return Decide(scored, pool[rng.Next(pool.Count)], AIDecisionKind.Blunder);
            }

            // 4. Otherwise pick loosely: better moves are likelier, not certain.
            return Decide(scored, SoftPick(scored, profile.temperature, rng), AIDecisionKind.Considered);
        }

        static AIDecision Decide(List<Scored> scored, int index, AIDecisionKind kind) => new AIDecision
        {
            move = scored[index].move,
            kind = kind,
            rank = index,
            score = scored[index].score,
            bestScore = scored[0].score
        };

        // ---------- Search ----------

        /// <summary>Negamax with alpha-beta; scores are from <paramref name="toMove"/>'s point of view.</summary>
        static float Search(SearchBoard board, Team toMove, Team aiSide, int depth, float alpha, float beta,
            int ply, PieceDefinition promotion)
        {
            var moves = new List<ChessMove>();
            ChessRules.GetAllLegalMoves(board, toMove, moves, ply);

            if (moves.Count == 0)
            {
                if (ChessRules.IsInCheck(board, toMove))
                    return -(MateScore - ply);   // quicker mates score higher
                // Stalemate is a loss for the player whichever side is stuck.
                return toMove == aiSide ? StalemateScore : -StalemateScore;
            }

            if (depth <= 0)
                return Evaluate(board, toMove);

            // Captures first: they decide most lines and make the cut-offs bite sooner.
            moves.Sort((a, b) => b.isCapture.CompareTo(a.isCapture));

            float bestScore = -Mathf.Infinity;
            foreach (var m in moves)
            {
                var promo = ChessRules.IsPromotion(board, m.piece, m.to) ? promotion : null;
                var undo = board.Make(m.piece, m.to, promo);
                float score = -Search(board, toMove.Opponent(), aiSide, depth - 1, -beta, -alpha, ply + 1, promotion);
                board.Unmake(undo);

                if (score > bestScore) bestScore = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            return bestScore;
        }

        // ---------- Evaluation ----------

        /// <summary>
        /// The position from <paramref name="side"/>'s point of view, in pawns:
        /// material first, then small nudges for the centre, pawn progress and a
        /// sheltered king. Works for any piece described by data, not just the six classics.
        /// </summary>
        public static float Evaluate(IBoardView board, Team side)
        {
            float total = 0f;
            float centre = (board.Width - 1) * 0.5f;
            float middle = (board.Height - 1) * 0.5f;

            foreach (var p in board.AllPieces)
            {
                float v = p.Def.aiValue;
                if (!p.IsObjective)
                {
                    // Up to +0.3 for standing in the middle of the board.
                    float dist = Mathf.Abs(p.Coord.x - centre) + Mathf.Abs(p.Coord.y - middle);
                    v += Mathf.Max(0f, 0.3f - dist * 0.05f);

                    if (p.Def.canPromote)
                    {
                        int advanced = p.Team.ForwardY() > 0 ? p.Coord.y : board.Height - 1 - p.Coord.y;
                        v += advanced * 0.05f;
                    }
                }
                else
                {
                    v += KingShelter(board, p) * 0.05f;
                }

                total += p.Team == side ? v : -v;
            }
            return total;
        }

        static int KingShelter(IBoardView board, Piece king)
        {
            int friends = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var p = board.GetPiece(new Coord(king.Coord.x + dx, king.Coord.y + dy));
                if (p != null && p.Team == king.Team) friends++;
            }
            return friends;
        }

        public static float Material(IBoardView board, Team side)
        {
            float sum = 0f;
            foreach (var p in board.AllPieces)
                if (p.Team == side) sum += p.Def.aiValue;
            return sum;
        }

        /// <summary>
        /// What the piece stands to lose where it now stands: everything if it is
        /// attacked and undefended, the difference if a cheaper piece can take it.
        /// </summary>
        static float HangingLoss(SearchBoard board, Piece piece)
        {
            if (piece.IsObjective)
                return 0f;

            float cheapest = float.MaxValue;
            foreach (var p in board.AllPieces)
                if (p.Team != piece.Team && ChessRules.Attacks(board, p, piece.Coord))
                    cheapest = Mathf.Min(cheapest, p.IsObjective ? 0f : p.Def.aiValue);

            if (cheapest == float.MaxValue)
                return 0f;

            bool defended = ChessRules.IsSquareAttacked(board, piece.Coord, piece.Team);
            return defended ? Mathf.Max(0f, piece.Def.aiValue - cheapest) : piece.Def.aiValue;
        }

        // ---------- Choosing ----------

        static int SoftPick(List<Scored> scored, float temperature, System.Random rng)
        {
            if (temperature <= 0f)
                return 0;

            float best = scored[0].score;
            var weights = new float[scored.Count];
            float sum = 0f;
            for (int i = 0; i < scored.Count; i++)
            {
                weights[i] = Mathf.Exp((scored[i].score - best) / temperature);
                sum += weights[i];
            }

            double roll = rng.NextDouble() * sum;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0) return i;
            }
            return 0;
        }

        /// <summary>
        /// Among moves that lose nothing, prefer a central pawn push or bringing a
        /// minor piece off the back rank. Returns -1 when there is no such move.
        /// </summary>
        static int PickOpening(SearchBoard board, List<Scored> scored, System.Random rng)
        {
            float best = scored[0].score;
            var candidates = new List<int>();
            float centre = (board.Width - 1) * 0.5f;

            for (int i = 0; i < scored.Count; i++)
            {
                if (scored[i].score < best - 0.5f)
                    continue;

                var m = scored[i].move;
                var def = m.piece.Def;
                bool centralPawn = def.canPromote && Mathf.Abs(m.to.x - centre) <= 1f;
                bool development = !def.canPromote && !m.piece.IsObjective && !m.piece.HasMoved
                                   && def.aiValue > 0f && def.aiValue <= 3.5f;
                if (centralPawn || development)
                    candidates.Add(i);
            }

            return candidates.Count == 0 ? -1 : candidates[rng.Next(candidates.Count)];
        }
    }
}
