using Promotion.Core;
using Promotion.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.UI
{
    public class HudView : MonoBehaviour
    // 전투 화면의 HUD(상태 줄, AP 표시, 행동 버튼, 덱 카드, 승패 배너, 승격 선택창)를 관리하는 클래스
    // -> HUD 배치는 프리팹(Assets/UI/Prefabs)에 있고, 이 스크립트는 내용을 채우고 버튼을 연결·켜고 끄기만 함
    // -> 게임 진행 이벤트(턴·AP·단계 변경 등)를 구독해서 바뀔 때마다 Refresh로 화면 전체를 다시 그림
    {
        // -----HUD 색상-----
        public static readonly Color Panel = new Color(0.07f, 0.07f, 0.10f, 0.85f);  // 기본 패널 바탕 색 (AP 패널 등)
        public static readonly Color Ink = new Color(0.94f, 0.93f, 0.90f);  // 기본 글자 색
        public static readonly Color ButtonIdle = new Color(0.20f, 0.20f, 0.26f, 0.95f);  // 누를 수 있는 버튼 색
        static readonly Color ButtonOff = new Color(0.14f, 0.14f, 0.17f, 0.80f);  // 누를 수 없는 버튼 색
        static readonly Color ApLockedPanel = new Color(0.32f, 0.10f, 0.10f, 0.90f);  // 최종전(체스 결투)에서 AP를 못 쓴다는 표시용 붉은 패널 색
        static readonly Color CheckInk = new Color(1f, 0.45f, 0.45f);  // 내 킹이 체크 상태일 때 상태 줄 글자 색 (빨강)

        const string TitleScene = "Title";  // 타이틀로 돌아갈 때 불러올 씬 이름

        StageDirector _director;  // 스테이지 진행·승패 판정을 담당하는 오브젝트
        TurnManager _turn;  // 턴·AP·현재 단계(배치/플레이어 턴/적 턴/승리/패배)를 관리하는 오브젝트
        DeployController _deploy;  // 기물 배치를 담당하는 오브젝트 -> 킹 배치 여부가 바뀌면 시작 버튼을 갱신하려고 구독함
        // -> 기물 소환 UI는 인벤토리(InventoryManager)의 기물 슬롯이 맡음 (예전 오른쪽 "소환" 목록은 삭제)

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] TextMeshProUGUI _status;  // 화면 위쪽 상태 줄 (턴·웨이브·적 수 또는 안내 문구)
        [SerializeField] TextMeshProUGUI _ap;  // AP(행동력) 표시 텍스트 UI
        [SerializeField] Image _apPanel;  // AP 표시 뒤 패널 이미지 -> 최종전에서는 붉게 바뀜
        [SerializeField] GameObject _bannerPanel;  // 승리/패배 배너 패널 (게임이 끝났을 때만 켜짐)
        [SerializeField] TextMeshProUGUI _banner;  // 승리/패배 문구를 표시하는 텍스트 UI
        [SerializeField] Button _action;  // 행동 버튼 (스테이지 시작 / 턴 종료 / 결투 시작)
        [SerializeField] TextMeshProUGUI _actionLabel;  // 행동 버튼의 글자
        [SerializeField] Button _toTitle;  // 타이틀로 버튼 -> 패배(런 종료)하거나 최종전에서 이겼을 때 보임
        [SerializeField] RectTransform _promotionPanel;  // 폰 승격 시 기물을 고르는 선택창
        [SerializeField] RectTransform _promotionRow;  // 승격 선택 버튼들이 들어가는 줄
        [SerializeField] Button _choiceButtonPrefab;  // 승격 선택 버튼 프리팹 -> 선택지 수만큼 복제

        void Start()  // 시작할 때 필요한 오브젝트를 찾고, 버튼과 이벤트를 연결하는 함수
        {
            _director = FindAnyObjectByType<StageDirector>();  // 씬에서 StageDirector를 찾아서
            if (_director == null)  // 만약 없다면 (전투 씬이 아님) -> 경고를 남기고 HUD를 끔
            {
                Debug.LogWarning("[HudView] No StageDirector in the scene - HUD disabled.");
                enabled = false;
                return;
            }

            _turn = _director.GetComponent<TurnManager>();  // 같은 오브젝트에 붙은 TurnManager와
            _deploy = _director.GetComponent<DeployController>();  // DeployController를 가져옴

            EnsureEventSystem();  // 버튼 클릭을 받기 위한 EventSystem 확인

            // 버튼을 눌렀을 때 실행할 함수 연결
            _action.onClick.AddListener(OnActionPressed);  // 행동 버튼 → 스테이지 시작 / 턴 종료
            _toTitle.onClick.AddListener(ReturnToTitle);  // 타이틀로 버튼 → 런 초기화 후 타이틀 씬으로

            // 처음에는 결과 관련 UI를 모두 숨겨 둠
            _bannerPanel.SetActive(false);
            _toTitle.gameObject.SetActive(false);
            _promotionPanel.gameObject.SetActive(false);

            // 게임 상태가 바뀔 때마다 HUD가 갱신되도록 이벤트 구독
            _turn.PhaseChanged += OnPhaseChanged;  // 단계(배치/플레이어 턴/적 턴/승패) 변경
            _turn.ApChanged += OnApChanged;  // AP 변경
            _turn.TurnChanged += OnTurnChanged;  // 턴 수 변경
            _director.StateChanged += Refresh;  // 스테이지 상태(웨이브·적 수 등) 변경
            _director.CheckDeclared += OnCheckDeclared;  // 최종전에서 체크 발생
            _director.PromotionRequested += ShowPromotionChoice;  // 최종전에서 폰이 끝 줄에 도달해 승격 선택이 필요할 때
            _deploy.DeckChanged += Refresh;  // 덱·선택이 바뀌었을 때 (킹 배치 → 시작 버튼 활성화)

            Refresh();  // HUD 전체를 한 번 그림
        }

        void OnDestroy()  // HUD가 파괴될 때 구독했던 이벤트를 모두 해제하는 함수 -> 씬 이동 후 사라진 HUD가 호출되는 에러 방지
        {
            if (_turn != null)
            {
                _turn.PhaseChanged -= OnPhaseChanged;
                _turn.ApChanged -= OnApChanged;
                _turn.TurnChanged -= OnTurnChanged;
            }

            if (_director != null)
            {
                _director.StateChanged -= Refresh;
                _director.CheckDeclared -= OnCheckDeclared;
                _director.PromotionRequested -= ShowPromotionChoice;
            }

            if (_deploy != null)
                _deploy.DeckChanged -= Refresh;
        }

        // 각 이벤트는 넘겨받는 값이 달라서 함수 모양을 맞춰 주기만 하고, 실제로는 모두 Refresh로 HUD 전체를 다시 그림
        void OnPhaseChanged(TurnPhase phase) => Refresh();  // 단계가 바뀌었을 때
        void OnApChanged(int current, int max) => Refresh();  // AP가 바뀌었을 때
        void OnTurnChanged(int turn) => Refresh();  // 턴이 바뀌었을 때
        void OnCheckDeclared(Team side) => Refresh();  // 체크가 선언됐을 때

        static void EnsureEventSystem()  // UI 클릭을 받을 EventSystem을 준비하는 함수
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing == null)  // 만약 씬에 EventSystem이 없다면 -> 신 Input System용 모듈과 함께 새로 만듦
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            if (existing.GetComponent<InputSystemUIInputModule>() == null)  // 만약 있는데 신 Input System 모듈이 아니라면
            // -> 이 프로젝트는 신 Input System을 쓰므로 구식 모듈로는 UI를 조작할 수 없음
            {
                var legacy = existing.GetComponent<BaseInputModule>();  // 기존(구식) 입력 모듈을 찾아서
                if (legacy != null)
                    Destroy(legacy);  // 제거하고

                existing.gameObject.AddComponent<InputSystemUIInputModule>();  // 신 Input System용 모듈로 교체
            }
        }


        static void ReturnToTitle()  // 런을 끝내고 타이틀 화면으로 돌아가는 함수 (최종전 승리 후)
        {
            RunState.Instance.ResetRun();  // 골드·보유 기물·유물 등 이번 판 진행 정보를 초기화하고
            SceneManager.LoadScene(TitleScene);  // 타이틀 씬으로 이동
        }

        void ShowPromotionChoice()  // 폰 승격 시 고를 수 있는 기물 버튼들을 만들어 선택창을 띄우는 함수
        // -> 결투는 버튼 중 하나가 눌릴 때까지 기다림
        {
            for (int i = _promotionRow.childCount - 1; i >= 0; i--)  // 이전에 만들어 둔 선택 버튼이 있다면 뒤에서부터 모두 삭제
                Destroy(_promotionRow.GetChild(i).gameObject);

            var choices = _director.PromotionChoices;  // 승격 가능한 기물 목록 (퀸, 룩 등)
            for (int i = 0; i < choices.Count; i++)  // 선택지마다 버튼을 하나씩 만듦
            {
                var def = choices[i];
                if (def == null)  // 비어 있는 선택지는 건너뜀
                    continue;

                var button = Instantiate(_choiceButtonPrefab, _promotionRow);  // 선택 버튼 프리팹을 복제하고
                button.name = "Promote_" + i;
                button.GetComponentInChildren<TextMeshProUGUI>().text = def.itemName;  // 버튼 글자에 기물 이름 표시
                button.onClick.AddListener(() =>  // 버튼을 누르면
                {
                    _promotionPanel.gameObject.SetActive(false);  // 선택창을 닫고
                    _director.ChoosePromotion(def);  // 고른 기물로 승격하라고 StageDirector에 전달
                });
            }

            _promotionPanel.gameObject.SetActive(true);  // 선택창을 화면에 표시
        }

        void OnActionPressed()  // 행동 버튼을 눌렀을 때 실행되는 함수 -> 현재 단계에 따라 하는 일이 다름
        {
            if (_turn.Phase == TurnPhase.Deploy)  // 만약 배치 단계라면 -> 배치를 끝내고 스테이지 시작
                _director.ConfirmDeploy();
            else if (_turn.Phase == TurnPhase.PlayerTurn)  // 만약 플레이어 턴이라면 -> 턴 종료 요청 (적 턴으로 넘어감)
                _director.RequestEndTurn();
        }

        void Refresh()  // 현재 게임 상태에 맞게 HUD 전체(상태 줄, AP, 행동 버튼, 배너, 덱 카드 색)를 다시 그리는 함수
        {
            if (_turn == null)  // 아직 Start에서 준비가 안 됐다면 -> 아무것도 하지 않음
                return;

            if (_director.IsChessDuel)  // 만약 최종전(체스 결투)이라면 -> 결투 전용 표시로 그리고 끝냄
            {
                RefreshDuel();
                return;
            }

            // 일반 스테이지: 결투에서 바뀌었을 수 있는 표시를 기본값으로 되돌림
            _status.color = Ink;
            _ap.fontSize = 34f;
            _apPanel.color = Panel;

            // 상태 줄: 배치 단계라면 안내 문구, 전투 중이라면 턴·웨이브·적 수를 표시
            _status.text = _turn.Phase == TurnPhase.Deploy
                ? (_director.KingPlaced
                    ? "배치 완료 - 스테이지 시작을 누르세요"
                    : "킹을 배치해야 시작합니다 - 인벤토리 기물 칸에서 킹을 고르세요")
                : "턴 " + _turn.Turn
                  + "   |   웨이브 " + _director.WavesSent + "/" + _director.TotalWaves
                  + "   |   적 " + _director.EnemiesOnBoard;

            // AP 표시: 배치 단계에서는 기물을 무료로 놓으므로 "무료", 전투 중에는 현재 AP / 최대 AP
            _ap.text = _turn.Phase == TurnPhase.Deploy
                ? "배치: 무료"
                : "AP  " + _turn.Ap + " / " + _turn.MaxAp;

            bool over = _turn.IsOver;  // 승리 또는 패배로 스테이지가 끝났는지 여부
            _action.gameObject.SetActive(!over);  // 끝났다면 행동 버튼을 숨김

            if (!over)  // 만약 아직 진행 중이라면 -> 행동 버튼의 상태와 글자를 단계에 맞게 설정
            {
                bool interactable = _turn.Phase == TurnPhase.PlayerTurn
                                    || (_turn.Phase == TurnPhase.Deploy && _director.KingPlaced);
                // 버튼을 누를 수 있는 경우: 플레이어 턴이거나, 배치 단계에서 킹을 이미 놓았을 때 (킹 없이는 시작 불가)
                _action.interactable = interactable;
                _action.image.color = interactable ? ButtonIdle : ButtonOff;  // 누를 수 없으면 어두운 색

                if (_turn.Phase == TurnPhase.Deploy)  // 배치 단계 -> 킹을 놓았는지에 따라 문구 변경
                    _actionLabel.text = _director.KingPlaced ? "스테이지 시작" : "킹을 먼저 배치";
                else if (_turn.Phase == TurnPhase.PlayerTurn)  // 플레이어 턴
                    _actionLabel.text = "턴 종료";
                else  // 적 턴 (버튼은 비활성 상태)
                    _actionLabel.text = "적 턴...";
            }

            _bannerPanel.SetActive(over);  // 끝났을 때만 승패 배너를 보여 주고
            _toTitle.gameObject.SetActive(_turn.Phase == TurnPhase.Defeat);  // 패배하면 런이 끝나므로 타이틀로 버튼을 보여 줌
            // -> 승리하면 다른 시스템(상점 등)이 결과를 받아서 다음으로 넘어감

            if (over)  // 만약 끝났다면 -> 승패에 맞는 문구와 색(초록/빨강)을 배너에 표시
            {
                bool won = _turn.Phase == TurnPhase.Victory;
                _banner.text = won ? "승리" : "패배\n<size=50%>런 종료</size>";
                _banner.color = won ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.45f, 0.45f);
            }

        }

        void RefreshDuel()  // 최종전(체스 결투)용 HUD를 그리는 함수
        // -> 한 턴에 한 수만 두므로 AP는 사용 불가로 표시하고, 상태 줄에는 수 진행·체크 여부·AI 상태를 보여 줌
        {
            var phase = _turn.Phase;  // 현재 단계
            bool over = _turn.IsOver;  // 결투가 끝났는지 여부

            // AP 패널을 붉게 바꾸고 "AP 사용 불가"로 표시
            _apPanel.color = ApLockedPanel;
            _ap.fontSize = 24f;
            _ap.text = "AP 사용 불가\n<size=80%>체스 결투 - 한 턴에 한 수</size>";

            _status.color = _director.InCheck == Team.Player && !over ? CheckInk : Ink;  // 내 킹이 체크 상태라면 상태 줄을 빨간 글자로
            if (phase == TurnPhase.Deploy)  // 만약 배치 단계라면 -> 킹을 놓았는지에 따라 안내 문구 표시
            {
                _status.text = _director.KingPlaced
                    ? "킹과 기물(폰 최대 8)을 배치한 뒤 결투 시작을 누르세요"
                    : "최종전 - 킹을 배치해야 시작합니다";
            }
            else  // 결투 진행 중이거나 끝난 상태라면
            {
                string line = "최종전   |   " + (_director.DuelMovesPlayed + (phase == TurnPhase.PlayerTurn ? 1 : 0))
                              + "수 / " + _director.stage.moveLimit;
                // 기본 문구: "최종전 | 현재 수 / 제한 수" -> 플레이어 차례라면 지금 둘 수까지 포함해서 +1
                if (over)  // 끝났다면 수 진행만 표시
                    _status.text = line;
                else if (_director.AwaitingPromotion)  // 폰 승격 선택을 기다리는 중
                    _status.text = line + "   |   승격할 기물을 고르세요";
                else if (_director.AIThinking)  // 적(흑) AI가 수를 계산하는 중
                    _status.text = line + "   |   흑이 생각하는 중...";
                else if (_director.InCheck == Team.Player)  // 내 킹이 체크 상태
                    _status.text = line + "   |   체크! 킹을 지키세요";
                else if (_director.InCheck == Team.Enemy)  // 상대 킹이 체크 상태
                    _status.text = line + "   |   체크!";
                else  // 그 외에는 누구 차례인지 표시
                    _status.text = line + (phase == TurnPhase.PlayerTurn ? "   |   백의 차례" : "   |   흑의 차례");
            }

            bool deploying = phase == TurnPhase.Deploy;
            _action.gameObject.SetActive(deploying);  // 결투에서는 수를 두면 바로 턴이 넘어가므로 행동 버튼은 결투 시작용으로만 사용 -> 배치 단계에서만 보임
            if (deploying)
            {
                _action.interactable = _director.KingPlaced;  // 킹을 놓아야 누를 수 있음
                _action.image.color = _director.KingPlaced ? ButtonIdle : ButtonOff;
                _actionLabel.text = _director.KingPlaced ? "결투 시작" : "킹을 먼저 배치";
            }

            bool won = phase == TurnPhase.Victory;
            _bannerPanel.SetActive(over);  // 끝났을 때만 배너 표시
            _toTitle.gameObject.SetActive(over);  // 이기든 지든 최종전이 끝나면 런이 끝나므로 타이틀로 버튼

            if (over)  // 만약 결투가 끝났다면 -> 결과 배너 문구를 채움
            {
                string reason = _director.OutcomeReason;  // 결투가 끝난 이유 (체크메이트, 수 제한 등)
                if (!won && reason == "체크메이트")  // 만약 졌는데 이유가 체크메이트라면 -> 플레이어 입장에 맞게 "당함"을 붙임
                    reason = "체크메이트 당함";

                _banner.text = won
                    ? "체크메이트!\n<size=50%>런 클리어</size>"
                    : "패배\n<size=50%>" + reason + "</size>";
                _banner.color = won ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.45f, 0.45f);  // 승리는 초록, 패배는 빨강
            }
        }
    }
}
