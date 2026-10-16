using Promotion.Core;
using Promotion.Data;
using Promotion.UI;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(TurnManager))]
    public class ShopLauncher : MonoBehaviour   // 스테이지에서 승리하면 보상 골드를 주고 상점을 여는 클래스
    // 승패를 직접 판정하지 않음 -> StageDirector가 승리를 선언하면(턴 단계가 Victory로 바뀌면) 그 결과만 받아서 움직임
    {
        [Header("상점")]
        public ShopPool shopPool;   // 상점에 진열할 상품 목록 데이터

        [Tooltip("이 씬에 (꺼진 상태로) 배치된 상점 화면. 비워 두면 자동으로 찾는다.")]
        public ShopScreen shopScreen;   // 상점 화면 UI

        [Tooltip("스테이지 클리어 보상 골드. 상점이 열리기 직전에 지급한다.")]
        [Min(0)] public int clearReward = 6;    // 스테이지 클리어 보상 골드

        [Tooltip("마지막 사망 연출 뒤의 여유 시간. 메뉴보다 승리 장면이 먼저 보이게 한다.")]
        [Min(0f)] public float shopDelay = 1.1f;    // 승리 후 상점이 열리기까지 기다리는 시간(초)

        [Header("다음 스테이지")]
        [Tooltip("상점의 확인 버튼이 불러올 씬. 비워 두면 전환 대신 로그만 남긴다.")]
        public string nextSceneName = "";   // 상점을 닫은 뒤 불러올 씬 이름

        TurnManager _turn;  // 턴 단계가 바뀌는 것을 구독할 컴포넌트
        bool _opened;   // 이미 상점을 열었는지 여부 -> 한 스테이지에서 두 번 열리지 않게 함

        void Awake() => _turn = GetComponent<TurnManager>();    // 오브젝트가 생성될 때 한 번 실행 -> TurnManager를 찾아 둠

        void OnEnable() => _turn.PhaseChanged += OnPhaseChanged;    // 컴포넌트가 켜질 때 턴 단계 변경 이벤트를 구독

        void OnDisable() => _turn.PhaseChanged -= OnPhaseChanged;   // 컴포넌트가 꺼질 때 구독 해제

        void OnPhaseChanged(TurnPhase phase)    // 턴 단계가 바뀔 때 발생하는 이벤트 -> 승리라면 상점을 열 준비를 함
        {
            if (_opened || phase != TurnPhase.Victory)  // 이미 열었거나 승리가 아니라면 무시
                return;

            var director = GetComponent<StageDirector>();
            if (director != null && director.IsChessDuel)   // 최종전(체스 대결)은 마지막 스테이지이므로 상점을 열지 않음
                return;

            _opened = true; // 열었다고 표시하고
            RunState.Instance.AddGold(clearReward); // 클리어 보상 골드 지급

            Tween.Delay(shopDelay, OpenShop, useUnscaledTime: true);
            // 승리 장면이 먼저 보이도록 잠깐 뒤에 상점을 엶(타격 멈춤으로 시간이 느려져도 실제 시간 기준으로 기다림)
        }

        void OpenShop() // 상점 화면을 여는 함수
        {
            if (this == null || !isActiveAndEnabled)    // 기다리는 동안 씬이 바뀌었거나 컴포넌트가 꺼졌다면 열지 않음
                return;

            var run = RunState.Instance;    // 런 전체 진행 정보(스테이지 번호, 골드 등)
            string headline = $"스테이지 {run.StageIndex + 1} 클리어   ·   골드 +{clearReward} 획득";    // 상점 위쪽에 보여 줄 문구
            if (shopScreen == null) // 상점 화면이 지정되지 않았다면 씬에서 꺼진 것까지 포함해 찾음
                shopScreen = FindAnyObjectByType<ShopScreen>(FindObjectsInactive.Include);
            if (shopScreen == null) // 그래도 없다면 경고를 남기고 상점 없이 다음 스테이지로 넘어감
            {
                Debug.LogWarning("[ShopLauncher] No ShopScreen in the scene - skipping the shop.");
                GoToNextStage();
                return;
            }

            shopScreen.Open(shopPool, headline, GoToNextStage); // 상점을 열고, 확인 버튼을 누르면 GoToNextStage가 호출되도록 넘겨줌
        }

        void GoToNextStage()    // 상점을 닫은 뒤 다음 스테이지로 넘어가는 함수
        {
            var run = RunState.Instance;
            run.AdvanceStage(); // 스테이지 번호를 다음으로 올림

            if (string.IsNullOrWhiteSpace(nextSceneName))   // 다음 씬 이름이 비어 있다면 이동하지 않고 현재 상태만 로그로 남김
            {
                Debug.Log($"[ShopLauncher] Shop confirmed → stage {run.StageIndex + 1}. " +
                          $"Gold {run.Gold}, deck {run.Deck.Count}, artifacts {run.Artifacts.Count}. " +
                          "Set nextSceneName once the stage-select scene exists.");
                return;
            }

            InputGate.Reset();  // 열려 있던 UI 때문에 막힌 입력 상태를 초기화하고
            SceneManager.LoadScene(nextSceneName);  // 다음 씬을 불러옴
        }
    }
}
