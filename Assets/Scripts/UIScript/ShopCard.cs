using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    public class ShopCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    // 상점에 진열되는 카드 한 장(기물 또는 유물)을 담당하는 클래스
    // -> 카드 모양은 프리팹(Assets/UI/Prefabs)에 있고, 이 스크립트는 내용 채우기·상태 표시·마우스 연출만 담당함
    // -> 실제 구매 처리는 하지 않고 클릭 사실만 ShopScreen에 알림
    {
        public event Action<ShopCard> Clicked;  // 카드가 클릭됐을 때 ShopScreen에 알리는 이벤트 -> 어떤 카드인지 자신을 함께 넘겨줌

        public int Cost { get; private set; }  // 이 카드의 가격(골드)
        public bool Sold { get; private set; }  // 이미 구매(또는 보유)되어 더 이상 살 수 없는 카드인지 여부
        public bool Affordable { get; private set; } = true;  // 현재 보유 골드로 살 수 있는지 여부
        public RectTransform Rect => (RectTransform)transform;  // 이 카드의 RectTransform을 바로 꺼내 쓰기 위한 속성

        [SerializeField] CanvasGroup _group;  // 카드 전체의 투명도를 조절할 때 사용할 변수 (등장 연출용)
        [SerializeField] Image _face;  // 카드 바탕 이미지
        [SerializeField] Image _strip;  // 카드 위쪽의 강조색 띠 (기물: 하늘색 / 유물: 보라색)
        [SerializeField] Image _well;  // 아이콘 뒤의 옅은 강조색 배경
        [SerializeField] Image _icon;  // 기물·유물 아이콘 이미지
        [SerializeField] TextMeshProUGUI _title;  // 이름을 표시하는 텍스트 UI
        [SerializeField] TextMeshProUGUI _blurb;  // 짧은 설명을 표시하는 텍스트 UI
        [SerializeField] Image _priceBacking;  // 가격 표시 부분의 바탕 이미지 (금색)
        [SerializeField] Image _priceCoin;  // 가격 옆의 동전 아이콘
        [SerializeField] TextMeshProUGUI _priceLabel;  // 가격 숫자를 표시하는 텍스트 UI
        [SerializeField] RectTransform _soldOverlay;  // 구매 완료 시 카드 위에 덮이는 표시 (평소에는 꺼져 있음)
        [SerializeField] TextMeshProUGUI _soldLabel;  // 구매 완료 표시에 들어가는 글자 ("보유 중" 등)

        [SerializeField] List<Graphic> _contents = new();  // 골드가 부족할 때 흐리게 만들 그래픽 목록 (아이콘·이름·설명 등, 인스펙터에서 등록)

        Color _accent;  // 이 카드의 강조색 -> 살 수 있는 상태로 돌아올 때 원래 색을 복구하기 위해 기억
        Vector2 _home;  // 카드의 원래 위치 -> 마우스를 올려 떠올랐다가 돌아올 기준점
        bool _hovered;  // 현재 마우스가 카드 위에 올라와 있는지 여부

        public void Setup(Vector2 anchoredPosition, Sprite icon, string title, string blurb, int cost, Color accent)
        // 카드의 위치와 표시 내용(아이콘·이름·설명·가격·강조색)을 채우는 함수 -> ShopScreen이 상품을 진열할 때 호출함
        {
            Rect.anchoredPosition = anchoredPosition;  // 카드를 지정한 위치에 놓고
            _home = anchoredPosition;  // 그 위치를 원래 위치로 기억
            _accent = accent;
            Cost = cost;
            Sold = false;  // 새로 진열하는 카드이므로 판매 전 상태로 초기화

            _strip.color = accent;  // 위쪽 띠를 강조색으로 칠하고
            _well.color = UiTheme.Well(accent);  // 아이콘 배경은 강조색을 옅게 해서 칠함
            _icon.sprite = icon;
            _icon.color = UiTheme.Glyph(accent);  // 아이콘은 강조색보다 조금 밝게
            _title.text = title;
            _blurb.text = blurb ?? "";  // 설명이 null이면 빈 문자열로 처리
            _blurb.gameObject.SetActive(!string.IsNullOrEmpty(blurb));  // 설명이 없으면 설명 텍스트 UI 자체를 숨김
            _priceLabel.text = cost.ToString();  // 가격 숫자 표시
            _soldOverlay.gameObject.SetActive(false);  // 구매 완료 표시는 꺼 둠
        }

        public void SetAffordable(bool affordable)  // 보유 골드로 살 수 있는지에 따라 카드 모양을 바꾸는 함수
        {
            if (Sold)  // 만약 이미 판매된 카드라면 -> 모양을 바꾸지 않음 (구매 완료 표시 유지)
                return;

            Affordable = affordable;
            _face.color = affordable ? UiTheme.Card : UiTheme.CardDim;  // 살 수 없으면 카드 바탕을 어둡게
            _well.color = affordable ? UiTheme.Well(_accent) : UiTheme.Well(UiTheme.InkFaint);  // 아이콘 배경도 강조색 대신 회색 계열로
            _priceBacking.color = affordable ? UiTheme.Gold : UiTheme.Disabled;  // 가격 바탕을 금색 → 회색으로
            _priceLabel.color = affordable ? UiTheme.InkOnAccent : UiTheme.InkMuted;  // 가격 글자 색 변경
            _priceCoin.color = affordable ? UiTheme.InkOnAccent : UiTheme.InkMuted;  // 동전 아이콘 색 변경

            float fade = affordable ? 1f : 0.4f;  // 살 수 없으면 내용물을 40% 투명도로 흐리게 함
            foreach (var graphic in _contents)  // 등록된 모든 그래픽에 대해
            {
                if (graphic == null)  // 비어 있는 칸은 건너뜀 -> 인스펙터에서 빠진 항목이 있어도 에러 방지
                    continue;

                var c = graphic.color;  // 기존 색은 그대로 두고
                c.a = fade;  // 투명도만 바꿔서
                graphic.color = c;  // 다시 적용
            }
        }

        public void MarkSold(string label = "보유 중")  // 카드를 판매 완료 상태로 바꾸는 함수 -> 구매 직후 또는 이미 가진 유물일 때 호출
        {
            if (Sold)  // 만약 이미 판매 완료 상태라면 -> 연출이 두 번 나오지 않도록 무시
                return;

            Sold = true;  // 판매 완료로 표시하고
            _hovered = false;
            _soldLabel.text = label;  // 덮개에 표시할 글자를 넣고
            _soldOverlay.gameObject.SetActive(true);  // 구매 완료 덮개를 켬

            UiFx.Punch(transform);  // 카드가 톡 튀는 연출
            UiFx.HoverOut(Rect, _home);  // 떠올라 있던 카드를 원래 위치로 내림
        }

        public void PlayIn(int index) => UiFx.CardIn(Rect, _group, index);  // 카드 등장 연출 함수 -> index 순서대로 차례차례 나타남

        public void PlayDeny()  // 구매할 수 없을 때(골드 부족) 거절 연출을 재생하는 함수
        {
            UiFx.Deny(transform);  // 카드를 좌우로 흔들고
            UiFx.FlashColor(_priceLabel, UiTheme.Deny, UiTheme.InkOnAccent);  // 가격 글자를 빨갛게 번쩍였다가 원래 색으로 되돌림
        }

        // 마우스가 카드 위에 올라왔을 때 발생하는 이벤트
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Sold)  // 만약 판매 완료된 카드라면 -> 연출 없음
                return;

            _hovered = true;
            UiFx.HoverIn(Rect, _home);  // 카드를 살짝 키우고 위로 띄움
        }

        // 마우스가 카드 밖으로 나갔을 때 발생하는 이벤트
        public void OnPointerExit(PointerEventData eventData)
        {
            if (Sold)
                return;

            _hovered = false;
            UiFx.HoverOut(Rect, _home);  // 카드를 원래 크기·위치로 되돌림
        }

        // 마우스 버튼을 눌렀을 때 발생하는 이벤트
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Sold)  // 판매 전 카드일 때만
                UiFx.Press(transform);  // 눌린 것처럼 작아지는 연출
        }

        // 마우스 버튼을 뗐을 때 발생하는 이벤트
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Sold)
                UiFx.Release(transform, _hovered);  // 원래 크기로 복귀 (마우스가 위에 있으면 떠오른 크기로)
        }

        // 카드가 클릭됐을 때 발생하는 이벤트
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Sold)  // 판매 전 카드일 때만
                Clicked?.Invoke(this);  // ShopScreen에 이 카드가 클릭됐다고 알림 -> 골드 확인·구매 처리는 ShopScreen이 함
        }
    }
}
