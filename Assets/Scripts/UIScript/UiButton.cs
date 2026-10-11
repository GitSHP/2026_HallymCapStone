using System;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// An icon-led button: the symbol carries the meaning and the caption is a
    /// small confirmation underneath it, not the button itself. Built here rather
    /// than on Unity's Button so every interactive surface in the game reacts
    /// through UiFx and retunes from the one presets asset.
    /// </summary>
    public class UiButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public event Action Clicked;

        public RectTransform Rect => (RectTransform)transform;

        [SerializeField] Image _face;
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _label;
        [SerializeField] Color _baseColor = Color.white;

        Vector2 _home;
        bool _homeKnown;
        bool _hovered;
        bool _interactable = true;

        void Awake() => RememberHome();

        /// <summary>Where the button rests; hover lifts it from here and returns it here.</summary>
        void RememberHome()
        {
            if (_homeKnown)
                return;
            _home = Rect.anchoredPosition;
            _homeKnown = true;
        }

        public void SetCaption(string text)
        {
            if (_label != null)
                _label.text = text;
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            _face.color = interactable ? _baseColor : UiTheme.Disabled;

            var content = interactable ? UiTheme.InkOnAccent : UiTheme.InkMuted;
            if (_label != null) _label.color = content;
            if (_icon != null) _icon.color = content;
        }

        public void PlayIn(int index)
        {
            RememberHome();
            Rect.anchoredPosition = _home;
            UiFx.CardIn(Rect, UiBuilder.Group(gameObject), index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _hovered = true;
            RememberHome();
            UiFx.HoverIn(Rect, _home);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _hovered = false;
            RememberHome();
            UiFx.HoverOut(Rect, _home);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_interactable)
                UiFx.Press(transform);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_interactable)
                UiFx.Release(transform, _hovered);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            UiFx.Punch(transform);
            Clicked?.Invoke();
        }
    }
}
