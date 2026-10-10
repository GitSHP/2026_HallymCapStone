using System;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Promotion.UI
{
    /// <summary>
    /// The between-stages shop. Drawn as an overlay on top of the live board
    /// rather than in its own scene, so the player never loses sight of the
    /// position they just won — the purchase is read against that board.
    /// The whole screen is built in code and destroyed on confirm; nothing of it
    /// persists but the purchases, which live on the RunState.
    /// </summary>
    // [아이템 담당] 상점 화면 전체. 유물 줄은 ShopPool.artifactOffers 를, 기물 줄은
    // ShopPool.pieces 를 읽어 카드로 만든다. 구매 처리는 아래 OnCardClicked 에 있다.
    public class ShopScreen : MonoBehaviour
    {
        /// <summary>Only one shop can be up at a time; a second clear must not stack a second screen.</summary>
        public static ShopScreen Current { get; private set; }

        Action _onConfirm;
        RunState _run;

        RectTransform _panel;
        CanvasGroup _panelGroup;
        CanvasGroup _backdropGroup;
        RectTransform _walletChip;
        TextMeshProUGUI _walletLabel;
        UiButton _confirmButton;

        readonly List<ShopCard> _cards = new();
        readonly Dictionary<ShopCard, ItemData> _artifactOf = new();
        readonly Dictionary<ShopCard, PieceDefinition> _pieceOf = new();

        readonly List<ShopOffer> _artifactOffer = new();
        readonly List<PieceDefinition> _pieceOffer = new();

        bool _closing;

        // Row geometry inside the panel, measured from its middle. Each row is a
        // label sitting directly above its cards, with the header above both and
        // the confirm button below them.
        const float ArtifactLabelY = 310f;
        const float ArtifactRowY = 144f;
        const float PieceLabelY = -26f;
        const float PieceRowY = -192f;

        public static ShopScreen Open(ShopPool pool, string headline, Action onConfirm)
        {
            if (Current != null)
                return Current;

            EnsureEventSystem();

            var canvas = UiBuilder.OverlayCanvas("ShopScreen", UiTheme.OverlaySortingOrder);
            var screen = canvas.gameObject.AddComponent<ShopScreen>();
            screen._onConfirm = onConfirm;
            screen.BuildAndShow(canvas, pool, headline);

            Current = screen;
            return screen;
        }

        void BuildAndShow(Canvas canvas, ShopPool pool, string headline)
        {
            _run = RunState.Instance;

            // The board reads the pointer directly, so the overlay has to say
            // explicitly that the world below it is not being played right now.
            InputGate.Push();

            BuildBackdrop(canvas.transform);
            BuildPanel(canvas.transform, headline);
            BuildOffers(pool);

            _run.GoldChanged += OnGoldChanged;
            RefreshAffordability();
            PlayIntro();
        }

        // ---------- Construction ----------

        void BuildBackdrop(Transform parent)
        {
            var backdrop = UiBuilder.Panel("Backdrop", parent, UiTheme.Backdrop, false);
            UiBuilder.Stretch(backdrop.rectTransform);
            // Raycast target left on: it swallows clicks that miss the panel,
            // which is what stops a stray click from reaching the board.
            _backdropGroup = UiBuilder.Group(backdrop.gameObject);
        }

        void BuildPanel(Transform parent, string headline)
        {
            // A one-pixel-lighter plate behind the panel reads as an edge without
            // needing an outline sprite, which a 9-sliced rounded rect cannot carry.
            var edge = UiBuilder.Panel("PanelEdge", parent, UiTheme.PanelEdge);
            UiBuilder.Place(edge.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                Vector2.zero, new Vector2(UiTheme.PanelWidth + 4f, UiTheme.PanelHeight + 4f));
            edge.raycastTarget = false;

            var panel = UiBuilder.Panel("Panel", parent, UiTheme.Panel);
            _panel = UiBuilder.Place(panel.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                Vector2.zero, new Vector2(UiTheme.PanelWidth, UiTheme.PanelHeight));
            _panelGroup = UiBuilder.Group(panel.gameObject);

            // The edge follows the panel so the entrance scales as one object.
            edge.transform.SetParent(_panel, true);
            edge.transform.SetAsFirstSibling();

            BuildHeader(headline);
            BuildWallet();
            BuildRowLabel("ArtifactLabel", IconId.Crown, "유물", UiTheme.ArtifactAccent, ArtifactLabelY);
            BuildRowLabel("PieceLabel", IconId.Chevrons, "기물", UiTheme.PieceAccent, PieceLabelY);
            BuildConfirmButton();
        }

        void BuildHeader(string headline)
        {
            var mark = UiBuilder.Panel("HeaderMark", _panel, UiTheme.Well(UiTheme.Confirm));
            UiBuilder.Place(mark.rectTransform, UiBuilder.TopLeft, UiBuilder.TopLeft,
                new Vector2(UiTheme.PanelPadding, -34f), new Vector2(72f, 72f));
            mark.raycastTarget = false;

            var bag = UiBuilder.Icon("HeaderIcon", mark.transform, IconId.Bag,
                UiTheme.Glyph(UiTheme.Confirm));
            UiBuilder.Place(bag.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                Vector2.zero, new Vector2(40f, 40f));

            var title = UiBuilder.Label("Title", _panel, "상점", 30f, UiTheme.Ink,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UiBuilder.Place(title.rectTransform, UiBuilder.TopLeft, UiBuilder.TopLeft,
                new Vector2(UiTheme.PanelPadding + 88f, -38f), new Vector2(500f, 34f));
            title.characterSpacing = 10f;

            var subtitle = UiBuilder.Label("Subtitle", _panel, headline, 20f, UiTheme.InkMuted,
                TextAlignmentOptions.Left);
            UiBuilder.Place(subtitle.rectTransform, UiBuilder.TopLeft, UiBuilder.TopLeft,
                new Vector2(UiTheme.PanelPadding + 90f, -74f), new Vector2(700f, 28f));
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void BuildWallet()
        {
            var chip = UiBuilder.Panel("Wallet", _panel, UiTheme.CardWell);
            _walletChip = UiBuilder.Place(chip.rectTransform, UiBuilder.TopRight, UiBuilder.TopRight,
                new Vector2(-UiTheme.PanelPadding, -34f), new Vector2(200f, 72f));

            var coin = UiBuilder.Icon("Coin", chip.transform, IconId.Coin, UiTheme.Gold);
            UiBuilder.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(36f, 36f));

            _walletLabel = UiBuilder.Label("Amount", chip.transform, _run.Gold.ToString(), 34f,
                UiTheme.Gold, TextAlignmentOptions.Right, FontStyles.Bold);
            UiBuilder.Place(_walletLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(120f, 44f));
        }

        void BuildRowLabel(string objectName, IconId icon, string text, Color accent, float y)
        {
            float left = -UiTheme.PanelWidth * 0.5f + UiTheme.PanelPadding;

            var symbol = UiBuilder.Icon(objectName + "Icon", _panel, icon, accent);
            UiBuilder.Place(symbol.rectTransform, UiBuilder.Centre, new Vector2(0f, 0.5f),
                new Vector2(left, y), new Vector2(22f, 22f));

            var label = UiBuilder.Label(objectName, _panel, text, 18f, UiTheme.InkMuted,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UiBuilder.Place(label.rectTransform, UiBuilder.Centre, new Vector2(0f, 0.5f),
                new Vector2(left + 32f, y), new Vector2(400f, 26f));
            label.characterSpacing = 10f;
        }

        void BuildConfirmButton()
        {
            _confirmButton = UiButton.CreateAction(_panel, "Confirm", IconId.Arrow, "다음",
                UiBuilder.BottomRight, UiBuilder.BottomRight,
                new Vector2(-UiTheme.PanelPadding, 34f), new Vector2(230f, 76f),
                UiTheme.Confirm, UiTheme.InkOnAccent);
            _confirmButton.Clicked += Close;
        }

        void BuildOffers(ShopPool pool)
        {
            if (pool == null)
            {
                Debug.LogWarning("[ShopScreen] No ShopPool assigned — the shop will be empty.");
                return;
            }

            pool.Roll(_artifactOffer, _pieceOffer, _run.Artifacts);

            for (int i = 0; i < _artifactOffer.Count; i++)
            {
                var offer = _artifactOffer[i];
                var item = offer.item;

                // 아이템에 그림이 아직 없으면 임시 아이콘으로 대신 세워 둔다.
                var icon = item.itemImage != null ? item.itemImage : UiSprites.Get(IconId.Star);
                var title = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;

                var card = ShopCard.Create(_panel, "ArtifactCard_" + i,
                    SlotPosition(i, _artifactOffer.Count, ArtifactRowY),
                    icon, title, offer.blurb, offer.cost, UiTheme.ArtifactAccent);

                _artifactOf[card] = item;
                Register(card);
            }

            // 유물 매물이 하나도 없을 때. 빈 줄만 남기면 화면이 고장난 것처럼 보이므로,
            // 아직 등록되지 않았다는 사실을 자리에 그대로 적어 둔다.
            if (_artifactOffer.Count == 0)
                BuildEmptyRowHint(ArtifactRowY, "등록된 유물이 없습니다");

            for (int i = 0; i < _pieceOffer.Count; i++)
            {
                var piece = _pieceOffer[i];
                var icon = piece.sprite != null ? piece.sprite : UiSprites.Get(UiSprites.ForGlyph(piece.glyph));

                var card = ShopCard.Create(_panel, "PieceCard_" + i,
                    SlotPosition(i, _pieceOffer.Count, PieceRowY),
                    icon, piece.displayName, PieceBlurb.For(piece),
                    piece.shopCost, UiTheme.PieceAccent);

                _pieceOf[card] = piece;
                Register(card);
            }
        }

        void BuildEmptyRowHint(float y, string message)
        {
            var hint = UiBuilder.Label("EmptyHint", _panel, message, 20f, UiTheme.InkFaint,
                TextAlignmentOptions.Center);
            UiBuilder.Place(hint.rectTransform, UiBuilder.Centre, UiBuilder.Centre,
                new Vector2(0f, y), new Vector2(UiTheme.PanelWidth - UiTheme.PanelPadding * 2f, 40f));
        }

        void Register(ShopCard card)
        {
            card.Clicked += OnCardClicked;
            _cards.Add(card);
        }

        /// <summary>Centres a row of <paramref name="count"/> cards on the panel.</summary>
        static Vector2 SlotPosition(int index, int count, float y)
        {
            float step = UiTheme.CardWidth + UiTheme.CardGap;
            float offset = (count - 1) * 0.5f * step;
            return new Vector2(index * step - offset, y);
        }

        // ---------- Behaviour ----------

        void OnCardClicked(ShopCard card)
        {
            if (_closing || card.Sold)
                return;

            if (!_run.TrySpend(card.Cost))
            {
                card.PlayDeny();
                UiFx.Deny(_walletChip);
                return;
            }

            if (_artifactOf.TryGetValue(card, out var artifact))
            {
                _run.AddArtifact(artifact);
                card.MarkSold("보유 중");
            }
            else if (_pieceOf.TryGetValue(card, out var piece))
            {
                _run.AddPiece(piece);
                card.MarkSold("추가됨");
            }

            RefreshAffordability();
        }

        void OnGoldChanged(int previous, int current)
        {
            UiFx.CountTo(_walletLabel, previous, current);
            UiFx.Punch(_walletChip);
        }

        void RefreshAffordability()
        {
            foreach (var card in _cards)
                card.SetAffordable(_run.CanAfford(card.Cost));
        }

        void PlayIntro()
        {
            UiFx.BackdropIn(_backdropGroup);
            UiFx.PanelIn(_panel, _panelGroup);

            for (int i = 0; i < _cards.Count; i++)
                _cards[i].PlayIn(i);

            _confirmButton.PlayIn(_cards.Count);
        }

        void Close()
        {
            if (_closing)
                return;

            _closing = true;
            foreach (var card in _cards)
                card.Clicked -= OnCardClicked;

            UiFx.BackdropOut(_backdropGroup);
            UiFx.PanelOut(_panel, _panelGroup, () =>
            {
                var callback = _onConfirm;
                Destroy(gameObject);
                callback?.Invoke();
            });
        }

        void OnDestroy()
        {
            if (_run != null)
                _run.GoldChanged -= OnGoldChanged;

            if (Current == this)
            {
                Current = null;
                InputGate.Pop();
            }
        }

        // ---------- Scene plumbing ----------

        /// <summary>
        /// A scene without an EventSystem has silently dead UI. Creating one here
        /// means the shop works from any scene, including ones added later.
        /// </summary>
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
