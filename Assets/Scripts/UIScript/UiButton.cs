using System;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    public class UiButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    // 마우스를 올리면 떠오르고, 누르면 눌리고, 클릭하면 튀는 연출이 들어간 공용 버튼 클래스
    // 버튼 모양은 프리팹에서 만들고, 이 스크립트는 연출과 클릭 이벤트 전달만 담당함
    {
        public event Action Clicked;  // 버튼이 클릭됐을 때 실행할 함수들을 등록하는 이벤트 -> 상점·타이틀 화면 등에서 구독함

        public RectTransform Rect => (RectTransform)transform;  // 이 버튼의 RectTransform을 바로 꺼내 쓰기 위한 속성

        [SerializeField] Image _face;  // 버튼 바탕 이미지 (색이 바뀌는 부분)
        [SerializeField] Image _icon;  // 버튼 안의 아이콘 이미지 (없어도 됨)
        [SerializeField] TextMeshProUGUI _label;  // 버튼 글자를 표시하는 텍스트 UI (없어도 됨)
        [SerializeField] Color _baseColor = Color.white;  // 누를 수 있는 상태일 때 버튼 바탕 색

        Vector2 _home;  // 버튼의 원래 위치 -> 마우스를 올려 떠올랐다가 돌아올 기준점
        bool _homeKnown;  // 원래 위치를 이미 기억했는지 여부
        bool _hovered;  // 현재 마우스가 버튼 위에 올라와 있는지 여부
        bool _interactable = true;  // 지금 버튼을 누를 수 있는 상태인지 여부

        void Awake() => RememberHome();  // 시작할 때 버튼의 원래 위치를 기억해 둠

        void RememberHome()  // 버튼의 원래 위치를 한 번만 기억하는 함수
        {
            if (_homeKnown)  // 만약 이미 기억했다면 -> 다시 저장하지 않음 (떠오른 위치를 원래 위치로 착각하지 않도록)
                return;
            _home = Rect.anchoredPosition;  // 현재 위치를 원래 위치로 저장하고
            _homeKnown = true;  // 기억했다고 표시함
        }

        public void SetCaption(string text)  // 버튼 글자를 바꾸는 함수
        {
            if (_label != null)  // 텍스트 UI가 연결되어 있을 때만 변경
                _label.text = text;
        }

        public void SetInteractable(bool interactable)  // 버튼을 누를 수 있는 상태/없는 상태로 바꾸는 함수
        {
            _interactable = interactable;
            _face.color = interactable ? _baseColor : UiTheme.Disabled;  // 누를 수 있으면 원래 색, 없으면 회색 계열로 바탕 색을 바꿈

            var content = interactable ? UiTheme.InkOnAccent : UiTheme.InkMuted;  // 글자·아이콘 색도 상태에 맞게 고르고
            if (_label != null) _label.color = content;  // 글자 색 적용
            if (_icon != null) _icon.color = content;  // 아이콘 색 적용
        }

        public void PlayIn(int index)  // 버튼이 화면에 등장하는 연출을 재생하는 함수 (index 순서만큼 늦게 등장)
        {
            RememberHome();
            Rect.anchoredPosition = _home;  // 혹시 떠올라 있던 위치를 원래 위치로 되돌린 뒤
            UiFx.CardIn(Rect, UiBuilder.Group(gameObject), index);  // 아래에서 올라오며 나타나는 연출 재생
        }

        // 마우스가 버튼 위에 올라왔을 때 발생하는 이벤트
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable)  // 만약 누를 수 없는 버튼이라면 -> 연출 없이 무시
                return;

            _hovered = true;  // 마우스가 올라와 있다고 표시하고
            RememberHome();
            UiFx.HoverIn(Rect, _home);  // 버튼을 살짝 키우고 위로 띄움
        }

        // 마우스가 버튼 밖으로 나갔을 때 발생하는 이벤트
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _hovered = false;  // 마우스가 벗어났다고 표시하고
            RememberHome();
            UiFx.HoverOut(Rect, _home);  // 버튼을 원래 크기·위치로 되돌림
        }

        // 마우스 버튼을 눌렀을 때 발생하는 이벤트
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_interactable)
                UiFx.Press(transform);  // 버튼이 눌린 것처럼 작아지는 연출
        }

        // 마우스 버튼을 뗐을 때 발생하는 이벤트
        public void OnPointerUp(PointerEventData eventData)
        {
            if (_interactable)
                UiFx.Release(transform, _hovered);  // 원래 크기로 복귀 -> 아직 마우스가 위에 있으면 떠오른 크기로 복귀
        }

        // 버튼이 클릭(누르고 같은 버튼 위에서 뗌)됐을 때 발생하는 이벤트
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)  // 만약 누를 수 없는 버튼이라면 -> 클릭 무시
                return;

            UiFx.Punch(transform);  // 버튼이 톡 튀는 연출을 재생하고
            Clicked?.Invoke();  // 등록된 클릭 함수들을 실행함 (등록된 게 없으면 아무것도 안 함)
        }
    }
}
