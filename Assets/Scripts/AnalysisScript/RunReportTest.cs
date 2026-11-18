using System.Collections.Generic;
using System.Linq;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Promotion.Analysis
{
    public class RunReportTest : MonoBehaviour
    // 복기 리포트 화면을 바로 확인하는 테스트 씬 전용 클래스 (Scenes/ReportTest)
    // -> 예시 런 기록을 실제 분석기로 돌려 화면에 띄움. 숫자 키로 다른 예시를 볼 수 있음
    {
        [SerializeField] RunReportView _reportPrefab;
        [SerializeField] PieceDefinition[] _pieces;

        Dictionary<string, PieceDefinition> _catalog;
        RunReportView _current;
        bool _useServer;    // 0번 키로 켬 -> 켜면 예시를 열 때마다 실제 AI를 호출해 비용이 듦

        void Start()
        {
            _catalog = _pieces.Where(p => p != null).ToDictionary(p => p.name);
            Open(MistakeRun());
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                Open(MistakeRun());
            else if (keyboard.digit2Key.wasPressedThisFrame)
                Open(CleanWinRun());
            else if (keyboard.digit3Key.wasPressedThisFrame)
                Open(DefenseOnlyRun());
            else if (keyboard.digit0Key.wasPressedThisFrame)
                _useServer = !_useServer;
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12f, 6f, 900f, 26f), "1: 실수 많은 패배   2: 깔끔한 승리   3: 디펜스에서 패배   0: 실제 AI 호출 " + (_useServer ? "켜짐 (열 때마다 비용 발생)" : "꺼짐"));
        }

        void Open(RunLog log)
        {
            if (_current != null)
                Destroy(_current.gameObject);
            _current = Instantiate(_reportPrefab);
            _current.UseServer = _useServer;
            _current.TestRun = true;
            _current.SaveToArchive = false;     // 예시 런은 기록소에 남기지 않음
            _current.Show(log, OnClosed);
        }

        void OnClosed()     // 실제 게임에서는 타이틀로 이동하는 자리 -> 테스트에서는 첫 예시를 다시 엶
        {
            Open(MistakeRun());
        }

        RunLog NewLog(string result)
        {
            var log = new RunLog { result = result, finished = true };
            foreach (var def in _pieces)
                log.RegisterName(def);
            return log;
        }

        SearchBoard Parse(string snapshot)  // 판 문자열을 엔진이 평가할 수 있는 보드로 만드는 함수
        {
            var board = new SearchBoard(8, 8);
            foreach (var entry in snapshot.Split('|'))
            {
                var p = entry.Split('.');
                board.Add(_catalog[p[0]], p[1] == "P" ? Team.Player : Team.Enemy,
                    new Coord(int.Parse(p[2]), int.Parse(p[3])), p[4] == "1");
            }
            return board;
        }

        void AddDuelMove(RunLog log, string snapshot, string piece, string move, int stage, int turn, float hold)
        {
            log.moves.Add(new MoveRecord
            {
                stage = stage,
                turn = turn,
                piece = piece,
                fromX = move[0] - 'a',
                fromY = move[1] - '1',
                toX = move[3] - 'a',
                toY = move[4] - '1',
                outcome = "Moved",
                holdSec = hold,
                board = snapshot,
                before = Parse(snapshot),
                promotion = _catalog["Queen"]
            });
        }

        static void AddDefenseMove(RunLog log, string piece, int turn, float hold, bool captured)
        {
            log.moves.Add(new MoveRecord
            {
                stage = 0,
                turn = turn,
                piece = piece,
                outcome = "Moved",
                holdSec = hold,
                captured = captured,
                victim = captured ? "EnemyGrunt" : "",
                board = ""
            });
        }

        static void AddMissedTurn(RunLog log, int turn, string snapshot)
        {
            log.turnEnds.Add(new TurnEndRecord
            {
                stage = 0,
                turn = turn,
                apLeft = 2,
                board = snapshot,
                missedCaptures = new List<string> { "Rook>EnemyGrunt" }
            });
        }

        static void AddLoss(RunLog log, string piece, int turn)
        {
            log.losses.Add(new LossRecord { stage = 0, turn = turn, piece = piece, by = "EnemyGrunt" });
        }

        const string DefenseBoard = "King.P.4.0.1|Rook.P.2.4.1|Bishop.P.5.2.1|EnemyGrunt.E.3.4.0|EnemyGrunt.E.1.5.0|EnemyGrunt.E.6.6.0";
        const string BackRank = "King.P.6.0.1|Rook.P.0.0.0|King.E.6.7.1|Pawn_Duel.E.5.6.0|Pawn_Duel.E.6.6.0|Pawn_Duel.E.7.6.0";

        RunLog MistakeRun()     // 디펜스를 지나 최종전에서 실수를 많이 하고 진 런
        {
            var log = NewLog("Defeat");
            log.stages.Add(new StageRecord { index = 0, name = "Stage_01", outcome = "Victory", turns = 9 });
            log.stages.Add(new StageRecord { index = 1, name = "Stage_Final", isDuel = true, outcome = "Defeat", reason = "체크메이트", turns = 12 });

            AddDefenseMove(log, "Rook", 2, 2f, true);
            AddDefenseMove(log, "Rook", 3, 1.5f, true);
            for (int i = 0; i < 3; i++)
                AddDefenseMove(log, "Bishop", 3 + i, 6.5f, false);
            log.deploys.Add(new PieceEvent { stage = 0, turn = 1, piece = "Rook" });
            log.deploys.Add(new PieceEvent { stage = 0, turn = 2, piece = "Bishop" });
            log.deploys.Add(new PieceEvent { stage = 0, turn = 4, piece = "Knight" });
            AddMissedTurn(log, 4, DefenseBoard);
            AddMissedTurn(log, 6, DefenseBoard);
            AddLoss(log, "Bishop", 7);
            AddLoss(log, "Bishop", 8);

            AddDuelMove(log, "King.P.4.0.1|Queen.P.3.0.0|Pawn_Duel.P.0.1.0|Pawn_Duel.P.7.1.0|King.E.6.7.1|Rook.E.3.7.0|Pawn_Duel.E.6.6.0", "Queen", "d1-d7", 1, 3, 4f);
            AddDuelMove(log, BackRank, "Rook", "a1-a2", 1, 7, 8f);
            AddDuelMove(log, "King.P.6.0.1|Pawn_Duel.P.5.1.0|Pawn_Duel.P.6.1.0|Pawn_Duel.P.7.1.0|Knight.P.1.2.1|King.E.6.7.1|Rook.E.0.7.0|Pawn_Duel.E.5.6.0|Pawn_Duel.E.6.6.0|Pawn_Duel.E.7.6.0", "Knight", "b3-d4", 1, 11, 3f);
            return log;
        }

        RunLog CleanWinRun()    // 최종전까지 큰 실수 없이 이긴 런
        {
            var log = NewLog("Victory");
            log.stages.Add(new StageRecord { index = 0, name = "Stage_01", outcome = "Victory", turns = 7 });
            log.stages.Add(new StageRecord { index = 1, name = "Stage_Final", isDuel = true, outcome = "Victory", reason = "체크메이트", turns = 5 });

            for (int i = 0; i < 4; i++)
                AddDefenseMove(log, "Rook", 2 + i, 1.2f, true);
            log.promotions.Add(new PieceEvent { stage = 0, turn = 5, piece = "Queen" });
            log.moves.Add(new MoveRecord { stage = 0, turn = 6, piece = "King", outcome = "Castled", holdSec = 1f, board = "" });
            for (int i = 0; i < 5; i++)
                AddDuelMove(log, BackRank, "Rook", "a1-a8", 1, 1 + i, 2f);
            return log;
        }

        RunLog DefenseOnlyRun() // 최종전까지 가지 못하고 디펜스에서 진 런
        {
            var log = NewLog("Defeat");
            log.stages.Add(new StageRecord { index = 0, name = "Stage_01", outcome = "Defeat", reason = "킹 함락", turns = 6 });

            AddDefenseMove(log, "Rook", 2, 2f, false);
            log.deploys.Add(new PieceEvent { stage = 0, turn = 1, piece = "Rook" });
            log.deploys.Add(new PieceEvent { stage = 0, turn = 2, piece = "Knight" });
            AddMissedTurn(log, 3, DefenseBoard);
            AddLoss(log, "Rook", 4);
            AddLoss(log, "Knight", 5);
            return log;
        }
    }
}
