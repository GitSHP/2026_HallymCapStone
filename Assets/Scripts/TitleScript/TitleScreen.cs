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
    /// 첫 화면이다. 로고, 제목, 메뉴 버튼 세 개로 이루어진다. 게임을 시작하면 튜토리얼을
    /// 할지, 바로 맵으로 갈지 묻는다.
    /// 배치는 TitleScreen 프리팹에 있고, 이 클래스는 버튼 연결만 담당한다.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("흐름")]
        public TutorialScript tutorial;
        public string battleScene = "Game";
        public string mapScene = "Map";

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] UiButton _start;
        [SerializeField] UiButton _options;
        [SerializeField] UiButton _quit;

        [Header("튜토리얼 선택")]
        [SerializeField] GameObject _choice;
        [SerializeField] Button _choiceBackdrop;
        [SerializeField] RectTransform _choicePanel;
        [SerializeField] UiButton _playTutorial;
        [SerializeField] UiButton _skipTutorial;

        bool _starting;

        void Start()
        {
            // 어디서든 이 화면에 왔다면 보드를 막던 오버레이는 이미 사라진 것이다.
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

        // ---------- 튜토리얼 선택 ----------

        /// <summary>튜토리얼을 할지 묻는다. 바깥을 클릭하거나 Esc를 누르면 취소된다.</summary>
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

        /// <summary>새 런을 시작한다. 튜토리얼 전투를 거치거나 곧바로 맵으로 간다.</summary>
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
