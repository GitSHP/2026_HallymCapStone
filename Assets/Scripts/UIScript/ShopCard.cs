using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// 구매할 수 있는 상품 하나를 그림처럼 읽히게 보여준다. 큰 실루엣이 무엇인지를,
    /// 짧은 문장이 무엇을 하는지를, 동전이 얼마인지를 알려준다.
    ///
    /// 카드는 자신의 생김새와 포인터에 대한 반응만 알고, 무엇을 파는지나 플레이어가
    /// 살 수 있는지는 모른다. 그런 판단은 화면이 맡고, 카드에는 어떤 상태를 보여줄지만 알려준다.
    /// </summary>
    // [아이템 담당] 상점에 놓이는 카드 한 장의 생김새와 반응. 생김새는 프리팹
    // (Assets/UI/Prefabs/ShopCard)에 있고, 그림·이름·설명·가격은 Setup 에서 채운다.
    // 카드에 새 정보를 더 보여주고 싶다면 프리팹에 요소를 추가하고 여기서 연결한다.
    public class ShopCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public event Action<ShopCard> Clicked;

        public int Cost { get; private set; }
        public bool Sold { get; private set; }
        public bool Affordable { get; private set; } = true;
        public RectTransform Rect => (RectTransform)transform;

        [SerializeField] CanvasGroup _group;
        [SerializeField] Image _face;
        [SerializeField] Image _strip;
        [SerializeField] Image _well;
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _blurb;
        [SerializeField] Image _priceBacking;
        [SerializeField] Image _priceCoin;
        [SerializeField] TextMeshProUGUI _priceLabel;
        [SerializeField] RectTransform _soldOverlay;
        [SerializeField] TextMeshProUGUI _soldLabel;

        // 카드를 살 수 없을 때 함께 흐려지는 요소들이다.
        [SerializeField] List<Graphic> _contents = new();

        Color _accent;
        Vector2 _home;
        bool _hovered;

        /// <summary>프리팹으로 만든 카드에 상품 하나를 채우고 <paramref name="anchoredPosition"/>에 배치한다.</summary>
        public void Setup(Vector2 anchoredPosition, Sprite icon, string title, string blurb, int cost, Color accent)
        {
            Rect.anchoredPosition = anchoredPosition;
            _home = anchoredPosition;
            _accent = accent;
            Cost = cost;
            Sold = false;

            _strip.color = accent;
            _well.color = UiTheme.Well(accent);
            _icon.sprite = icon;
            _icon.color = UiTheme.Glyph(accent);
            _title.text = title;
            _blurb.text = blurb ?? "";
            _blurb.gameObject.SetActive(!string.IsNullOrEmpty(blurb));
            _priceLabel.text = cost.ToString();
            _soldOverlay.gameObject.SetActive(false);
        }

        // ---------- 상태 ----------

        /// <summary>
        /// 살 수 없는 상태가 되면 카드의 색을 빼고 동전을 회색으로 바꾸지만, 클릭은 그대로
        /// 받는다. 반응 없는 카드는 아무것도 알려주지 못하지만, 흔들리며 가격을 번쩍이는
        /// 카드는 구매가 왜 실패했는지 정확히 알려준다.
        /// </summary>
        public void SetAffordable(bool affordable)
        {
            if (Sold)
                return;

            Affordable = affordable;
            _face.color = affordable ? UiTheme.Card : UiTheme.CardDim;
            _well.color = affordable ? UiTheme.Well(_accent) : UiTheme.Well(UiTheme.InkFaint);
            _priceBacking.color = affordable ? UiTheme.Gold : UiTheme.Disabled;
            _priceLabel.color = affordable ? UiTheme.InkOnAccent : UiTheme.InkMuted;
            _priceCoin.color = affordable ? UiTheme.InkOnAccent : UiTheme.InkMuted;

            float fade = affordable ? 1f : 0.4f;
            foreach (var graphic in _contents)
            {
                if (graphic == null)
                    continue;

                var c = graphic.color;
                c.a = fade;
                graphic.color = c;
            }
        }

        public void MarkSold(string label = "보유 중")
        {
            if (Sold)
                return;

            Sold = true;
            _hovered = false;
            _soldLabel.text = label;
            _soldOverlay.gameObject.SetActive(true);

            UiFx.Punch(transform);
            UiFx.HoverOut(Rect, _home);
        }

        // ---------- 애니메이션 진입점 ----------

        public void PlayIn(int index) => UiFx.CardIn(Rect, _group, index);

        public void PlayDeny()
        {
            UiFx.Deny(transform);
            UiFx.FlashColor(_priceLabel, UiTheme.Deny, UiTheme.InkOnAccent);
        }

        // ---------- 포인터 ----------

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Sold)
                return;

            _hovered = true;
            UiFx.HoverIn(Rect, _home);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Sold)
                return;

            _hovered = false;
            UiFx.HoverOut(Rect, _home);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Sold)
                UiFx.Press(transform);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Sold)
                UiFx.Release(transform, _hovered);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Sold)
                Clicked?.Invoke(this);
        }
    }
}
