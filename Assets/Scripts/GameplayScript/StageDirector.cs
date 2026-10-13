using System;
using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class StageDirector : MonoBehaviour   // 스테이지 전체 진행을 맡는 클래스 - 배치 단계 -> 플레이어 턴 -> 적 턴을 반복하고 승패를 선언함
    // 승패를 선언하는 곳은 이 클래스 한 곳뿐이다 -> 상점, HUD 등 다른 시스템은 StateChanged 이벤트를 구독해서 결과만 받아 감
    // 일반 스테이지: 마지막 웨이브까지 다 나온 뒤 적이 전멸하면 승리, 적이 킹 칸에 들어오면 패배
    // 체스 대결(최종전): 적 킹을 체크메이트하면 승리, 그 밖의 종료(내가 체크메이트, 스테일메이트, 수 제한 초과)는 패배
    {
        [Tooltip("비워 두면 DebugStageSetup의 배치에서 스테이지를 만들어 낸다.")]
        public StageDefinition stage;   // 현재 진행할 스테이지 데이터(웨이브, AP, 배치 줄 수 등이 들어있는 에셋)

        [Header("진행 속도")]
        public float phaseDelay = 0.35f;    // 적 턴으로 넘어가거나 웨이브가 하나 나올 때마다 잠깐 기다리는 시간(초)
        public float enemyResolveDelay = 0.5f;  // 적이 모두 움직인 뒤 승패를 판정하기 전에 기다리는 시간(초) -> 움직이는 연출이 보이도록

        [Header("체스 대결")]
        [Tooltip("수를 둔 뒤 규칙을 판정하기 전의 대기 시간. 수가 놓이는 장면이 보이게 한다.")]
        public float duelSettleDelay = 0.35f;   // 체스 대결에서 수를 둔 뒤 판정 전까지 기다리는 시간(초)
        [Tooltip("난이도 조정을 위해 AI의 모든 선택을 콘솔에 기록한다.")]
        public bool logAIDecisions; // 체크하면 상대(컴퓨터)가 고른 수를 매번 콘솔에 출력함 -> 난이도 조정용

        public event Action StateChanged;   // 턴, 단계, 적 수 등 스테이지 상태가 바뀔 때 발생하는 이벤트 -> HUD가 화면을 갱신함

        public event Action<Team> CheckDeclared;    // 체스 대결에서 한 진영이 체크(킹이 공격받는 상태)가 됐을 때 발생하는 이벤트 -> 체크당한 진영을 넘겨줌

        public event Action PromotionRequested; // 플레이어의 폰이 끝 줄에 도달해 무엇으로 바뀔지(프로모션) 골라야 할 때 발생하는 이벤트

        public bool IsChessDuel => stage != null && stage.IsChessDuel;  // 지금 스테이지가 체스 대결(최종전)인지 여부
        public int DuelMovesPlayed { get; private set; }    // 체스 대결에서 플레이어가 지금까지 둔 수의 개수 -> 수 제한 판정에 사용
        public Team? InCheck { get; private set; }  // 체스 대결에서 현재 체크 상태인 진영(없으면 null)
        public bool AIThinking { get; private set; }    // 상대(컴퓨터)가 수를 고민하는 중인지 여부 -> HUD에 "생각 중" 표시용
        public string OutcomeReason { get; private set; } = "";    // 스테이지가 끝난 이유(체크메이트, 스테일메이트, 수 제한 초과) -> 일반 스테이지에서는 비어 있음
        public bool AwaitingPromotion { get; private set; } // 플레이어가 프로모션할 기물을 고르길 기다리는 중인지 여부
        public IReadOnlyList<PieceDefinition> PromotionChoices =>
            stage != null && stage.promotionChoices != null ? stage.promotionChoices : Array.Empty<PieceDefinition>();
        // 프로모션 때 고를 수 있는 기물 목록(퀸, 룩 등) -> 스테이지 데이터에 없으면 빈 목록

        PieceDefinition _promotionPick; // 플레이어가 프로모션 선택창에서 고른 기물(고르기 전에는 null)

        public int WavesSent { get; private set; }  // 지금까지 소환한 웨이브(적 무리)의 개수
        public int TotalWaves => stage != null && stage.waves != null ? stage.waves.Length : 0;    // 이번 스테이지의 전체 웨이브 개수
        public int EnemiesOnBoard { get; private set; } // 현재 보드 위에 남아 있는 적의 수

        TurnManager _turn;  // 턴 단계와 AP(행동력)를 관리하는 컴포넌트
        BoardState _state;  // 보드의 어느 칸에 어떤 기물이 있는지 저장하는 컴포넌트
        WaveSpawner _spawner;   // 웨이브의 적을 보드 위쪽 줄에 소환하는 컴포넌트
        EnemyMover _mover;  // 적 턴에 적들을 킹 쪽으로 한 칸씩 움직이는 컴포넌트
        CombatResolver _combat; // 잡기(상대 기물이 있는 칸으로 들어가 차지하기)를 처리하는 컴포넌트
        DeployController _deploy;   // 배치 단계와 전투 중 기물 소환을 담당하는 컴포넌트
        DebugStageSetup _debugSetup;    // 테스트용 시작 배치 정보(스테이지 에셋이 없을 때 대신 사용)
        PieceRegistry _registry;    // 기물 데이터(Piece)와 화면 오브젝트(PieceView)를 짝지어 관리하는 컴포넌트
        DragController _drag;   // 플레이어가 기물을 드래그해서 움직이는 것을 처리하는 컴포넌트
        BoardGrid _grid;    // 격자 좌표 <-> 월드 좌표 변환을 담당하는 컴포넌트

        readonly System.Random _rng = new(Environment.TickCount);  // 상대(컴퓨터)가 수를 고를 때 쓰는 난수 생성기 -> 매번 다른 수를 두도록 시간값으로 초기화

        readonly List<Piece> _scratch = new();  // 적 수를 셀 때 재사용하는 임시 리스트 -> 매번 새 리스트를 만들지 않으려고
        bool _resolving;    // 적 턴이나 상대의 응수를 처리하는 중인지 여부 -> 처리 중에는 턴 종료 요청 등을 무시함

        public TurnManager Turn => _turn;   // 외부(HUD 등)에서 턴 정보를 읽을 수 있게 열어 둔 속성

        void Awake() => Resolve();  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트들을 찾아서 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
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

        IEnumerator Start()    // 첫 프레임에 실행 -> 스테이지 데이터를 정하고 스테이지를 시작함
        {
            Resolve();  // 필요한 컴포넌트를 다시 한 번 확인

            yield return null;  // 한 프레임 기다림 -> 다른 컴포넌트(DebugStageSetup 등)가 시작 보드를 먼저 배치하도록

            var chosen = RunState.Instance.CurrentStage;    // 맵 화면에서 고르고 들어온 스테이지를 가져옴
            if (chosen != null) // 만약 맵에서 고른 스테이지가 있다면 그 스테이지를 사용
                stage = chosen; // -> 이 씬을 직접 열었다면 인스펙터에 지정된 스테이지를 그대로 씀

            if (stage == null)  // 스테이지 데이터가 아예 없다면 테스트용 시작 배치에서 스테이지를 만들어 냄
                stage = BuildStageFromOpeningBoard();

            ApplyStageSettings();   // 스테이지 설정값을 각 컴포넌트에 적용하고
            BeginStage();   // 배치 단계부터 스테이지를 시작함
        }

        void ApplyStageSettings()   // 스테이지 데이터의 설정값(AP, 배치 줄 수, 소환 줄 수 등)을 각 컴포넌트에 나눠 넣는 함수
        {
            if (stage.boardArt != null) // 만약 스테이지 전용 보드 그림이 있다면 보드 그림을 교체
                _grid.SetBoardArt(stage.boardArt);

            _drag.MoveCommitted -= OnPlayerMoved;   // 이전에 연결된 이벤트를 먼저 해제 -> 두 번 연결되어 응수가 두 번 실행되는 것을 방지

            if (stage.IsChessDuel)  // 만약 체스 대결(최종전) 스테이지라면
            {
                _turn.ConfigureOneMovePerTurn();    // 한 턴에 한 수만 둘 수 있도록 설정(AP 개념 대신)
                _deploy.deployRows = stage.deployRows;  // 기물을 놓을 수 있는 아래쪽 줄 수 설정
                _deploy.deployAllAtSetup = true;    // 배치 단계에서 킹뿐 아니라 모든 기물을 놓을 수 있게 함
                _deploy.summonDuringBattle = false; // 전투 중에는 기물을 새로 소환할 수 없게 함
                _drag.chessLegality = true; // 드래그 이동에 실제 체스 규칙(체크 상태에서 금지된 수 등)을 적용
                _drag.MoveCommitted += OnPlayerMoved;   // 플레이어가 수를 두면 OnPlayerMoved가 호출되도록 연결 -> 상대가 바로 응수함
                return; // 체스 대결은 여기서 설정 끝
            }

            // 일반 스테이지(타워디펜스)인 경우
            _deploy.deployAllAtSetup = false;   // 배치 단계에서는 킹만 놓을 수 있음
            _deploy.summonDuringBattle = true;  // 나머지 기물은 전투 중 AP를 써서 소환
            _drag.chessLegality = false;    // 체크 같은 체스 규칙 검사는 하지 않음
            _turn.Configure(stage.apPerTurn);   // 한 턴에 받는 AP(행동력) 설정
            _spawner.spawnRows = stage.spawnRows;   // 적이 나타나는 위쪽 줄 수 설정
            _mover.moveDiagonally = stage.enemiesMoveDiagonally;    // 적이 대각선으로도 움직일 수 있는지 설정
            _deploy.deployRows = stage.deployRows;  // 아군 기물을 놓을 수 있는 아래쪽 줄 수 설정
        }

        public bool KingPlaced => _state.FindObjective(Team.Player) != null;    // 보드 위에 플레이어 킹이 놓여 있는지 여부 -> 킹이 없으면 스테이지를 시작할 수 없음

        public void BeginStage()    // 스테이지를 처음 상태(배치 단계)로 시작하는 함수 -> 재시작할 때도 사용
        {
            WavesSent = 0;  // 진행 기록을 모두 초기화
            DuelMovesPlayed = 0;
            InCheck = null;
            OutcomeReason = "";

            if (stage.IsChessDuel)  // 만약 체스 대결이라면 적 기물들을 정해진 칸에 미리 배치
                SetUpDuelBoard();

            _turn.ResetForStage();  // 턴을 배치 단계로 되돌리고
            _deploy.LoadDeck(BuildDeployDeck());    // 이번 스테이지에서 쓸 수 있는 기물 목록(덱)을 만들어 넘겨줌
            RefreshEnemyCount();    // 적 수를 다시 세고
            StateChanged?.Invoke(); // HUD 등에 상태가 바뀌었음을 알림
        }

        List<PieceDefinition> BuildDeployDeck()   // 이번 스테이지에서 놓을 수 있는 기물 목록(덱)을 만드는 함수
        // 순서: 킹 -> 런(이번 판)에서 모은 기물 -> 스테이지가 기본으로 주는 기물 -> (체스 대결이면) 무료 폰
        {
            var deck = new List<PieceDefinition>();

            if (stage.kingDefinition != null && !KingPlaced)    // 아직 킹이 보드에 없다면 킹을 덱 맨 앞에 넣음
                deck.Add(stage.kingDefinition);

            var run = RunState.Instance;    // 런 전체 진행 정보(상점에서 산 기물 등)
            if (run != null)    // 상점에서 구매한 기물은 RunState에 있으므로 다음 스테이지 덱에 여기서 들어옴
                deck.AddRange(run.Deck);

            if (stage.startingDeck != null) // 스테이지가 기본으로 주는 기물 추가
                deck.AddRange(stage.startingDeck);

            if (stage.IsChessDuel && stage.freePawn != null)    // 체스 대결이라면 앞줄을 채울 폰을 정해진 개수만큼 무료로 줌
                for (int i = 0; i < stage.maxFreePawns; i++)
                    deck.Add(stage.freePawn);

            return deck;
        }

        public void ConfirmDeploy() // 배치 단계를 끝내고 1턴(플레이어 턴)을 시작하는 함수 -> HUD의 시작 버튼에 연결
        {
            if (_turn.Phase != TurnPhase.Deploy || !KingPlaced) // 배치 단계가 아니거나 킹을 아직 놓지 않았다면 시작하지 않음
                return;

            _turn.BeginPlayerTurn();    // 플레이어 턴 시작 -> AP 지급
            StateChanged?.Invoke();
        }

        public void RequestEndTurn()    // 플레이어가 턴 종료 버튼을 눌렀을 때 호출되는 함수 -> 적 턴을 시작함
        {
            if (_resolving || _turn.Phase != TurnPhase.PlayerTurn || IsChessDuel)
            // 이미 적 턴을 처리 중이거나, 플레이어 턴이 아니거나, 체스 대결이라면 무시 -> 체스 대결은 수를 두면 턴이 저절로 넘어감
                return;

            StartCoroutine(EnemyPhase());   // 적 턴 처리를 코루틴으로 시작(중간중간 연출 대기가 필요하므로)
        }

        IEnumerator EnemyPhase()    // 일반 스테이지의 적 턴 전체 흐름을 처리하는 코루틴
        // 흐름: 적 턴 표시 -> 이번 턴에 예정된 웨이브 소환 -> 적 이동 -> 패배/승리 판정 -> 다시 플레이어 턴
        {
            _resolving = true;  // 처리 중 표시 -> 그동안 턴 종료 요청이 또 들어와도 무시함
            _turn.SetPhase(TurnPhase.EnemyTurn);    // 단계를 적 턴으로 바꿈
            StateChanged?.Invoke();

            yield return new WaitForSeconds(phaseDelay);    // 적 턴으로 넘어간 것이 보이도록 잠깐 대기

            while (WavesSent < TotalWaves && stage.waves[WavesSent].spawnOnTurn <= _turn.Turn)
            // 아직 안 나온 웨이브 중 등장 턴이 지금 턴 이하인 것을 모두 소환 -> 누구든 움직이기 전에 등장함
            {
                _spawner.Spawn(stage.waves[WavesSent]); // 웨이브의 적들을 보드 위쪽에 소환하고
                WavesSent++;    // 소환한 웨이브 수 +1
                RefreshEnemyCount();
                StateChanged?.Invoke();
                yield return new WaitForSeconds(phaseDelay);    // 웨이브가 하나씩 나오는 것이 보이도록 대기
            }

            bool kingReached = _mover.ResolveEnemyTurn();   // 모든 적을 킹 쪽으로 한 칸씩 이동 -> 적이 킹 칸에 들어갔다면 true
            RefreshEnemyCount();
            StateChanged?.Invoke();

            yield return new WaitForSeconds(enemyResolveDelay); // 적이 움직이는 연출이 끝나도록 대기

            _resolving = false; // 처리 끝

            if (kingReached)    // 만약 적이 킹 칸에 들어왔다면 -> 즉시 패배
            {
                Finish(TurnPhase.Defeat);
                yield break;
            }

            if (WavesSent >= TotalWaves && EnemiesOnBoard == 0) // 모든 웨이브가 나왔고 보드에 적이 하나도 없다면 -> 승리
            {
                Finish(TurnPhase.Victory);
                yield break;
            }

            _turn.BeginPlayerTurn();    // 승패가 안 났다면 다음 플레이어 턴 시작(턴 수 +1, AP 다시 지급)
            StateChanged?.Invoke();
        }

        void Finish(TurnPhase outcome, string reason = "")  // 스테이지를 승리 또는 패배로 끝내는 함수 -> reason에는 끝난 이유를 넣음
        {
            OutcomeReason = reason; // 끝난 이유 저장(HUD에서 표시)
            _resolving = false; // 처리 중 표시와 생각 중 표시를 모두 해제
            AIThinking = false;
            _turn.SetPhase(outcome);    // 단계를 승리/패배로 바꿈 -> 이후 입력이 막힘
            StateChanged?.Invoke(); // 상점 등 결과를 기다리는 시스템에 알림
        }

        // ---------- 체스 대결(최종전) ----------

        void SetUpDuelBoard()   // 체스 대결용 보드를 준비하는 함수 -> 보드를 비우고 적 군대만 정해진 칸에 배치함
        {
            _registry.RemoveAll();  // 보드 위의 모든 기물 제거

            if (stage.enemyLayout == null)  // 적 배치 정보가 없다면 그냥 종료
                return;

            foreach (var placement in stage.enemyLayout)    // 적 배치 정보를 하나씩 읽어서
            {
                if (placement?.piece == null)   // 기물이 지정되지 않은 항목은 건너뜀
                    continue;
                var view = _registry.Spawn(placement.piece, Team.Enemy,
                    new Coord(placement.square.x, placement.square.y)); // 지정된 칸에 적 기물을 생성하고
                if (view != null)   // 생성에 성공했다면 튀어나오는 등장 연출 재생(열 번호만큼 순서대로 늦게)
                    Fx.PopIn(view.transform, placement.square.x, view.BaseScale);
            }
        }

        void OnPlayerMoved(Piece piece, Coord to)   // 체스 대결에서 플레이어가 수를 하나 뒀을 때 발생하는 이벤트
        {
            if (!IsChessDuel || _resolving || _turn.IsOver) // 체스 대결이 아니거나, 이미 처리 중이거나, 스테이지가 끝났다면 무시
                return;

            StartCoroutine(DuelReply(piece));   // 상대(컴퓨터)의 응수 처리를 시작
        }

        IEnumerator DuelReply(Piece playerPiece)    // 플레이어가 수를 둔 뒤의 처리 전체를 담당하는 코루틴
        // 흐름: 플레이어 폰 프로모션 -> 상대 기준 판정 -> 수 제한 확인 -> 상대가 수 선택 -> 수 실행
        //       -> 상대 폰 프로모션 -> 플레이어 기준 판정 -> 다시 플레이어 턴
        {
            _resolving = true;  // 처리 중 표시 -> 그동안 들어오는 입력/이벤트는 무시
            DuelMovesPlayed++;  // 플레이어가 둔 수 +1
            _turn.SetPhase(TurnPhase.EnemyTurn);    // 상대 차례로 바꿈 -> 플레이어는 기물을 움직일 수 없음
            StateChanged?.Invoke();

            yield return new WaitForSecondsRealtime(duelSettleDelay);  // 플레이어의 수가 놓이는 장면이 보이도록 대기
            yield return PromotePlayer(playerPiece);    // 플레이어 폰이 끝 줄에 닿았다면 프로모션 처리(선택이 끝날 때까지 기다림)

            if (Judge(Team.Enemy))  // 이제 둘 차례인 상대 기준으로 국면 판정 -> 상대가 체크메이트 등으로 끝났다면 종료
                yield break;

            if (DuelMovesPlayed >= stage.moveLimit) // 플레이어가 정해진 수 제한을 다 썼는데 이기지 못했다면 -> 패배
            {
                Finish(TurnPhase.Defeat, "수 제한 초과");
                yield break;
            }

            var profile = stage.aiProfile != null ? stage.aiProfile : ScriptableObject.CreateInstance<AIProfile>();
            // 상대의 실력 설정(실수 확률, 고민 시간 등) -> 스테이지에 지정되지 않았다면 기본값으로 새로 만듦
            AIThinking = true;  // "생각 중" 표시 시작
            StateChanged?.Invoke();

            var snapshot = SearchBoard.CopyOf(_state);  // 실제 보드를 복사한 계산용 보드 -> 수를 시험해 봐도 실제 보드는 바뀌지 않음
            var decision = ChessAI.ChooseMove(snapshot, Team.Enemy, profile, _rng,
                DuelMovesPlayed - 1, FirstPromotionChoice());   // 상대가 둘 수를 고름(몇 번째 수인지, 프로모션 시 바꿀 기물도 함께 넘김)

            if (logAIDecisions) // 기록 옵션이 켜져 있다면 고른 수와 점수를 콘솔에 출력
                Debug.Log($"[ChessAI] move {DuelMovesPlayed}: {decision.move.piece?.Def.itemName} " +
                          $"{decision.move.piece?.Coord}->{decision.move.to}  {decision.kind}, rank {decision.rank}, " +
                          $"score {decision.score:0.00} (best {decision.bestScore:0.00})");

            yield return new WaitForSecondsRealtime(profile.minThinkSeconds);
            // 수는 즉시 정해지지만 고민하는 것처럼 보이도록 최소 시간만큼 기다림
            AIThinking = false; // "생각 중" 표시 끝

            var mover = decision.move.piece != null ? _state.GetPiece(decision.move.piece.Coord) : null;
            // 계산용 보드에서 고른 기물과 같은 칸에 있는 실제 보드의 기물을 찾음
            if (mover == null)  // 만약 움직일 기물이 없다면(둘 수가 없는 경우) 승리로 처리
            // -> 원래는 위의 Judge에서 이미 끝나므로 일어나지 않는 경우를 막는 방어 코드
            {
                Finish(TurnPhase.Victory, "체크메이트");
                yield break;
            }

            PerformMove(mover, decision.move.to);   // 실제 보드에 상대의 수를 실행
            StateChanged?.Invoke();

            yield return new WaitForSecondsRealtime(duelSettleDelay);  // 상대의 수가 놓이는 장면이 보이도록 대기
            PromoteIfDue(mover);    // 상대 폰이 끝 줄에 닿았다면 프로모션(항상 첫 번째 선택지로)

            if (Judge(Team.Player)) // 이제 둘 차례인 플레이어 기준으로 국면 판정 -> 플레이어가 체크메이트 등으로 끝났다면 종료
                yield break;

            _resolving = false; // 처리 끝
            _turn.BeginPlayerTurn();    // 다시 플레이어 차례
            StateChanged?.Invoke();
        }

        bool Judge(Team sideToMove) // 이제 수를 둘 진영 기준으로 국면을 판정하는 함수 -> 대결이 끝났으면 true 반환
        // 상대를 체크메이트하면 승리, 그 밖의 모든 종료(내가 체크메이트, 스테일메이트)는 패배
        {
            var status = ChessRules.GetStatus(SearchBoard.CopyOf(_state), sideToMove);  // 복사한 보드로 현재 국면 상태를 계산

            switch (status)
            {
                case ChessStatus.Checkmate: // 체크메이트(킹이 공격받고 피할 방법이 없음)라면
                    InCheck = sideToMove;
                    Finish(sideToMove == Team.Enemy ? TurnPhase.Victory : TurnPhase.Defeat, "체크메이트");
                    // 체크메이트당한 쪽이 상대면 승리, 플레이어면 패배
                    return true;

                case ChessStatus.Stalemate: // 스테일메이트(체크는 아니지만 둘 수 있는 수가 없음)라면 -> 이 게임에서는 패배로 처리
                    Finish(TurnPhase.Defeat, "스테일메이트");
                    return true;

                case ChessStatus.Check: // 체크(킹이 공격받는 중)라면 -> 게임은 계속되고 체크 알림만 보냄
                    InCheck = sideToMove;
                    CheckDeclared?.Invoke(sideToMove);
                    return false;

                default:    // 아무 일도 없다면 체크 표시를 지움
                    InCheck = null;
                    return false;
            }
        }

        void PerformMove(Piece piece, Coord to) // 실제 보드에 수를 실행하는 함수 -> 플레이어의 수와 같은 연출을 사용
        {
            var target = _state.GetPiece(to);   // 도착 칸에 있는 기물 확인
            if (target != null && target.Team != piece.Team)    // 만약 도착 칸에 상대 기물이 있다면 -> 잡기 처리 후 종료
            {
                _combat.CaptureInto(piece, target, to);
                return;
            }

            _state.Move(piece, to); // 빈 칸이라면 보드 데이터에서 기물을 옮기고
            var view = _registry.GetView(piece);    // 기물의 화면 오브젝트를 찾아서
            if (view != null)   // 도착 칸 위치로 이동하는 연출 재생
                Fx.StepTo(view.transform, _grid.CoordToWorld(to));
        }

        void PromoteIfDue(Piece piece)  // 상대 기물이 프로모션 조건(폰이 끝 줄 도착)을 만족하면 프로모션하는 함수
        // 상대는 항상 첫 번째 선택지(보통 퀸)로 바뀜
        {
            if (piece == null || !ChessRules.IsPromotion(_state, piece, piece.Coord))  // 프로모션 조건이 아니라면 아무것도 하지 않음
                return;

            var into = FirstPromotionChoice();  // 첫 번째 선택지를 가져와서
            if (into != null)   // 기물을 그 기물로 교체
                _registry.Replace(piece, into);
        }

        IEnumerator PromotePlayer(Piece piece)  // 플레이어 기물이 프로모션 조건을 만족하면 무엇으로 바꿀지 고르게 하는 코루틴
        // 플레이어가 고를 때까지 대결은 멈춰서 기다림
        {
            if (piece == null || !ChessRules.IsPromotion(_state, piece, piece.Coord))  // 프로모션 조건이 아니라면 바로 종료
                yield break;

            if (PromotionChoices.Count > 1) // 선택지가 2개 이상이라면 -> 플레이어에게 선택창을 띄움
            {
                _promotionPick = null;  // 이전 선택 초기화
                AwaitingPromotion = true;   // 선택 대기 중 표시
                PromotionRequested?.Invoke();   // HUD에 선택창을 띄우라고 알림
                StateChanged?.Invoke();

                while (_promotionPick == null)  // 플레이어가 고를 때까지(ChoosePromotion이 호출될 때까지) 매 프레임 대기
                    yield return null;

                AwaitingPromotion = false;  // 선택 완료
            }
            else    // 선택지가 1개 이하라면 고를 필요 없이 첫 번째 선택지를 사용
            {
                _promotionPick = FirstPromotionChoice();
            }

            if (_promotionPick != null) // 고른 기물로 교체
                _registry.Replace(piece, _promotionPick);
            StateChanged?.Invoke();
        }

        public void ChoosePromotion(PieceDefinition into)   // 플레이어가 선택창에서 프로모션할 기물을 골랐을 때 HUD가 호출하는 함수
        {
            if (AwaitingPromotion && into != null)  // 선택을 기다리는 중일 때만 받아들임 -> PromotePlayer의 대기가 풀림
                _promotionPick = into;
        }

        PieceDefinition FirstPromotionChoice()  // 프로모션 선택지 중 첫 번째 기물을 돌려주는 함수(없으면 null)
            => stage.promotionChoices != null && stage.promotionChoices.Length > 0 ? stage.promotionChoices[0] : null;

        public bool SpawnNextWaveNow()  // 다음 웨이브를 등장 턴을 기다리지 않고 바로 소환하는 함수 -> DevTools(F7)에서 사용
        {
            if (WavesSent >= TotalWaves)    // 남은 웨이브가 없다면 false 반환
                return false;

            _spawner.Spawn(stage.waves[WavesSent]); // 다음 웨이브를 소환하고
            WavesSent++;    // 소환한 웨이브 수 +1
            RefreshEnemyCount();
            StateChanged?.Invoke();
            return true;
        }

        public void RefreshEnemyCount() // 보드 위의 적 수를 다시 세서 EnemiesOnBoard에 저장하는 함수
        {
            _state.GetPiecesOf(Team.Enemy, _scratch);   // 적 기물을 모두 임시 리스트에 담고
            EnemiesOnBoard = _scratch.Count;    // 그 개수를 저장
        }

        StageDefinition BuildStageFromOpeningBoard()    // 스테이지 에셋이 없을 때 테스트용 시작 배치(DebugStageSetup)로 스테이지를 만드는 함수
        // 아군 기물은 덱이 되고, 적 종류는 웨이브가 됨 -> 제대로 구성하려면 실제 스테이지 에셋을 지정해야 함
        {
            var built = ScriptableObject.CreateInstance<StageDefinition>(); // 빈 스테이지 데이터를 코드로 생성하고 기본값을 넣음
            built.name = "Stage (derived)";
            built.apPerTurn = 5;    // 턴당 AP 5
            built.deployRows = 3;   // 아래 3줄에 배치 가능
            built.spawnRows = 2;    // 위 2줄에서 적 등장

            var deck = new List<PieceDefinition>(); // 아군 기물 목록
            PieceDefinition enemy = null;   // 웨이브로 쓸 적 종류

            if (_debugSetup != null && _debugSetup.placements != null)  // 테스트용 배치 정보가 있다면
            {
                foreach (var placement in _debugSetup.placements)   // 배치 항목을 하나씩 확인
                {
                    if (placement == null || placement.definition == null)  // 비어 있는 항목은 건너뜀
                        continue;

                    if (placement.team == Team.Enemy)   // 적이라면 처음 나온 적 종류를 웨이브용으로 기억
                        enemy ??= placement.definition;
                    else if (!placement.definition.isObjective) // 킹이 아닌 아군 기물이라면 덱에 추가
                        deck.Add(placement.definition);
                }
            }

            built.startingDeck = deck.ToArray();    // 모은 아군 기물을 기본 덱으로 설정
            built.waves = enemy == null
                ? Array.Empty<WaveDefinition>() // 적 종류가 없다면 웨이브 없음
                : new[] // 적 종류가 있다면 2, 4, 6턴에 3, 4, 5마리씩 나오는 웨이브 3개를 만듦
                {
                    new WaveDefinition { spawnOnTurn = 2, enemy = enemy, count = 3 },
                    new WaveDefinition { spawnOnTurn = 4, enemy = enemy, count = 4 },
                    new WaveDefinition { spawnOnTurn = 6, enemy = enemy, count = 5 }
                };

            return built;
        }
    }
}
