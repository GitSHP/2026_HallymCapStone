using System;
using System.Collections.Generic;
using Promotion.Data;
using Promotion.Map;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// Everything that survives a stage: gold, the pieces in the player's deck and
    /// the artifacts they hold. Lives across scene loads so the stage-select scene
    /// and the next battle read the same run.
    /// </summary>
    // [아이템 담당] 런 전체에서 유지되는 보유 현황. 구매한 유물은 Artifacts,
    // 기물은 Deck, 재화는 Gold 에 쌓인다. 씬이 바뀌어도 사라지지 않는다.
    public class RunState : MonoBehaviour
    {
        static RunState _instance;

        /// <summary>Creates the run on first access so no scene has to remember to host it.</summary>
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

        [Header("Starting values")]
        [SerializeField] int startingGold = 12;

        [Header("Starting deck (the king is placed by the stage, not the deck)")]
        [SerializeField] List<PieceDefinition> startingDeck = new();

        readonly List<PieceDefinition> _deck = new();
        readonly List<ItemData> _artifacts = new();

        bool _initialised;

        public int Gold { get; private set; }
        public int StageIndex { get; private set; }

        public IReadOnlyList<PieceDefinition> Deck => _deck;
        // [아이템 담당] 이번 런에서 보유 중인 유물 목록. 상점에서 구매하면 여기에 쌓인다.
        public IReadOnlyList<ItemData> Artifacts => _artifacts;

        /// <summary>This run's map, generated the first time the map scene opens.</summary>
        public MapData Map { get; set; }

        /// <summary>Every node entered so far, in order. The last one is where the player stands.</summary>
        public List<int> MapPath { get; } = new();

        public int CurrentMapNode => MapPath.Count > 0 ? MapPath[MapPath.Count - 1] : -1;

        /// <summary>The battle the map sent the player into. Null when the battle scene is opened directly.</summary>
        public StageDefinition CurrentStage { get; set; }

        /// <summary>Raised with (previous, current) so counters can tween between them.</summary>
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

        /// <summary>Spends only if the whole cost is covered — never leaves a partial purchase.</summary>
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

        /// <summary>Drops the run so a new one starts clean, without a scene reload.</summary>
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
