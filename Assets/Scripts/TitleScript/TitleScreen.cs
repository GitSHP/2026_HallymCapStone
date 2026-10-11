using Promotion.Core;
using Promotion.Tutorial;
using Promotion.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.Title
{
    /// <summary>
    /// The first screen: logo, title and the three menu buttons. Starting a game
    /// asks whether to play the tutorial or go straight to the map.
    /// The layout is the TitleScreen prefab; this only wires its buttons.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("Flow")]
        public TutorialScript tutorial;
        public string battleScene = "Game";
        public string mapScene = "Map";

        [Header("Parts (set in the prefab)")]
        [SerializeField] UiButton _start;
        [SerializeField] UiButton _options;
        [SerializeField] UiButton _quit;

        [Header("Tutorial choice")]
        [SerializeField] GameObject _choice;
        [SerializeField] Button _choiceBackdrop;
        [SerializeField] RectTransform _choicePanel;
        [SerializeField] UiButton _playTutorial;
        [SerializeField] UiButton _skipTutorial;

        bool _starting;

        void Start()
        {
            // Arriving here from anywhere means any overlay that blocked the board is gone.
            InputGate.Reset();

            _start.Clicked += OpenChoice;
            _options.SetInteractable(false);
            _quit.Clicked += Quit;
            _choiceBackdrop.onClick.AddListener(CloseChoice);
            _playTutorial.Clicked += () => Begin(true);
            _skipTutorial.Clicked += () => Begin(false);

            _choice.SetActive(false);
            _start.PlayIn(0);
            _options.PlayIn(1);
            _quit.PlayIn(2);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (_choice.activeSelf && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                CloseChoice();
        }

        // ---------- Tutorial choice ----------

        /// <summary>Asks whether to play the tutorial. Clicking outside or Esc backs out.</summary>
        void OpenChoice()
        {
            if (_choice.activeSelf || _starting)
                return;

            if (tutorial == null)
            {
                Begin(false);
                return;
            }

            _choice.SetActive(true);
            UiFx.BackdropIn(UiBuilder.Group(_choice));
            UiFx.PanelIn(_choicePanel, UiBuilder.Group(_choicePanel.gameObject));
            _playTutorial.PlayIn(0);
            _skipTutorial.PlayIn(1);
        }

        void CloseChoice()
        {
            if (!_choice.activeSelf || _starting)
                return;

            _choice.SetActive(false);
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
