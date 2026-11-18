using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Promotion.Analysis
{
    public static class RunReportWriter
    // 분석 결과로 리포트 문장을 만드는 클래스 -> AI 응답을 받지 못했을 때 쓰는 정해진 문장(오프라인 요약)
    {
        static readonly Dictionary<string, string> Labels = new()   // 항목 종류 -> 화면에 보일 이름
        {
            { "MissedMate", "외통수 놓침" },
            { "AllowsMate", "외통수 허용" },
            { "HangingPiece", "기물을 공짜로 내줌" },
            { "Blunder", "큰 실수" },
            { "MissedCaptures", "잡기 기회 놓침" },
            { "PieceLosses", "기물 손실" },
            { "NeverMoved", "안 움직인 기물" },
            { "SlowDecision", "오래 고민한 기물" },
        };

        static readonly Dictionary<string, string> Tips = new()     // 항목 종류 -> 실제 체스에서 쓸 수 있는 조언
        {
            { "MissedMate", "매 수마다 상대 킹에게 체크를 걸 수 있는 수부터 찾아보세요. 체크를 먼저 살피면 외통수가 잘 보입니다." },
            { "AllowsMate", "수를 두기 전에 상대가 바로 끝낼 수 있는 수가 있는지 확인하세요. 폰에 막혀 도망칠 곳이 없는 킹이 특히 위험합니다." },
            { "HangingPiece", "기물을 옮기기 전에 그 칸을 상대가 공격하고 있는지, 잡히면 다시 잡을 수 있는지 먼저 확인하세요." },
            { "Blunder", "둘 만한 수를 두세 개 떠올린 뒤, 각각 상대가 어떻게 받을지 비교해 보세요." },
            { "MissedCaptures", "체스에서도 공짜로 잡을 수 있는 기물은 가장 먼저 찾아야 합니다. 턴을 넘기기 전에 상대 기물을 한 번 훑어보세요." },
            { "PieceLosses", "기물을 앞으로 보낼 때는 다른 기물이 뒤를 받쳐 주는지 확인하세요. 서로 지키는 기물은 쉽게 잡히지 않습니다." },
            { "NeverMoved", "체스에서는 모든 기물을 전투에 참여시키는 것이 중요합니다. 초반에 가벼운 기물부터 꺼내 보세요." },
            { "SlowDecision", "행마법이 헷갈리는 기물은 [설명]을 다시 보고, 갈 수 있는 칸을 머릿속으로 그려 보는 연습을 해 보세요." },
        };

        static readonly Dictionary<string, string> Goals = new()    // 항목 종류 -> 다음 런 과제
        {
            { "MissedMate", "최종전에서 매 수마다 체크가 가능한지 먼저 살펴보기" },
            { "AllowsMate", "수를 두기 전에 상대의 체크메이트 수가 있는지 확인하기" },
            { "HangingPiece", "기물을 옮기기 전에 그 칸이 안전한지 확인하기" },
            { "Blunder", "수를 두기 전에 후보 수를 두 개 이상 비교하기" },
            { "MissedCaptures", "턴을 넘기기 전에 잡을 수 있는 적이 있는지 확인하기" },
            { "PieceLosses", "기물을 앞으로 보낼 때 뒤를 받칠 기물을 함께 두기" },
            { "NeverMoved", "소환한 기물을 전부 한 번 이상 움직여 보기" },
            { "SlowDecision", "어려웠던 기물을 일부러 더 많이 써 보기" },
        };

        public static string Label(string kind)
        {
            return Labels.TryGetValue(kind, out var label) ? label : "주요 장면";
        }

        public static string BuildOffline(RunAnalysis analysis)
        {
            var problems = analysis.findings.Where(f => !f.positive).ToList();
            var strengths = analysis.findings.Where(f => f.positive).ToList();
            var sb = new StringBuilder();

            Heading(sb, "총평");
            sb.Append(analysis.result == "Victory"
                ? "최종전까지 돌파해 승리했습니다. "
                : analysis.stagesCleared > 0 ? analysis.stagesCleared + "개의 스테이지를 돌파했습니다. " : "아직 돌파한 스테이지가 없습니다. ");
            sb.Append("이번 런에서 상대를 ").Append(analysis.totalCaptures).Append("번 잡고, 기물을 ")
              .Append(analysis.totalLost).Append("개 잃었습니다.\n\n");

            if (strengths.Count > 0)
            {
                Heading(sb, "잘한 점");
                foreach (var f in strengths)
                    sb.Append("· ").Append(Polite(f.fact)).Append("\n");
                sb.Append("\n");
            }

            if (problems.Count > 0)
            {
                Heading(sb, "돌아볼 점");
                foreach (var f in problems)
                {
                    sb.Append("· ").Append(Polite(f.fact)).Append("\n");
                    if (Tips.TryGetValue(f.kind, out var tip))
                        sb.Append("<color=#74A0D8>").Append(tip).Append("</color>\n");
                }
                sb.Append("\n");
            }

            Heading(sb, "다음 런 과제");
            var goal = problems.Select(f => Goals.TryGetValue(f.kind, out var g) ? g : null).FirstOrDefault(g => g != null);
            sb.Append(goal ?? (analysis.result == "Victory" ? "이번에 쓰지 않은 기물로 다시 승리에 도전해 보기" : "최종전까지 가서 체크메이트에 도전해 보기"));
            return sb.ToString();
        }

        public static string Polite(string fact)    // 분석 문장의 끝맺음을 존댓말로 바꾸는 함수
        {
            return Regex.Replace(fact, @"(었|였|았|렸|했|뒀)다\.|이다\.|보인다\.", match =>
            {
                if (match.Value == "이다.")
                    return "입니다.";
                if (match.Value == "보인다.")
                    return "보입니다.";
                return match.Groups[1].Value + "습니다.";
            });
        }

        static void Heading(StringBuilder sb, string title)
        {
            sb.Append("<b><color=#F2BC51>").Append(title).Append("</color></b>\n");
        }
    }
}
