using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.Tutorial;
using Gambonanza.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gambonanza.Title
{
    /// <summary>
    /// The first screen: logo, title and the three menu buttons. Starting a game
    /// asks whether to play the tutorial or go straight to the map.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("Text")]
        public string gameTitle = "Promotion";
        [Tooltip("Required by the art licences. One line per credit.")]
        [TextArea(1, 4)]
        public string credits = "Chess art: Pixel Chess by DANI MACCARI";

        [Header("Flow")]
        public TutorialScript tutorial;
        public string battleScene = "Game";
        public string mapScene = "Map";

        RectTransform _root;
        GameObject _choice;
        bool _starting;

        void Start()
        {
            // Arriving here from anywhere means any overlay that blocked the board is gone.
            InputGate.Reset();
            Build();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (_choice != null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                CloseChoice();
        }

        void Build()
        {
            var canvas = UiBuilder.OverlayCanvas("TitleCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            _root = root;

            var background = UiBuilder.Panel("Background", root, UiTheme.Panel, false);
            UiBuilder.Stretch(background.rectTransform);

            // Placeholder logo: the king the whole game is about protecting.
            var logo = UiBuilder.Icon("Logo", root, IconId.King, UiTheme.Gold);
            UiBuilder.Place(logo.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, 280f), new Vector2(170f, 170f));

            var title = UiBuilder.Label("Title", root, gameTitle, 120f, UiTheme.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Place(title.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, 120f), new Vector2(1400f, 150f));
            title.characterSpacing = 8f;

            var start = MenuButton(root, "Start", "게임 시작", IconId.Arrow, -90f, UiTheme.Confirm);
            start.Clicked += OpenChoice;

            var options = MenuButton(root, "Options", "옵션", IconId.Star, -180f, UiTheme.CardWell);
            options.SetInteractable(false);

            var quit = MenuButton(root, "Quit", "게임 종료", IconId.Chevrons, -270f, UiTheme.CardWell);
            quit.Clicked += Quit;

            start.PlayIn(0);
            options.PlayIn(1);
            quit.PlayIn(2);

            var credit = UiBuilder.Label("Credits", root, credits, 18f, UiTheme.InkFaint,
                TextAlignmentOptions.BottomRight);
            UiBuilder.Place(credit.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-30f, 20f), new Vector2(700f, 100f));
        }

        static UiButton MenuButton(RectTransform root, string name, string caption, IconId icon,
            float y, Color face)
        {
            var content = face == UiTheme.Confirm ? UiTheme.InkOnAccent : UiTheme.Ink;
            return UiButton.CreateAction(root, name, icon, caption,
                UiBuilder.Centre, UiBuilder.Centre, new Vector2(0f, y), new Vector2(380f, 72f),
                face, content, 26f);
        }

        // ---------- Tutorial choice ----------

        /// <summary>Asks whether to play the tutorial. Clicking outside or Esc backs out.</summary>
        void OpenChoice()
        {
            if (_choice != null || _starting)
                return;

            if (tutorial == null)
            {
                Begin(false);
                return;
            }

            var backdrop = UiBuilder.Panel("TutorialChoice", _root, UiTheme.Backdrop, false);
            UiBuilder.Stretch(backdrop.rectTransform);
            backdrop.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseChoice);
            _choice = backdrop.gameObject;

            var edge = UiBuilder.Panel("PanelEdge", backdrop.transform, UiTheme.PanelEdge);
            UiBuilder.Place(edge.rectTransform, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero,
                new Vector2(766f, 336f));

            var panel = UiBuilder.Panel("Panel", backdrop.transform, UiTheme.Panel);
            UiBuilder.Place(panel.rectTransform, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero,
                new Vector2(760f, 330f));

            var question = UiBuilder.Label("Question", panel.transform, "튜토리얼을 진행할까요?", 38f,
                UiTheme.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Place(question.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, 90f), new Vector2(700f, 54f));

            var note = UiBuilder.Label("Note", panel.transform,
                "처음 플레이한다면 튜토리얼을 추천해요.", 22f, UiTheme.InkMuted);
            UiBuilder.Place(note.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, 38f), new Vector2(700f, 34f));

            var play = UiButton.CreateAction(panel.transform, "PlayTutorial", IconId.Arrow, "튜토리얼 보기",
                UiBuilder.Centre, UiBuilder.Centre, new Vector2(-170f, -70f), new Vector2(310f, 72f),
                UiTheme.Confirm, UiTheme.InkOnAccent, 24f);
            play.Clicked += () => Begin(true);

            var skip = UiButton.CreateAction(panel.transform, "SkipTutorial", IconId.Chevrons, "건너뛰기",
                UiBuilder.Centre, UiBuilder.Centre, new Vector2(170f, -70f), new Vector2(310f, 72f),
                UiTheme.CardWell, UiTheme.Ink, 24f);
            skip.Clicked += () => Begin(false);

            UiFx.BackdropIn(UiBuilder.Group(backdrop.gameObject));
            UiFx.PanelIn(panel.rectTransform, UiBuilder.Group(panel.gameObject));
            play.PlayIn(0);
            skip.PlayIn(1);
        }

        void CloseChoice()
        {
            if (_choice == null || _starting)
                return;

            Destroy(_choice);
            _choice = null;
        }

        /// <summary>A fresh run, either through the tutorial battle or straight onto the map.</summary>
        void Begin(bool withTutorial)
        {
            if (_starting)
                return;

            _starting = true;
            RunState.Instance.ResetRun();

            if (withTutorial)
            {
                TutorialSession.Begin(tutorial);
                SceneManager.LoadScene(battleScene);
                return;
            }

            TutorialSession.Complete();
            SceneManager.LoadScene(mapScene);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
