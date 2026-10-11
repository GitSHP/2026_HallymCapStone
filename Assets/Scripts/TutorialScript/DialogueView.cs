using System.Collections;
using Promotion.Core;
using Promotion.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Promotion.Tutorial
{
    /// <summary>
    /// 튜토리얼의 대사를 담당한다. 두 가지 모드가 있다.
    /// <list type="bullet">
    /// <item>Talk — 화자 초상화가 있는 비주얼 노벨식 대화창이다. 플레이어가 클릭해
    /// 넘길 때까지 화면이 어두워지고 보드가 잠긴다.</item>
    /// <item>Hint — 플레이어가 행동하는 동안 보드 아래에 뜨는 짧은 한 줄이다. 직접 해 보는 것
    /// 자체가 학습이므로 아무것도 막지 않는다.</item>
    /// </list>
    /// 레이아웃은 전투 씬에 꺼진 상태로 배치된 프리팹이며, 튜토리얼이 이를 켜고
    /// 스크립트의 화자 정보를 채운다.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        static readonly Color TalkDim = new Color(0f, 0f, 0f, 0.7f);
        // 만화가 떠 있는 동안은 보드가 아니라 페이지에 시선이 가도록 더 어둡게 한다.
        static readonly Color StoryDim = new Color(0f, 0f, 0f, 0.88f);

        [Header("대화")]
        [SerializeField] GameObject _talk;
        [SerializeField] Image _backdrop;
        [SerializeField] Button _backdropButton;
        [SerializeField] TextMeshProUGUI _speakerName;
        [SerializeField] TextMeshProUGUI _talkText;
        [SerializeField] GameObject _continueMark;
        [SerializeField] Image _portrait;
        [Tooltip("스크립트에 일러스트가 없을 때 초상화 대신 보여 준다.")]
        [SerializeField] GameObject _portraitPlaceholder;

        [Header("보드 가림막")]
        [SerializeField] CanvasGroup _curtain;
        [SerializeField] Image _background;
        [SerializeField] AspectRatioFitter _backgroundFitter;
        [SerializeField] Image _shade;

        [Header("스토리")]
        [SerializeField] GameObject _story;
        [SerializeField] Image _storyBack;
        [SerializeField] Image _storyFront;

        [Header("힌트")]
        [SerializeField] GameObject _hint;
        [SerializeField] TextMeshProUGUI _hintText;

        TutorialScript _script;
        bool _covered;
        bool _typing;
        bool _advance;
        bool _blocking;

        /// <summary>튜토리얼용으로 뷰를 켜고 화자와 그림을 채운다.</summary>
        public void Init(TutorialScript script)
        {
            _script = script;
            gameObject.SetActive(true);

            _backdropButton.onClick.RemoveListener(Continue);
            _backdropButton.onClick.AddListener(Continue);

            _speakerName.text = script.speakerName;
            ShowPortrait(script.portrait);

            bool hasBackground = script.storyBackground != null;
            _background.gameObject.SetActive(hasBackground);
            _shade.gameObject.SetActive(hasBackground);
            if (hasBackground)
            {
                _background.sprite = script.storyBackground;
                // 어떤 화면비에서도 늘리지 않고 잘라 내는 방식으로 화면을 채운다.
                _backgroundFitter.aspectRatio = script.storyBackground.rect.width / script.storyBackground.rect.height;
                // 만화가 시작될 때까지는 투명하다. SetShade를 참고한다.
                _shade.color = Color.clear;
            }

            HideAll();
        }

        // ---------- 공개 API ----------

        /// <summary>
        /// 대화창에 대사 한 줄을 보여 주고, 플레이어가 클릭해 넘기면 끝난다.
        /// <paramref name="expression"/>는 이 대사의 초상화를 바꾸며, null이면 기본 초상화를 보여 준다.
        /// </summary>
        public IEnumerator Say(string text, Sprite expression = null, Sprite storyFrame = null)
        {
            ShowPortrait(expression != null ? expression : _script.portrait);
            ShowStory(storyFrame);

            _hint.SetActive(false);
            _talk.SetActive(true);
            SetBlocking(true);

            _talkText.text = text;
            _talkText.maxVisibleCharacters = 0;
            _talkText.ForceMeshUpdate();
            int total = _talkText.textInfo.characterCount;

            _continueMark.SetActive(false);
            _advance = false;
            _typing = true;

            // 다른 곳의 히트스톱 때문에 대사가 멈추면 안 되므로 unscaled 시간을 쓴다.
            float shown = 0f;
            while (_typing && shown < total)
            {
                shown += Time.unscaledDeltaTime * _script.charactersPerSecond;
                _talkText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }

            _typing = false;
            _talkText.maxVisibleCharacters = total;
            _continueMark.SetActive(true);
            _advance = false;

            while (!_advance)
                yield return null;
        }

        /// <summary>플레이어가 플레이하는 동안 보여 주는 짧은 안내다. 아무것도 막지 않는다.</summary>
        public void ShowHint(string text)
        {
            // 플레이어가 곧 보드에서 행동해야 하므로 어떤 경우에도 보드가 보여야 한다.
            ForceUncover();
            _talk.SetActive(false);
            SetBlocking(false);
            ShowStory(null);

            _hintText.text = text;
            _hint.SetActive(true);
        }

        public void HideAll()
        {
            ForceUncover();
            ShowStory(null);
            _talk.SetActive(false);
            _hint.SetActive(false);
            SetBlocking(false);
        }

        /// <summary>
        /// 스토리 장면을 위해 보드와 HUD를 불투명한 배경 뒤에 숨긴다. 가릴 때는 즉시 가리고,
        /// 걷을 때는 서서히 사라지게 해서 전장이 뚝 끊겨 나타나지 않고 드러나도록 한다.
        /// </summary>
        public void SetBoardCovered(bool covered)
        {
            bool showing = _curtain.gameObject.activeSelf;
            _covered = covered;
            UpdateDim();

            if (covered)
            {
                _curtain.gameObject.SetActive(true);
                _curtain.alpha = 1f;
                return;
            }

            if (showing)
                Fx.FadeOutCurtain(_curtain);
        }

        void ForceUncover()
        {
            _covered = false;
            _curtain.gameObject.SetActive(false);
            UpdateDim();
        }

        /// <summary>스크립트의 일러스트를 보여 주고, 없으면 임시 틀을 보여 준다.</summary>
        void ShowPortrait(Sprite sprite)
        {
            bool hasArt = sprite != null;
            _portrait.gameObject.SetActive(hasArt);
            if (_portraitPlaceholder != null)
                _portraitPlaceholder.SetActive(!hasArt);
            if (hasArt)
                _portrait.sprite = sprite;
        }

        /// <summary>
        /// 대화용 어둡게 처리는 보드를 어둡게 한다. 스토리 배경 위에서는 그림을 묻어 버릴 뿐이므로,
        /// 그때는 배경 자체의 음영만으로 어둡게 한다.
        /// </summary>
        void UpdateDim()
        {
            if (_covered)
                _backdrop.color = Color.clear;
            else
                _backdrop.color = _story.activeSelf ? StoryDim : TalkDim;
        }

        /// <summary>
        /// 만화 한 페이지를 보여 준다. 각 프레임에는 지금까지의 모든 컷이 담겨 있으므로,
        /// 새 프레임을 이전 프레임 위에 겹쳐 서서히 나타나게 하면 다음 컷이 등장하는 것처럼 보인다.
        /// </summary>
        void ShowStory(Sprite frame)
        {
            if (frame == null)
            {
                if (_story.activeSelf)
                    SetShade(false);

                _story.SetActive(false);
                _storyFront.sprite = null;
                UpdateDim();
                return;
            }

            if (_story.activeSelf && _storyFront.sprite == frame)
                return;

            if (!_story.activeSelf)
                SetShade(true);

            var previous = _story.activeSelf ? _storyFront.sprite : null;
            _story.SetActive(true);
            UpdateDim();

            _storyBack.sprite = previous;
            _storyBack.enabled = previous != null;

            _storyFront.sprite = frame;
            Fx.FadeIn(_storyFront);
        }

        /// <summary>만화가 떠 있는 동안 배경을 어둡게 하고, 사라지면 다시 밝힌다.</summary>
        void SetShade(bool on)
        {
            if (_shade != null && _shade.gameObject.activeSelf)
                Fx.FadeTo(_shade, on ? _script.storyBackgroundShade : 0f);
        }

        // ---------- 입력 ----------

        void Update()
        {
            if (!_talk.activeSelf)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                Continue();
        }

        /// <summary>첫 입력은 타이핑을 끝내고, 다음 입력은 다음 대사로 넘어간다.</summary>
        void Continue()
        {
            if (_typing)
                _typing = false;
            else
                _advance = true;
        }

        void SetBlocking(bool blocking)
        {
            if (_blocking == blocking)
                return;

            _blocking = blocking;
            if (blocking)
                InputGate.Push();
            else
                InputGate.Pop();
        }

        void OnDestroy() => SetBlocking(false);
    }
}
