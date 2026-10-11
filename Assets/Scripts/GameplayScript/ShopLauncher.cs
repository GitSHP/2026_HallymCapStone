using Promotion.Core;
using Promotion.Data;
using Promotion.UI;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    /// <summary>
    /// Opens the shop when the stage is won. It listens for the victory the
    /// StageDirector already declares rather than judging the board itself, so
    /// there is exactly one definition of "the stage is over" in the project.
    /// </summary>
    [RequireComponent(typeof(TurnManager))]
    // [아이템 담당] 스테이지 승리 시 상점을 여는 진입점. 어떤 ShopPool 을 쓸지는
    // 씬의 Board 오브젝트 인스펙터에서 지정한다.
    public class ShopLauncher : MonoBehaviour
    {
        [Header("Shop")]
        public ShopPool shopPool;

        [Tooltip("The shop screen placed (switched off) in this scene. Found automatically when empty.")]
        public ShopScreen shopScreen;

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

            // Winning the chess duel ends the run; there is nothing left to shop for.
            var director = GetComponent<StageDirector>();
            if (director != null && director.IsChessDuel)
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
            string headline = $"스테이지 {run.StageIndex + 1} 클리어   ·   골드 +{clearReward} 획득";
            if (shopScreen == null)
                shopScreen = FindAnyObjectByType<ShopScreen>(FindObjectsInactive.Include);
            if (shopScreen == null)
            {
                Debug.LogWarning("[ShopLauncher] No ShopScreen in the scene - skipping the shop.");
                GoToNextStage();
                return;
            }

            shopScreen.Open(shopPool, headline, GoToNextStage);
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
