using System;
using System.Collections.Generic;
using Gambonanza.Data;
using Gambonanza.Map;
using UnityEngine;

namespace Gambonanza.Core
{
    /// <summary>
    /// Everything that survives a stage: gold, the pieces in the player's deck and
    /// the artifacts they hold. Lives across scene loads so the stage-select scene
    /// and the next battle read the same run.
    /// </summary>
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
        readonly List<ArtifactDefinition> _artifacts = new();

        bool _initialised;

        public int Gold { get; private set; }
        public int StageIndex { get; private set; }

        public IReadOnlyList<PieceDefinition> Deck => _deck;
        public IReadOnlyList<ArtifactDefinition> Artifacts => _artifacts;

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

        public void AddArtifact(ArtifactDefinition artifact)
        {
            if (artifact != null)
                _artifacts.Add(artifact);
        }

        public bool Owns(ArtifactDefinition artifact)
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
