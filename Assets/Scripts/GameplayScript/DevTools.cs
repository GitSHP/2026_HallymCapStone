using System.Collections.Generic;
using Gambonanza.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Playtest shortcuts: jump straight to the shop, force an outcome, refill
    /// action points, pull the next wave in early. Drawn with IMGUI so it needs
    /// no canvas and cannot interfere with the game's own UI.
    ///
    /// Strip this component (or the whole object) before shipping.
    /// </summary>
    public class DevTools : MonoBehaviour
    {
        [Tooltip("Show the shortcut panel when the stage starts.")]
        public bool visibleOnStart = true;

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
        }

        // ---------- actions ----------

        /// <summary>
        /// Declares victory rather than opening the shop directly, so the shop is
        /// reached through the same path the real game uses.
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

        /// <summary>Drops the king on the middle of the back rank and starts the stage.</summary>
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

            _director.ConfirmDeploy();
            _lastAction = "King placed, stage started";
        }

        static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // ---------- overlay ----------

        void OnGUI()
        {
            if (!_visible)
            {
                GUI.Label(new Rect(10f, 10f, 300f, 22f), "F1: dev tools");
                return;
            }

            const float width = 330f;
            GUILayout.BeginArea(new Rect(10f, 10f, width, 340f), GUI.skin.box);

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
                   + "\nking placed: " + _director.KingPlaced;
        }
    }
}
