using System;
using System.Collections.Generic;
using System.Linq;
using Promotion.Analysis;
using Promotion.UI;
using TMPro;
using UnityEngine;

namespace Promotion.Title
{
    public class ArchiveView : MonoBehaviour
    // 타이틀의 기록소 창 -> 저장된 복기 리포트 목록과 요약(성장 추이)을 보여 주고, 카드를 누르면 리포트를 다시 엶
    // -> 모양은 TitleScreen 프리팹 안의 Archive가 원본이고, 여기서는 내용만 채움
    {
        const int TrendWindow = 5;  // 성장 추이: 최근 5런과 그 이전 5런을 비교

        [SerializeField] RectTransform _panel;
        [SerializeField] TextMeshProUGUI _runs;
        [SerializeField] TextMeshProUGUI _winRate;
        [SerializeField] TextMeshProUGUI _best;
        [SerializeField] TextMeshProUGUI _mistake;
        [SerializeField] TextMeshProUGUI _trend;
        [SerializeField] RectTransform _list;       // 카드가 들어가는 스크롤 내용물
        [SerializeField] ArchiveCard _cardPrefab;
        [SerializeField] GameObject _empty;         // 기록이 없을 때 안내
        [SerializeField] UiButton _close;
        [SerializeField] RunReportView _reportPrefab;

        RunReportView _report;  // 지금 열려 있는 리포트 (없으면 null)

        public event Action Closed;

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        void Awake()
        {
            _close.Clicked += Close;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            UiFx.BackdropIn(UiBuilder.Group(gameObject));
            UiFx.PanelIn(_panel, UiBuilder.Group(_panel.gameObject));
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            CloseReport();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        public void Back()  // ESC: 리포트가 열려 있으면 리포트만 닫고, 아니면 기록소를 닫음
        {
            if (_report != null)
                CloseReport();
            else
                Close();
        }

        void Refresh()
        {
            var entries = RunArchive.LoadAll();

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);
            foreach (var entry in entries)
            {
                var card = Instantiate(_cardPrefab, _list);
                var e = entry;
                card.Fill(e, () => OpenReport(e), () => TogglePin(e), () => Delete(e));
            }
            _empty.SetActive(entries.Count == 0);

            FillSummary(entries);
        }

        void FillSummary(List<ArchiveEntry> entries)    // 위쪽 요약: 총 런, 승률, 최고 기록, 가장 잦은 실수와 최근 추세
        {
            int total = entries.Count;
            int wins = entries.Count(e => e.analysis.result == "Victory");
            _runs.text = total.ToString();
            _winRate.text = total > 0 ? Mathf.RoundToInt(wins * 100f / total) + "%" : "-";

            if (wins > 0)
                _best.text = "최종전 승리";
            else if (entries.Any(e => e.analysis.reachedDuel))
                _best.text = "최종전 도달";
            else
                _best.text = total > 0 ? entries.Max(e => e.analysis.stagesCleared) + "스테이지" : "-";

            var counts = new Dictionary<string, int>();
            foreach (var e in entries)
            {
                foreach (var kind in e.analysis.findings.Where(f => !f.positive).Select(f => f.kind).Distinct())
                    counts[kind] = counts.TryGetValue(kind, out var c) ? c + 1 : 1;
            }
            if (counts.Count == 0)
            {
                _mistake.text = "-";
                _trend.text = total > 0 ? "큰 실수가 기록되지 않았어요" : "";
                return;
            }

            string top = counts.OrderByDescending(p => p.Value).First().Key;
            _mistake.text = RunReportWriter.Label(top);
            _trend.text = Trend(entries, top);
        }

        static string Trend(List<ArchiveEntry> entries, string kind)
        // 가장 잦은 실수가 최근 5런에서 그 이전 5런보다 줄었는지 늘었는지 (entries는 최신순)
        {
            int Count(IEnumerable<ArchiveEntry> part)
            {
                return part.Count(e => e.analysis.findings.Any(f => !f.positive && f.kind == kind));
            }

            var recent = entries.Take(TrendWindow).ToList();
            var before = entries.Skip(TrendWindow).Take(TrendWindow).ToList();
            int now = Count(recent);
            if (before.Count == 0)
                return "최근 " + recent.Count + "런 중 " + now + "번";

            int prev = Count(before);
            string change = now < prev ? "줄었어요" : now > prev ? "늘었어요" : "그대로예요";
            return "최근 " + recent.Count + "런 " + now + "번 · 이전 " + before.Count + "런 " + prev + "번 · " + change;
        }

        void OpenReport(ArchiveEntry entry)
        {
            if (_report != null)
                return;
            _report = Instantiate(_reportPrefab);
            _report.ShowArchived(entry, CloseReport);
        }

        void CloseReport()
        {
            if (_report == null)
                return;
            Destroy(_report.gameObject);
            _report = null;
        }

        void TogglePin(ArchiveEntry entry)
        {
            entry.pinned = !entry.pinned;
            RunArchive.Save(entry);
            Refresh();
        }

        void Delete(ArchiveEntry entry)
        {
            RunArchive.Delete(entry.id);
            Refresh();
        }
    }
}
