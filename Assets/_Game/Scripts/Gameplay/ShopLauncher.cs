using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.UI;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Opens the shop when the stage is won. It listens for the victory the
    /// StageDirector already declares rather than judging the board itself, so
    /// there is exactly one definition of "the stage is over" in the project.
    /// </summary>
    [RequireComponent(typeof(TurnManager))]
    public class ShopLauncher : MonoBehaviour
    {
        [Header("Shop")]
        public ShopPool shopPool;

        [Tooltip("Gold awarded for clearing the stage, paid in just before the shop opens.")]
        [Min(0)] public int clearReward = 6;

        [Tooltip("Breathing room after the last death animation, so the win is seen before the menu.")]
        [Min(0f)] public float shopDelay = 1.1f;

        [Header("Next stage")]
        [Tooltip("Scene loaded by the shop's confirm button. Empty logs the transition instead.")]
        public string nextSceneName = "";

        TurnManager _turn;
        bool _opened;

        void Awake() => _turn = GetComponent<TurnManager>();

        void OnEnable() => _turn.PhaseChanged += OnPhaseChanged;

        void OnDisable() => _turn.PhaseChanged -= OnPhaseChanged;

        void OnPhaseChanged(TurnPhase phase)
        {
            if (_opened || phase != TurnPhase.Victory)
                return;

            _opened = true;
            RunState.Instance.AddGold(clearReward);

            // Unscaled: the killing blow may still be inside its hit-stop.
            Tween.Delay(shopDelay, OpenShop, useUnscaledTime: true);
        }

        void OpenShop()
        {
            // Destroyed during the delay (scene reloaded, stage restarted): nothing to open.
            if (this == null || !isActiveAndEnabled)
                return;

            var run = RunState.Instance;
            string headline = $"Stage {run.StageIndex + 1} cleared   ·   +{clearReward} gold earned";
            ShopScreen.Open(shopPool, headline, GoToNextStage);
        }

        void GoToNextStage()
        {
            var run = RunState.Instance;
            run.AdvanceStage();

            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.Log($"[ShopLauncher] Shop confirmed → stage {run.StageIndex + 1}. " +
                          $"Gold {run.Gold}, deck {run.Deck.Count}, artifacts {run.Artifacts.Count}. " +
                          "Set nextSceneName once the stage-select scene exists.");
                return;
            }

            // Screens die with the scene, so the block they pushed has to die with them.
            InputGate.Reset();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
