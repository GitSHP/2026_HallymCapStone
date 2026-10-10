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
    // [아이템 담당] 상점에 놓이는 카드 한 장의 생김새와 반응. 그림·이름·설명·가격을
    // 받아서 그린다. 카드에 새 정보를 더 보여주고 싶다면 Build 안에 추가한다.
    public class ShopCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public event Action<ShopCard> Clicked;

        public int Cost { get; private set; }
        public bool Sold { get; private set; }
        public bool Affordable { get; private set; } = true;
        public RectTransform Rect { get; private set; }

        CanvasGroup _group;
        Image _face;
        Image _well;
        Image _priceBacking;
        Image _priceCoin;
        TextMeshProUGUI _priceLabel;
        RectTransform _soldOverlay;
        TextMeshProUGUI _soldLabel;

        readonly List<Graphic> _contents = new();

        Color _accent;
        Vector2 _home;
        bool _hovered;

        /// <param name="blurb">카드에 들어갈 한두 줄 설명. 비어 있으면 그림과 이름만 보인다.</param>
        public static ShopCard Create(Transform parent, string objectName, Vector2 anchoredPosition,
            Sprite icon, string title, string blurb, int cost, Color accent)
        {
            var face = UiBuilder.Panel(objectName, parent, UiTheme.Card);
            var rect = face.rectTransform;
            UiBuilder.Place(rect, UiBuilder.Centre, UiBuilder.Centre, anchoredPosition,
                new Vector2(UiTheme.CardWidth, UiTheme.CardHeight));

            var card = face.gameObject.AddComponent<ShopCard>();
            card.Rect = rect;
            card._face = face;
            card._accent = accent;
            card._home = anchoredPosition;
            card.Cost = cost;
            card._group = UiBuilder.Group(face.gameObject);
            card.Build(icon, title, blurb, cost, accent);
            return card;
        }

        void Build(Sprite icon, string title, string blurb, int cost, Color accent)
        {
            // Accent strip: the only place a card family announces itself, so the two
            // rows stay distinguishable without needing two different card shapes.
            var strip = UiBuilder.Panel("AccentStrip", transform, accent);
            UiBuilder.Place(strip.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                new Vector2(0f, -8f), new Vector2(UiTheme.CardWidth - 28f, 6f));
            strip.raycastTarget = false;
            _contents.Add(strip);

            _well = UiBuilder.Panel("IconWell", transform, UiTheme.Well(accent));
            UiBuilder.Place(_well.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                new Vector2(0f, -26f), new Vector2(132f, 116f));
            _well.raycastTarget = false;

            var iconImage = UiBuilder.Icon("Icon", _well.transform, icon, UiTheme.Glyph(accent));
            UiBuilder.Place(iconImage.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                Vector2.zero, new Vector2(84f, 84f));
            _contents.Add(iconImage);

            var titleLabel = UiBuilder.Label("Title", transform, title, 24f, UiTheme.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Place(titleLabel.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                new Vector2(0f, -148f), new Vector2(UiTheme.CardWidth - 32f, 32f));
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _contents.Add(titleLabel);

            BuildBlurb(blurb);
            BuildPrice(cost);
            BuildSoldOverlay();
        }

        /// <summary>
        /// Sits in the same band the chips would occupy, so a card carrying prose
        /// and a card carrying chips are the same shape on the shelf.
        /// </summary>
        void BuildBlurb(string blurb)
        {
            if (string.IsNullOrEmpty(blurb))
                return;

            var label = UiBuilder.Label("Blurb", transform, blurb, 17f, UiTheme.InkMuted,
                TextAlignmentOptions.Top);
            UiBuilder.Place(label.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                new Vector2(0f, -172f), new Vector2(UiTheme.CardWidth - 44f, 50f));
            label.lineSpacing = -8f;
            _contents.Add(label);
        }

        void BuildPrice(int cost)
        {
            _priceBacking = UiBuilder.Panel("Price", transform, UiTheme.Gold);
            UiBuilder.Place(_priceBacking.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(110f, 40f));
            _priceBacking.raycastTarget = false;

            _priceCoin = UiBuilder.Icon("Coin", _priceBacking.transform, IconId.Coin, UiTheme.InkOnAccent);
            UiBuilder.Place(_priceCoin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(22f, 22f));

            _priceLabel = UiBuilder.Label("PriceText", _priceBacking.transform, cost.ToString(), 22f,
                UiTheme.InkOnAccent, TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Place(_priceLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-10f, 0f), new Vector2(56f, 30f));
        }

        void BuildSoldOverlay()
        {
            // Opaque, not translucent: a half-visible card underneath reads as two
            // overlapping images rather than as one bought card.
            var overlay = UiBuilder.Panel("Sold", transform, UiTheme.CardDim);
            _soldOverlay = UiBuilder.Stretch(overlay.rectTransform);
            overlay.raycastTarget = false;

            var tick = UiBuilder.Icon("Tick", overlay.transform, IconId.Check, UiTheme.Confirm);
            UiBuilder.Place(tick.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, 20f), new Vector2(68f, 68f));

            _soldLabel = UiBuilder.Label("SoldText", overlay.transform, "보유 중", 20f, UiTheme.InkFaint,
                TextAlignmentOptions.Center, FontStyles.Bold);
            UiBuilder.Place(_soldLabel.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, -44f), new Vector2(UiTheme.CardWidth - 32f, 28f));
            _soldLabel.characterSpacing = 8f;

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
