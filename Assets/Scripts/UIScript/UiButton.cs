using System;
using Gambonanza.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.UI
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

        public RectTransform Rect { get; private set; }

        Image _face;
        Image _icon;
        TextMeshProUGUI _label;
        Color _baseColor;
        Vector2 _home;
        bool _hovered;
        bool _interactable = true;

        /// <summary>A square icon button with no caption at all — used for close and back.</summary>
        public static UiButton CreateIcon(Transform parent, string objectName, IconId icon,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, float size,
            Color face, Color iconColor)
        {
            var button = Shell(parent, objectName, anchor, pivot, anchoredPosition,
                new Vector2(size, size), face);

            button._icon = UiBuilder.Icon("Icon", button.transform, icon, iconColor);
            UiBuilder.Place(button._icon.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                Vector2.zero, Vector2.one * (size * 0.5f));

            return button;
        }

        /// <summary>An icon with a short caption beside it, for the one action that ends a screen.</summary>
        public static UiButton CreateAction(Transform parent, string objectName, IconId icon, string caption,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size,
            Color face, Color contentColor, float fontSize = 22f)
        {
            var button = Shell(parent, objectName, anchor, pivot, anchoredPosition, size, face);

            button._label = UiBuilder.Label("Caption", button.transform, caption, fontSize, contentColor,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UiBuilder.Place(button._label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(30f, 0f), new Vector2(size.x - 90f, 30f));
            button._label.characterSpacing = 6f;
            button._label.textWrappingMode = TextWrappingModes.NoWrap;

            button._icon = UiBuilder.Icon("Icon", button.transform, icon, contentColor);
            UiBuilder.Place(button._icon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-26f, 0f), Vector2.one * (size.y * 0.46f));

            return button;
        }

        static UiButton Shell(Transform parent, string objectName, Vector2 anchor, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size, Color face)
        {
            var image = UiBuilder.Panel(objectName, parent, face);
            UiBuilder.Place(image.rectTransform, anchor, pivot, anchoredPosition, size);

            var button = image.gameObject.AddComponent<UiButton>();
            button.Rect = image.rectTransform;
            button._face = image;
            button._baseColor = face;
            button._home = anchoredPosition;
            return button;
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
            => UiFx.CardIn(Rect, UiBuilder.Group(gameObject), index);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _hovered = true;
            UiFx.HoverIn(Rect, _home);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _hovered = false;
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
