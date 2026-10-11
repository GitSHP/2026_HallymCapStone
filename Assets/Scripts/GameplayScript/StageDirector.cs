using System;
using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// Runs the stage: deploy, then player and enemy turns until the last wave is
    /// wiped out or an enemy reaches the king — or, in the chess duel, until a
    /// king is checkmated. Every other system reacts to this; it is the one place
    /// a stage is declared won or lost.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        [Tooltip("Leave empty to derive a stage from DebugStageSetup's placements.")]
        public StageDefinition stage;

        [Header("Pacing")]
        public float phaseDelay = 0.35f;
        public float enemyResolveDelay = 0.5f;

        [Header("Chess duel")]
        [Tooltip("Pause after a move before rules are read, so the move is seen landing.")]
        public float duelSettleDelay = 0.35f;
        [Tooltip("Write every AI choice to the console, for tuning difficulty.")]
        public bool logAIDecisions;

        public event Action StateChanged;

        /// <summary>Raised when a side is put in check (the side that is in check).</summary>
        public event Action<Team> CheckDeclared;

        /// <summary>Raised when the player's pawn reaches the far rank and a piece must be chosen.</summary>
        public event Action PromotionRequested;

        public bool IsChessDuel => stage != null && stage.IsChessDuel;
        public int DuelMovesPlayed { get; private set; }
        /// <summary>The side currently in check in the duel, if any.</summary>
        public Team? InCheck { get; private set; }
        public bool AIThinking { get; private set; }
        /// <summary>Why the stage ended (checkmate, stalemate, move limit); empty for ordinary stages.</summary>
        public string OutcomeReason { get; private set; } = "";
        public bool AwaitingPromotion { get; private set; }
        public IReadOnlyList<PieceDefinition> PromotionChoices =>
            stage != null && stage.promotionChoices != null ? stage.promotionChoices : Array.Empty<PieceDefinition>();

        PieceDefinition _promotionPick;

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
        PieceRegistry _registry;
        DragController _drag;
        BoardGrid _grid;

        readonly System.Random _rng = new(Environment.TickCount);

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
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_drag == null) _drag = GetComponent<DragController>();
            if (_grid == null) _grid = GetComponent<BoardGrid>();
        }

        IEnumerator Start()
        {
            Resolve();

            // One frame so the opening board is placed before the stage reads it.
            yield return null;

            // A battle entered from the map brings its own stage; opening this
            // scene directly keeps the one assigned in the inspector.
            var chosen = RunState.Instance.CurrentStage;
            if (chosen != null)
                stage = chosen;

            if (stage == null)
                stage = BuildStageFromOpeningBoard();

            ApplyStageSettings();
            BeginStage();
        }

        void ApplyStageSettings()
        {
            if (stage.boardArt != null)
                _grid.SetBoardArt(stage.boardArt);

            _drag.MoveCommitted -= OnPlayerMoved;

            if (stage.IsChessDuel)
            {
                _turn.ConfigureOneMovePerTurn();
                _deploy.deployRows = stage.deployRows;
                _deploy.deployAllAtSetup = true;
                _deploy.summonDuringBattle = false;
                _drag.chessLegality = true;
                _drag.MoveCommitted += OnPlayerMoved;
                return;
            }

            _deploy.deployAllAtSetup = false;
            _deploy.summonDuringBattle = true;
            _drag.chessLegality = false;
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
            DuelMovesPlayed = 0;
            InCheck = null;
            OutcomeReason = "";

            if (stage.IsChessDuel)
                SetUpDuelBoard();

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

            // The duel hands out pawns to fill the front rank, on top of the run's pieces.
            if (stage.IsChessDuel && stage.freePawn != null)
                for (int i = 0; i < stage.maxFreePawns; i++)
                    deck.Add(stage.freePawn);

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
            // In the duel the turn passes by itself once the move is made.
            if (_resolving || _turn.Phase != TurnPhase.PlayerTurn || IsChessDuel)
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

        void Finish(TurnPhase outcome, string reason = "")
        {
            OutcomeReason = reason;
            _resolving = false;
            AIThinking = false;
            _turn.SetPhase(outcome);
            StateChanged?.Invoke();
        }

        // ---------- Chess duel ----------

        /// <summary>The duel starts from an empty board with the enemy army in place.</summary>
        void SetUpDuelBoard()
        {
            _registry.RemoveAll();

            if (stage.enemyLayout == null)
                return;

            foreach (var placement in stage.enemyLayout)
            {
                if (placement?.piece == null)
                    continue;
                var view = _registry.Spawn(placement.piece, Team.Enemy,
                    new Coord(placement.square.x, placement.square.y));
                if (view != null)
                    Fx.PopIn(view.transform, placement.square.x, view.BaseScale);
            }
        }

        void OnPlayerMoved(Piece piece, Coord to)
        {
            if (!IsChessDuel || _resolving || _turn.IsOver)
                return;

            StartCoroutine(DuelReply(piece));
        }

        /// <summary>
        /// After the player's move: promote, judge the position for black, let the
        /// AI think and move, promote, judge for white, hand the turn back.
        /// </summary>
        IEnumerator DuelReply(Piece playerPiece)
        {
            _resolving = true;
            DuelMovesPlayed++;
            _turn.SetPhase(TurnPhase.EnemyTurn);
            StateChanged?.Invoke();

            yield return new WaitForSecondsRealtime(duelSettleDelay);
            yield return PromotePlayer(playerPiece);

            if (Judge(Team.Enemy))
                yield break;

            if (DuelMovesPlayed >= stage.moveLimit)
            {
                Finish(TurnPhase.Defeat, "수 제한 초과");
                yield break;
            }

            // The AI answers at once; the pause is only so it looks considered.
            var profile = stage.aiProfile != null ? stage.aiProfile : ScriptableObject.CreateInstance<AIProfile>();
            AIThinking = true;
            StateChanged?.Invoke();

            var snapshot = SearchBoard.CopyOf(_state);
            var decision = ChessAI.ChooseMove(snapshot, Team.Enemy, profile, _rng,
                DuelMovesPlayed - 1, FirstPromotionChoice());

            if (logAIDecisions)
                Debug.Log($"[ChessAI] move {DuelMovesPlayed}: {decision.move.piece?.Def.displayName} " +
                          $"{decision.move.piece?.Coord}->{decision.move.to}  {decision.kind}, rank {decision.rank}, " +
                          $"score {decision.score:0.00} (best {decision.bestScore:0.00})");

            yield return new WaitForSecondsRealtime(profile.minThinkSeconds);
            AIThinking = false;

            var mover = decision.move.piece != null ? _state.GetPiece(decision.move.piece.Coord) : null;
            if (mover == null)
            {
                // Judge already ends the duel when black has no move; this only guards the impossible.
                Finish(TurnPhase.Victory, "체크메이트");
                yield break;
            }

            PerformMove(mover, decision.move.to);
            StateChanged?.Invoke();

            yield return new WaitForSecondsRealtime(duelSettleDelay);
            PromoteIfDue(mover);

            if (Judge(Team.Player))
                yield break;

            _resolving = false;
            _turn.BeginPlayerTurn();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Reads the position for the side about to move. Returns true when the
        /// duel is over: checkmating black wins, every other ending is a loss.
        /// </summary>
        bool Judge(Team sideToMove)
        {
            var status = ChessRules.GetStatus(SearchBoard.CopyOf(_state), sideToMove);

            switch (status)
            {
                case ChessStatus.Checkmate:
                    InCheck = sideToMove;
                    Finish(sideToMove == Team.Enemy ? TurnPhase.Victory : TurnPhase.Defeat, "체크메이트");
                    return true;

                case ChessStatus.Stalemate:
                    Finish(TurnPhase.Defeat, "스테일메이트");
                    return true;

                case ChessStatus.Check:
                    InCheck = sideToMove;
                    CheckDeclared?.Invoke(sideToMove);
                    return false;

                default:
                    InCheck = null;
                    return false;
            }
        }

        /// <summary>Plays a move on the live board with the same animations as the player's moves.</summary>
        void PerformMove(Piece piece, Coord to)
        {
            var target = _state.GetPiece(to);
            if (target != null && target.Team != piece.Team)
            {
                _combat.CaptureInto(piece, target, to);
                return;
            }

            _state.Move(piece, to);
            var view = _registry.GetView(piece);
            if (view != null)
                Fx.StepTo(view.transform, _grid.CoordToWorld(to));
        }

        /// <summary>Black always promotes to the first choice (the queen).</summary>
        void PromoteIfDue(Piece piece)
        {
            if (piece == null || !ChessRules.IsPromotion(_state, piece, piece.Coord))
                return;

            var into = FirstPromotionChoice();
            if (into != null)
                _registry.Replace(piece, into);
        }

        /// <summary>The player picks what their pawn becomes; the duel waits until they do.</summary>
        IEnumerator PromotePlayer(Piece piece)
        {
            if (piece == null || !ChessRules.IsPromotion(_state, piece, piece.Coord))
                yield break;

            if (PromotionChoices.Count > 1)
            {
                _promotionPick = null;
                AwaitingPromotion = true;
                PromotionRequested?.Invoke();
                StateChanged?.Invoke();

                while (_promotionPick == null)
                    yield return null;

                AwaitingPromotion = false;
            }
            else
            {
                _promotionPick = FirstPromotionChoice();
            }

            if (_promotionPick != null)
                _registry.Replace(piece, _promotionPick);
            StateChanged?.Invoke();
        }

        /// <summary>Called by the HUD with the piece the player chose to promote into.</summary>
        public void ChoosePromotion(PieceDefinition into)
        {
            if (AwaitingPromotion && into != null)
                _promotionPick = into;
        }

        PieceDefinition FirstPromotionChoice()
            => stage.promotionChoices != null && stage.promotionChoices.Length > 0 ? stage.promotionChoices[0] : null;

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
