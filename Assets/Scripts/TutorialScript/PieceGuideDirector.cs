using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using Promotion.Gameplay;
using UnityEngine;

namespace Promotion.Tutorial
{
    public class PieceGuideDirector : MonoBehaviour // 기물을 처음 소환(또는 폰이 처음 그 기물로 승격)했을 때 그 기물의 가이드 창을 띄우는 클래스
    // -> DeployController가 보내는 소환 이벤트(PieceDeployed)를 구독함 -> DeployController는 가이드가 있는지조차 모름
    // -> 가이드가 떠 있는 동안 게임 시간을 멈추고(timeScale 0) 보드 입력을 막음
    // -> 본 기록은 PlayerPrefs에 영구 저장되므로 튜토리얼 포함 기물마다 딱 한 번만 보여 줌. 최종전(체스 대결)에서는 띄우지 않음
    {
        [SerializeField] PieceGuideCatalog _catalog; // 기물 가이드 목록
        [SerializeField] PieceGuideView _view; // 가이드 창

        WaveSpawner _spawner; // 적 소환 이벤트를 보내는 컴포넌트 -> 처음 보는 적의 가이드
        DeployController _deploy; // 소환 이벤트를 보내는 컴포넌트
        StageDirector _stage; // 지금 스테이지가 최종전인지 확인하는 데 사용
        TurnManager _turn; // 승패가 이미 났는지 확인하는 데 사용
        DialogueView _dialogue; // 튜토리얼 대화창 -> 대화가 끝날 때까지 가이드를 미룸 (튜토리얼이 아니면 꺼져 있음)

        readonly Queue<PieceDefinition> _queue = new(); // 보여 줄 차례를 기다리는 기물 목록 -> 짧은 사이에 여러 기물을 소환해도 하나씩 차례로 보여 줌
        bool _running; // 대기열을 처리하는 코루틴이 돌고 있는지 여부
        bool _paused; // 지금 가이드 때문에 게임을 멈춰 두었는지 여부

        void Start()
        {
            _view.Hide(); // 처음에는 창을 숨겨 둠

            _deploy = FindAnyObjectByType<DeployController>(); // 전투 씬의 Board 오브젝트에 붙은 소환 담당 컴포넌트를 찾아
            if (_deploy != null)
            {
                _deploy.PieceDeployed += OnPieceDeployed; // 기물이 소환될 때마다 OnPieceDeployed가 실행되게 연결
                _turn = _deploy.GetComponent<TurnManager>();
                _stage = _deploy.GetComponent<StageDirector>();
                if (_stage != null)
                    _stage.PlayerPromoted += OnPieceDeployed;   // 폰이 처음 보는 기물로 승격해도 소환과 똑같이 가이드를 띄움 (본 기록도 함께 남음)
                _spawner = _deploy.GetComponent<WaveSpawner>();
                if (_spawner != null)
                    _spawner.EnemySpawned += OnPieceDeployed;   // 적이 처음 나왔을 때도 같은 방식으로 가이드를 띄움 (적 가이드가 있는 적만: 중갑병)
            }
            _dialogue = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include); // 꺼져 있는 대화창까지 포함해서 찾음
        }

        void OnDestroy()
        {
            if (_deploy != null)
                _deploy.PieceDeployed -= OnPieceDeployed; // 연결을 끊고
            if (_spawner != null)
                _spawner.EnemySpawned -= OnPieceDeployed;
            if (_stage != null)
                _stage.PlayerPromoted -= OnPieceDeployed;

            if (_paused) // 가이드가 떠 있는 채로 씬이 바뀌었다면 멈춘 게임 시간을 되돌림 -> 다음 씬이 멈춘 채로 시작되지 않게
                Resume();
        }

        void Update()
        {
            if (_paused && Time.timeScale != 0f) // 가이드가 떠 있는데 다른 곳(히트 스톱 등)이 게임 시간을 되돌렸다면
                Time.timeScale = 0f; // 다시 멈춤
        }

        // ---------- 본 기록 (영구 저장) ----------
        // -> 튜토리얼을 포함해 기물마다 딱 한 번만 보여 주기 위해, 런이 바뀌거나 게임을 껐다 켜도 남도록 PlayerPrefs에 저장함
        // -> 튜토리얼이 끝날 때 런 정보(RunState)를 초기화해도 이 기록은 지워지지 않음

        const string SeenKey = "PieceGuideSeen"; // 본 기물 이름을 '|'로 이어 저장하는 키

        static bool HasSeen(PieceDefinition piece) // 그 기물의 가이드를 이미 본 적이 있는지
        {
            if (piece == null)
                return false;
            return RunState.Instance.HasSeenGuide(piece) || ("|" + PlayerPrefs.GetString(SeenKey, "") + "|").Contains("|" + piece.name + "|");
        }

        static void MarkSeen(PieceDefinition piece) // 그 기물의 가이드를 본 것으로 기록 (이번 런 + 영구)
        {
            RunState.Instance.MarkGuideSeen(piece);
            if (("|" + PlayerPrefs.GetString(SeenKey, "") + "|").Contains("|" + piece.name + "|"))
                return;
            string seen = PlayerPrefs.GetString(SeenKey, "");
            PlayerPrefs.SetString(SeenKey, seen.Length == 0 ? piece.name : seen + "|" + piece.name);
            PlayerPrefs.Save();
        }

        public static void ResetSeen() // 본 기록을 모두 지우는 함수 (개발자 도구 F12) -> 다음 소환 때 다시 보여 줌
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
            RunState.Instance.ClearSeenGuides();
        }

        void OnPieceDeployed(PieceDefinition piece)
        // 기물이 보드에 소환될 때마다 실행되는 함수 -> 처음 보는 기물이라면 대기열에 넣음
        // piece: 방금 소환된 기물 (DeployController.TryDeployAt에서 보냄)
        {
            if (piece == null || _catalog == null)
                return;
            if (_stage != null && _stage.IsChessDuel) // 최종전에서는 띄우지 않음
                return;
            if (HasSeen(piece) || _queue.Contains(piece)) // 이미 본 적이 있거나(튜토리얼 포함, 영구 기록) 이미 기다리는 중이라면
                return;
            if (_catalog.Find(piece) == null) // 가이드가 없는 기물이라면
                return;

            _queue.Enqueue(piece); // 대기열에 넣고
            if (!_running) // 처리 중이 아니라면 처리를 시작함
                StartCoroutine(RunQueue());
        }

        IEnumerator RunQueue()
        // 대기열의 기물 가이드를 하나씩 띄우는 코루틴
        {
            _running = true;

            while (_queue.Count > 0)
            {
                yield return new WaitForSecondsRealtime(Fx.P.guideShowDelay); // 기물이 칸에 내려앉는 연출을 먼저 보여 주고

                while (_dialogue != null && _dialogue.IsTalking) // 튜토리얼 대화가 진행 중이라면 끝날 때까지 기다림
                    yield return null;

                if (Ended) // 그사이 승패가 났다면 가이드를 띄우지 않음
                {
                    _queue.Clear();
                    break;
                }

                var piece = _queue.Dequeue();
                if (HasSeen(piece)) // 이미 본 기물이라면 건너뜀
                    continue;
                MarkSeen(piece); // 본 것으로 영구 기록

                Pause(); // 게임을 멈추고
                yield return _view.Show(_catalog.Find(piece)); // 플레이어가 [확인]을 누를 때까지 창을 보여 준 뒤
                Resume(); // 게임을 다시 진행함
            }

            _running = false;
        }

        // ---------- 직접 열기 (인벤토리 우클릭 메뉴의 [설명]) ----------

        public bool HasGuide(PieceDefinition piece) // 그 기물의 가이드가 있는지 확인하는 함수 -> 없으면 [설명] 버튼을 숨김
        {
            return piece != null && _catalog != null && _catalog.Find(piece) != null;
        }

        public bool ShowGuide(PieceDefinition piece)
        // 플레이어가 고른 기물의 가이드를 바로 띄우는 함수 -> 띄웠으면 true
        // -> 처음 소환 때 자동으로 뜨는 것과 같은 창을 쓰며, 본 기록은 건드리지 않음
        {
            if (!HasGuide(piece) || _running || _paused) // 가이드가 없거나 다른 가이드가 이미 떠 있다면 무시
                return false;
            StartCoroutine(RunManual(piece));
            return true;
        }

        IEnumerator RunManual(PieceDefinition piece)    // 직접 연 가이드를 보여 주는 코루틴 -> 자동 가이드와 같이 게임을 멈췄다가 닫으면 되돌림
        {
            _running = true;    // 그동안 자동 가이드 대기열은 기다리게 함
            Pause();
            yield return _view.Show(_catalog.Find(piece));
            Resume();
            _running = false;

            if (_queue.Count > 0)   // 보는 사이에 새로 소환한 기물이 있다면 이어서 자동 가이드를 처리함
                StartCoroutine(RunQueue());
        }

        bool Ended // 스테이지의 승패가 이미 났는지 여부
        {
            get { return _turn != null && (_turn.Phase == TurnPhase.Victory || _turn.Phase == TurnPhase.Defeat); }
        }

        void Pause() // 게임 시간을 멈추고 보드 입력을 막는 함수
        {
            _paused = true;
            Time.timeScale = 0f;
            InputGate.Push();
        }

        void Resume() // 멈춘 게임 시간과 막은 보드 입력을 되돌리는 함수
        {
            _paused = false;
            Time.timeScale = 1f;
            InputGate.Pop();
        }
    }
}
