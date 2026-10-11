using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>AI가 그 수를 고른 이유다. 난이도를 조정할 때 로그로 남긴다.</summary>
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
        /// <summary>0 = AI가 최선으로 평가한 수이다.</summary>
        public int rank;
        public float score;
        public float bestScore;
    }

    /// <summary>
    /// 체스 상대다. 정해진 수만큼 앞을 내다보며 모든 합법 수를 평가한 뒤,
    /// 초심자처럼 그중에서 느슨하게 고른다. 대체로 무난한 수를 두고, 가끔
    /// 그럴듯한 실수를 하지만, 체크메이트는 절대 놓치지 않는다. 난이도를 정하는
    /// 값은 전부 AIProfile에 있다. SearchBoard 위의 순수 로직이며 실제 보드는 건드리지 않는다.
    /// </summary>
    public static class ChessAI
    {
        const float MateScore = 1000f;
        // 스테일메이트는 어느 쪽이 막히든 AI에게 유리하게 대결을 끝내므로 AI는 이를
        // 가치 있게 본다. 다만 실제 체크메이트보다는 훨씬 낮게 평가한다.
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

                // 한 수 앞만 보면 상대의 응수를 볼 수 없으므로, 큰 기물을 그냥
                // 내주는 것만은 막는 장치를 더한다. 작은 손실은 여전히 보이지 않는데,
                // 이것이 바로 초심자가 하는 종류의 실수다.
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

            // 1. 승리 수는 놓치지 않는다(프로필 설정에 따름).
            if (scored[0].mates && rng.NextDouble() < profile.mateFindRate)
                return Decide(scored, 0, AIDecisionKind.Mate);

            // 2. 무난한 시작: 중앙 폰 전진과 기물 전개.
            if (movesPlayed < profile.sensibleOpeningMoves)
            {
                int pick = PickOpening(board, scored, rng);
                if (pick >= 0)
                    return Decide(scored, pick, AIDecisionKind.Opening);
            }

            // 3. 이따금 그럴듯한 실수를 한다 — 크게 앞서 있을수록 더 자주 한다.
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

            // 4. 그 외에는 느슨하게 고른다. 좋은 수일수록 뽑힐 확률이 높을 뿐 확정은 아니다.
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

        // ---------- 탐색 ----------

        /// <summary>알파-베타 가지치기를 적용한 네가맥스다. 점수는 <paramref name="toMove"/>의 관점이다.</summary>
        static float Search(SearchBoard board, Team toMove, Team aiSide, int depth, float alpha, float beta,
            int ply, PieceDefinition promotion)
        {
            var moves = new List<ChessMove>();
            ChessRules.GetAllLegalMoves(board, toMove, moves, ply);

            if (moves.Count == 0)
            {
                if (ChessRules.IsInCheck(board, toMove))
                    return -(MateScore - ply);   // 더 빠른 메이트일수록 점수가 높다
                // 스테일메이트는 어느 쪽이 막히든 플레이어의 패배다.
                return toMove == aiSide ? StalemateScore : -StalemateScore;
            }

            if (depth <= 0)
                return Evaluate(board, toMove);

            // 잡는 수를 먼저 본다. 대부분의 수순을 결정하고 가지치기가 더 일찍 일어나게 한다.
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

        // ---------- 평가 ----------

        /// <summary>
        /// <paramref name="side"/> 관점의 국면을 폰 단위로 평가한다. 기물 가치가 우선이고,
        /// 중앙 장악·폰 전진·킹 보호에 작은 가산점을 준다. 전통적인 6종뿐 아니라
        /// 데이터로 정의된 어떤 기물에도 동작한다.
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
                    // 보드 중앙에 서 있으면 최대 +0.3을 준다.
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
        /// 기물이 현재 위치에서 잃을 수 있는 가치다. 공격받는데 보호받지 못하면 전부를,
        /// 더 싼 기물이 잡을 수 있으면 그 차이만큼을 잃는다.
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

        // ---------- 선택 ----------

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
        /// 손해 없는 수 중에서 중앙 폰 전진이나 마이너 기물을 뒷줄에서 꺼내는 수를
        /// 우선한다. 그런 수가 없으면 -1을 반환한다.
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
