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
    public class ShopScreen : MonoBehaviour
    // 상점 화면 전체를 관리하는 클래스 -> 상품 진열, 구매 처리, 골드 표시, 열기/닫기 연출을 담당
    // -> 화면 레이아웃은 프리팹(Assets/UI/Prefabs)이 원본이고, 이 스크립트는 화면을 켜고 끄고 카드 내용을 채우기만 함
    // 흐름: Open(상점 열기) → BuildOffers(상품 뽑아 카드 진열) → 카드 클릭 시 OnCardClicked(구매) → 확인 버튼으로 Close(닫기)
    {
        public static ShopScreen Current { get; private set; }  // 지금 열려 있는 상점 화면 -> 없으면 null (상점이 두 개 동시에 열리지 않도록 확인용)

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] RectTransform _panel;  // 상점 창 패널 -> 카드들이 이 밑에 생성됨
        [SerializeField] CanvasGroup _panelGroup;  // 패널 전체 투명도 조절용 (열기/닫기 연출)
        [SerializeField] CanvasGroup _backdropGroup;  // 화면 전체를 어둡게 덮는 배경의 투명도 조절용
        [SerializeField] TextMeshProUGUI _subtitle;  // 상점 제목 아래 안내 문구를 표시하는 텍스트 UI
        [SerializeField] RectTransform _walletChip;  // 보유 골드 표시 영역 -> 골드가 바뀌거나 부족할 때 흔드는 연출 대상
        [SerializeField] TextMeshProUGUI _walletLabel;  // 보유 골드 숫자를 표시하는 텍스트 UI
        [SerializeField] UiButton _confirmButton;  // 상점을 닫고 다음으로 넘어가는 확인 버튼
        [SerializeField] UiButton _rerollButton;  // 골드를 내고 진열 상품을 다시 뽑는 새로고침 버튼
        [SerializeField] GameObject _artifactEmptyHint;  // 진열할 유물이 하나도 없을 때 보여 주는 안내 문구
        [SerializeField] ShopCard _cardPrefab;  // 상품 카드 한 장의 프리팹 -> 상품 수만큼 복제해서 사용

        [Header("줄 위치 (패널 기준 Y)")]
        [SerializeField] float _artifactRowY = 144f;  // 유물 카드 줄의 세로 위치 (위쪽 줄)
        [SerializeField] float _pieceRowY = -192f;  // 기물 카드 줄의 세로 위치 (아래쪽 줄)

        Action _onConfirm;  // 상점이 완전히 닫힌 뒤 실행할 함수 (다음 화면으로 넘어가기 등, 상점을 연 쪽에서 넘겨줌)
        RunState _run;  // 현재 판의 진행 정보 (골드, 보유 기물·유물)

        readonly List<ShopCard> _cards = new();  // 현재 진열된 모든 카드 목록
        readonly Dictionary<ShopCard, ItemData> _artifactOf = new();  // 유물 카드 → 그 카드가 파는 유물 데이터
        readonly Dictionary<ShopCard, PieceDefinition> _pieceOf = new();  // 기물 카드 → 그 카드가 파는 기물 데이터

        readonly List<ShopOffer> _artifactOffer = new();  // 이번에 뽑힌 유물 상품 목록 (ShopPool.Roll이 채워 줌)
        readonly List<PieceDefinition> _pieceOffer = new();  // 이번에 뽑힌 기물 상품 목록

        bool _closing;  // 닫히는 연출이 진행 중인지 여부 -> 닫히는 동안 구매·중복 닫기를 막음
        ShopPool _pool;  // 이번에 연 상점의 상품 후보 목록 -> 새로고침할 때 다시 뽑는 데 사용
        int _rerollCount;  // 이번 상점에서 새로고침한 횟수 -> 횟수가 늘수록 새로고침 비용이 오름

        void Awake()  // 시작할 때 확인 버튼을 누르면 Close 함수가 실행되도록 연결
        {
            _confirmButton.Clicked += Close;
            if (_rerollButton != null)
                _rerollButton.Clicked += Reroll;  // 새로고침 버튼을 누르면 Reroll 함수가 실행되도록 연결
        }

        public void Open(ShopPool pool, string headline, Action onConfirm)  // 상점 화면을 여는 함수
        // pool: 상품 후보 목록 / headline: 안내 문구 / onConfirm: 상점이 닫힌 뒤 실행할 함수
        {
            if (Current != null)  // 만약 이미 열린 상점이 있다면 -> 중복으로 열지 않음
                return;

            EnsureEventSystem();  // 버튼 클릭을 받으려면 EventSystem이 필요하므로 없으면 만들어 둠
            Current = this;  // 이 상점을 현재 열린 상점으로 등록하고
            _onConfirm = onConfirm;
            _closing = false;
            _run = RunState.Instance;  // 현재 판의 진행 정보를 가져옴
            _pool = pool;
            _rerollCount = 0;  // 새로고침 비용은 상점을 열 때마다 처음 비용으로 돌아감
            gameObject.SetActive(true);  // 상점 화면을 켬

            InputGate.Push();  // 상점이 떠 있는 동안 뒤쪽 체스판 조작(드래그 등)을 막음

            _confirmButton.SetInteractable(true);  // 확인 버튼은 항상 누를 수 있는 상태로 시작 -> 이전 튜토리얼이 막아 둔 상태가 남지 않게
            _subtitle.text = headline;  // 안내 문구 표시
            _walletLabel.text = _run.Gold.ToString();  // 현재 보유 골드 표시
            BuildOffers(pool);  // 상품을 뽑아 카드로 진열
            RefreshRerollButton();  // 새로고침 버튼의 비용 표시

            _run.GoldChanged += OnGoldChanged;  // 골드가 바뀔 때마다 골드 표시를 갱신하도록 구독
            RefreshAffordability();  // 카드마다 살 수 있는지 표시
            PlayIntro();  // 등장 연출 재생
        }

        void BuildOffers(ShopPool pool)  // 상품을 무작위로 뽑아서 카드로 진열하는 함수
        {
            ClearCards();  // 이전에 진열된 카드가 남아 있다면 모두 지우고 시작

            if (pool == null)  // 만약 상품 목록 에셋이 연결되지 않았다면 -> 경고만 남기고 빈 상점으로 진행
            {
                Debug.LogWarning("[ShopScreen] No ShopPool assigned — the shop will be empty.");
                return;
            }

            pool.Roll(_artifactOffer, _pieceOffer, _run.Artifacts);  // 유물·기물 상품을 뽑음 -> 이미 가진 유물을 넘겨서 고유 유물이 다시 나오지 않게 함

            for (int i = 0; i < _artifactOffer.Count; i++)  // 뽑힌 유물마다 카드를 한 장씩 만듦
            {
                var offer = _artifactOffer[i];
                var item = offer.item;

                var icon = item.itemImage != null ? item.itemImage : UiSprites.Get(IconId.Star);  // 유물 이미지가 없으면 별 아이콘으로 대체
                var title = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;  // 표시 이름이 비어 있으면 에셋 파일 이름으로 대체

                var card = MakeCard("ArtifactCard_" + i, SlotPosition(i, _artifactOffer.Count, _artifactRowY),
                    icon, title, offer.blurb, offer.cost, UiTheme.ArtifactAccent);  // 위쪽 줄에 보라색 유물 카드 생성
                _artifactOf[card] = item;  // 이 카드가 어떤 유물을 파는지 기록 -> 구매할 때 사용
            }

            if (_artifactEmptyHint != null)
                _artifactEmptyHint.SetActive(_artifactOffer.Count == 0);  // 유물이 하나도 없을 때만 안내 문구를 보여 줌

            for (int i = 0; i < _pieceOffer.Count; i++)  // 뽑힌 기물마다 카드를 한 장씩 만듦
            {
                var piece = _pieceOffer[i];
                var icon = piece.itemImage != null ? piece.itemImage : UiSprites.Get(UiSprites.ForGlyph(piece.glyph));  // 기물 이미지가 없으면 기물 글자에 맞는 아이콘으로 대체

                var card = MakeCard("PieceCard_" + i, SlotPosition(i, _pieceOffer.Count, _pieceRowY),
                    icon, piece.itemName, PieceBlurb.For(piece), piece.shopCost, UiTheme.PieceAccent);  // 아래쪽 줄에 하늘색 기물 카드 생성 (설명은 행마법 문구)
                _pieceOf[card] = piece;  // 이 카드가 어떤 기물을 파는지 기록
            }
        }

        ShopCard MakeCard(string objectName, Vector2 position, Sprite icon, string title, string blurb,
            int cost, Color accent)  // 카드 프리팹을 복제해서 내용을 채우고 목록에 등록하는 함수
        {
            var card = Instantiate(_cardPrefab, _panel);  // 카드 프리팹을 패널 밑에 복제하고
            card.name = objectName;  // 하이어라키에서 구분하기 쉽게 이름을 붙임
            card.Setup(position, icon, title, blurb, cost, accent);  // 위치와 표시 내용을 채움
            card.Clicked += OnCardClicked;  // 카드를 클릭하면 OnCardClicked가 실행되도록 연결
            _cards.Add(card);  // 진열 목록에 추가
            return card;
        }

        void ClearCards()  // 진열된 카드를 모두 지우고 기록을 비우는 함수
        {
            foreach (var card in _cards)
                if (card != null)  // 이미 사라진 카드는 건너뜀
                    Destroy(card.gameObject);

            _cards.Clear();
            _artifactOf.Clear();
            _pieceOf.Clear();
        }

        static Vector2 SlotPosition(int index, int count, float y)  // count장의 카드를 가운데 정렬로 놓을 때 index번째 카드의 위치를 계산하는 함수
        {
            float step = UiTheme.CardWidth + UiTheme.CardGap;  // 카드 한 장이 차지하는 가로 간격 (카드 폭 + 사이 간격)
            float offset = (count - 1) * 0.5f * step;  // 전체 줄 길이의 절반 -> 이만큼 왼쪽으로 당겨서 가운데 정렬
            return new Vector2(index * step - offset, y);
        }

        void OnCardClicked(ShopCard card)  // 카드를 클릭했을 때 구매를 처리하는 함수
        {
            if (_closing || card.Sold)  // 만약 상점이 닫히는 중이거나 이미 팔린 카드라면 -> 무시
                return;

            if (!_run.TrySpend(card.Cost))  // 골드 차감을 시도하고 -> 만약 골드가 부족해서 실패했다면
            {
                card.PlayDeny();  // 카드를 흔들고 가격을 빨갛게 번쩍이며
                UiFx.Deny(_walletChip);  // 골드 표시도 흔들어서 골드가 부족함을 알림
                return;
            }

            // 골드 차감에 성공했다면 카드 종류에 맞게 지급
            if (_artifactOf.TryGetValue(card, out var artifact))  // 만약 유물 카드라면
            {
                _run.AddArtifact(artifact);  // 유물을 보유 목록에 추가하고
                card.MarkSold("보유 중");  // 카드를 "보유 중" 상태로 표시
                GameEventManager.RaiseItemPurchased(artifact);  
                // GameEventManager 에게 유물을 샀음을 알림
            }
            else if (_pieceOf.TryGetValue(card, out var piece))  // 만약 기물 카드라면
            {
                _run.AddPiece(piece);  // 기물을 보유 목록에 추가하고
                card.MarkSold("추가됨");  // 카드를 "추가됨" 상태로 표시
                GameEventManager.RaiseItemPurchased(piece);  
                // GameEventManager 에게 기물을 샀음을 알림
            }

            RefreshAffordability();  // 골드가 줄었으므로 나머지 카드들이 아직 살 수 있는지 다시 표시
        }

        void Reroll()  // 새로고침 버튼을 눌렀을 때 골드를 내고 진열 상품을 전부 다시 뽑는 함수
        // -> 이미 산 카드도 포함해 두 줄 모두 새로 뽑음. 비용은 같은 상점에서 누를 때마다 rerollCostStep만큼 오름
        {
            if (_closing || _pool == null)  // 만약 상점이 닫히는 중이거나 상품 목록이 없다면 -> 무시
                return;

            int cost = _pool.RerollCostAt(_rerollCount);
            if (!_run.TrySpend(cost))  // 골드 차감을 시도하고 -> 만약 골드가 부족해서 실패했다면
            {
                UiFx.Deny(_rerollButton.Rect);  // 버튼과 골드 표시를 흔들어 골드가 부족함을 알림
                UiFx.Deny(_walletChip);
                return;
            }

            _rerollCount++;
            BuildOffers(_pool);  // 이전 카드를 지우고 새로 뽑아 진열
            RefreshAffordability();
            RefreshRerollButton();
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].PlayIn(i);  // 새 카드들이 순서대로 올라오며 등장
        }

        void RefreshRerollButton()  // 새로고침 버튼의 비용 글자를 갱신하는 함수 -> 고정 진열 상점(튜토리얼)에서는 버튼을 숨김
        // -> 고정 진열은 다시 뽑아도 같은 상품이 나오므로 새로고침이 의미가 없음
        {
            if (_rerollButton == null)
                return;

            bool available = _pool != null && !_pool.fixedOrder;
            _rerollButton.gameObject.SetActive(available);
            if (available)
                _rerollButton.SetCaption("새로고침 " + _pool.RerollCostAt(_rerollCount) + "G");
        }

        void OnGoldChanged(int previous, int current)  // 골드가 바뀌었을 때 실행되는 함수 (RunState.GoldChanged 이벤트)
        {
            UiFx.CountTo(_walletLabel, previous, current);  // 골드 숫자가 이전 값에서 새 값으로 차르륵 바뀌는 연출
            UiFx.Punch(_walletChip);  // 골드 표시가 톡 튀는 연출
        }

        void RefreshAffordability()  // 모든 카드에 대해 현재 골드로 살 수 있는지 다시 표시하는 함수
        {
            foreach (var card in _cards)
                card.SetAffordable(_run.CanAfford(card.Cost));  // 살 수 없는 카드는 흐리게 표시됨
        }

        void PlayIntro()  // 상점이 열릴 때의 등장 연출을 재생하는 함수
        {
            UiFx.BackdropIn(_backdropGroup);  // 어두운 배경이 서서히 나타나고
            UiFx.PanelIn(_panel, _panelGroup);  // 상점 창이 커지면서 나타남

            for (int i = 0; i < _cards.Count; i++)
                _cards[i].PlayIn(i);  // 카드들이 순서대로 한 장씩 올라오며 등장

            _confirmButton.PlayIn(_cards.Count);  // 확인 버튼은 마지막 카드 다음 순서로 등장
            if (_rerollButton != null && _rerollButton.gameObject.activeSelf)
                _rerollButton.PlayIn(_cards.Count);  // 새로고침 버튼도 확인 버튼과 함께 등장
        }

        // ---------- 튜토리얼이 상점의 각 부분을 가리킬 때 쓰는 함수 ----------
        // -> 튜토리얼은 이 함수들로 위치만 받아 가고, 상점은 튜토리얼이 있는지조차 모름

        public RectTransform WalletRect  // 보유 골드 표시 영역
        {
            get { return _walletChip; }
        }

        public RectTransform ConfirmRect  // 상점을 닫는 확인 버튼 영역
        {
            get { return _confirmButton.Rect; }
        }

        public void GetArtifactRow(List<RectTransform> results)  // 유물 줄의 카드들을 results에 담아 주는 함수
        // -> 진열된 유물이 하나도 없으면 그 자리의 "유물 없음" 안내 문구를 대신 담음
        {
            results.Clear();
            foreach (var card in _artifactOf.Keys)  // 유물 카드를 모두 담고
                results.Add(card.Rect);

            if (results.Count == 0 && _artifactEmptyHint != null)  // 만약 유물 카드가 없다면
                results.Add((RectTransform)_artifactEmptyHint.transform);  // 안내 문구 자리를 대신 가리킴
        }

        public void GetPieceRow(List<RectTransform> results)  // 기물 줄의 카드들을 results에 담아 주는 함수
        {
            results.Clear();
            foreach (var card in _pieceOf.Keys)
                results.Add(card.Rect);
        }

        public void SetConfirmInteractable(bool interactable)  // 확인 버튼을 누를 수 있게/없게 하는 함수
        // -> 튜토리얼이 "구매해 보세요" 단계에서 상점이 먼저 닫혀 버리지 않도록 잠깐 막는 데 사용
        {
            _confirmButton.SetInteractable(interactable);
        }

        void Close()  // 상점을 닫는 함수 (확인 버튼을 눌렀을 때 실행)
        {
            if (_closing || Current != this)  // 만약 이미 닫히는 중이거나 이 상점이 열려 있지 않다면 -> 무시
                return;

            _closing = true;  // 닫히는 중으로 표시하고
            foreach (var card in _cards)
                card.Clicked -= OnCardClicked;  // 카드 클릭 연결을 끊어서 닫히는 동안 구매가 일어나지 않게 함

            UiFx.BackdropOut(_backdropGroup);  // 배경이 서서히 사라지고
            UiFx.PanelOut(_panel, _panelGroup, () =>  // 창이 작아지며 사라진 뒤 아래 내용을 실행
            {
                var callback = _onConfirm;  // Release에서 정리되기 전에 완료 함수를 미리 꺼내 두고
                Release();  // 상점 상태를 정리한 뒤
                gameObject.SetActive(false);  // 상점 화면을 끄고
                callback?.Invoke();  // 상점을 연 쪽이 넘겨준 함수를 실행 (다음 화면으로 진행 등)
            });
        }

        void Release()  // 상점이 사용하던 구독·입력 차단·카드를 정리하는 함수
        {
            if (_run != null)
                _run.GoldChanged -= OnGoldChanged;  // 골드 변경 구독 해제 -> 닫힌 뒤에도 골드 연출이 실행되지 않도록

            if (Current == this)  // 만약 이 상점이 현재 열린 상점이라면
            {
                Current = null;  // 열린 상점 없음으로 바꾸고
                InputGate.Pop();  // Open에서 막았던 체스판 조작을 다시 풀어 줌
            }

            ClearCards();  // 진열된 카드 모두 삭제
        }

        void OnDestroy()  // 상점 오브젝트가 파괴될 때(씬 이동 등)도 정리 함수를 실행 -> 입력이 막힌 채 남지 않도록
        {
            Release();
        }

        static void EnsureEventSystem()  // 씬에 EventSystem이 없으면 새로 만드는 함수 -> UI 버튼 클릭을 받으려면 꼭 필요함
        {
            if (EventSystem.current != null)  // 만약 이미 있다면 -> 그대로 사용
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();  // 신 Input System용 UI 입력 모듈을 붙임 (구식 StandaloneInputModule 사용 안 함)
        }
    }
}
