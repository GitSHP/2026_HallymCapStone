using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Promotion.Data;
using Promotion.Feel;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Analysis
{
    public class RunReportView : MonoBehaviour
    // 런이 끝난 뒤 복기 리포트를 보여 주는 화면 -> 배치와 모양은 프리팹이 원본이고, 여기서는 내용만 채움
    {
        const float BoardTile = 48f;    // 리포트 체스판 한 칸의 크기 (원본 16픽셀의 3배)
        const float TypeCharsPerSec = 90f;  // 글자가 나타나는 속도

        [SerializeField] RectTransform _panel;
        [SerializeField] TextMeshProUGUI _result;
        [SerializeField] Image _resultChip;
        [SerializeField] TextMeshProUGUI _status;
        [SerializeField] TextMeshProUGUI _statStages;
        [SerializeField] TextMeshProUGUI _statCaptures;
        [SerializeField] TextMeshProUGUI _statLost;
        [SerializeField] TextMeshProUGUI _statDuel;
        [SerializeField] TextMeshProUGUI _body;
        [SerializeField] ScrollRect _scroll;

        [SerializeField] TextMeshProUGUI _sceneTitle;
        [SerializeField] TextMeshProUGUI _sceneNote;
        [SerializeField] UiButton _prev;
        [SerializeField] UiButton _next;
        [SerializeField] RectTransform _boardGrid;  // 8x8 칸의 중심에 놓인 기준점
        [SerializeField] GameObject _board;
        [SerializeField] GameObject _legend;
        [SerializeField] GameObject _noScene;
        [SerializeField] Image[] _pieceImages;      // 체스판 위에 쓸 기물 그림 (미리 64개)
        [SerializeField] RectTransform _playedArrow;
        [SerializeField] RectTransform _betterArrow;
        [SerializeField] PieceDefinition[] _pieces; // 기물 그림을 찾을 목록

        [SerializeField] UiButton _close;
        [SerializeField] GameObject _notice;
        [SerializeField] ReportLoadingView _loading;    // 글을 기다리는 동안 보여 주는 로봇    // 플레이 기록을 서버로 보낸다는 안내 (서버를 쓸 때만 켬)

        readonly List<Finding> _scenes = new();     // 체스판에 재현할 수 있는 항목
        Dictionary<string, PieceDefinition> _catalog;
        int _sceneIndex;
        Action _onClose;
        Coroutine _typing;
        ReportStreamFilter _filter;
        bool _streaming;        // 서버에서 글을 받고 있는지
        bool _textChanged;      // 글이 늘어나 글자 수를 다시 세야 하는지

        public bool UseServer { get; set; } = true;     // false면 서버를 부르지 않고 오프라인 요약만 사용 (테스트 씬용)
        public bool TestRun { get; set; }
        public bool SaveToArchive { get; set; } = true;    // true면 이 리포트를 기록소에 저장 (테스트 씬은 false)

        ArchiveEntry _entry;    // 기록소에 저장한 이번 리포트 -> AI 글이 다 써지면 글을 덧붙여 다시 저장               // true면 서버가 플레이 데이터로 저장하지 않음

        void Awake()
        {
            _prev.Clicked += ShowPrev;
            _next.Clicked += ShowNext;
            _close.Clicked += OnClose;
            _catalog = new Dictionary<string, PieceDefinition>();
            foreach (var def in _pieces)
            {
                if (def != null)
                    _catalog[def.name] = def;
            }
        }

        public void Show(RunLog log, Action onClose)    // 런 기록을 분석해 화면에 보여 주는 함수
        {
            Open(onClose);
            StartCoroutine(RunAnalyzer.Run(log, OnAnalyzed));
        }

        public void ShowSaved(RunAnalysis analysis, Action onClose) // 저장해 둔 분석 결과를 바로 보여 주는 함수 (시연용)
        {
            Open(onClose);
            ShowAnalysis(analysis);
        }

        void Open(Action onClose)
        {
            _onClose = onClose;
            gameObject.SetActive(true);
            UiFx.BackdropIn(UiBuilder.Group(gameObject));
            UiFx.PanelIn(_panel, UiBuilder.Group(_panel.gameObject));

            _status.text = "분석 중";
            _result.text = "";
            _body.text = "";
            _statStages.text = "-";
            _statCaptures.text = "-";
            _statLost.text = "-";
            _statDuel.text = "-";
            _notice.SetActive(CanUseServer);
            _loading.Show("플레이 기록을 살펴보고 있어요");
            ShowNoScene();
        }

        bool CanUseServer
        {
            get { return UseServer && ReportServerConfig.IsReady; }
        }

        void OnAnalyzed(RunAnalysis analysis)   // 분석이 끝나면 기록소에 먼저 저장하고 화면을 채움 -> 리포트를 끝까지 보지 않고 닫아도 기록이 남음
        {
            if (SaveToArchive)
            {
                _entry = RunArchive.Create(analysis);
                RunArchive.Save(_entry);
            }
            ShowAnalysis(analysis);
        }

        public void ShowArchived(ArchiveEntry entry, Action onClose)
        // 기록소에 저장된 리포트를 다시 보여 주는 함수 -> AI를 다시 부르지 않고 저장된 글을 그대로 표시
        {
            Open(onClose);
            _close.SetCaption("닫기");
            _notice.SetActive(false);
            _loading.Hide();
            FillAnalysis(entry.analysis);

            bool hasAi = !string.IsNullOrEmpty(entry.report);
            _status.text = hasAi ? "저장된 AI 리포트" : "오프라인 요약";
            _body.text = hasAi ? entry.report : RunReportWriter.BuildOffline(entry.analysis);
            _body.maxVisibleCharacters = int.MaxValue;  // 다시 보는 것이므로 글을 한 번에 모두 보여 줌
            _scroll.verticalNormalizedPosition = 1f;
        }

        void ShowAnalysis(RunAnalysis analysis)
        {
            FillAnalysis(analysis);
            if (CanUseServer)
                StartCoroutine(WriteWithServer(analysis));
            else
                ShowOffline(analysis);
        }

        void FillAnalysis(RunAnalysis analysis)    // 결과·통계·실수 장면을 채우는 함수
        {
            bool won = analysis.result == "Victory";
            _result.text = won ? "승리" : "패배";
            _resultChip.color = UiTheme.Well(won ? UiTheme.Confirm : UiTheme.Deny);
            _result.color = won ? UiTheme.PieceAccent : UiTheme.Deny;

            _statStages.text = analysis.stagesCleared.ToString();
            _statCaptures.text = analysis.totalCaptures.ToString();
            _statLost.text = analysis.totalLost.ToString();
            _statDuel.text = analysis.duelMoves > 0 ? analysis.duelCleanMoves + " / " + analysis.duelMoves : "-";

            _scenes.Clear();
            _scenes.AddRange(analysis.findings.Where(f => !f.positive && !string.IsNullOrEmpty(f.board)));
            _sceneIndex = 0;
            if (_scenes.Count > 0)
                ShowScene();
        }

        void ShowOffline(RunAnalysis analysis)
        {
            _loading.Hide();
            _status.text = "오프라인 요약";
            Type(RunReportWriter.BuildOffline(analysis));
        }

        IEnumerator WriteWithServer(RunAnalysis analysis)   // 서버가 쓰는 글을 받는 대로 화면에 표시하는 코루틴 -> 실패하면 오프라인 요약으로 바꿈
        {
            _status.text = "AI 작성 중";
            _loading.Show("AI가 리포트를 쓰고 있어요");
            _body.text = "";
            _streaming = true;
            _filter = new ReportStreamFilter(analysis.findings.Select(f => f.id), CanDraw, AppendText, FocusFinding);
            if (_typing != null)
                StopCoroutine(_typing);
            _typing = StartCoroutine(RevealRoutine());

            bool ok = false;
            yield return ReportClient.Send(analysis, TestRun, chunk => _filter.Push(chunk), result => ok = result);
            _filter.Finish();
            _streaming = false;

            if (ok && _filter.Completed && _filter.HasOutput)
            {
                _status.text = "AI 리포트";
                if (_entry != null)     // 기록소의 이번 기록에 AI 글을 덧붙여 저장
                {
                    _entry.report = _body.text;
                    _entry.aiStatus = "ok";
                    RunArchive.Save(_entry);
                }
            }
            else
                ShowOffline(analysis);
        }

        void FocusFinding(string id)    // AI가 어떤 항목을 설명하기 시작하면 왼쪽 체스판을 그 장면으로 넘기는 함수
        {
            int index = _scenes.FindIndex(f => f.id == id);
            if (index < 0)  // 체스판으로 보여 줄 수 없는 항목(잘한 점 등)이면 그대로 둠
                return;
            if (index != _sceneIndex || !_board.activeSelf)
            {
                _sceneIndex = index;
                ShowScene();
            }
            Fx.ReportSceneFocus(_board.transform);
        }

        void AppendText(string text)
        {
            _loading.Hide();    // 첫 글자가 오면 로봇을 치우고 글을 보여 줌
            _body.text += text;
            _textChanged = true;
        }

        bool CanDraw(char c)    // 폰트로 그릴 수 있는 글자인지 -> 없는 글자는 빈 칸으로 보이므로 지움
        {
            if (char.IsWhiteSpace(c))
                return true;
            if (_body.font.HasCharacter(c, true))
                return true;
            foreach (var fallback in TMP_Settings.fallbackFontAssets)
            {
                if (fallback != null && fallback.HasCharacter(c, true))
                    return true;
            }
            return false;
        }

        IEnumerator RevealRoutine() // 받은 글을 일정한 속도로 드러내는 코루틴
        {
            float shown = 0f;
            int total = 0;
            _body.maxVisibleCharacters = 0;
            while (_streaming || shown < total)
            {
                if (_textChanged)
                {
                    _textChanged = false;
                    _body.ForceMeshUpdate();
                    total = _body.textInfo.characterCount;
                }
                if (shown < total)
                    shown += TypeCharsPerSec * Time.unscaledDeltaTime;
                _body.maxVisibleCharacters = Mathf.FloorToInt(shown);
                yield return null;
            }
            _body.maxVisibleCharacters = int.MaxValue;
            _typing = null;
        }

        void Type(string text)  // 글자가 차례로 나타나게 하는 함수
        {
            if (_typing != null)
                StopCoroutine(_typing);
            _body.text = text;
            _scroll.verticalNormalizedPosition = 1f;
            _typing = StartCoroutine(TypeRoutine());
        }

        IEnumerator TypeRoutine()
        {
            _body.ForceMeshUpdate();
            int total = _body.textInfo.characterCount;
            float shown = 0f;
            while (shown < total)
            {
                _body.maxVisibleCharacters = Mathf.FloorToInt(shown);
                shown += TypeCharsPerSec * Time.unscaledDeltaTime;
                yield return null;
            }
            _body.maxVisibleCharacters = int.MaxValue;
            _typing = null;
        }

        // ---------- 실수 장면 ----------

        void ShowNoScene()
        {
            _board.SetActive(false);
            _legend.SetActive(false);
            _noScene.SetActive(true);
            _sceneTitle.text = "주요 장면";
            _sceneNote.text = "";
            _prev.gameObject.SetActive(false);
            _next.gameObject.SetActive(false);
        }

        void ShowPrev()
        {
            if (_scenes.Count == 0)
                return;
            _sceneIndex = (_sceneIndex + _scenes.Count - 1) % _scenes.Count;
            ShowScene();
        }

        void ShowNext()
        {
            if (_scenes.Count == 0)
                return;
            _sceneIndex = (_sceneIndex + 1) % _scenes.Count;
            ShowScene();
        }

        void ShowScene()
        {
            var finding = _scenes[_sceneIndex];
            _board.SetActive(true);
            _noScene.SetActive(false);
            _prev.gameObject.SetActive(_scenes.Count > 1);
            _next.gameObject.SetActive(_scenes.Count > 1);
            _sceneTitle.text = "장면 " + (_sceneIndex + 1) + " / " + _scenes.Count + "  " + RunReportWriter.Label(finding.kind);
            _sceneNote.text = RunReportWriter.Polite(finding.fact);

            DrawBoard(finding.board);
            bool hasMove = finding.hasMove;
            _legend.SetActive(hasMove);
            DrawArrow(_playedArrow, hasMove ? finding.played : "");
            DrawArrow(_betterArrow, hasMove ? finding.better : "");
        }

        static Vector2 TilePos(int x, int y)
        {
            return new Vector2((x - 3.5f) * BoardTile, (y - 3.5f) * BoardTile);
        }

        void DrawBoard(string snapshot) // "이름.진영.x.y.움직임여부|..." 문자열을 체스판 그림으로 그리는 함수
        {
            int used = 0;
            foreach (var entry in snapshot.Split('|'))
            {
                var parts = entry.Split('.');
                if (parts.Length < 4 || used >= _pieceImages.Length || !_catalog.TryGetValue(parts[0], out var def))
                    continue;

                var art = parts[1] == "E" && def.enemySprite != null ? def.enemySprite : def.itemImage;
                if (art == null)
                    continue;

                var image = _pieceImages[used++];
                image.gameObject.SetActive(true);
                image.sprite = art;
                var rect = image.rectTransform;
                rect.pivot = new Vector2(art.pivot.x / art.rect.width, art.pivot.y / art.rect.height);
                rect.sizeDelta = art.rect.size / art.pixelsPerUnit * BoardTile;
                rect.anchoredPosition = TilePos(int.Parse(parts[2]), int.Parse(parts[3]));
            }
            for (int i = used; i < _pieceImages.Length; i++)
                _pieceImages[i].gameObject.SetActive(false);
        }

        static void DrawArrow(RectTransform arrow, string move)    // "d1-d7" 형식의 수를 화살표로 그리는 함수
        {
            if (string.IsNullOrEmpty(move) || move.Length < 5)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            var from = TilePos(move[0] - 'a', move[1] - '1');
            var to = TilePos(move[3] - 'a', move[4] - '1');
            var delta = to - from;
            arrow.gameObject.SetActive(true);
            arrow.anchoredPosition = from;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            arrow.sizeDelta = new Vector2(delta.magnitude, arrow.sizeDelta.y);
        }

        void OnClose()
        {
            var callback = _onClose;
            _onClose = null;
            callback?.Invoke();
        }
    }
}
