using System;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// 아이콘 중심의 버튼이다. 의미는 기호가 전달하고, 캡션은 버튼 자체가 아니라 그 아래에
    /// 붙는 작은 확인 문구다. Unity의 Button 대신 직접 만든 이유는 게임의 모든 상호작용
    /// 요소가 UiFx를 거쳐 반응하고, 프리셋 에셋 하나로 다시 조정되게 하기 위해서다.
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

        /// <summary>버튼이 쉬는 위치를 기억한다. 호버 시 여기서 들어 올렸다가 여기로 되돌린다.</summary>
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
