using System;
using System.Collections.Generic;
using Gambonanza.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gambonanza.UI
{
    /// <summary>
    /// One buyable offer, read as a picture: a large silhouette says what it is,
    /// a row of symbol-and-number chips says what it does, and a coin says what
    /// it costs. The only prose on the card is its name.
    ///
    /// It knows how it looks and how it reacts to a pointer, but not what it is
    /// selling or whether the player can afford it — the screen owns those
    /// decisions and tells the card which state to wear.
    /// </summary>
    public class ShopCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public event Action<ShopCard> Clicked;

        public int Cost { get; private set; }
        public bool Sold { get; private set; }
        public bool Affordable { get; private set; } = true;
        public RectTransform Rect { get; private set; }

        /// <summary>At most three chips fit across a card before they stop being glanceable.</summary>
        public const int MaxChips = 3;

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

        /// <param name="chips">Symbol-and-number effects. Null or empty falls back to <paramref name="blurb"/>.</param>
        /// <param name="blurb">A short sentence, used where an effect has no number to show.</param>
        public static ShopCard Create(Transform parent, string objectName, Vector2 anchoredPosition,
            Sprite icon, string title, IReadOnlyList<EffectChip> chips, string blurb, int cost, Color accent)
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
            card.Build(icon, title, chips, blurb, cost, accent);
            return card;
        }

        void Build(Sprite icon, string title, IReadOnlyList<EffectChip> chips, string blurb,
            int cost, Color accent)
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

            if (chips != null && chips.Count > 0)
                BuildChips(chips, accent);
            else
                BuildBlurb(blurb);

            BuildPrice(cost);
            BuildSoldOverlay();
        }

        void BuildChips(IReadOnlyList<EffectChip> chips, Color accent)
        {
            if (chips == null || chips.Count == 0)
                return;

            int count = Mathf.Min(chips.Count, MaxChips);
            const float width = 80f;
            const float gap = 8f;
            float step = width + gap;
            float offset = (count - 1) * 0.5f * step;

            for (int i = 0; i < count; i++)
            {
                var chip = chips[i];

                var backing = UiBuilder.Panel("Chip_" + i, transform, UiTheme.CardWell);
                UiBuilder.Place(backing.rectTransform, UiBuilder.TopCentre, UiBuilder.TopCentre,
                    new Vector2(i * step - offset, -186f), new Vector2(width, 34f));
                backing.raycastTarget = false;
                _contents.Add(backing);

                var symbol = UiBuilder.Icon("Chip_" + i + "_Icon", backing.transform, chip.icon,
                    UiTheme.Glyph(accent));
                UiBuilder.Place(symbol.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(10f, 0f), new Vector2(20f, 20f));
                _contents.Add(symbol);

                var value = UiBuilder.Label("Chip_" + i + "_Value", backing.transform, chip.value, 19f,
                    UiTheme.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
                UiBuilder.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-8f, 0f), new Vector2(width - 34f, 26f));
                value.textWrappingMode = TextWrappingModes.NoWrap;
                _contents.Add(value);
            }
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

            _soldLabel = UiBuilder.Label("SoldText", overlay.transform, "OWNED", 20f, UiTheme.InkFaint,
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

        public void MarkSold(string label = "OWNED")
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
