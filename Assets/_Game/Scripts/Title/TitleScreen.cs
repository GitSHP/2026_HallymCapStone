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
    /// plays the tutorial the first time, and goes straight to the map after that.
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

        TextMeshProUGUI _notice;

        void Start()
        {
            // Arriving here from anywhere means any overlay that blocked the board is gone.
            InputGate.Reset();
            Build();
        }

        void Update()
        {
            // Playtest shortcut: make the next start play the tutorial again.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f12Key.wasPressedThisFrame)
            {
                TutorialSession.ResetProgress();
                _notice.text = "튜토리얼 기록을 지웠습니다. 다음 시작 때 튜토리얼이 진행됩니다.";
            }
        }

        void Build()
        {
            var canvas = UiBuilder.OverlayCanvas("TitleCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;

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
            start.Clicked += StartGame;

            var options = MenuButton(root, "Options", "옵션", IconId.Star, -180f, UiTheme.CardWell);
            options.SetInteractable(false);

            var quit = MenuButton(root, "Quit", "게임 종료", IconId.Chevrons, -270f, UiTheme.CardWell);
            quit.Clicked += Quit;

            start.PlayIn(0);
            options.PlayIn(1);
            quit.PlayIn(2);

            _notice = UiBuilder.Label("Notice", root, "", 22f, UiTheme.InkMuted);
            UiBuilder.Place(_notice.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(1400f, 34f));

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

        void StartGame()
        {
            RunState.Instance.ResetRun();

            if (!TutorialSession.IsDone && tutorial != null)
            {
                TutorialSession.Begin(tutorial);
                SceneManager.LoadScene(battleScene);
                return;
            }

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
