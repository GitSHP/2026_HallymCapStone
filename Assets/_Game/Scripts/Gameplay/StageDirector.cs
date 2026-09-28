using System;
using System.Collections;
using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Data;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Runs the stage: deploy, then player and enemy turns until the last wave is
    /// wiped out or an enemy reaches the king. Every other system reacts to this.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        [Tooltip("Leave empty to derive a stage from DebugStageSetup's placements.")]
        public StageDefinition stage;

        [Header("Pacing")]
        public float phaseDelay = 0.35f;
        public float enemyResolveDelay = 0.5f;

        public event Action StateChanged;

        public int WavesSent { get; private set; }
        public int TotalWaves => stage != null && stage.waves != null ? stage.waves.Length : 0;
        public int EnemiesOnBoard { get; private set; }

        TurnManager _turn;
        BoardState _state;
        WaveSpawner _spawner;
        EnemyMover _mover;
        CombatResolver _combat;
        DeployController _deploy;
        DebugStageSetup _debugSetup;

        readonly List<Piece> _scratch = new();
        bool _resolving;

        public TurnManager Turn => _turn;

        void Awake() => Resolve();

        void Resolve()
        {
            if (_turn == null) _turn = GetComponent<TurnManager>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_spawner == null) _spawner = GetComponent<WaveSpawner>();
            if (_mover == null) _mover = GetComponent<EnemyMover>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
            if (_deploy == null) _deploy = GetComponent<DeployController>();
            if (_debugSetup == null) _debugSetup = GetComponent<DebugStageSetup>();
        }

        IEnumerator Start()
        {
            Resolve();

            // One frame so the opening board is placed before the stage reads it.
            yield return null;

            if (stage == null)
                stage = BuildStageFromOpeningBoard();

            ApplyStageSettings();
            BeginStage();
        }

        void ApplyStageSettings()
        {
            _turn.Configure(stage.apPerTurn);
            _spawner.spawnRows = stage.spawnRows;
            _mover.moveDiagonally = stage.enemiesMoveDiagonally;
            _deploy.deployRows = stage.deployRows;
        }

        /// <summary>The stage cannot start until the king has a square.</summary>
        public bool KingPlaced => _state.FindObjective(Team.Player) != null;

        public void BeginStage()
        {
            WavesSent = 0;
            _turn.ResetForStage();
            _deploy.LoadDeck(BuildDeployDeck());
            RefreshEnemyCount();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// The king first, then whatever the run carries, then anything the stage
        /// grants. Shop purchases live in RunState, so they show up here next stage.
        /// </summary>
        List<PieceDefinition> BuildDeployDeck()
        {
            var deck = new List<PieceDefinition>();

            if (stage.kingDefinition != null && !KingPlaced)
                deck.Add(stage.kingDefinition);

            var run = RunState.Instance;
            if (run != null)
                deck.AddRange(run.Deck);

            if (stage.startingDeck != null)
                deck.AddRange(stage.startingDeck);

            return deck;
        }

        /// <summary>Leaves the deploy phase and starts turn 1.</summary>
        public void ConfirmDeploy()
        {
            if (_turn.Phase != TurnPhase.Deploy || !KingPlaced)
                return;

            _turn.BeginPlayerTurn();
            StateChanged?.Invoke();
        }

        public void RequestEndTurn()
        {
            if (_resolving || _turn.Phase != TurnPhase.PlayerTurn)
                return;

            StartCoroutine(EnemyPhase());
        }

        IEnumerator EnemyPhase()
        {
            _resolving = true;
            _turn.SetPhase(TurnPhase.EnemyTurn);
            StateChanged?.Invoke();

            yield return new WaitForSeconds(phaseDelay);

            // Everything scheduled for this turn marches in before anyone moves.
            while (WavesSent < TotalWaves && stage.waves[WavesSent].spawnOnTurn <= _turn.Turn)
            {
                _spawner.Spawn(stage.waves[WavesSent]);
                WavesSent++;
                RefreshEnemyCount();
                StateChanged?.Invoke();
                yield return new WaitForSeconds(phaseDelay);
            }

            bool kingReached = _mover.ResolveEnemyTurn();
            RefreshEnemyCount();
            StateChanged?.Invoke();

            yield return new WaitForSeconds(enemyResolveDelay);

            _resolving = false;

            if (kingReached)
            {
                Finish(TurnPhase.Defeat);
                yield break;
            }

            if (WavesSent >= TotalWaves && EnemiesOnBoard == 0)
            {
                Finish(TurnPhase.Victory);
                yield break;
            }

            _turn.BeginPlayerTurn();
            StateChanged?.Invoke();
        }

        void Finish(TurnPhase outcome)
        {
            _turn.SetPhase(outcome);
            StateChanged?.Invoke();
        }

        /// <summary>Pulls the next scheduled wave in ahead of time. Used by DevTools.</summary>
        public bool SpawnNextWaveNow()
        {
            if (WavesSent >= TotalWaves)
                return false;

            _spawner.Spawn(stage.waves[WavesSent]);
            WavesSent++;
            RefreshEnemyCount();
            StateChanged?.Invoke();
            return true;
        }

        public void RefreshEnemyCount()
        {
            _state.GetPiecesOf(Team.Enemy, _scratch);
            EnemiesOnBoard = _scratch.Count;
        }

        /// <summary>
        /// Without a StageDefinition asset, the opening board doubles as the
        /// spec: player pieces become the deck, the enemy type becomes the waves.
        /// Assign a real asset to take authored control.
        /// </summary>
        StageDefinition BuildStageFromOpeningBoard()
        {
            var built = ScriptableObject.CreateInstance<StageDefinition>();
            built.name = "Stage (derived)";
            built.apPerTurn = 5;
            built.deployRows = 3;
            built.spawnRows = 2;

            var deck = new List<PieceDefinition>();
            PieceDefinition enemy = null;

            if (_debugSetup != null && _debugSetup.placements != null)
            {
                foreach (var placement in _debugSetup.placements)
                {
                    if (placement == null || placement.definition == null)
                        continue;

                    if (placement.team == Team.Enemy)
                        enemy ??= placement.definition;
                    else if (!placement.definition.isObjective)
                        deck.Add(placement.definition);
                }
            }

            built.startingDeck = deck.ToArray();
            built.waves = enemy == null
                ? Array.Empty<WaveDefinition>()
                : new[]
                {
                    new WaveDefinition { spawnOnTurn = 2, enemy = enemy, count = 3 },
                    new WaveDefinition { spawnOnTurn = 4, enemy = enemy, count = 4 },
                    new WaveDefinition { spawnOnTurn = 6, enemy = enemy, count = 5 }
                };

            return built;
        }
    }
}
