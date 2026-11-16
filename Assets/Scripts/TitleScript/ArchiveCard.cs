using System;
using System.Linq;
using Promotion.Analysis;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Title
{
    public class ArchiveCard : MonoBehaviour
    // 기록소 목록의 카드 한 장 (날짜·승패·요약·주요 실수 + 보관 ★ + 삭제)
    // -> 모양은 ArchiveCard 프리팹이 원본이고, 여기서는 내용만 채움
    {
        static readonly Color PinOn = new Color(0.949f, 0.737f, 0.318f, 1f);
        static readonly Color PinOff = new Color(0.278f, 0.345f, 0.459f, 1f);

        [SerializeField] UiButton _open;    // 카드 전체 -> 누르면 리포트 열기
        [SerializeField] Image _resultChip;
        [SerializeField] TextMeshProUGUI _result;
        [SerializeField] TextMeshProUGUI _date;
        [SerializeField] TextMeshProUGUI _info;
        [SerializeField] TextMeshProUGUI _mistake;
        [SerializeField] Button _pin;
        [SerializeField] Image _pinIcon;
        [SerializeField] Button _delete;
        [SerializeField] TextMeshProUGUI _deleteLabel;

        Action _onOpen;
        Action _onPin;
        Action _onDelete;
        bool _confirmingDelete;     // 삭제는 두 번 눌러야 실행 (실수로 지우지 않게)

        void Awake()
        {
            _open.Clicked += () => _onOpen?.Invoke();
            _pin.onClick.AddListener(() => _onPin?.Invoke());
            _delete.onClick.AddListener(OnDeleteClicked);
        }

        public void Fill(ArchiveEntry entry, Action onOpen, Action onPin, Action onDelete)
        {
            _onOpen = onOpen;
            _onPin = onPin;
            _onDelete = onDelete;

            var a = entry.analysis;
            bool won = a.result == "Victory";
            _result.text = won ? "승리" : "패배";
            _result.color = won ? UiTheme.PieceAccent : UiTheme.Deny;
            _resultChip.color = UiTheme.Well(won ? UiTheme.Confirm : UiTheme.Deny);

            _date.text = entry.savedAt.Length >= 16 ? entry.savedAt.Substring(0, 16).Replace('-', '.') : entry.savedAt;

            string progress = won ? "최종전 승리" : a.reachedDuel ? "최종전 도달" : a.stagesCleared + "스테이지 돌파";
            string duel = a.duelMoves > 0 ? " · 최종전 깔끔한 수 " + a.duelCleanMoves + "/" + a.duelMoves : "";
            string ai = entry.aiStatus == "ok" ? " · AI 리포트" : "";
            _info.text = progress + " · 잡기 " + a.totalCaptures + " · 잃음 " + a.totalLost + duel + ai;

            var top = a.findings.FirstOrDefault(f => !f.positive);
            _mistake.text = top != null ? "주요 실수  " + RunReportWriter.Label(top.kind) : "큰 실수 없음";

            _pinIcon.color = entry.pinned ? PinOn : PinOff;
            _confirmingDelete = false;
            _deleteLabel.text = "삭제";
        }

        void OnDeleteClicked()
        {
            if (!_confirmingDelete)
            {
                _confirmingDelete = true;
                _deleteLabel.text = "정말?";
                return;
            }
            _onDelete?.Invoke();
        }
    }
}
