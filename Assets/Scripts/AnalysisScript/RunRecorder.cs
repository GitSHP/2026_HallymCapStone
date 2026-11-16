using System.Collections.Generic;
using System.Text;
using Promotion.Core;
using Promotion.Data;
using Promotion.Gameplay;
using Promotion.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Analysis
{
    public class RunRecorder : MonoBehaviour
    // 플레이어의 행동을 RunLog에 기록하는 클래스
    // -> 게임 로직이 보내는 이벤트를 듣기만 함 -> 게임 진행에는 아무 영향을 주지 않음
    // -> 게임 시작 시 스스로 만들어져 씬이 바뀌어도 유지됨. 씬에 따로 놓을 필요가 없음
    // -> 튜토리얼은 기록하지 않음
    {
        StageDirector _director;
        DragController _drag;
        TurnManager _turn;
        BoardState _state;
        DeployController _deploy;
        CombatResolver _combat;

        StageRecord _stage;     // 지금 기록 중인 스테이지 (없으면 null)
        bool _stageClosed;      // 이번 스테이지의 결과를 이미 적었는지

        Piece _heldPiece;       // 지금 집고 있는 기물
        Coord _heldFrom;
        float _heldAt;
        int _heldOptions;
        bool _heldCapture;
        string _heldBoard;
        SearchBoard _heldCopy;
        readonly Dictionary<Coord, string> _heldVictims = new();    // 잡을 수 있던 칸 -> 그 칸의 상대 이름
        readonly List<MoveOption> _optionBuffer = new();
        readonly List<Coord> _blockedBuffer = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("RunRecorder");
            DontDestroyOnLoad(go);
            go.AddComponent<RunRecorder>();
        }

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            PieceGuideDirector.ManualGuideOpened += OnGuideOpened;
            Bind();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PieceGuideDirector.ManualGuideOpened -= OnGuideOpened;
            Unbind();
        }

        void OnSceneLoaded(Scene _scene, LoadSceneMode _mode)
        {
            Unbind();
            Bind();
        }

        void Bind() // 전투 씬이면 이벤트를 연결함 (타이틀·지도에서는 아무것도 하지 않음)
        {
            _stage = null;
            _stageClosed = false;
            _heldPiece = null;

            _director = FindAnyObjectByType<StageDirector>();
            if (_director == null)
                return;

            _drag = _director.GetComponent<DragController>();
            _turn = _director.GetComponent<TurnManager>();
            _state = _director.GetComponent<BoardState>();
            _deploy = _director.GetComponent<DeployController>();
            _combat = _director.GetComponent<CombatResolver>();

            if (_drag != null)
            {
                _drag.DragStarted += OnDragStarted;
                _drag.DragEnded += OnDragEnded;
            }
            if (_turn != null)
                _turn.PhaseChanged += OnPhaseChanged;
            if (_deploy != null)
                _deploy.PieceDeployed += OnDeployed;
            if (_combat != null)
                _combat.Captured += OnCaptured;
            _director.PlayerPromoted += OnPromoted;
            _director.CheckDeclared += OnCheck;
        }

        void Unbind()   // 이전 씬의 연결을 끊음
        {
            if (_drag != null)
            {
                _drag.DragStarted -= OnDragStarted;
                _drag.DragEnded -= OnDragEnded;
            }
            if (_turn != null)
                _turn.PhaseChanged -= OnPhaseChanged;
            if (_deploy != null)
                _deploy.PieceDeployed -= OnDeployed;
            if (_combat != null)
                _combat.Captured -= OnCaptured;
            if (_director != null)
            {
                _director.PlayerPromoted -= OnPromoted;
                _director.CheckDeclared -= OnCheck;
            }
            _director = null;
        }

        bool EnsureStage()  // 기록할 스테이지를 준비하는 함수 -> 기록하면 안 되는 상황이면 false
        {
            if (_stageClosed || _director == null || _director.stage == null)
                return false;
            if (TutorialSession.Active != null)
                return false;
            if (_stage != null)
                return true;

            var run = RunState.Instance;
            bool firstStage = run == null || run.StageIndex == 0;   // 클리어 수가 0이면 새 런의 첫 전투
            if (RunLog.Current == null || RunLog.Current.finished || firstStage)
                RunLog.Current = new RunLog();

            _stage = new StageRecord
            {
                index = RunLog.Current.stages.Count,
                name = _director.stage.name,
                isDuel = _director.IsChessDuel
            };
            RunLog.Current.stages.Add(_stage);
            return true;
        }

        int CurrentTurn
        {
            get { return _turn != null ? _turn.Turn : 0; }
        }

        static string Snapshot(IBoardView board)
        // 판을 "이름.진영.x.y.움직임여부|..." 문자열로 만드는 함수 -> 진영 P/E, 움직임 0/1
        {
            var sb = new StringBuilder();
            foreach (var p in board.AllPieces)
            {
                if (sb.Length > 0)
                    sb.Append('|');
                sb.Append(p.Def.name).Append('.')
                  .Append(p.Team == Team.Player ? 'P' : 'E').Append('.')
                  .Append(p.Coord.x).Append('.')
                  .Append(p.Coord.y).Append('.')
                  .Append(p.HasMoved ? '1' : '0');
            }
            return sb.ToString();
        }

        // ---------- 이벤트 처리 ----------

        void OnDragStarted(Piece piece, IReadOnlyList<MoveOption> options)
        {
            if (!EnsureStage())
                return;

            _heldPiece = piece;
            _heldFrom = piece.Coord;
            _heldAt = Time.unscaledTime;
            _heldOptions = options.Count;
            _heldBoard = Snapshot(_state);
            _heldCopy = _stage.isDuel ? SearchBoard.CopyOf(_state) : null;
            _heldCapture = false;
            _heldVictims.Clear();

            foreach (var p in _state.AllPieces)
                RunLog.Current.RegisterName(p.Def);

            foreach (var option in options)
            {
                if (!option.isCapture)
                    continue;
                _heldCapture = true;
                var victim = _state.GetPiece(option.target);
                if (victim != null)
                    _heldVictims[option.target] = victim.Def.name;
            }
        }

        void OnDragEnded(Piece piece, Coord target, DragController.DragOutcome outcome)
        {
            if (_heldPiece != piece || _stage == null)
                return;
            _heldPiece = null;

            string victim = "";
            bool captured = outcome == DragController.DragOutcome.Moved && _heldVictims.TryGetValue(target, out victim);

            RunLog.Current.moves.Add(new MoveRecord
            {
                stage = _stage.index,
                turn = CurrentTurn,
                piece = piece.Def.name,
                fromX = _heldFrom.x,
                fromY = _heldFrom.y,
                toX = target.x,
                toY = target.y,
                outcome = outcome.ToString(),
                holdSec = Time.unscaledTime - _heldAt,
                optionCount = _heldOptions,
                hadCaptureOption = _heldCapture,
                captured = captured,
                victim = captured ? victim : "",
                board = _heldBoard,
                before = _heldCopy,
                promotion = _stage.isDuel && _director.PromotionChoices.Count > 0 ? _director.PromotionChoices[0] : null
            });
        }

        void OnPhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.Deploy)  // 같은 씬에서 스테이지를 다시 시작하면 새 스테이지로 기록
            {
                _stageClosed = false;
                _stage = null;
                return;
            }
            if (!EnsureStage())
                return;

            if (phase == TurnPhase.EnemyTurn && !_stage.isDuel)
                RecordTurnEnd();
            else if (phase == TurnPhase.Victory || phase == TurnPhase.Defeat)
                CloseStage(phase);
        }

        void RecordTurnEnd()    // 턴을 넘기는 순간 남은 AP로 잡을 수 있었던 기회를 찾아 적는 함수
        {
            var record = new TurnEndRecord
            {
                stage = _stage.index,
                turn = CurrentTurn,
                apLeft = _turn.Ap,
                board = Snapshot(_state)
            };

            foreach (var piece in _state.AllPieces)
            {
                if (piece.Team != Team.Player || piece.Def.moveApCost > _turn.Ap)   // 이 시점은 이미 적 턴이라 CanAfford는 항상 false -> AP를 직접 비교
                    continue;
                MoveResolver.GetLegalMoves(_state, piece, _optionBuffer, _blockedBuffer);
                foreach (var option in _optionBuffer)
                {
                    if (!option.isCapture)
                        continue;
                    var victim = _state.GetPiece(option.target);
                    if (victim != null)
                        record.missedCaptures.Add(piece.Def.name + ">" + victim.Def.name);
                }
            }

            RunLog.Current.turnEnds.Add(record);
        }

        void CloseStage(TurnPhase phase)    // 스테이지 결과를 적는 함수 -> 패배하거나 최종전을 이기면 런도 끝남
        {
            _stageClosed = true;
            _stage.outcome = phase.ToString();
            _stage.reason = _director.OutcomeReason;
            _stage.turns = CurrentTurn;

            if (phase == TurnPhase.Defeat || _stage.isDuel)
            {
                RunLog.Current.finished = true;
                RunLog.Current.result = phase.ToString();
            }
        }

        void OnDeployed(PieceDefinition def)
        {
            if (EnsureStage())
                RunLog.Current.deploys.Add(MakeEvent(def));
        }

        void OnPromoted(PieceDefinition def)
        {
            if (EnsureStage())
                RunLog.Current.promotions.Add(MakeEvent(def));
        }

        void OnGuideOpened(PieceDefinition def)
        {
            if (EnsureStage())
                RunLog.Current.guideOpens.Add(MakeEvent(def));
        }

        PieceEvent MakeEvent(PieceDefinition def)
        {
            RunLog.Current.RegisterName(def);
            return new PieceEvent { stage = _stage.index, turn = CurrentTurn, piece = def.name };
        }

        void OnCaptured(Piece attacker, Piece target)
        {
            if (target.Team != Team.Player || !EnsureStage())
                return;
            RunLog.Current.RegisterName(target.Def);
            RunLog.Current.RegisterName(attacker.Def);
            RunLog.Current.losses.Add(new LossRecord
            {
                stage = _stage.index,
                turn = CurrentTurn,
                piece = target.Def.name,
                by = attacker.Def.name
            });
        }

        void OnCheck(Team team)
        {
            if (!EnsureStage())
                return;
            RunLog.Current.checks.Add(new CheckRecord
            {
                stage = _stage.index,
                turn = CurrentTurn,
                checkedTeam = team.ToString()
            });
        }
    }
}
