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
    /// 스테이지 사이의 상점이다. 별도 씬이 아니라 진행 중인 보드 위에 오버레이로 그린다.
    /// 그래서 플레이어는 방금 이긴 판을 계속 볼 수 있고, 구매를 그 보드와 비교하며 판단한다.
    /// 화면은 전투 씬에 꺼진 상태로 놓인 프리팹이며, 열 때 카드 프리팹으로 만든 카드로 진열대를
    /// 채운다. 구매 내역만 RunState에 남고 나머지는 남지 않는다.
    /// </summary>
    // [아이템 담당] 상점 화면 전체. 화면 배치는 프리팹(Assets/UI/Prefabs/ShopScreen)에서
    // 고친다. 유물 줄은 ShopPool.artifactOffers 를, 기물 줄은 ShopPool.pieces 를 읽어
    // 카드 프리팹으로 만든다. 구매 처리는 아래 OnCardClicked 에 있다.
    public class ShopScreen : MonoBehaviour
    {
        /// <summary>상점은 한 번에 하나만 열린다. 클리어가 두 번 들어와도 화면이 겹쳐 쌓이면 안 된다.</summary>
        public static ShopScreen Current { get; private set; }

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] RectTransform _panel;
        [SerializeField] CanvasGroup _panelGroup;
        [SerializeField] CanvasGroup _backdropGroup;
        [SerializeField] TextMeshProUGUI _subtitle;
        [SerializeField] RectTransform _walletChip;
        [SerializeField] TextMeshProUGUI _walletLabel;
        [SerializeField] UiButton _confirmButton;
        [SerializeField] GameObject _artifactEmptyHint;
        [SerializeField] ShopCard _cardPrefab;

        [Header("줄 위치 (패널 기준 Y)")]
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

            // 보드는 포인터를 직접 읽으므로, 오버레이가 지금은 아래 세계를 조작하는 중이
            // 아니라는 것을 명시적으로 알려야 한다.
            InputGate.Push();

            _subtitle.text = headline;
            _walletLabel.text = _run.Gold.ToString();
            BuildOffers(pool);

            _run.GoldChanged += OnGoldChanged;
            RefreshAffordability();
            PlayIntro();
        }

        // ---------- 매물 ----------

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
                var icon = piece.itemImage != null ? piece.itemImage : UiSprites.Get(UiSprites.ForGlyph(piece.glyph));

                var card = MakeCard("PieceCard_" + i, SlotPosition(i, _pieceOffer.Count, _pieceRowY),
                    icon, piece.itemName, PieceBlurb.For(piece), piece.shopCost, UiTheme.PieceAccent);
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

        /// <summary><paramref name="count"/>장의 카드로 이루어진 한 줄을 패널 가운데에 정렬한다.</summary>
        static Vector2 SlotPosition(int index, int count, float y)
        {
            float step = UiTheme.CardWidth + UiTheme.CardGap;
            float offset = (count - 1) * 0.5f * step;
            return new Vector2(index * step - offset, y);
        }

        // ---------- 동작 ----------

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

        /// <summary>열린 상점이 붙잡고 있던 것을 모두 놓는다. 골드 구독, 입력 차단, 카드가 대상이다.</summary>
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

        // ---------- 씬 기반 설정 ----------

        /// <summary>
        /// EventSystem이 없는 씬에서는 UI가 아무 경고 없이 반응하지 않는다. 여기서 직접 만들어 두면
        /// 나중에 추가될 씬을 포함해 어느 씬에서든 상점이 동작한다.
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
