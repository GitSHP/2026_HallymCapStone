using System.Collections;
using Promotion.Core;
using Promotion.Feel;
using Promotion.Tutorial;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.Title
{
    public class TitleScreen : MonoBehaviour
    // 타이틀 화면을 관리하는 클래스 -> 등장 연출, 메뉴 버튼(시작/옵션/종료), 튜토리얼 선택창, 게임 시작 처리를 담당
    // -> 화면 배치는 프리팹(Assets/UI/Prefabs)에 있고, 이 스크립트는 버튼 연결·켜고 끄기·연출 실행만 함
    {
        [Header("흐름")]
        public TutorialScript tutorial;  // 튜토리얼 대본 에셋 -> 비어 있으면 튜토리얼 선택 없이 바로 시작함
        public string battleScene = "Game";  // 튜토리얼을 할 때 불러올 전투 씬 이름
        public string mapScene = "Map";  // 튜토리얼을 건너뛸 때 불러올 맵 씬 이름

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] UiButton _start;  // 게임 시작 버튼
        [SerializeField] UiButton _options;  // 옵션 버튼 (아직 기능이 없어서 비활성 상태로 둠)
        [SerializeField] UiButton _quit;  // 게임 종료 버튼

        [Header("튜토리얼 선택")]
        [SerializeField] GameObject _choice;  // 튜토리얼을 할지 고르는 선택창 전체 (평소에는 꺼져 있음)
        [SerializeField] Button _choiceBackdrop;  // 선택창 뒤의 어두운 배경 -> 클릭하면 선택창이 닫힘
        [SerializeField] RectTransform _choicePanel;  // 선택창 패널 (등장 연출 대상)
        [SerializeField] UiButton _playTutorial;  // "튜토리얼 하기" 버튼
        [SerializeField] UiButton _skipTutorial;  // "튜토리얼 건너뛰기" 버튼

        [Header("등장 연출 (프리팹에서 지정)")]
        [Tooltip("킹이 착지할 때 함께 흔들리는 묶음. 배경은 빠져 있어 화면 가장자리가 비지 않는다.")]
        [SerializeField] RectTransform _stage;  // 킹 로고가 착지할 때 '쿵' 하고 함께 흔들리는 화면 요소 묶음
        [Tooltip("체스판 무늬 칸들의 부모. GridLayoutGroup의 열 수로 대각선 순서를 계산한다.")]
        [SerializeField] UnityEngine.UI.GridLayoutGroup _tiles;  // 배경 체스판 무늬 칸들의 부모 -> 칸들이 대각선 순서로 물결처럼 나타남
        [SerializeField] RectTransform _logo;  // 킹 로고 이미지 (위에서 떨어지는 연출 대상)
        [SerializeField] TMP_Text _title;  // 게임 제목 텍스트 (한 글자씩 솟아오르는 연출 대상)
        [SerializeField] CanvasGroup _credits;  // 화면 아래 크레딧 묶음 (서서히 나타나는 연출 대상)
        [Tooltip("연출 중에만 켜지는 투명한 판. 어디를 클릭해도 연출을 건너뛴다.")]
        [SerializeField] Button _skipCatcher;  // 연출 중 화면 아무 곳이나 클릭하면 연출을 건너뛰게 해주는 투명 버튼

        bool _starting;  // 게임 시작(씬 이동)을 이미 눌렀는지 여부 -> 버튼 중복 클릭으로 씬을 두 번 불러오지 않도록 함
        bool _introDone;  // 등장 연출이 끝났는지(또는 건너뛰었는지) 여부
        Coroutine _intro;  // 실행 중인 등장 연출 코루틴 -> 건너뛸 때 중단하기 위해 저장

        void Start()  // 타이틀 화면이 시작될 때 버튼을 연결하고 등장 연출을 시작하는 함수
        {
            InputGate.Reset();  // 이전 씬에서 남아 있을 수 있는 입력 차단 수를 0으로 초기화

            // 메뉴 버튼 연결
            _start.Clicked += OpenChoice;  // 시작 버튼 → 튜토리얼 선택창 열기
            _options.SetInteractable(false);  // 옵션 버튼은 아직 기능이 없으므로 누를 수 없게 함
            _quit.Clicked += Quit;  // 종료 버튼 → 게임 종료
            _choiceBackdrop.onClick.AddListener(CloseChoice);  // 선택창 바깥(배경) 클릭 → 선택창 닫기
            _playTutorial.Clicked += () => Begin(true);  // 튜토리얼 하기 → 튜토리얼과 함께 시작
            _skipTutorial.Clicked += () => Begin(false);  // 건너뛰기 → 바로 맵으로 시작

            _choice.SetActive(false);  // 선택창은 처음에 숨겨 둠
            _skipCatcher.onClick.AddListener(FinishIntro);  // 연출 중 화면을 클릭하면 연출을 바로 끝냄
            _intro = StartCoroutine(PlayIntro());  // 등장 연출 시작
        }

        void Update()  // 매 프레임 키보드 입력을 확인하는 함수
        {
            var keyboard = Keyboard.current;  // 신 Input System으로 현재 키보드를 가져옴
            if (keyboard == null)  // 키보드가 연결되지 않았다면 -> 아무것도 하지 않음
                return;

            if (!_introDone && keyboard.anyKey.wasPressedThisFrame)  // 만약 연출 중에 아무 키나 눌렀다면 -> 연출 건너뛰기
                FinishIntro();
            else if (_choice.activeSelf && keyboard.escapeKey.wasPressedThisFrame)  // 만약 선택창이 열린 상태에서 ESC를 눌렀다면 -> 선택창 닫기
                CloseChoice();
        }

        IEnumerator PlayIntro()  // 타이틀 등장 연출을 순서대로 재생하는 코루틴
        // 순서: 체스판 칸이 대각선 물결로 깔림 → 킹 로고가 떨어져 '쿵' → 제목 글자가 솟아오름 → 크레딧 페이드 → 메뉴 버튼 등장
        // -> 각 연출의 시작 시간(지연)은 TweenPresets 에셋에서 조정함
        {
            var p = Fx.P;  // 연출 수치 에셋
            _skipCatcher.gameObject.SetActive(true);  // 연출 중에는 클릭으로 건너뛸 수 있도록 투명 판을 켬

            foreach (var button in new[] { _start, _options, _quit })  // 메뉴 버튼 3개를
                UiBuilder.Group(button.gameObject).alpha = 0f;  // 처음에는 투명하게 숨겨 둠 (나중에 차례로 등장)

            // 배경 체스판 칸 연출: 왼쪽 위 칸부터 대각선 방향으로 차례차례 나타남
            int columns = Mathf.Max(1, _tiles.constraintCount);  // 한 줄에 놓인 칸 수 (0으로 나누지 않도록 최소 1)
            for (int i = 0; i < _tiles.transform.childCount; i++)  // 모든 칸에 대해
            {
                var tile = _tiles.transform.GetChild(i);
                var graphic = tile.GetComponent<Graphic>();
                if (graphic == null)  // 그래픽이 없는 칸은 건너뜀
                    continue;
                int diagonal = i % columns + i / columns;  // 열 번호 + 행 번호 = 몇 번째 대각선인지 -> 같은 대각선의 칸은 동시에 나타남
                Fx.TileIn(tile, graphic, graphic.color.a, p.titleTileStart + diagonal * p.titleTileStagger);  // 대각선 순서만큼 늦게 시작해서 원래 투명도까지 나타남
            }

            Fx.DropOnto(_logo, p.titleLogoDelay, () =>  // 킹 로고를 위에서 떨어뜨리고, 착지하면
            {
                if (!_introDone)  // 아직 연출을 건너뛰지 않았을 때만 (건너뛴 뒤에 화면이 흔들리면 어색하므로)
                    Fx.TitleImpact(_logo, _stage);  // 로고가 눌렸다 퍼지고 화면 전체가 '쿵' 흔들림
            });
            Fx.LettersRise(_title, p.titleLetterDelay);  // 제목 글자가 한 글자씩 아래에서 솟아오름
            Fx.FadeInGroup(_credits, p.titleCreditDelay);  // 크레딧이 서서히 나타남

            yield return new WaitForSecondsRealtime(p.titleMenuDelay);  // 메뉴가 등장할 시간까지 기다렸다가 (Realtime: 게임 시간이 멈춰도 진행)
            _start.PlayIn(0);  // 메뉴 버튼을 위에서부터 차례로 등장시킴
            _options.PlayIn(1);
            _quit.PlayIn(2);

            yield return new WaitForSecondsRealtime(
                Mathf.Max(p.titleCreditDelay + p.titleCreditFade, p.titleShakeDuration + p.titleLogoDelay) - p.titleMenuDelay + 0.3f);
            // 크레딧 페이드와 로고 흔들림 중 늦게 끝나는 쪽 + 여유 0.3초까지 기다린 뒤
            FinishIntro();  // 연출 마무리
        }

        void FinishIntro()  // 등장 연출을 끝내고 모든 요소를 최종 상태로 맞추는 함수 -> 연출이 정상 종료될 때와 건너뛸 때 모두 사용
        {
            if (_introDone)  // 만약 이미 끝났다면 -> 중복 실행 방지
                return;

            _introDone = true;  // 연출 끝으로 표시하고
            if (_intro != null)
                StopCoroutine(_intro);  // 아직 진행 중인 연출 코루틴이 있다면 중단

            Fx.CompleteAll();  // 진행 중인 모든 트윈을 즉시 끝 상태로 만듦 (칸·로고·제목이 바로 제자리에)
            foreach (var button in new[] { _start, _options, _quit })
                UiBuilder.Group(button.gameObject).alpha = 1f;  // 메뉴 버튼이 확실히 보이도록 불투명하게
            _credits.alpha = 1f;  // 크레딧도 확실히 보이게
            _stage.anchoredPosition = Vector2.zero;  // 흔들리던 화면 묶음을 제자리로

            _skipCatcher.gameObject.SetActive(false);  // 건너뛰기용 투명 판을 꺼서 메뉴 버튼을 누를 수 있게 함
            Fx.IdleBreathe(_logo);  // 이후 킹 로고가 천천히 숨 쉬듯 커졌다 작아지는 대기 연출 시작
        }

        void OpenChoice()  // 시작 버튼을 눌렀을 때 튜토리얼 선택창을 여는 함수
        {
            if (_choice.activeSelf || _starting)  // 만약 이미 열려 있거나 시작 중이라면 -> 무시
                return;

            if (tutorial == null)  // 만약 튜토리얼 대본이 연결되지 않았다면 -> 선택창 없이 바로 일반 시작
            {
                Begin(false);
                return;
            }

            _choice.SetActive(true);  // 선택창을 켜고
            UiFx.BackdropIn(UiBuilder.Group(_choice));  // 어두운 배경이 서서히 나타나고
            UiFx.PanelIn(_choicePanel, UiBuilder.Group(_choicePanel.gameObject));  // 선택창 패널이 커지며 나타남
            _playTutorial.PlayIn(0);  // 두 버튼이 차례로 등장
            _skipTutorial.PlayIn(1);
        }

        void CloseChoice()  // 튜토리얼 선택창을 닫는 함수
        {
            if (!_choice.activeSelf || _starting)  // 만약 열려 있지 않거나 이미 시작 중이라면 -> 무시
                return;

            _choice.SetActive(false);
        }

        void Begin(bool withTutorial)  // 새 게임을 시작하는 함수 (withTutorial: 튜토리얼을 할지 여부)
        {
            if (_starting)  // 만약 이미 시작 처리 중이라면 -> 중복 실행 방지
                return;

            _starting = true;
            RunState.Instance.ResetRun();  // 이전 판의 골드·기물·유물 등 진행 정보를 초기화

            if (withTutorial)  // 만약 튜토리얼을 선택했다면
            {
                TutorialSession.Begin(tutorial);  // 튜토리얼 대본을 등록하고
                SceneManager.LoadScene(battleScene);  // 전투 씬으로 바로 이동 -> 전투 씬이 등록된 대본을 보고 튜토리얼로 진행
                return;
            }

            TutorialSession.Complete();  // 튜토리얼을 건너뛰면 등록된 대본을 지워서 일반 전투로 진행되게 하고
            SceneManager.LoadScene(mapScene);  // 맵 씬으로 이동
        }

        static void Quit()  // 게임을 종료하는 함수
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;  // 에디터에서는 플레이 모드를 멈춤
#else
            Application.Quit();  // 빌드된 게임에서는 프로그램 종료
#endif
        }
    }
}
