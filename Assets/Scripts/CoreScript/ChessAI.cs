using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public enum AIDecisionKind   // AI가 그 수를 고른 이유 -> 난이도를 조정할 때 로그로 확인하기 위함
    {
        Mate,   // 체크메이트(승리) 수를 찾아서 둠
        Opening,   // 게임 초반이라 무난한 시작 수(중앙 폰 전진, 기물 꺼내기)를 둠
        Blunder,   // 일부러 그럴듯한 실수를 둠 (초심자 상대로 난이도를 낮추기 위함)
        Considered   // 점수를 보고 확률적으로 고른 일반적인 수
    }

    public struct AIDecision   // AI가 고른 수와 그 이유·점수를 함께 담는 결과 구조체
    {
        public ChessMove move;   // 실제로 둘 수
        public AIDecisionKind kind;   // 그 수를 고른 이유
        public int rank;   // 점수 순위 (0 = AI가 가장 좋다고 평가한 수)
        public float score;   // 고른 수의 평가 점수
        public float bestScore;   // 가장 좋은 수의 평가 점수 -> score와 비교해 얼마나 손해 보는 수를 뒀는지 확인
    }

    public static class ChessAI   // 최종전 체스 대결의 상대 AI
    // -> 정해진 수만큼 앞을 내다보며 모든 합법 수를 평가한 뒤, 초심자처럼 그중에서 느슨하게 고름
    // -> 대체로 무난한 수를 두고 가끔 그럴듯한 실수도 하지만, 체크메이트는 (설정에 따라) 놓치지 않음
    // -> 난이도를 정하는 값은 전부 AIProfile(SO)에 있고, SearchBoard 위에서만 계산하므로 실제 보드는 건드리지 않음
    {
        const float MateScore = 1000f;   // 체크메이트 점수 -> 어떤 기물 가치 합보다도 훨씬 커서 메이트가 항상 최우선이 됨
        const float StalemateScore = 500f;   // 스테일메이트 점수 -> 어느 쪽이 막히든 AI에게 유리하게 대결이 끝나므로 가치 있게 보지만 메이트보다는 낮게 둠

        struct Scored   // 후보 수 하나와 그 평가 결과를 묶은 구조체
        {
            public ChessMove move;   // 후보 수
            public float score;   // 그 수를 둔 뒤의 평가 점수 (AI 입장, 높을수록 좋음)
            public bool mates;   // 이 수로 체크메이트가 되는지 여부
        }

        public static AIDecision ChooseMove(SearchBoard board, Team side, AIProfile profile,
            System.Random rng, int movesPlayed, PieceDefinition promotion)
        // AI가 이번 차례에 둘 수를 고르는 함수
        // side: AI 진영, profile: 난이도 설정, rng: 난수, movesPlayed: 지금까지 둔 수, promotion: 프로모션 시 바뀔 기물
        {
            var moves = new List<ChessMove>();
            ChessRules.GetAllLegalMoves(board, side, moves, 0);   // 지금 둘 수 있는 모든 합법 수를 구함
            if (moves.Count == 0)   // 만약 둘 수가 없다면(체크메이트·스테일메이트) 빈 결과를 돌려줌
                return default;

            int depth = Mathf.Max(1, profile.maxDepth);   // 몇 수 앞까지 내다볼지 (최소 1)
            var scored = new List<Scored>(moves.Count);   // 수마다 점수를 매겨 담을 리스트

            foreach (var m in moves)   // 모든 후보 수에 대해 점수를 매김
            {
                var promo = ChessRules.IsPromotion(board, m.piece, m.to) ? promotion : null;   // 프로모션 수라면 바뀔 기물 지정
                var undo = board.Make(m.piece, m.to, promo);   // 복사 보드에 실제로 둬보고

                float score = -Search(board, side.Opponent(), side, depth - 1, -Mathf.Infinity, Mathf.Infinity, 1, promotion);
                // 상대 차례부터 남은 깊이만큼 탐색 -> 상대 입장 점수가 나오므로 부호를 뒤집어 AI 입장 점수로 바꿈
                bool mates = score >= MateScore - 10f;   // 메이트 점수 근처라면 체크메이트로 이어지는 수

                if (depth == 1 && !mates)   // 만약 한 수 앞만 본다면 -> 상대의 응수를 볼 수 없으므로
                {
                    var moved = undo.promoted ?? m.piece;   // 움직인 기물 (프로모션됐다면 바뀐 기물)
                    float loss = HangingLoss(board, moved);   // 그 기물이 이 자리에서 그냥 잡혀 잃을 수 있는 가치를 계산하고
                    if (loss > profile.maxBlunderLoss)   // 허용 실수 범위보다 크게 잃는다면 그만큼 감점
                        score -= loss;   // -> 큰 기물을 공짜로 내주는 것만 막고, 작은 손실은 여전히 못 봄 = 초심자다운 실수
                }

                board.Unmake(undo);   // 시험한 수를 되돌리고
                scored.Add(new Scored { move = m, score = score, mates = mates });   // 결과를 기록
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));   // 점수가 높은 순으로 정렬 -> 0번이 최선의 수
            float best = scored[0].score;   // 최선의 점수

            // 1. 승리 수는 놓치지 않음 (mateFindRate 확률로 찾음)
            if (scored[0].mates && rng.NextDouble() < profile.mateFindRate)
                return Decide(scored, 0, AIDecisionKind.Mate);

            // 2. 초반 몇 수는 무난한 시작 -> 중앙 폰 전진, 기물 꺼내기
            if (movesPlayed < profile.sensibleOpeningMoves)
            {
                int pick = PickOpening(board, scored, rng);
                if (pick >= 0)   // 만약 그런 수가 있다면 그중 하나를 둠
                    return Decide(scored, pick, AIDecisionKind.Opening);
            }

            // 3. 이따금 그럴듯한 실수를 함 -> AI가 크게 앞서 있을수록 더 자주 함 (플레이어를 봐주는 장치)
            float lead = Material(board, side) - Material(board, side.Opponent());   // AI 기물 가치 합 - 플레이어 기물 가치 합 = 앞선 정도
            float blunderRate = lead >= profile.mercyLead ? profile.mercyBlunderRate : profile.blunderRate;   // 많이 앞서면 봐주기용 실수 확률 사용
            if (rng.NextDouble() < blunderRate)   // 만약 실수할 차례로 뽑혔다면
            {
                var pool = new List<int>();   // 실수로 둘 수 있는 후보들
                for (int i = 0; i < scored.Count; i++)
                    if (scored[i].score >= best - profile.maxBlunderLoss && scored[i].score > -StalemateScore)
                    // 최선보다 허용 범위 이내로만 나쁘고 + 메이트·스테일메이트를 당하는 수는 아닌 것만 -> 너무 티 나는 실수는 제외
                        pool.Add(i);
                if (pool.Count > 0)   // 후보가 있다면 그중 무작위로 하나를 둠
                    return Decide(scored, pool[rng.Next(pool.Count)], AIDecisionKind.Blunder);
            }

            // 4. 그 외에는 느슨하게 고름 -> 좋은 수일수록 뽑힐 확률이 높을 뿐 확정은 아님
            return Decide(scored, SoftPick(scored, profile.temperature, rng), AIDecisionKind.Considered);
        }

        static AIDecision Decide(List<Scored> scored, int index, AIDecisionKind kind) => new AIDecision
        // 고른 후보(index)와 이유(kind)로 최종 결과를 만드는 함수
        {
            move = scored[index].move,   // 둘 수
            kind = kind,   // 고른 이유
            rank = index,   // 점수 순위 (정렬된 상태이므로 인덱스가 곧 순위)
            score = scored[index].score,   // 고른 수의 점수
            bestScore = scored[0].score   // 최선의 수 점수
        };

        static float Search(SearchBoard board, Team toMove, Team aiSide, int depth, float alpha, float beta,
            int ply, PieceDefinition promotion)
        // 몇 수 앞까지 번갈아 둬보며 현재 국면의 점수를 구하는 탐색 함수 (알파-베타 가지치기를 적용한 네가맥스 방식)
        // -> 네가맥스: "상대에게 가장 나쁜 수 = 나에게 가장 좋은 수"라서 상대 점수에 -를 붙여 한 함수로 양쪽을 계산함
        // -> 알파-베타: 이미 찾은 수보다 나쁠 게 확실한 가지는 끝까지 보지 않고 잘라내 계산량을 줄임
        // -> 돌려주는 점수는 toMove(지금 둘 차례인 쪽) 입장, ply는 처음부터 몇 수째인지
        {
            var moves = new List<ChessMove>();
            ChessRules.GetAllLegalMoves(board, toMove, moves, ply);   // 둘 차례인 쪽의 모든 합법 수

            if (moves.Count == 0)   // 만약 둘 수가 없다면 게임이 끝난 국면
            {
                if (ChessRules.IsInCheck(board, toMove))   // 체크 상태라면 체크메이트 -> 둘 차례인 쪽이 진 것
                    return -(MateScore - ply);   // ply를 빼서 더 빨리 끝나는 메이트일수록 점수 차이가 커지게 함
                return toMove == aiSide ? StalemateScore : -StalemateScore;   // 스테일메이트 -> 어느 쪽이 막히든 AI에게 유리한 점수
            }

            if (depth <= 0)   // 더 내다볼 깊이가 없다면 지금 국면을 바로 평가
                return Evaluate(board, toMove);

            moves.Sort((a, b) => b.isCapture.CompareTo(a.isCapture));   // 잡는 수를 먼저 봄 -> 좋은 수를 일찍 찾을수록 가지치기가 더 빨리 일어남

            float bestScore = -Mathf.Infinity;   // 지금까지 찾은 최고 점수
            foreach (var m in moves)   // 모든 수를 하나씩
            {
                var promo = ChessRules.IsPromotion(board, m.piece, m.to) ? promotion : null;
                var undo = board.Make(m.piece, m.to, promo);   // 둬보고
                float score = -Search(board, toMove.Opponent(), aiSide, depth - 1, -beta, -alpha, ply + 1, promotion);   // 상대 차례로 한 단계 더 탐색 (상대 점수를 뒤집어 내 점수로)
                board.Unmake(undo);   // 되돌림

                if (score > bestScore) bestScore = score;   // 더 좋은 수면 최고 점수 갱신
                if (score > alpha) alpha = score;   // 내가 확보한 최소 점수(alpha) 갱신
                if (alpha >= beta) break;   // 상대가 이미 이보다 나은 선택지를 갖고 있다면 상대는 이 길을 택하지 않음 -> 나머지 수는 볼 필요 없음(가지치기)
            }
            return bestScore;
        }

        public static float Evaluate(IBoardView board, Team side)
        // side 입장에서 현재 국면이 얼마나 유리한지 점수로 계산하는 함수 (폰 1개 = 약 1점 단위)
        // -> 기물 가치가 가장 크고, 중앙 차지·폰 전진·킹 주변 보호에 작은 가산점을 줌
        // -> 기물 가치(aiValue)를 데이터에서 읽으므로 기본 6종뿐 아니라 새로 추가한 기물에도 그대로 동작함
        {
            float total = 0f;   // 최종 점수 (내 기물은 +, 상대 기물은 -)
            float centre = (board.Width - 1) * 0.5f;   // 보드 가로 중앙 위치
            float middle = (board.Height - 1) * 0.5f;   // 보드 세로 중앙 위치

            foreach (var p in board.AllPieces)   // 모든 기물에 대해
            {
                float v = p.Def.aiValue;   // 기본 가치 (예: 폰 1, 나이트·비숍 3, 룩 5, 퀸 9)
                if (!p.IsObjective)   // 만약 킹이 아니라면
                {
                    float dist = Mathf.Abs(p.Coord.x - centre) + Mathf.Abs(p.Coord.y - middle);   // 보드 중앙에서 얼마나 떨어져 있는지
                    v += Mathf.Max(0f, 0.3f - dist * 0.05f);   // 중앙에 가까울수록 최대 +0.3 가산 -> 중앙 기물이 더 많은 칸을 공격할 수 있기 때문

                    if (p.Def.canPromote)   // 만약 폰(프로모션 가능 기물)이라면
                    {
                        int advanced = p.Team.ForwardY() > 0 ? p.Coord.y : board.Height - 1 - p.Coord.y;   // 자기 진영 기준 얼마나 앞으로 나갔는지
                        v += advanced * 0.05f;   // 많이 전진할수록 가산 -> 프로모션에 가까워지기 때문
                    }
                }
                else   // 킹이라면
                {
                    v += KingShelter(board, p) * 0.05f;   // 주변을 지켜주는 아군 수만큼 가산 -> 킹 안전
                }

                total += p.Team == side ? v : -v;   // 내 기물이면 더하고 상대 기물이면 뺌
            }
            return total;
        }

        static int KingShelter(IBoardView board, Piece king)   // 킹 주변 8칸에 있는 아군 기물 수를 세는 함수
        {
            int friends = 0;   // 주변 아군 수
            for (int dx = -1; dx <= 1; dx++)   // 킹을 둘러싼 3x3 칸을 검사
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;   // 킹 자신의 칸은 제외
                var p = board.GetPiece(new Coord(king.Coord.x + dx, king.Coord.y + dy));
                if (p != null && p.Team == king.Team) friends++;   // 같은 편 기물이면 +1
            }
            return friends;
        }

        public static float Material(IBoardView board, Team side)   // 한 진영의 기물 가치 합계를 구하는 함수 -> 누가 얼마나 앞서 있는지 비교할 때 사용
        {
            float sum = 0f;
            foreach (var p in board.AllPieces)
                if (p.Team == side) sum += p.Def.aiValue;   // 그 진영 기물의 가치를 모두 더함
            return sum;
        }

        static float HangingLoss(SearchBoard board, Piece piece)
        // 기물이 지금 자리에서 상대에게 잡혀 잃을 수 있는 가치를 계산하는 함수
        // -> 공격받는데 아군이 지켜주지 않으면 가치 전부를, 지켜주더라도 더 싼 기물에게 잡히면 그 차이만큼을 잃음
        {
            if (piece.IsObjective)   // 킹은 잡히지 않으므로 잃을 것이 없음
                return 0f;

            float cheapest = float.MaxValue;   // 이 기물을 공격하는 상대 기물 중 가장 싼 기물의 가치
            foreach (var p in board.AllPieces)
                if (p.Team != piece.Team && ChessRules.Attacks(board, p, piece.Coord))   // 만약 상대 기물이 이 칸을 공격하고 있다면
                    cheapest = Mathf.Min(cheapest, p.IsObjective ? 0f : p.Def.aiValue);   // 가장 싼 공격자 가치를 갱신 (킹은 잃어도 되는 가치가 없으니 0으로 취급)

            if (cheapest == float.MaxValue)   // 아무도 공격하지 않는다면 안전 -> 손실 없음
                return 0f;

            bool defended = ChessRules.IsSquareAttacked(board, piece.Coord, piece.Team);   // 아군이 이 칸을 지키고 있는지 (잡히면 바로 다시 잡을 수 있는지)
            return defended ? Mathf.Max(0f, piece.Def.aiValue - cheapest) : piece.Def.aiValue;   // 지켜지면 교환 손해만큼, 아니면 가치 전부를 잃음
        }

        static int SoftPick(List<Scored> scored, float temperature, System.Random rng)
        // 점수가 높은 수일수록 더 잘 뽑히도록 확률적으로 하나를 고르는 함수 (점수 순 정렬된 리스트 기준)
        // -> temperature가 클수록 점수 차이를 덜 따져 더 무작위로, 작을수록 최선의 수만 고름
        {
            if (temperature <= 0f)   // 만약 temperature가 0 이하라면 무작위 없이 항상 최선의 수
                return 0;

            float best = scored[0].score;
            var weights = new float[scored.Count];   // 수마다 뽑힐 가중치
            float sum = 0f;   // 가중치 합계
            for (int i = 0; i < scored.Count; i++)
            {
                weights[i] = Mathf.Exp((scored[i].score - best) / temperature);   // 최선과의 점수 차이가 클수록 가중치가 급격히 작아짐 (최선의 수 = 1)
                sum += weights[i];
            }

            double roll = rng.NextDouble() * sum;   // 0 ~ 가중치 합 사이에서 무작위 값을 뽑고
            for (int i = 0; i < weights.Length; i++)   // 가중치를 차례로 빼가다가
            {
                roll -= weights[i];
                if (roll <= 0) return i;   // 0 이하가 되는 순간의 수를 선택 -> 가중치가 큰 수일수록 걸릴 확률이 높음
            }
            return 0;   // 계산 오차로 끝까지 못 골랐다면 최선의 수
        }

        static int PickOpening(SearchBoard board, List<Scored> scored, System.Random rng)
        // 게임 초반에 둘 무난한 시작 수를 고르는 함수 -> 손해 없는 수 중에서 중앙 폰 전진이나 가벼운 기물 꺼내기를 우선, 없으면 -1
        {
            float best = scored[0].score;
            var candidates = new List<int>();   // 조건에 맞는 후보 수의 인덱스
            float centre = (board.Width - 1) * 0.5f;   // 보드 가로 중앙

            for (int i = 0; i < scored.Count; i++)
            {
                if (scored[i].score < best - 0.5f)   // 만약 최선보다 0.5점 넘게 나쁜 수라면 제외 (손해 보는 수)
                    continue;

                var m = scored[i].move;
                var def = m.piece.Def;
                bool centralPawn = def.canPromote && Mathf.Abs(m.to.x - centre) <= 1f;   // 중앙 근처 열로 가는 폰 이동인지
                bool development = !def.canPromote && !m.piece.IsObjective && !m.piece.HasMoved
                                   && def.aiValue > 0f && def.aiValue <= 3.5f;
                // 기물 전개인지 -> 폰·킹이 아니고 + 아직 안 움직였고 + 가치 3.5 이하인 가벼운 기물(나이트·비숍)을 처음 꺼내는 수
                if (centralPawn || development)   // 둘 중 하나라도 해당하면 후보에 추가
                    candidates.Add(i);
            }

            return candidates.Count == 0 ? -1 : candidates[rng.Next(candidates.Count)];   // 후보가 없으면 -1, 있으면 무작위로 하나
        }
    }
}
