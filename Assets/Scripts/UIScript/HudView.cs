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
    /// The stage HUD, built in code so the scene needs no wiring.
    /// Korean renders through the Pretendard fallback registered in TMP Settings,
    /// so labels can be translated without touching the font setup.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        static readonly Color Panel = new Color(0.07f, 0.07f, 0.10f, 0.85f);
        static readonly Color Ink = new Color(0.94f, 0.93f, 0.90f);
        static readonly Color ButtonIdle = new Color(0.20f, 0.20f, 0.26f, 0.95f);
        static readonly Color ButtonOff = new Color(0.14f, 0.14f, 0.17f, 0.80f);
        static readonly Color CardIdle = new Color(0.18f, 0.18f, 0.24f, 0.95f);
        static readonly Color CardPicked = new Color(0.95f, 0.78f, 0.25f, 0.95f);
        static readonly Color CardLocked = new Color(0.13f, 0.13f, 0.16f, 0.75f);

        StageDirector _director;
        TurnManager _turn;
        DeployController _deploy;

        TextMeshProUGUI _status;
        TextMeshProUGUI _ap;
        TextMeshProUGUI _banner;
        TextMeshProUGUI _actionLabel;
        Button _action;
        Button _restart;
        RectTransform _deckBar;

        readonly List<Button> _deckButtons = new();

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
            Build();

            _turn.PhaseChanged += OnPhaseChanged;
            _turn.ApChanged += OnApChanged;
            _turn.TurnChanged += OnTurnChanged;
            _director.StateChanged += Refresh;
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
                _director.StateChanged -= Refresh;

            if (_deploy != null)
                _deploy.DeckChanged -= RebuildDeck;
        }

        void OnPhaseChanged(TurnPhase phase) => Refresh();
        void OnApChanged(int current, int max) => Refresh();
        void OnTurnChanged(int turn) => Refresh();

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

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)transform;

            var top = MakePanel(root, "TopBar", new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(900f, 64f));
            _status = MakeText(top, "Status", 30f);

            var apPanel = MakePanel(root, "ApPanel", new Vector2(0f, 0f), new Vector2(200f, 70f), new Vector2(320f, 76f));
            _ap = MakeText(apPanel, "Ap", 34f);

            _action = MakeButton(root, "ActionButton", new Vector2(1f, 0f), new Vector2(-220f, 70f),
                new Vector2(340f, 84f), out _actionLabel, OnActionPressed);

            // The deck sits in the right-hand strip the camera already reserves,
            // so it never overlaps the board at any aspect ratio.
            var deckPanel = MakePanel(root, "DeckBar", new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(220f, 660f));
            var layout = deckPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 16, 16);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            _deckBar = deckPanel;

            var deckTitle = MakePanel(root, "DeckTitle", new Vector2(1f, 0.5f), new Vector2(-150f, 360f), new Vector2(220f, 52f));
            MakeText(deckTitle, "DeckTitleText", 26f).text = "소환";

            var bannerPanel = MakePanel(root, "Banner", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 200f));
            _banner = MakeText(bannerPanel, "BannerText", 64f);

            _restart = MakeButton(root, "RestartButton", new Vector2(0.5f, 0.5f), new Vector2(0f, -150f),
                new Vector2(260f, 76f), out var restartLabel, ReloadStage);
            restartLabel.text = "다시 시작";

            bannerPanel.gameObject.SetActive(false);
            _restart.gameObject.SetActive(false);
        }

        static void ReloadStage() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        static RectTransform MakePanel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = Panel;
            return rect;
        }

        static TextMeshProUGUI MakeText(RectTransform parent, string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Ink;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        static Button MakeButton(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size,
            out TextMeshProUGUI label, UnityEngine.Events.UnityAction onClick)
        {
            var rect = MakePanel(parent, name, anchor, position, size);
            var image = rect.GetComponent<Image>();
            image.color = ButtonIdle;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            label = MakeText(rect, "Label", 30f);
            return button;
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

            var bannerPanel = _banner.transform.parent.gameObject;
            bannerPanel.SetActive(over);
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

        void RebuildDeck()
        {
            foreach (var button in _deckButtons)
                if (button != null)
                    Destroy(button.gameObject);

            _deckButtons.Clear();

            var deck = _deploy.Deck;
            for (int i = 0; i < deck.Count; i++)
            {
                int index = i;
                var def = deck[i];

                var button = MakeButton(_deckBar, "Card_" + i, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(192f, 62f), out var label, () => _deploy.Select(index));

                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 192f;
                element.preferredHeight = 62f;

                label.fontSize = 22f;
                label.text = def.displayName + "\n" + def.deployApCost + " AP"
                             + (_deploy.IsUnlimited(i) ? "  · 상시" : "");
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
                if (_deckButtons[i] == null)
                    continue;

                // During setup only the king is playable, so the rest of the
                // deck is shown greyed out rather than silently doing nothing.
                bool playable = i < deck.Count && _deploy.IsDeployableNow(deck[i]);
                _deckButtons[i].interactable = playable;

                _deckButtons[i].image.color = !playable
                    ? CardLocked
                    : (i == _deploy.SelectedIndex ? CardPicked : CardIdle);
            }
        }
    }
}
