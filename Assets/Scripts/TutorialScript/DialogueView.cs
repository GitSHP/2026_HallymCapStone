using System.Collections;
using Promotion.Core;
using Promotion.Feel;
using Promotion.UI;
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
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        const int SortingOrder = 150; // above the HUD, below the shop overlay

        static readonly Color TalkDim = new Color(0f, 0f, 0f, 0.7f);
        // Darker while the comic is up, so the page and not the board holds the eye.
        static readonly Color StoryDim = new Color(0f, 0f, 0f, 0.88f);

        TutorialScript _script;

        GameObject _talk;
        TextMeshProUGUI _talkText;
        GameObject _continueMark;
        Image _portrait;
        Image _backdrop;

        CanvasGroup _curtain;
        Image _shade;
        bool _covered;

        GameObject _story;
        Image _storyBack;
        Image _storyFront;

        GameObject _hint;
        TextMeshProUGUI _hintText;

        bool _typing;
        bool _advance;
        bool _blocking;

        public static DialogueView Create(TutorialScript script)
        {
            var canvas = UiBuilder.OverlayCanvas("TutorialDialogue", SortingOrder);
            var view = canvas.gameObject.AddComponent<DialogueView>();
            view._script = script;
            view.Build((RectTransform)canvas.transform);
            view.HideAll();
            return view;
        }

        // ---------- Public API ----------

        /// <summary>
        /// Shows one line in the talk box and finishes when the player clicks past it.
        /// <paramref name="expression"/> swaps the portrait for this line; null shows the default.
        /// </summary>
        public IEnumerator Say(string text, Sprite expression = null, Sprite storyFrame = null)
        {
            if (_portrait != null)
                _portrait.sprite = expression != null ? expression : _script.portrait;

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
            if (_shade != null)
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

        // ---------- Construction ----------

        void Build(RectTransform root)
        {
            BuildCurtain(root);

            BuildTalk(root);
            BuildHint(root);
        }

        void BuildTalk(RectTransform root)
        {
            // The backdrop is the click target, so a click anywhere continues.
            var backdrop = UiBuilder.Panel("Talk", root, TalkDim, false);
            UiBuilder.Stretch(backdrop.rectTransform);
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Continue);
            _talk = backdrop.gameObject;
            _backdrop = backdrop;

            BuildStory(backdrop.transform);
            BuildPortrait(backdrop.transform);

            var box = UiBuilder.Panel("Box", backdrop.transform, UiTheme.Panel);
            UiBuilder.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(110f, 40f), new Vector2(1100f, 250f));
            box.raycastTarget = false;

            var edge = UiBuilder.Panel("Edge", box.transform, UiTheme.PanelEdge);
            UiBuilder.Stretch(edge.rectTransform, -3f);
            edge.rectTransform.SetAsFirstSibling();
            edge.raycastTarget = false;

            var nameTag = UiBuilder.Panel("NameTag", box.transform, UiTheme.Gold);
            UiBuilder.Place(nameTag.rectTransform, UiBuilder.TopLeft, new Vector2(0f, 0.5f),
                new Vector2(36f, 0f), new Vector2(220f, 52f));
            nameTag.raycastTarget = false;

            var name = UiBuilder.Label("Name", nameTag.transform, _script.speakerName, 26f,
                UiTheme.InkOnAccent, TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Stretch(name.rectTransform);

            _talkText = UiBuilder.Label("Text", box.transform, "", 30f, UiTheme.Ink, TextAlignmentOptions.TopLeft);
            UiBuilder.Stretch(_talkText.rectTransform);
            _talkText.rectTransform.offsetMin = new Vector2(44f, 50f);
            _talkText.rectTransform.offsetMax = new Vector2(-44f, -46f);
            _talkText.lineSpacing = 18f;

            var mark = UiBuilder.Label("Continue", box.transform, "클릭하여 계속  ▼", 20f, UiTheme.InkMuted,
                TextAlignmentOptions.Right);
            UiBuilder.Place(mark.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-36f, 18f), new Vector2(300f, 30f));
            _continueMark = mark.gameObject;
        }

        /// <summary>
        /// The cover for story scenes: the background art under a dark shade. First child,
        /// so it sits under the talk box and the hint but over the HUD and the board.
        /// </summary>
        void BuildCurtain(RectTransform root)
        {
            var cover = UiBuilder.Panel("BoardCover", root, Color.black, false);
            UiBuilder.Stretch(cover.rectTransform);
            cover.raycastTarget = false;

            if (_script.storyBackground != null)
            {
                var art = UiBuilder.Icon("Background", cover.transform, _script.storyBackground, Color.white);
                art.preserveAspect = false;

                // Fill the screen at any aspect ratio, cropping rather than stretching.
                var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = _script.storyBackground.rect.width / _script.storyBackground.rect.height;

                // Clear until the comic starts; see SetShade.
                _shade = UiBuilder.Panel("Shade", cover.transform, Color.clear, false);
                UiBuilder.Stretch(_shade.rectTransform);
                _shade.raycastTarget = false;
            }

            _curtain = UiBuilder.Group(cover.gameObject);
            _curtain.blocksRaycasts = false;
            cover.gameObject.SetActive(false);
        }

        /// <summary>
        /// The comic page, in the space above the dialogue box and right of the portrait.
        /// No frame or backing: unfilled panels are transparent in the art, so the page
        /// simply grows over the dimmed screen.
        /// </summary>
        void BuildStory(Transform parent)
        {
            const float size = 760f;
            var centre = new Vector2(1220f, 680f); // from the bottom-left, reference pixels

            var page = UiBuilder.Node("Story", parent);
            UiBuilder.Place(page, Vector2.zero, UiBuilder.Centre, centre, Vector2.one * size);
            _story = page.gameObject;

            _storyBack = UiBuilder.Icon("Previous", page, (Sprite)null, Color.white);
            UiBuilder.Stretch(_storyBack.rectTransform);

            _storyFront = UiBuilder.Icon("Current", page, (Sprite)null, Color.white);
            UiBuilder.Stretch(_storyFront.rectTransform);

            _story.SetActive(false);
        }

        void BuildPortrait(Transform parent)
        {
            // Upper-body art is roughly 2:3; the box covers the bottom of it, visual-novel style.
            var size = new Vector2(500f, 730f);
            var anchor = Vector2.zero;
            var position = new Vector2(40f, 0f);

            if (_script.portrait != null)
            {
                _portrait = UiBuilder.Icon("Portrait", parent, _script.portrait, Color.white);
                UiBuilder.Place(_portrait.rectTransform, anchor, anchor, position, size);
                return;
            }

            // Placeholder until the illustration exists: a frame the art will fill.
            var frame = UiBuilder.Panel("Portrait", parent, UiTheme.Card);
            UiBuilder.Place(frame.rectTransform, anchor, anchor, position, size);
            frame.raycastTarget = false;

            var silhouette = UiBuilder.Icon("Silhouette", frame.transform, PlaceholderArt.Circle,
                UiTheme.CardWell);
            UiBuilder.Place(silhouette.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                new Vector2(0f, -90f), new Vector2(200f, 200f));

            var shoulders = UiBuilder.Panel("Shoulders", frame.transform, UiTheme.CardWell);
            UiBuilder.Place(shoulders.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 0f), new Vector2(340f, 300f));
            shoulders.raycastTarget = false;

            var note = UiBuilder.Label("Note", frame.transform, "일러스트 준비 중", 22f, UiTheme.InkMuted);
            UiBuilder.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 320f), new Vector2(400f, 34f));
        }

        void BuildHint(RectTransform root)
        {
            var panel = UiBuilder.Panel("Hint", root, new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.92f));
            // Below the board, between the AP panel and the action button: the top
            // rows are where enemies arrive, so the hint must never cover them.
            UiBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 96f), new Vector2(1000f, 76f));
            panel.raycastTarget = false;
            _hint = panel.gameObject;

            var accent = UiBuilder.Panel("Accent", panel.transform, UiTheme.Gold);
            UiBuilder.Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(8f, 76f));
            accent.raycastTarget = false;

            _hintText = UiBuilder.Label("Text", panel.transform, "", 26f, UiTheme.Ink);
            UiBuilder.Stretch(_hintText.rectTransform);
            _hintText.rectTransform.offsetMin = new Vector2(30f, 6f);
            _hintText.rectTransform.offsetMax = new Vector2(-30f, -6f);
        }
    }
}
