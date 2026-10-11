using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 플레이테스트용 단축 기능이다. 상점으로 바로 이동하고, 결과를 강제하고,
    /// AP를 채우고, 다음 웨이브를 앞당긴다. IMGUI로 그리므로 캔버스가 필요 없고
    /// 게임 자체의 UI를 방해하지 않는다.
    ///
    /// 출시 전에 이 컴포넌트(또는 오브젝트 전체)를 제거한다.
    /// </summary>
    public class DevTools : MonoBehaviour
    {
        [Tooltip("스테이지가 시작될 때 단축 기능 패널을 표시한다.")]
        public bool visibleOnStart = true;

        [Tooltip("F11로 이동할 스테이지(최종 체스 대결).")]
        public StageDefinition finalStage;

        StageDirector _director;
        TurnManager _turn;
        DeployController _deploy;
        PieceRegistry _registry;
        BoardState _state;
        BoardGrid _grid;

        readonly List<Piece> _scratch = new();
        bool _visible;
        string _lastAction = "";

        void Awake()
        {
            _visible = visibleOnStart;
            _director = GetComponent<StageDirector>();
            _turn = GetComponent<TurnManager>();
            _deploy = GetComponent<DeployController>();
            _registry = GetComponent<PieceRegistry>();
            _state = GetComponent<BoardState>();
            _grid = GetComponent<BoardGrid>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame) _visible = !_visible;
            if (keyboard.f2Key.wasPressedThisFrame) OpenShop();
            if (keyboard.f3Key.wasPressedThisFrame) ForceOutcome(TurnPhase.Victory);
            if (keyboard.f4Key.wasPressedThisFrame) ForceOutcome(TurnPhase.Defeat);
            if (keyboard.f5Key.wasPressedThisFrame) GrantGold(10);
            if (keyboard.f6Key.wasPressedThisFrame) RefillAp();
            if (keyboard.f7Key.wasPressedThisFrame) SpawnWave();
            if (keyboard.f8Key.wasPressedThisFrame) ClearEnemies();
            if (keyboard.f9Key.wasPressedThisFrame) AutoDeployKing();
            if (keyboard.f10Key.wasPressedThisFrame) Restart();
            if (keyboard.f11Key.wasPressedThisFrame) JumpToFinal();
        }

        // ---------- 동작 ----------

        /// <summary>
        /// 상점을 직접 열지 않고 승리를 선언한다. 그래서 실제 게임과 같은 경로로
        /// 상점에 도달한다.
        /// </summary>
        void OpenShop()
        {
            ForceOutcome(TurnPhase.Victory);
            _lastAction = "Victory declared - shop opens via ShopLauncher";
        }

        void ForceOutcome(TurnPhase outcome)
        {
            StopAllCoroutines();
            _director.StopAllCoroutines();
            _turn.SetPhase(outcome);
            _lastAction = outcome.ToString() + " forced";
        }

        void GrantGold(int amount)
        {
            RunState.Instance.AddGold(amount);
            _lastAction = "+" + amount + " gold (now " + RunState.Instance.Gold + ")";
        }

        void RefillAp()
        {
            _turn.RefillAp();
            _lastAction = "AP refilled";
        }

        void SpawnWave()
        {
            _lastAction = _director.SpawnNextWaveNow()
                ? "Wave " + _director.WavesSent + " spawned early"
                : "No waves left";
        }

        void ClearEnemies()
        {
            _state.GetPiecesOf(Team.Enemy, _scratch);
            int count = _scratch.Count;

            foreach (var enemy in _scratch)
                _registry.Kill(enemy);

            _director.RefreshEnemyCount();
            _lastAction = count + " enemies removed";
        }

        /// <summary>킹을 맨 뒷줄 가운데에 놓고 스테이지를 시작한다.</summary>
        void AutoDeployKing()
        {
            if (_turn.Phase != TurnPhase.Deploy)
            {
                _lastAction = "Not in deploy phase";
                return;
            }

            if (!_director.KingPlaced)
            {
                int kingIndex = -1;
                for (int i = 0; i < _deploy.Deck.Count; i++)
                {
                    if (_deploy.Deck[i] != null && _deploy.Deck[i].isObjective)
                    {
                        kingIndex = i;
                        break;
                    }
                }

                if (kingIndex < 0)
                {
                    _lastAction = "No king in deck";
                    return;
                }

                _deploy.Select(kingIndex);
                if (!_deploy.TryDeployAt(new Coord(_grid.width / 2, 0)))
                {
                    _lastAction = "Could not place king";
                    return;
                }
            }

            // 체스 대결은 모든 기물을 미리 배치한다. 둘째 줄을 먼저 채우고 그다음 첫째 줄을 채운다.
            if (_director.IsChessDuel)
                PlaceRestOfDeck();

            _director.ConfirmDeploy();
            _lastAction = "King placed, stage started";
        }

        void PlaceRestOfDeck()
        {
            int rows = Mathf.Clamp(_deploy.deployRows, 1, _grid.height);
            for (int y = rows - 1; y >= 0 && _deploy.Deck.Count > 0; y--)
            {
                for (int x = 0; x < _grid.width && _deploy.Deck.Count > 0; x++)
                {
                    var square = new Coord(x, y);
                    if (!_state.IsEmpty(square))
                        continue;

                    _deploy.Select(0);
                    _deploy.TryDeployAt(square);
                }
            }
        }

        /// <summary>최종 체스 대결을 스테이지로 삼아 전투 씬을 다시 불러온다.</summary>
        void JumpToFinal()
        {
            if (finalStage == null)
            {
                _lastAction = "No final stage assigned on DevTools";
                return;
            }

            RunState.Instance.CurrentStage = finalStage;
            Restart();
        }

        static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // ---------- 오버레이 ----------

        void OnGUI()
        {
            if (!_visible)
            {
                GUI.Label(new Rect(10f, 10f, 300f, 22f), "F1: dev tools");
                return;
            }

            const float width = 330f;
            GUILayout.BeginArea(new Rect(10f, 10f, width, 370f), GUI.skin.box);

            GUILayout.Label("DEV TOOLS   (F1 to hide)");
            GUILayout.Label(Status());
            GUILayout.Space(6f);

            if (GUILayout.Button("F2  Open shop (force victory)")) OpenShop();
            if (GUILayout.Button("F3  Win stage")) ForceOutcome(TurnPhase.Victory);
            if (GUILayout.Button("F4  Lose stage")) ForceOutcome(TurnPhase.Defeat);
            if (GUILayout.Button("F5  +10 gold")) GrantGold(10);
            if (GUILayout.Button("F6  Refill AP")) RefillAp();
            if (GUILayout.Button("F7  Spawn next wave now")) SpawnWave();
            if (GUILayout.Button("F8  Remove all enemies")) ClearEnemies();
            if (GUILayout.Button("F9  Auto-place king and start")) AutoDeployKing();
            if (GUILayout.Button("F10 Restart stage")) Restart();
            if (GUILayout.Button("F11 Jump to final duel")) JumpToFinal();

            if (!string.IsNullOrEmpty(_lastAction))
            {
                GUILayout.Space(4f);
                GUILayout.Label("> " + _lastAction);
            }

            GUILayout.EndArea();
        }

        string Status()
        {
            if (_director == null || _turn == null)
                return "no stage";

            return _turn.Phase
                   + "   turn " + _turn.Turn
                   + "   AP " + _turn.Ap + "/" + _turn.MaxAp
                   + "\nwaves " + _director.WavesSent + "/" + _director.TotalWaves
                   + "   enemies " + _director.EnemiesOnBoard
                   + "   gold " + RunState.Instance.Gold
                   + "\nking placed: " + _director.KingPlaced
                   + (_director.IsChessDuel ? "   duel moves " + _director.DuelMovesPlayed : "");
        }
    }
}
