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
    /// The stage HUD. The layout is the HUD prefab; this fills it in and wires its
    /// buttons. Deck cards and promotion choices are copies of their own prefabs.
    /// Korean renders through the Pretendard fallback registered in TMP Settings,
    /// so labels can be translated without touching the font setup.
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

        [Header("Parts (set in the prefab)")]
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
        // Identical cards share one button; each entry lists the deck indices behind it.
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

        // ---------- construction ----------

        static void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            // This project uses the Input System package, so the legacy module cannot drive UI.
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacy = existing.GetComponent<BaseInputModule>();
                if (legacy != null)
                    Destroy(legacy);

                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        static void ReloadStage() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        /// <summary>Winning the final duel ends the run.</summary>
        static void ReturnToTitle()
        {
            RunState.Instance.ResetRun();
            SceneManager.LoadScene(TitleScene);
        }

        /// <summary>One button per promotion choice; the duel waits until one is pressed.</summary>
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
                button.GetComponentInChildren<TextMeshProUGUI>().text = def.displayName;
                button.onClick.AddListener(() =>
                {
                    _promotionPanel.gameObject.SetActive(false);
                    _director.ChoosePromotion(def);
                });
            }

            _promotionPanel.gameObject.SetActive(true);
        }

        // ---------- reactions ----------

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
            // A win moves on (shop, or the tutorial's closing lines); only a loss offers a retry.
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
        /// The final duel: one move per turn, so AP is shown as switched off,
        /// the turn passes by itself, and the status line reports check and the AI.
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

            // The turn passes as soon as a move is made, so the button only starts the duel.
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

            // Eight free pawns would overflow the strip, so identical cards stack into one.
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

                string name = def.displayName + (count > 1 ? " x" + count : "");
                label.text = _director.IsChessDuel
                    ? name + "\n배치 전용"
                    : name + "\n" + def.deployApCost + " AP" + (_deploy.IsUnlimited(index) ? "  · 상시" : "");
                _deckButtons.Add(button);
            }

            // Deploying the king is what unlocks the start button, and that
            // arrives as a deck change - so refresh the whole HUD, not just
            // the card colours.
            Refresh();
        }

        void RefreshDeckColors()
        {
            var deck = _deploy.Deck;

            for (int i = 0; i < _deckButtons.Count; i++)
            {
                if (_deckButtons[i] == null || i >= _cardGroups.Count)
                    continue;

                // During setup only the king is playable, so the rest of the
                // deck is shown greyed out rather than silently doing nothing.
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
