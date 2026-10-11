using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// One buyable offer, read as a picture: a large silhouette says what it is,
    /// a short line says what it does, and a coin says what it costs.
    ///
    /// It knows how it looks and how it reacts to a pointer, but not what it is
    /// selling or whether the player can afford it — the screen owns those
    /// decisions and tells the card which state to wear.
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

        // Faded together when the card is out of reach.
        [SerializeField] List<Graphic> _contents = new();

        Color _accent;
        Vector2 _home;
        bool _hovered;

        /// <summary>Fills a card made from the prefab with one offer, placed at <paramref name="anchoredPosition"/>.</summary>
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

        // ---------- State ----------

        /// <summary>
        /// Out of reach drains the colour out of the card and greys its coin, but
        /// leaves it clickable: a dead card teaches nothing, while a card that
        /// shakes and flashes its price says exactly why the purchase failed.
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

        // ---------- Animation hooks ----------

        public void PlayIn(int index) => UiFx.CardIn(Rect, _group, index);

        public void PlayDeny()
        {
            UiFx.Deny(transform);
            UiFx.FlashColor(_priceLabel, UiTheme.Deny, UiTheme.InkOnAccent);
        }

        // ---------- Pointer ----------

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
