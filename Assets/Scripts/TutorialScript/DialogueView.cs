using System.Collections;
using Promotion.Core;
using Promotion.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Promotion.Tutorial
{
    public class DialogueView : MonoBehaviour // 튜토리얼의 대사 화면을 담당하는 클래스
    // -> 모드가 두 가지 있음
    //    Talk: 화자 초상화가 있는 비주얼 노벨식 대화창 -> 플레이어가 클릭해 넘길 때까지 화면이 어두워지고 보드 입력이 잠김
    //    Hint: 플레이어가 행동하는 동안 보드 아래에 뜨는 짧은 한 줄 안내 -> 직접 해 보는 것이 학습이므로 아무것도 막지 않음
    // -> 레이아웃은 전투 씬에 꺼진 상태로 놓인 프리팹이고, 튜토리얼이 시작될 때 이를 켜고 스크립트의 화자 정보를 채움
    {
        static readonly Color TalkDim = new Color(0f, 0f, 0f, 0.7f); // 대화 중 화면을 어둡게 덮는 색 (반투명 검정)
        static readonly Color StoryDim = new Color(0f, 0f, 0f, 0.88f); // 만화(스토리)가 떠 있을 때 덮는 색 -> 보드가 아니라 만화에 시선이 가도록 더 어둡게

        [Header("대화")]
        [SerializeField] GameObject _talk; // 대화창 전체 오브젝트 (켜고 끄는 용도)
        [SerializeField] Image _backdrop; // 대화 중 화면 전체를 어둡게 덮는 이미지
        [SerializeField] Button _backdropButton; // 어두운 배경 아무 곳이나 클릭하면 대사를 넘기기 위한 버튼
        [SerializeField] TextMeshProUGUI _speakerName; // 화자 이름 텍스트
        [SerializeField] TextMeshProUGUI _talkText; // 대사 본문 텍스트
        [SerializeField] GameObject _continueMark; // 대사가 다 나왔을 때 "다음으로 넘기기" 표시 오브젝트
        [SerializeField] Image _portrait; // 화자 초상화 이미지
        [Tooltip("스크립트에 일러스트가 없을 때 초상화 대신 보여 준다.")]
        [SerializeField] GameObject _portraitPlaceholder; // 초상화 일러스트가 없을 때 대신 보여 주는 임시 틀

        [Header("보드 가림막")]
        [SerializeField] CanvasGroup _curtain; // 스토리 장면 동안 보드와 HUD를 가리는 가림막 (투명도로 페이드)
        [SerializeField] Image _background; // 가림막에 깔리는 스토리 배경 그림
        [SerializeField] AspectRatioFitter _backgroundFitter; // 배경 그림의 비율을 유지한 채 화면을 채우게 해 주는 컴포넌트
        [SerializeField] Image _shade; // 배경 그림 위에 덮는 음영 -> 만화가 떠 있을 때만 어두워짐

        [Header("스토리")]
        [SerializeField] GameObject _story; // 만화(스토리) 페이지 전체 오브젝트
        [SerializeField] Image _storyBack; // 이전 만화 프레임 (뒤에 깔려 있음)
        [SerializeField] Image _storyFront; // 새 만화 프레임 (앞에서 서서히 나타남)

        [Header("힌트")]
        [SerializeField] GameObject _hint; // 힌트(한 줄 안내) 오브젝트
        [SerializeField] TextMeshProUGUI _hintText; // 힌트 문구 텍스트

        TutorialScript _script; // 현재 튜토리얼 대본 데이터 (화자 정보, 타이핑 속도 등)
        bool _covered; // 지금 가림막이 보드를 가리고 있는지 여부
        bool _typing; // 지금 대사가 한 글자씩 타이핑되는 중인지 여부
        bool _advance; // 플레이어가 다음 대사로 넘기기를 눌렀는지 여부
        bool _blocking; // 지금 보드 입력을 막고 있는지 여부 (InputGate에 Push한 상태인지)

        public void Init(TutorialScript script)
        // 튜토리얼용으로 대사 화면을 켜고 화자 이름·초상화·스토리 배경을 채우는 함수
        {
            _script = script; // 대본 데이터를 저장하고
            gameObject.SetActive(true); // 꺼져 있던 대사 화면을 켬

            _backdropButton.onClick.RemoveListener(Continue); // 혹시 이미 연결되어 있다면 먼저 빼고 (중복 연결 방지)
            _backdropButton.onClick.AddListener(Continue); // 배경을 클릭하면 Continue가 실행되도록 연결

            _speakerName.text = script.speakerName; // 화자 이름을 표시하고
            ShowPortrait(script.portrait); // 기본 초상화를 보여 줌

            bool hasBackground = script.storyBackground != null; // 대본에 스토리 배경 그림이 있는지
            _background.gameObject.SetActive(hasBackground); // 배경 그림이 있을 때만 배경과 음영을 켬
            _shade.gameObject.SetActive(hasBackground);
            if (hasBackground) // 만약 배경 그림이 있다면
            {
                _background.sprite = script.storyBackground; // 배경 그림을 넣고
                _backgroundFitter.aspectRatio = script.storyBackground.rect.width / script.storyBackground.rect.height; // 그림의 가로/세로 비율을 알려 줌 -> 어떤 화면비에서도 늘리지 않고 잘라 내는 방식으로 화면을 채움
                _shade.color = Color.clear; // 만화가 시작될 때까지 음영은 투명하게 둠 (SetShade 참고)
            }

            HideAll(); // 처음에는 대화창·힌트·스토리를 모두 숨긴 상태로 시작
        }

        public IEnumerator Say(string text, Sprite expression = null, Sprite storyFrame = null)
        // 대화창에 대사 한 줄을 타이핑 효과로 보여 주고, 플레이어가 넘길 때까지 기다리는 것을 순서대로 진행하는 코루틴
        // -> expression: 이 대사에서 쓸 초상화 (null이면 기본 초상화), storyFrame: 함께 띄울 만화 프레임 (null이면 만화 없음)
        {
            ShowPortrait(expression != null ? expression : _script.portrait); // 표정 그림이 있으면 그것을, 없으면 기본 초상화를 보여 주고
            ShowStory(storyFrame); // 만화 프레임을 보여 주거나 숨김

            _hint.SetActive(false); // 힌트는 숨기고
            _talk.SetActive(true); // 대화창을 켜고
            SetBlocking(true); // 대화 중에는 보드 입력을 막음

            _talkText.text = text; // 대사 전체를 텍스트에 넣되
            _talkText.maxVisibleCharacters = 0; // 처음에는 한 글자도 보이지 않게 하고
            _talkText.ForceMeshUpdate(); // 글자 수를 정확히 알기 위해 텍스트 정보를 즉시 갱신
            int total = _talkText.textInfo.characterCount; // 실제로 보여 줄 전체 글자 수 (태그 등은 제외된 값)

            _continueMark.SetActive(false); // 타이핑 중에는 넘기기 표시를 숨기고
            _advance = false; // 넘기기 입력을 초기화하고
            _typing = true; // 타이핑 시작

            // 타이핑 효과: 매 프레임 보이는 글자 수를 조금씩 늘림
            // -> 다른 곳의 히트스톱(Time.timeScale 조절) 때문에 대사가 멈추면 안 되므로 unscaled 시간을 사용
            float shown = 0f; // 지금까지 보여 준 글자 수 (소수점까지 누적)
            while (_typing && shown < total) // 플레이어가 건너뛰지 않았고 아직 다 보여 주지 않았다면 반복
            {
                shown += Time.unscaledDeltaTime * _script.charactersPerSecond; // 지난 프레임 시간 × 초당 글자 수만큼 늘리고
                _talkText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown)); // 그만큼의 글자만 화면에 보이게 함
                yield return null; // 다음 프레임까지 대기
            }

            // 타이핑이 끝났거나 플레이어가 건너뛰었다면 -> 대사 전체를 한 번에 보여 줌
            _typing = false;
            _talkText.maxVisibleCharacters = total;
            _continueMark.SetActive(true); // 넘기기 표시를 켜고
            _advance = false; // 건너뛰기에 쓴 입력이 바로 넘기기로 처리되지 않도록 초기화

            while (!_advance) // 플레이어가 넘기기를 누를 때까지 기다림
                yield return null;
        }

        public void ShowHint(string text)
        // 플레이어가 직접 행동하는 동안 보여 주는 짧은 한 줄 안내를 띄우는 함수 -> 아무 입력도 막지 않음
        {
            ForceUncover(); // 플레이어가 곧 보드에서 행동해야 하므로 어떤 경우에도 가림막을 걷음
            _talk.SetActive(false); // 대화창을 끄고
            SetBlocking(false); // 보드 입력 막기를 풀고
            ShowStory(null); // 만화도 숨김

            _hintText.text = text; // 힌트 문구를 넣고
            _hint.SetActive(true); // 힌트를 화면에 띄움
        }

        public void HideAll() // 가림막·만화·대화창·힌트를 모두 숨기고 입력 막기를 푸는 함수
        {
            ForceUncover();
            ShowStory(null);
            _talk.SetActive(false);
            _hint.SetActive(false);
            SetBlocking(false);
        }

        public void SetBoardCovered(bool covered)
        // 스토리 장면을 위해 보드와 HUD를 가림막 뒤로 숨기거나 다시 드러내는 함수
        // -> 가릴 때는 즉시 가리고, 걷을 때는 서서히 사라지게 해서 전장이 뚝 끊겨 나타나지 않도록 함
        {
            bool showing = _curtain.gameObject.activeSelf; // 지금 가림막이 켜져 있는지 (바꾸기 전 상태)
            _covered = covered; // 새 상태를 저장하고
            UpdateDim(); // 어둡게 덮는 색도 상태에 맞게 갱신

            if (covered) // 만약 가려야 한다면
            {
                _curtain.gameObject.SetActive(true); // 가림막을 켜고
                _curtain.alpha = 1f; // 즉시 완전히 불투명하게 함
                return;
            }

            if (showing) // 걷어야 하고 가림막이 켜져 있었다면
                Fx.FadeOutCurtain(_curtain); // 서서히 사라지게 함
        }

        void ForceUncover() // 연출 없이 가림막을 즉시 걷는 함수
        {
            _covered = false;
            _curtain.gameObject.SetActive(false);
            UpdateDim();
        }

        void ShowPortrait(Sprite sprite)
        // 초상화를 보여 주는 함수 -> 일러스트가 없으면 대신 임시 틀을 보여 줌
        {
            bool hasArt = sprite != null; // 보여 줄 일러스트가 있는지
            _portrait.gameObject.SetActive(hasArt); // 일러스트가 있을 때만 초상화를 켜고
            if (_portraitPlaceholder != null) // 임시 틀이 지정되어 있다면
                _portraitPlaceholder.SetActive(!hasArt); // 일러스트가 없을 때만 임시 틀을 켬
            if (hasArt) // 만약 일러스트가 있다면 초상화 이미지를 교체
                _portrait.sprite = sprite;
        }

        void UpdateDim()
        // 대화 중 화면을 어둡게 덮는 색을 상황에 맞게 정하는 함수
        // -> 가림막(스토리 배경)이 있을 때는 어둡게 덮으면 그림을 묻어 버리므로 투명하게 두고, 배경 자체의 음영으로만 어둡게 함
        {
            if (_covered) // 만약 가림막이 보드를 가리고 있다면
                _backdrop.color = Color.clear; // 덮는 색은 투명
            else
                _backdrop.color = _story.activeSelf ? StoryDim : TalkDim; // 만화가 떠 있으면 더 어둡게, 아니면 기본 대화 어둡기
        }

        void ShowStory(Sprite frame)
        // 만화 한 페이지(프레임)를 보여 주는 함수 (frame이 null이면 만화를 숨김)
        // -> 각 프레임에는 지금까지의 모든 컷이 그려져 있음
        // -> 새 프레임을 이전 프레임 위에 겹쳐 서서히 나타나게 하면 다음 컷이 새로 등장하는 것처럼 보임
        {
            if (frame == null) // 만약 보여 줄 프레임이 없다면 -> 만화를 숨김
            {
                if (_story.activeSelf) // 만화가 떠 있었다면
                    SetShade(false); // 배경 음영을 다시 밝히고

                _story.SetActive(false); // 만화를 끄고
                _storyFront.sprite = null; // 앞 프레임을 비움 -> 다음에 처음부터 다시 시작
                UpdateDim();
                return;
            }

            if (_story.activeSelf && _storyFront.sprite == frame) // 이미 같은 프레임이 떠 있다면 다시 연출하지 않음
                return;

            if (!_story.activeSelf) // 만화가 처음 나타나는 순간이라면
                SetShade(true); // 배경 음영을 어둡게 함

            var previous = _story.activeSelf ? _storyFront.sprite : null; // 직전에 보이던 프레임 (처음이면 없음)
            _story.SetActive(true); // 만화를 켜고
            UpdateDim();

            _storyBack.sprite = previous; // 직전 프레임을 뒤쪽에 깔아 두고
            _storyBack.enabled = previous != null; // 직전 프레임이 없다면 뒤쪽 이미지는 끔

            _storyFront.sprite = frame; // 새 프레임을 앞쪽에 넣고
            Fx.FadeIn(_storyFront); // 서서히 나타나게 함 -> 다음 컷이 등장하는 것처럼 보임
        }

        void SetShade(bool on)
        // 만화가 떠 있는 동안 배경 그림을 어둡게 하고, 사라지면 다시 밝히는 함수
        {
            if (_shade != null && _shade.gameObject.activeSelf) // 음영 오브젝트가 있고 켜져 있을 때만 (배경 그림이 있을 때만)
                Fx.FadeTo(_shade, on ? _script.storyBackgroundShade : 0f); // 켜면 대본에 정한 어둡기로, 끄면 투명하게 서서히 바꿈
        }

        void Update() // 매 프레임 키보드 입력을 검사해서 대사를 넘기는 함수
        {
            if (!_talk.activeSelf) // 대화창이 꺼져 있다면 검사할 필요 없음
                return;

            var keyboard = Keyboard.current; // 현재 연결된 키보드 (신 Input System)
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) // 만약 이번 프레임에 스페이스나 엔터를 눌렀다면
                Continue(); // 대사를 넘김 (클릭과 같은 처리)
        }

        void Continue()
        // 넘기기 입력을 처리하는 함수 -> 첫 입력은 타이핑을 즉시 끝내고, 다음 입력은 다음 대사로 넘어감
        {
            if (_typing) // 만약 아직 타이핑 중이라면
                _typing = false; // 타이핑을 멈춤 -> Say에서 대사 전체가 한 번에 표시됨
            else
                _advance = true; // 타이핑이 끝났다면 다음 대사로 넘어가도록 표시
        }

        void SetBlocking(bool blocking)
        // 보드 입력 막기를 켜고 끄는 함수 -> InputGate는 Push/Pop 횟수로 관리되므로 상태가 바뀔 때만 한 번씩 호출해야 함
        {
            if (_blocking == blocking) // 이미 같은 상태라면 아무것도 하지 않음 -> Push/Pop이 중복되지 않도록
                return;

            _blocking = blocking; // 새 상태를 저장하고
            if (blocking) // 막아야 한다면
                InputGate.Push(); // 입력 막기 횟수를 하나 늘리고
            else
                InputGate.Pop(); // 풀어야 한다면 하나 줄임
        }

        void OnDestroy() => SetBlocking(false); // 대사 화면이 사라질 때 입력 막기가 남아 있지 않도록 반드시 풀어 주는 함수
    }
}
