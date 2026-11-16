using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Promotion.Core;

namespace Promotion.Analysis
{
    public static class RunAnalyzer
    // 런 기록(RunLog)에서 실수와 잘한 점을 규칙으로 찾아내는 클래스
    // -> 판정은 전부 여기서 끝냄. 리포트 문장은 이 결과만 근거로 작성됨
    // -> 최종전 수 평가는 무거우므로 코루틴으로 한 수씩 나눠 처리해 화면이 멈추지 않게 함
    {
        const int ReviewDepth = 3;          // 내 수 -> 상대 응수 -> 내 재응수까지 봄 (지켜진 기물을 공짜로 착각하지 않으려면 3수가 필요)
        const float BlunderLoss = 2.5f;     // 최선의 수보다 이만큼(폰 단위) 나쁘면 큰 실수
        const float HangingValue = 3f;      // 기물을 공짜로 내준 것으로 볼 최소 가치
        const float SlowHoldSec = 4f;       // 이 시간 이상 고민하면 어려워한 기물로 봄
        const int MinMovesForHabit = 3;     // 습관으로 판단할 최소 이동 횟수
        const int MaxProblems = 6;          // 리포트에 넘길 실수 항목 수
        const int MaxStrengths = 3;         // 리포트에 넘길 잘한 점 항목 수

        public static IEnumerator Run(RunLog log, Action<RunAnalysis> done)
        // 분석을 실행하는 코루틴 -> 최종전 수를 하나 평가할 때마다 한 프레임 쉬고, 끝나면 done으로 결과를 넘김
        {
            var analysis = new RunAnalysis { result = log.result };
            var findings = new List<Finding>();

            FillTotals(log, analysis);
            FillPieceStats(log, analysis);
            AddDefenseFindings(log, analysis, findings);

            foreach (var move in log.moves)
            {
                if (!IsDuel(log, move) || move.outcome != "Moved" || move.before == null)
                    continue;
                ReviewDuelMove(log, move, analysis, findings);
                yield return null;
            }

            AddStrengths(log, analysis, findings);
            Finalize(analysis, findings);
            done?.Invoke(analysis);
        }

        static bool IsDuel(RunLog log, MoveRecord move)
        {
            return move.stage >= 0 && move.stage < log.stages.Count && log.stages[move.stage].isDuel;
        }

        static string Square(int x, int y)  // 좌표를 체스 표기(a1 등)로 바꾸는 함수
        {
            return (char)('a' + x) + (y + 1).ToString();
        }

        static void FillTotals(RunLog log, RunAnalysis analysis)
        {
            analysis.stagesPlayed = log.stages.Count;
            analysis.stagesCleared = log.stages.Count(s => s.outcome == "Victory");
            analysis.reachedDuel = log.stages.Any(s => s.isDuel);
            analysis.totalTurns = log.stages.Sum(s => s.turns);
            analysis.totalMoves = log.moves.Count(m => m.outcome == "Moved");
            analysis.totalCaptures = log.moves.Count(m => m.captured);
            analysis.totalLost = log.losses.Count;
            analysis.duelMoves = log.moves.Count(m => m.outcome == "Moved" && IsDuel(log, m));
            analysis.duelCleanMoves = analysis.duelMoves;   // 큰 실수를 찾을 때마다 하나씩 뺌
        }

        static void FillPieceStats(RunLog log, RunAnalysis analysis)
        {
            var stats = new Dictionary<string, PieceStat>();

            PieceStat Get(string asset)
            {
                if (!stats.TryGetValue(asset, out var stat))
                {
                    stat = new PieceStat { piece = log.DisplayName(asset) };
                    stats[asset] = stat;
                }
                return stat;
            }

            foreach (var group in log.moves.GroupBy(m => m.piece))
            {
                var stat = Get(group.Key);
                stat.moved = group.Count(m => m.outcome == "Moved");
                stat.captures = group.Count(m => m.captured);
                stat.cancelled = group.Count(m => m.outcome == "Cancelled");
                var decided = group.Where(m => m.outcome == "Moved").ToList();
                stat.avgHoldSec = decided.Count > 0 ? decided.Average(m => m.holdSec) : 0f;
            }
            foreach (var e in log.deploys)
                Get(e.piece).deployed++;
            foreach (var e in log.guideOpens)
                Get(e.piece).guideOpens++;
            foreach (var e in log.losses)
                Get(e.piece).lost++;

            analysis.pieces = stats.Values.OrderByDescending(s => s.moved + s.deployed).ToList();
        }

        static void AddDefenseFindings(RunLog log, RunAnalysis analysis, List<Finding> findings)
        {
            var missedTurns = log.turnEnds.Where(t => t.missedCaptures.Count > 0).ToList();
            if (missedTurns.Count > 0)
            {
                var top = missedTurns.SelectMany(t => t.missedCaptures)
                    .GroupBy(c => c.Split('>')[0])
                    .OrderByDescending(g => g.Count())
                    .First();
                var example = missedTurns[0];
                findings.Add(new Finding
                {
                    kind = "MissedCaptures",
                    severity = 2,
                    stage = example.stage,
                    turn = example.turn,
                    piece = log.DisplayName(top.Key),
                    count = missedTurns.Count,
                    board = example.board,
                    fact = "AP가 남아 있고 잡을 수 있는 적이 있는데 턴을 넘긴 적이 " + missedTurns.Count + "번 있었다. "
                           + log.DisplayName(top.Key) + "(으)로 잡을 수 있던 경우가 가장 많았다."
                });
            }

            if (log.losses.Count >= 2)
            {
                var top = log.losses.GroupBy(l => l.piece).OrderByDescending(g => g.Count()).First();
                findings.Add(new Finding
                {
                    kind = "PieceLosses",
                    severity = 1,
                    stage = top.First().stage,
                    turn = top.First().turn,
                    piece = log.DisplayName(top.Key),
                    count = log.losses.Count,
                    fact = "이번 런에서 아군 기물을 " + log.losses.Count + "개 잃었다. 가장 많이 잃은 기물은 "
                           + log.DisplayName(top.Key) + "(" + top.Count() + "개)이다."
                });
            }

            foreach (var stat in analysis.pieces)
            {
                if (stat.deployed > 0 && stat.moved == 0 && stat.piece != "킹")
                {
                    findings.Add(new Finding
                    {
                        kind = "NeverMoved",
                        severity = 1,
                        piece = stat.piece,
                        count = stat.deployed,
                        fact = stat.piece + "을(를) " + stat.deployed + "번 소환했지만 한 번도 움직이지 않았다."
                    });
                }
            }

            var slow = analysis.pieces
                .Where(s => s.moved >= MinMovesForHabit && s.avgHoldSec >= SlowHoldSec)
                .OrderByDescending(s => s.avgHoldSec)
                .FirstOrDefault();
            if (slow != null)
            {
                findings.Add(new Finding
                {
                    kind = "SlowDecision",
                    severity = 1,
                    piece = slow.piece,
                    count = slow.moved,
                    value = slow.avgHoldSec,
                    fact = slow.piece + "을(를) 움직일 때 평균 " + slow.avgHoldSec.ToString("0.0") + "초 고민했다. 다른 기물보다 어려워한 것으로 보인다."
                });
            }
        }

        static void ReviewDuelMove(RunLog log, MoveRecord move, RunAnalysis analysis, List<Finding> findings)
        // 최종전의 수 하나를 평가해 큰 실수라면 항목으로 추가하는 함수
        {
            var from = new Coord(move.fromX, move.fromY);
            var to = new Coord(move.toX, move.toY);
            var review = ChessAI.ReviewMove(move.before, Team.Player, from, to, ReviewDepth, move.promotion);
            if (!review.found)
                return;

            float loss = review.bestScore - review.playedScore;
            string kind = null;
            int severity = 0;
            string fact = null;
            string name = log.DisplayName(move.piece);
            string played = Square(move.fromX, move.fromY) + "-" + Square(move.toX, move.toY);
            string betterName = log.DisplayName(review.best.piece.Def.name);
            string better = Square(review.bestFrom.x, review.bestFrom.y) + "-" + Square(review.best.to.x, review.best.to.y);

            if (review.bestMates && !review.playedMates)
            {
                kind = "MissedMate";
                severity = 4;
                fact = name + "을(를) " + played + "(으)로 뒀지만, " + betterName + " " + better + "(으)로 체크메이트까지 이어갈 수 있었다.";
            }
            else if (review.playedGetsMated && !review.bestGetsMated)
            {
                kind = "AllowsMate";
                severity = 4;
                fact = name + "을(를) " + played + "(으)로 둔 뒤 상대에게 체크메이트를 당하는 길이 열렸다. " + betterName + " " + better + "(으)로 막을 수 있었다.";
            }
            else if (!review.bestMates && !review.bestGetsMated && !review.playedGetsMated && loss >= BlunderLoss)
            {
                bool hanging = review.hangingLoss >= HangingValue;
                kind = hanging ? "HangingPiece" : "Blunder";
                severity = 3;
                fact = hanging
                    ? name + "을(를) " + played + "(으)로 옮겼는데 그 칸에서 공짜로 잡힐 수 있었다. 최선의 수보다 폰 약 " + loss.ToString("0.0") + "개만큼 손해였다."
                    : name + " " + played + "은(는) 최선의 수보다 폰 약 " + loss.ToString("0.0") + "개만큼 손해인 수였다.";
            }

            if (kind == null)
                return;

            analysis.duelCleanMoves--;
            findings.Add(new Finding
            {
                kind = kind,
                severity = severity,
                stage = move.stage,
                turn = move.turn,
                piece = name,
                value = loss,
                fact = fact,
                hasMove = true,
                board = move.board,
                played = played,
                better = better,
                betterPiece = betterName
            });
        }

        static void AddStrengths(RunLog log, RunAnalysis analysis, List<Finding> findings)
        {
            var hunter = analysis.pieces.Where(s => s.captures > 0).OrderByDescending(s => s.captures).FirstOrDefault();
            if (hunter != null)
            {
                findings.Add(new Finding
                {
                    kind = "TopCapturer",
                    positive = true,
                    severity = 2,
                    piece = hunter.piece,
                    count = hunter.captures,
                    fact = hunter.piece + "(으)로 상대를 " + hunter.captures + "번 잡았다. 이번 런에서 가장 활약한 기물이다."
                });
            }

            int castles = log.moves.Count(m => m.outcome == "Castled");
            if (castles > 0)
            {
                findings.Add(new Finding
                {
                    kind = "UsedCastling",
                    positive = true,
                    severity = 2,
                    count = castles,
                    fact = "캐슬링을 " + castles + "번 사용했다."
                });
            }

            if (log.promotions.Count > 0)
            {
                findings.Add(new Finding
                {
                    kind = "UsedPromotion",
                    positive = true,
                    severity = 2,
                    piece = log.DisplayName(log.promotions[0].piece),
                    count = log.promotions.Count,
                    fact = "폰을 끝까지 전진시켜 " + log.promotions.Count + "번 프로모션했다."
                });
            }

            if (analysis.duelMoves >= 5 && analysis.duelCleanMoves == analysis.duelMoves)
            {
                findings.Add(new Finding
                {
                    kind = "CleanDuel",
                    positive = true,
                    severity = 3,
                    count = analysis.duelMoves,
                    fact = "최종전 " + analysis.duelMoves + "수 동안 큰 실수가 하나도 없었다."
                });
            }

            var duel = log.stages.FirstOrDefault(s => s.isDuel && s.outcome == "Victory");
            if (duel != null)
            {
                findings.Add(new Finding
                {
                    kind = "WonDuel",
                    positive = true,
                    severity = 4,
                    stage = duel.index,
                    turn = duel.turns,
                    fact = "최종전에서 " + duel.reason + "(으)로 승리했다."
                });
            }
        }

        static void Finalize(RunAnalysis analysis, List<Finding> findings)
        // 중요한 항목만 남기고 근거 번호를 매기는 함수
        {
            var problems = findings.Where(f => !f.positive)
                .OrderByDescending(f => f.severity).ThenByDescending(f => f.value).Take(MaxProblems);
            var strengths = findings.Where(f => f.positive)
                .OrderByDescending(f => f.severity).Take(MaxStrengths);

            analysis.findings = problems.Concat(strengths).ToList();
            for (int i = 0; i < analysis.findings.Count; i++)
            {
                analysis.findings[i].id = "F" + (i + 1);
                analysis.findings[i].fact = FixJosa(analysis.findings[i].fact);
            }
        }

        static string FixJosa(string text)  // "을(를)" 같은 표기를 앞 글자의 받침에 맞는 조사로 바꾸는 함수
        {
            return Regex.Replace(text, @"([가-힣0-9])(을\(를\)|은\(는\)|\(으\)로)", match =>
            {
                char last = match.Groups[1].Value[0];
                string josa = match.Groups[2].Value;
                int final = Final(last);
                if (josa == "(으)로")
                    return last + (final == 0 || final == 8 ? "로" : "으로");   // 받침이 없거나 ㄹ이면 '로'
                if (josa == "을(를)")
                    return last + (final == 0 ? "를" : "을");
                return last + (final == 0 ? "는" : "은");
            });
        }

        static int Final(char c)    // 글자의 받침 번호 (0이면 받침 없음, 8이면 ㄹ)
        {
            if (c >= '0' && c <= '9')
                return "4 8 0 4 0 0 4 8 8 0"[(c - '0') * 2] - '0';  // 숫자의 한글 발음: 영(ㅇ) 일(ㄹ) 이 삼(ㅁ) 사 오 육(ㄱ) 칠(ㄹ) 팔(ㄹ) 구
            return (c - 0xAC00) % 28;
        }
    }
}
