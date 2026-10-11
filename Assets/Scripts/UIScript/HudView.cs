using System.Collections.Generic;
using Promotion.Core;
using Promotion.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// 스테이지 HUD다. 배치는 HUD 프리팹에 있고, 이 클래스는 내용을 채우고 버튼을 연결한다.
    /// 덱 카드와 승격 선택지는 각자의 프리팹을 복제해 만든다.
    /// 한글은 TMP Settings에 등록된 Pretendard 대체 폰트로 표시되므로, 폰트 설정을 건드리지
    /// 않고도 라벨 문구를 바꿀 수 있다.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        public static readonly Color Panel = new Color(0.07f, 0.07f, 0.10f, 0.85f);
        public static readonly Color Ink = new Color(0.94f, 0.93f, 0.90f);
        public static readonly Color ButtonIdle = new Color(0.20f, 0.20f, 0.26f, 0.95f);
        static readonly Color ButtonOff = new Color(0.14f, 0.14f, 0.17f, 0.80f);
        public static readonly Color CardIdle = new Color(0.18f, 0.18f, 0.24f, 0.95f);
        static readonly Color CardPicked = new Color(0.95f, 0.78f, 0.25f, 0.95f);
        static readonly Color CardLocked = new Color(0.13f, 0.13f, 0.16f, 0.75f);
        static readonly Color ApLockedPanel = new Color(0.32f, 0.10f, 0.10f, 0.90f);
        static readonly Color CheckInk = new Color(1f, 0.45f, 0.45f);

        const string TitleScene = "Title";

        StageDirector _director;
        TurnManager _turn;
        DeployController _deploy;

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] TextMeshProUGUI _status;
        [SerializeField] TextMeshProUGUI _ap;
        [SerializeField] Image _apPanel;
        [SerializeField] GameObject _bannerPanel;
        [SerializeField] TextMeshProUGUI _banner;
        [SerializeField] Button _action;
        [SerializeField] TextMeshProUGUI _actionLabel;
        [SerializeField] Button _restart;
        [SerializeField] Button _toTitle;
        [SerializeField] RectTransform _deckBar;
        [SerializeField] RectTransform _promotionPanel;
        [SerializeField] RectTransform _promotionRow;
        [SerializeField] Button _deckCardPrefab;
        [SerializeField] Button _choiceButtonPrefab;

        readonly List<Button> _deckButtons = new();
        // 같은 카드는 버튼 하나를 공유한다. 각 항목은 그 버튼에 묶인 덱 인덱스 목록이다.
        readonly List<List<int>> _cardGroups = new();

        void Start()
        {
            _director = FindAnyObjectByType<StageDirector>();
            if (_director == null)
            {
                Debug.LogWarning("[HudView] No StageDirector in the scene - HUD disabled.");
                enabled = false;
                return;
            }

            _turn = _director.GetComponent<TurnManager>();
            _deploy = _director.GetComponent<DeployController>();

            EnsureEventSystem();

            _action.onClick.AddListener(OnActionPressed);
            _restart.onClick.AddListener(ReloadStage);
            _toTitle.onClick.AddListener(ReturnToTitle);

            _bannerPanel.SetActive(false);
            _restart.gameObject.SetActive(false);
            _toTitle.gameObject.SetActive(false);
            _promotionPanel.gameObject.SetActive(false);

            _turn.PhaseChanged += OnPhaseChanged;
            _turn.ApChanged += OnApChanged;
            _turn.TurnChanged += OnTurnChanged;
            _director.StateChanged += Refresh;
            _director.CheckDeclared += OnCheckDeclared;
            _director.PromotionRequested += ShowPromotionChoice;
            _deploy.DeckChanged += RebuildDeck;

            RebuildDeck();
            Refresh();
        }

        void OnDestroy()
        {
            if (_turn != null)
            {
                _turn.PhaseChanged -= OnPhaseChanged;
                _turn.ApChanged -= OnApChanged;
                _turn.TurnChanged -= OnTurnChanged;
            }

            if (_director != null)
            {
                _director.StateChanged -= Refresh;
                _director.CheckDeclared -= OnCheckDeclared;
                _director.PromotionRequested -= ShowPromotionChoice;
            }

            if (_deploy != null)
                _deploy.DeckChanged -= RebuildDeck;
        }

        void OnPhaseChanged(TurnPhase phase) => Refresh();
        void OnApChanged(int current, int max) => Refresh();
        void OnTurnChanged(int turn) => Refresh();
        void OnCheckDeclared(Team side) => Refresh();

        // ---------- 구성 ----------

        static void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            // 이 프로젝트는 Input System 패키지를 쓰므로 구식 모듈로는 UI를 조작할 수 없다.
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacy = existing.GetComponent<BaseInputModule>();
                if (legacy != null)
                    Destroy(legacy);

                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static void ReloadStage() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        /// <summary>최종 결투에서 이기면 런이 끝난다.</summary>
        static void ReturnToTitle()
        {
            RunState.Instance.ResetRun();
            SceneManager.LoadScene(TitleScene);
        }

        /// <summary>승격 선택지마다 버튼을 하나씩 만든다. 결투는 그중 하나가 눌릴 때까지 기다린다.</summary>
        void ShowPromotionChoice()
        {
            for (int i = _promotionRow.childCount - 1; i >= 0; i--)
                Destroy(_promotionRow.GetChild(i).gameObject);

            var choices = _director.PromotionChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                var def = choices[i];
                if (def == null)
                    continue;

                var button = Instantiate(_choiceButtonPrefab, _promotionRow);
                button.name = "Promote_" + i;
                button.GetComponentInChildren<TextMeshProUGUI>().text = def.itemName;
                button.onClick.AddListener(() =>
                {
                    _promotionPanel.gameObject.SetActive(false);
                    _director.ChoosePromotion(def);
                });
            }

            _promotionPanel.gameObject.SetActive(true);
        }

        // ---------- 반응 ----------

        void OnActionPressed()
        {
            if (_turn.Phase == TurnPhase.Deploy)
                _director.ConfirmDeploy();
            else if (_turn.Phase == TurnPhase.PlayerTurn)
                _director.RequestEndTurn();
        }

        void Refresh()
        {
            if (_turn == null)
                return;

            if (_director.IsChessDuel)
            {
                RefreshDuel();
                RefreshDeckColors();
                return;
            }

            _status.color = Ink;
            _ap.fontSize = 34f;
            _apPanel.color = Panel;
            _toTitle.gameObject.SetActive(false);

            _status.text = _turn.Phase == TurnPhase.Deploy
                ? (_director.KingPlaced
                    ? "배치 완료 - 스테이지 시작을 누르세요"
                    : "킹을 배치해야 시작합니다 - 오른쪽 덱에서 선택하세요")
                : "턴 " + _turn.Turn
                  + "   |   웨이브 " + _director.WavesSent + "/" + _director.TotalWaves
                  + "   |   적 " + _director.EnemiesOnBoard;

            _ap.text = _turn.Phase == TurnPhase.Deploy
                ? "배치: 무료"
                : "AP  " + _turn.Ap + " / " + _turn.MaxAp;

            bool over = _turn.IsOver;
            _action.gameObject.SetActive(!over);

            if (!over)
            {
                bool interactable = _turn.Phase == TurnPhase.PlayerTurn
                                    || (_turn.Phase == TurnPhase.Deploy && _director.KingPlaced);
                _action.interactable = interactable;
                _action.image.color = interactable ? ButtonIdle : ButtonOff;

                if (_turn.Phase == TurnPhase.Deploy)
                    _actionLabel.text = _director.KingPlaced ? "스테이지 시작" : "킹을 먼저 배치";
                else if (_turn.Phase == TurnPhase.PlayerTurn)
                    _actionLabel.text = "턴 종료";
                else
                    _actionLabel.text = "적 턴...";
            }

            _bannerPanel.SetActive(over);
            // 승리하면 다음으로 넘어간다(상점, 또는 튜토리얼의 마무리 대사). 재시도는 패배했을 때만 제공한다.
            _restart.gameObject.SetActive(_turn.Phase == TurnPhase.Defeat);

            if (over)
            {
                bool won = _turn.Phase == TurnPhase.Victory;
                _banner.text = won ? "승리" : "패배";
                _banner.color = won ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.45f, 0.45f);
            }

            RefreshDeckColors();
        }

        /// <summary>
        /// 최종 결투용 갱신이다. 한 턴에 한 수만 두므로 AP는 꺼진 것으로 표시하고, 턴은 저절로
        /// 넘어가며, 상태 줄에는 체크 여부와 AI 상태를 보여준다.
        /// </summary>
        void RefreshDuel()
        {
            var phase = _turn.Phase;
            bool over = _turn.IsOver;

            _apPanel.color = ApLockedPanel;
            _ap.fontSize = 24f;
            _ap.text = "AP 사용 불가\n<size=80%>체스 결투 - 한 턴에 한 수</size>";

            _status.color = _director.InCheck == Team.Player && !over ? CheckInk : Ink;
            if (phase == TurnPhase.Deploy)
            {
                _status.text = _director.KingPlaced
                    ? "킹과 기물(폰 최대 8)을 배치한 뒤 결투 시작을 누르세요"
                    : "최종전 - 킹을 배치해야 시작합니다";
            }
            else
            {
                string line = "최종전   |   " + (_director.DuelMovesPlayed + (phase == TurnPhase.PlayerTurn ? 1 : 0))
                              + "수 / " + _director.stage.moveLimit;
                if (over)
                    _status.text = line;
                else if (_director.AwaitingPromotion)
                    _status.text = line + "   |   승격할 기물을 고르세요";
                else if (_director.AIThinking)
                    _status.text = line + "   |   흑이 생각하는 중...";
                else if (_director.InCheck == Team.Player)
                    _status.text = line + "   |   체크! 킹을 지키세요";
                else if (_director.InCheck == Team.Enemy)
                    _status.text = line + "   |   체크!";
                else
                    _status.text = line + (phase == TurnPhase.PlayerTurn ? "   |   백의 차례" : "   |   흑의 차례");
            }

            // 수를 두는 즉시 턴이 넘어가므로, 버튼은 결투를 시작하는 용도로만 쓴다.
            bool deploying = phase == TurnPhase.Deploy;
            _action.gameObject.SetActive(deploying);
            if (deploying)
            {
                _action.interactable = _director.KingPlaced;
                _action.image.color = _director.KingPlaced ? ButtonIdle : ButtonOff;
                _actionLabel.text = _director.KingPlaced ? "결투 시작" : "킹을 먼저 배치";
            }

            bool won = phase == TurnPhase.Victory;
            _bannerPanel.SetActive(over);
            _restart.gameObject.SetActive(phase == TurnPhase.Defeat);
            _toTitle.gameObject.SetActive(won);

            if (over)
            {
                string reason = _director.OutcomeReason;
                if (!won && reason == "체크메이트")
                    reason = "체크메이트 당함";

                _banner.text = won
                    ? "체크메이트!\n<size=50%>런 클리어</size>"
                    : "패배\n<size=50%>" + reason + "</size>";
                _banner.color = won ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.45f, 0.45f);
            }
        }

        void RebuildDeck()
        {
            foreach (var button in _deckButtons)
                if (button != null)
                    Destroy(button.gameObject);

            _deckButtons.Clear();
            _cardGroups.Clear();

            // 무료 폰 8개를 따로 놓으면 덱 줄이 넘치므로, 같은 카드는 하나로 묶는다.
            var deck = _deploy.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                var group = _cardGroups.Find(g => deck[g[0]] == deck[i] && _deploy.IsUnlimited(g[0]) == _deploy.IsUnlimited(i));
                if (group != null)
                    group.Add(i);
                else
                    _cardGroups.Add(new List<int> { i });
            }

            for (int g = 0; g < _cardGroups.Count; g++)
            {
                int index = _cardGroups[g][0];
                int count = _cardGroups[g].Count;
                var def = deck[index];

                var button = Instantiate(_deckCardPrefab, _deckBar);
                button.name = "Card_" + g;
                button.onClick.AddListener(() => _deploy.Select(index));
                var label = button.GetComponentInChildren<TextMeshProUGUI>();

                string name = def.itemName + (count > 1 ? " x" + count : "");
                label.text = _director.IsChessDuel
                    ? name + "\n배치 전용"
                    : name + "\n" + def.deployApCost + " AP" + (_deploy.IsUnlimited(index) ? "  · 상시" : "");
                _deckButtons.Add(button);
            }

            // 시작 버튼은 킹을 배치해야 풀리는데, 그 사실은 덱 변경으로 전달된다.
            // 그래서 카드 색만이 아니라 HUD 전체를 갱신한다.
            Refresh();
        }

        void RefreshDeckColors()
        {
            var deck = _deploy.Deck;

            for (int i = 0; i < _deckButtons.Count; i++)
            {
                if (_deckButtons[i] == null || i >= _cardGroups.Count)
                    continue;

                // 배치 단계에서는 킹만 놓을 수 있으므로, 나머지 덱은 눌러도 조용히 아무 일이
                // 없게 두지 않고 회색으로 표시한다.
                var group = _cardGroups[i];
                bool playable = group[0] < deck.Count && _deploy.IsDeployableNow(deck[group[0]]);
                _deckButtons[i].interactable = playable;

                _deckButtons[i].image.color = !playable
                    ? CardLocked
                    : (group.Contains(_deploy.SelectedIndex) ? CardPicked : CardIdle);
            }
        }
    }
}
