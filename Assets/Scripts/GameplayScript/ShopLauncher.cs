using Promotion.Core;
using Promotion.Data;
using Promotion.UI;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 스테이지에서 승리하면 상점을 연다. 보드를 직접 판정하지 않고 StageDirector가
    /// 이미 선언한 승리를 구독한다. 그래서 프로젝트 안에서 "스테이지가 끝났다"의 정의는
    /// 정확히 하나뿐이다.
    /// </summary>
    [RequireComponent(typeof(TurnManager))]
    // [아이템 담당] 스테이지 승리 시 상점을 여는 진입점. 어떤 ShopPool 을 쓸지는
    // 씬의 Board 오브젝트 인스펙터에서 지정한다.
    public class ShopLauncher : MonoBehaviour
    {
        [Header("상점")]
        public ShopPool shopPool;

        [Tooltip("이 씬에 (꺼진 상태로) 배치된 상점 화면. 비워 두면 자동으로 찾는다.")]
        public ShopScreen shopScreen;

        [Tooltip("스테이지 클리어 보상 골드. 상점이 열리기 직전에 지급한다.")]
        [Min(0)] public int clearReward = 6;

        [Tooltip("마지막 사망 연출 뒤의 여유 시간. 메뉴보다 승리 장면이 먼저 보이게 한다.")]
        [Min(0f)] public float shopDelay = 1.1f;

        [Header("다음 스테이지")]
        [Tooltip("상점의 확인 버튼이 불러올 씬. 비워 두면 전환 대신 로그만 남긴다.")]
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

            // 체스 대결에서 이기면 런이 끝난다. 더 이상 상점에서 살 이유가 없다.
            var director = GetComponent<StageDirector>();
            if (director != null && director.IsChessDuel)
                return;

            _opened = true;
            RunState.Instance.AddGold(clearReward);

            // 스케일되지 않은 시간을 쓴다. 마지막 일격이 아직 히트스톱 중일 수 있기 때문이다.
            Tween.Delay(shopDelay, OpenShop, useUnscaledTime: true);
        }

        void OpenShop()
        {
            // 지연 중에 파괴되었다(씬 재로드, 스테이지 재시작). 열 것이 없다.
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

            // 화면은 씬과 함께 사라지므로, 화면이 걸어 둔 입력 차단도 함께 사라져야 한다.
            InputGate.Reset();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
