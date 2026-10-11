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
    /// The tutorial's voice. Two modes:
    /// <list type="bullet">
    /// <item>Talk — a visual-novel box with the speaker's portrait. The screen dims
    /// and the board is locked until the player clicks through.</item>
    /// <item>Hint — one compact line under the board while the player acts. Nothing is
    /// blocked, because doing the thing is the lesson.</item>
    /// </list>
    /// The layout is a prefab placed (switched off) in the battle scene; the
    /// tutorial switches it on and fills in the speaker from its script.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        static readonly Color TalkDim = new Color(0f, 0f, 0f, 0.7f);
        // Darker while the comic is up, so the page and not the board holds the eye.
        static readonly Color StoryDim = new Color(0f, 0f, 0f, 0.88f);

        [Header("Talk")]
        [SerializeField] GameObject _talk;
        [SerializeField] Image _backdrop;
        [SerializeField] Button _backdropButton;
        [SerializeField] TextMeshProUGUI _speakerName;
        [SerializeField] TextMeshProUGUI _talkText;
        [SerializeField] GameObject _continueMark;
        [SerializeField] Image _portrait;
        [Tooltip("Shown instead of the portrait while the script has no illustration.")]
        [SerializeField] GameObject _portraitPlaceholder;

        [Header("Board cover")]
        [SerializeField] CanvasGroup _curtain;
        [SerializeField] Image _background;
        [SerializeField] AspectRatioFitter _backgroundFitter;
        [SerializeField] Image _shade;

        [Header("Story")]
        [SerializeField] GameObject _story;
        [SerializeField] Image _storyBack;
        [SerializeField] Image _storyFront;

        [Header("Hint")]
        [SerializeField] GameObject _hint;
        [SerializeField] TextMeshProUGUI _hintText;

        TutorialScript _script;
        bool _covered;
        bool _typing;
        bool _advance;
        bool _blocking;

        /// <summary>Switches the view on for a tutorial and fills in its speaker and art.</summary>
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
                // Fill the screen at any aspect ratio, cropping rather than stretching.
                _backgroundFitter.aspectRatio = script.storyBackground.rect.width / script.storyBackground.rect.height;
                // Clear until the comic starts; see SetShade.
                _shade.color = Color.clear;
            }

            HideAll();
        }

        // ---------- Public API ----------

        /// <summary>
        /// Shows one line in the talk box and finishes when the player clicks past it.
        /// <paramref name="expression"/> swaps the portrait for this line; null shows the default.
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

            // Unscaled: a hit-stop elsewhere must not freeze the dialogue.
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

        /// <summary>A compact instruction while the player plays. Does not block anything.</summary>
        public void ShowHint(string text)
        {
            // The player is about to act on the board, so it must be visible no matter what.
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
        /// Hides the board and HUD behind a solid backdrop for story scenes. Covering is
        /// instant; uncovering fades, so the battlefield is revealed rather than cut to.
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

        /// <summary>The script's illustration, or the placeholder frame while it has none.</summary>
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
        /// The talk dim darkens the board. Over the story background it would only bury
        /// the art, so there the background's own shade is the only darkening.
        /// </summary>
        void UpdateDim()
        {
            if (_covered)
                _backdrop.color = Color.clear;
            else
                _backdrop.color = _story.activeSelf ? StoryDim : TalkDim;
        }

        /// <summary>
        /// Shows one page of the comic. Each frame holds every panel so far, so laying
        /// the new frame over the old and fading it in reads as the next panel appearing.
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

        /// <summary>The background darkens while the comic is up and clears once it is gone.</summary>
        void SetShade(bool on)
        {
            if (_shade != null && _shade.gameObject.activeSelf)
                Fx.FadeTo(_shade, on ? _script.storyBackgroundShade : 0f);
        }

        // ---------- Input ----------

        void Update()
        {
            if (!_talk.activeSelf)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                Continue();
        }

        /// <summary>First press finishes the typing, the next one moves on.</summary>
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
