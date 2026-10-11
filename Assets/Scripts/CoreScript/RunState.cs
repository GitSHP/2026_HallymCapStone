using System;
using System.Collections.Generic;
using Promotion.Data;
using Promotion.Map;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// 스테이지가 끝나도 유지되는 모든 것: 골드, 플레이어 덱의 기물, 보유한 유물이다.
    /// 씬 로드를 넘어 살아 있으므로 스테이지 선택 씬과 다음 전투가 같은 런을 읽는다.
    /// </summary>
    // [아이템 담당] 런 전체에서 유지되는 보유 현황. 구매한 유물은 Artifacts,
    // 기물은 Deck, 재화는 Gold 에 쌓인다. 씬이 바뀌어도 사라지지 않는다.
    public class RunState : MonoBehaviour
    {
        static RunState _instance;

        /// <summary>처음 접근할 때 런을 생성하므로, 어느 씬도 이를 직접 배치해 둘 필요가 없다.</summary>
        public static RunState Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<RunState>();
                if (_instance == null)
                {
                    var go = new GameObject("RunState");
                    _instance = go.AddComponent<RunState>();
                }
                return _instance;
            }
        }

        [Header("시작 값")]
        [SerializeField] int startingGold = 12;

        [Header("시작 덱 (킹은 덱이 아니라 스테이지가 배치한다)")]
        [SerializeField] List<PieceDefinition> startingDeck = new();

        readonly List<PieceDefinition> _deck = new();
        readonly List<ItemData> _artifacts = new();

        bool _initialised;

        public int Gold { get; private set; }
        public int StageIndex { get; private set; }

        public IReadOnlyList<PieceDefinition> Deck => _deck;
        // [아이템 담당] 이번 런에서 보유 중인 유물 목록. 상점에서 구매하면 여기에 쌓인다.
        public IReadOnlyList<ItemData> Artifacts => _artifacts;

        /// <summary>이번 런의 맵이다. 맵 씬이 처음 열릴 때 생성된다.</summary>
        public MapData Map { get; set; }

        /// <summary>지금까지 진입한 모든 노드를 순서대로 담는다. 마지막 노드가 플레이어의 현재 위치이다.</summary>
        public List<int> MapPath { get; } = new();

        public int CurrentMapNode => MapPath.Count > 0 ? MapPath[MapPath.Count - 1] : -1;

        /// <summary>맵에서 플레이어를 보낸 전투이다. 전투 씬을 직접 열었을 때는 null이다.</summary>
        public StageDefinition CurrentStage { get; set; }

        /// <summary>(이전 값, 현재 값)과 함께 발생하므로 카운터가 두 값 사이를 트윈할 수 있다.</summary>
        public event Action<int, int> GoldChanged;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Initialise();
        }

        void Initialise()
        {
            if (_initialised)
                return;

            _initialised = true;
            Gold = startingGold;
            _deck.Clear();
            foreach (var piece in startingDeck)
                if (piece != null)
                    _deck.Add(piece);
        }

        public void AddGold(int amount)
        {
            if (amount == 0)
                return;

            int previous = Gold;
            Gold = Mathf.Max(0, Gold + amount);
            GoldChanged?.Invoke(previous, Gold);
        }

        public bool CanAfford(int cost) => Gold >= cost;

        /// <summary>비용 전체를 감당할 수 있을 때만 지출한다 — 부분 구매가 남는 일은 없다.</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0 || !CanAfford(cost))
                return false;

            AddGold(-cost);
            return true;
        }

        public void AddPiece(PieceDefinition piece)
        {
            if (piece != null)
                _deck.Add(piece);
        }

        /// <summary>상점이 구매를 성사시켰을 때 호출한다. 효과 적용은 아직 붙어 있지 않다.</summary>
        public void AddArtifact(ItemData artifact)
        {
            if (artifact != null)
                _artifacts.Add(artifact);
        }

        public bool Owns(ItemData artifact)
            => artifact != null && _artifacts.Contains(artifact);

        public void AdvanceStage() => StageIndex++;

        /// <summary>현재 런을 버려, 씬을 다시 로드하지 않고도 새 런이 깨끗하게 시작되게 한다.</summary>
        public void ResetRun()
        {
            _initialised = false;
            StageIndex = 0;
            _artifacts.Clear();
            Map = null;
            MapPath.Clear();
            CurrentStage = null;
            Initialise();
            GoldChanged?.Invoke(Gold, Gold);
        }
    }
}
