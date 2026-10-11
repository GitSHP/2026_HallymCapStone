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
    /// 스테이지를 진행한다. 배치 단계 뒤에 플레이어 턴과 적 턴을 반복하며, 마지막 웨이브가
    /// 전멸하거나 적이 킹에 도달할 때까지 이어진다. 체스 대결에서는 어느 한쪽 킹이
    /// 체크메이트될 때까지 이어진다. 다른 모든 시스템은 이 클래스에 반응한다.
    /// 스테이지의 승패를 선언하는 곳은 여기 한 곳뿐이다.
    /// </summary>
    public class StageDirector : MonoBehaviour
    {
        [Tooltip("비워 두면 DebugStageSetup의 배치에서 스테이지를 만들어 낸다.")]
        public StageDefinition stage;

        [Header("진행 속도")]
        public float phaseDelay = 0.35f;
        public float enemyResolveDelay = 0.5f;

        [Header("체스 대결")]
        [Tooltip("수를 둔 뒤 규칙을 판정하기 전의 대기 시간. 수가 놓이는 장면이 보이게 한다.")]
        public float duelSettleDelay = 0.35f;
        [Tooltip("난이도 조정을 위해 AI의 모든 선택을 콘솔에 기록한다.")]
        public bool logAIDecisions;

        public event Action StateChanged;

        /// <summary>한 진영이 체크 상태가 되면 발생한다(체크당한 진영을 전달한다).</summary>
        public event Action<Team> CheckDeclared;

        /// <summary>플레이어의 폰이 끝 줄에 도달해 승급할 기물을 골라야 할 때 발생한다.</summary>
        public event Action PromotionRequested;

        public bool IsChessDuel => stage != null && stage.IsChessDuel;
        public int DuelMovesPlayed { get; private set; }
        /// <summary>체스 대결에서 현재 체크 상태인 진영이다. 없으면 null이다.</summary>
        public Team? InCheck { get; private set; }
        public bool AIThinking { get; private set; }
        /// <summary>스테이지가 끝난 이유(체크메이트, 스테일메이트, 수 제한)다. 일반 스테이지에서는 비어 있다.</summary>
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

            // 스테이지가 읽기 전에 시작 보드가 배치되도록 한 프레임 기다린다.
            yield return null;

            // 맵에서 들어온 전투는 자기 스테이지를 가져온다. 이 씬을 직접 열면
            // 인스펙터에 지정된 스테이지를 그대로 쓴다.
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

        /// <summary>킹이 칸에 놓이기 전에는 스테이지를 시작할 수 없다.</summary>
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
        /// 킹이 먼저 오고, 그다음 런에서 가지고 온 기물, 그다음 스테이지가 주는 기물이 온다.
        /// 상점 구매품은 RunState에 있으므로 다음 스테이지에서 여기에 나타난다.
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

            // 체스 대결에서는 런의 기물에 더해 앞줄을 채울 폰을 나눠 준다.
            if (stage.IsChessDuel && stage.freePawn != null)
                for (int i = 0; i < stage.maxFreePawns; i++)
                    deck.Add(stage.freePawn);

            return deck;
        }

        /// <summary>배치 단계를 마치고 1턴을 시작한다.</summary>
        public void ConfirmDeploy()
        {
            if (_turn.Phase != TurnPhase.Deploy || !KingPlaced)
                return;

            _turn.BeginPlayerTurn();
            StateChanged?.Invoke();
        }

        public void RequestEndTurn()
        {
            // 체스 대결에서는 수를 두면 턴이 저절로 넘어간다.
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

            // 누구든 움직이기 전에 이번 턴에 예정된 웨이브가 모두 등장한다.
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

        // ---------- 체스 대결 ----------

        /// <summary>체스 대결은 적 군대만 배치된 빈 보드에서 시작한다.</summary>
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
        /// 플레이어가 수를 둔 뒤의 처리다. 승급하고, 흑 기준으로 국면을 판정하고,
        /// AI가 생각한 뒤 수를 두게 하고, 승급하고, 백 기준으로 판정한 뒤 턴을 돌려준다.
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

            // AI는 즉시 답을 낸다. 대기 시간은 고민하는 것처럼 보이게 하려는 것뿐이다.
            var profile = stage.aiProfile != null ? stage.aiProfile : ScriptableObject.CreateInstance<AIProfile>();
            AIThinking = true;
            StateChanged?.Invoke();

            var snapshot = SearchBoard.CopyOf(_state);
            var decision = ChessAI.ChooseMove(snapshot, Team.Enemy, profile, _rng,
                DuelMovesPlayed - 1, FirstPromotionChoice());

            if (logAIDecisions)
                Debug.Log($"[ChessAI] move {DuelMovesPlayed}: {decision.move.piece?.Def.itemName} " +
                          $"{decision.move.piece?.Coord}->{decision.move.to}  {decision.kind}, rank {decision.rank}, " +
                          $"score {decision.score:0.00} (best {decision.bestScore:0.00})");

            yield return new WaitForSecondsRealtime(profile.minThinkSeconds);
            AIThinking = false;

            var mover = decision.move.piece != null ? _state.GetPiece(decision.move.piece.Coord) : null;
            if (mover == null)
            {
                // 흑에게 둘 수가 없으면 Judge가 이미 대결을 끝낸다. 여기는 일어날 수 없는 경우를 막는 방어 코드일 뿐이다.
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
        /// 이제 수를 둘 진영 기준으로 국면을 판정한다. 대결이 끝났으면 true를 반환한다.
        /// 흑을 체크메이트하면 승리이고, 그 밖의 모든 종료는 패배다.
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

        /// <summary>실제 보드에 수를 둔다. 플레이어의 수와 같은 연출을 쓴다.</summary>
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

        /// <summary>흑은 항상 첫 번째 선택지(퀸)로 승급한다.</summary>
        void PromoteIfDue(Piece piece)
        {
            if (piece == null || !ChessRules.IsPromotion(_state, piece, piece.Coord))
                return;

            var into = FirstPromotionChoice();
            if (into != null)
                _registry.Replace(piece, into);
        }

        /// <summary>플레이어가 폰이 무엇이 될지 고른다. 고를 때까지 대결은 기다린다.</summary>
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

        /// <summary>플레이어가 승급 대상으로 고른 기물을 HUD가 넘겨 호출한다.</summary>
        public void ChoosePromotion(PieceDefinition into)
        {
            if (AwaitingPromotion && into != null)
                _promotionPick = into;
        }

        PieceDefinition FirstPromotionChoice()
            => stage.promotionChoices != null && stage.promotionChoices.Length > 0 ? stage.promotionChoices[0] : null;

        /// <summary>다음 예정 웨이브를 앞당겨 소환한다. DevTools에서 쓴다.</summary>
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
        /// StageDefinition 에셋이 없으면 시작 보드를 명세로 삼는다. 아군 기물은 덱이 되고,
        /// 적 종류는 웨이브가 된다. 직접 구성하려면 실제 에셋을 지정한다.
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
