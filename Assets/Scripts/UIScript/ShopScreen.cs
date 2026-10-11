using System;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Promotion.UI
{
    /// <summary>
    /// The between-stages shop. Drawn as an overlay on top of the live board
    /// rather than in its own scene, so the player never loses sight of the
    /// position they just won — the purchase is read against that board.
    /// The screen is a prefab placed (switched off) in the battle scene; opening
    /// it fills the shelves with cards made from the card prefab. Nothing of it
    /// persists but the purchases, which live on the RunState.
    /// </summary>
    // [아이템 담당] 상점 화면 전체. 화면 배치는 프리팹(Assets/UI/Prefabs/ShopScreen)에서
    // 고친다. 유물 줄은 ShopPool.artifactOffers 를, 기물 줄은 ShopPool.pieces 를 읽어
    // 카드 프리팹으로 만든다. 구매 처리는 아래 OnCardClicked 에 있다.
    public class ShopScreen : MonoBehaviour
    {
        /// <summary>Only one shop can be up at a time; a second clear must not stack a second screen.</summary>
        public static ShopScreen Current { get; private set; }

        [Header("Parts (set in the prefab)")]
        [SerializeField] RectTransform _panel;
        [SerializeField] CanvasGroup _panelGroup;
        [SerializeField] CanvasGroup _backdropGroup;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] RectTransform _walletChip;
        [SerializeField] TextMeshProUGUI _walletLabel;
        [SerializeField] UiButton _confirmButton;
        [SerializeField] GameObject _artifactEmptyHint;
        [SerializeField] ShopCard _cardPrefab;

        [Header("Rows (panel-local Y)")]
        [SerializeField] float _artifactRowY = 144f;
        [SerializeField] float _pieceRowY = -192f;

        Action _onConfirm;
        RunState _run;

        readonly List<ShopCard> _cards = new();
        readonly Dictionary<ShopCard, ItemData> _artifactOf = new();
        readonly Dictionary<ShopCard, PieceDefinition> _pieceOf = new();

        readonly List<ShopOffer> _artifactOffer = new();
        readonly List<PieceDefinition> _pieceOffer = new();

        bool _closing;

        void Awake() => _confirmButton.Clicked += Close;

        public void Open(ShopPool pool, string headline, Action onConfirm)
        {
            if (Current != null)
                return;

            EnsureEventSystem();
            Current = this;
            _onConfirm = onConfirm;
            _closing = false;
            _run = RunState.Instance;
            gameObject.SetActive(true);

            // The board reads the pointer directly, so the overlay has to say
            // explicitly that the world below it is not being played right now.
            InputGate.Push();

            _subtitle.text = headline;
            _walletLabel.text = _run.Gold.ToString();
            BuildOffers(pool);

            _run.GoldChanged += OnGoldChanged;
            RefreshAffordability();
            PlayIntro();
        }

        // ---------- Offers ----------

        void BuildOffers(ShopPool pool)
        {
            ClearCards();

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

                var card = MakeCard("ArtifactCard_" + i, SlotPosition(i, _artifactOffer.Count, _artifactRowY),
                    icon, title, offer.blurb, offer.cost, UiTheme.ArtifactAccent);
                _artifactOf[card] = item;
            }

            // 유물 매물이 하나도 없을 때. 빈 줄만 남기면 화면이 고장난 것처럼 보이므로,
            // 아직 등록되지 않았다는 사실을 자리에 그대로 적어 둔다.
            if (_artifactEmptyHint != null)
                _artifactEmptyHint.SetActive(_artifactOffer.Count == 0);

            for (int i = 0; i < _pieceOffer.Count; i++)
            {
                var piece = _pieceOffer[i];
                var icon = piece.sprite != null ? piece.sprite : UiSprites.Get(UiSprites.ForGlyph(piece.glyph));

                var card = MakeCard("PieceCard_" + i, SlotPosition(i, _pieceOffer.Count, _pieceRowY),
                    icon, piece.displayName, PieceBlurb.For(piece), piece.shopCost, UiTheme.PieceAccent);
                _pieceOf[card] = piece;
            }
        }

        ShopCard MakeCard(string objectName, Vector2 position, Sprite icon, string title, string blurb,
            int cost, Color accent)
        {
            var card = Instantiate(_cardPrefab, _panel);
            card.name = objectName;
            card.Setup(position, icon, title, blurb, cost, accent);
            card.Clicked += OnCardClicked;
            _cards.Add(card);
            return card;
        }

        void ClearCards()
        {
            foreach (var card in _cards)
                if (card != null)
                    Destroy(card.gameObject);

            _cards.Clear();
            _artifactOf.Clear();
            _pieceOf.Clear();
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
            if (_closing || Current != this)
                return;

            _closing = true;
            foreach (var card in _cards)
                card.Clicked -= OnCardClicked;

            UiFx.BackdropOut(_backdropGroup);
            UiFx.PanelOut(_panel, _panelGroup, () =>
            {
                var callback = _onConfirm;
                Release();
                gameObject.SetActive(false);
                callback?.Invoke();
            });
        }

        /// <summary>Lets go of everything the open shop holds: gold listener, input block, cards.</summary>
        void Release()
        {
            if (_run != null)
                _run.GoldChanged -= OnGoldChanged;

            if (Current == this)
            {
                Current = null;
                InputGate.Pop();
            }

            ClearCards();
        }

        void OnDestroy() => Release();

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
