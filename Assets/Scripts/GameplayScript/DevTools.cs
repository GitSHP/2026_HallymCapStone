using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    public class DevTools : MonoBehaviour  // 개발 중 테스트를 빠르게 하기 위한 단축키(F1~F11) 모음 클래스 -> 출시 전에 제거해야 함
    // 화면 왼쪽 위에 버튼 패널(IMGUI)을 띄우고, 같은 기능을 키보드 단축키로도 실행할 수 있음
    {
        [Tooltip("스테이지가 시작될 때 단축 기능 패널을 표시한다.")]
        public bool visibleOnStart = true;  // 시작할 때 패널을 펼쳐서 보여 줄지 여부

        [Tooltip("F11로 이동할 스테이지(최종 체스 대결).")]
        public StageDefinition finalStage;  // F11을 눌렀을 때 바로 이동할 최종전 스테이지

        StageDirector _director;    // 스테이지 진행(웨이브, 승패 등)을 담당하는 컴포넌트
        TurnManager _turn;  // 턴 단계와 AP를 관리하는 컴포넌트
        DeployController _deploy;   // 덱과 기물 배치를 담당하는 컴포넌트
        PieceRegistry _registry;    // 기물 생성/제거를 담당하는 컴포넌트
        BoardState _state;  // 보드 위 기물 위치 데이터
        BoardGrid _grid;    // 보드 크기(너비/높이) 정보

        readonly List<Piece> _scratch = new();  // 적 목록을 담을 때 재사용하는 임시 리스트
        bool _visible;  // 현재 패널이 펼쳐져 있는지 여부
        string _lastAction = "";    // 마지막으로 실행한 기능의 결과 메시지 -> 패널 아래에 표시

        void Awake()    // 오브젝트가 생성될 때 한 번 실행 -> 같은 오브젝트(Board)의 컴포넌트를 찾아 둠
        {
            _visible = visibleOnStart;
            _director = GetComponent<StageDirector>();
            _turn = GetComponent<TurnManager>();
            _deploy = GetComponent<DeployController>();
            _registry = GetComponent<PieceRegistry>();
            _state = GetComponent<BoardState>();
            _grid = GetComponent<BoardGrid>();
        }

        void Update()   // 매 프레임 단축키 입력을 확인해서 해당 기능을 실행
        {
            var keyboard = Keyboard.current;    // 현재 연결된 키보드(새 Input System)
            if (keyboard == null)   // 키보드가 없다면 종료
                return;

            if (keyboard.f1Key.wasPressedThisFrame) _visible = !_visible;   // F1: 패널 열기/닫기
            if (keyboard.f2Key.wasPressedThisFrame) OpenShop(); // F2: 강제 승리 -> 상점 열기
            if (keyboard.f3Key.wasPressedThisFrame) ForceOutcome(TurnPhase.Victory);    // F3: 강제 승리
            if (keyboard.f4Key.wasPressedThisFrame) ForceOutcome(TurnPhase.Defeat); // F4: 강제 패배
            if (keyboard.f5Key.wasPressedThisFrame) GrantGold(10);  // F5: 골드 +10
            if (keyboard.f6Key.wasPressedThisFrame) RefillAp(); // F6: AP 가득 채우기
            if (keyboard.f7Key.wasPressedThisFrame) SpawnWave();    // F7: 다음 웨이브 즉시 소환
            if (keyboard.f8Key.wasPressedThisFrame) ClearEnemies(); // F8: 적 전부 제거
            if (keyboard.f9Key.wasPressedThisFrame) AutoDeployKing();   // F9: 킹 자동 배치 후 시작
            if (keyboard.f10Key.wasPressedThisFrame) Restart(); // F10: 스테이지 재시작
            if (keyboard.f11Key.wasPressedThisFrame) JumpToFinal(); // F11: 최종전으로 이동
        }

        void OpenShop() // 강제로 승리시켜 상점을 여는 함수 -> 승리가 선언되면 ShopLauncher가 알아서 상점을 염
        {
            ForceOutcome(TurnPhase.Victory);
            _lastAction = "Victory declared - shop opens via ShopLauncher";
        }

        void ForceOutcome(TurnPhase outcome)    // 스테이지를 강제로 승리/패배 상태로 만드는 함수
        {
            StopAllCoroutines();    // 진행 중인 코루틴을 모두 멈춤
            _director.StopAllCoroutines();  // 진행 중인 적 턴/응수 처리도 멈춤 -> 처리 도중 결과를 덮어쓰지 못하게
            _turn.SetPhase(outcome);    // 단계를 승리/패배로 바꿈
            _lastAction = outcome.ToString() + " forced";
        }

        void GrantGold(int amount)  // 골드를 amount만큼 추가하는 함수
        {
            RunState.Instance.AddGold(amount);
            _lastAction = "+" + amount + " gold (now " + RunState.Instance.Gold + ")";
        }

        void RefillAp() // 이번 턴의 AP를 최대치로 다시 채우는 함수
        {
            _turn.RefillAp();
            _lastAction = "AP refilled";
        }

        void SpawnWave()    // 다음 웨이브를 등장 턴을 기다리지 않고 바로 소환하는 함수
        {
            _lastAction = _director.SpawnNextWaveNow()  // 소환에 성공했다면 몇 번째 웨이브인지, 남은 웨이브가 없다면 그 사실을 표시
                ? "Wave " + _director.WavesSent + " spawned early"
                : "No waves left";
        }

        void ClearEnemies() // 보드 위의 적을 모두 제거하는 함수
        {
            _state.GetPiecesOf(Team.Enemy, _scratch);   // 적 기물을 모두 임시 리스트에 담고
            int count = _scratch.Count; // 제거할 적의 수를 기억

            foreach (var enemy in _scratch) // 적을 하나씩 제거
                _registry.Kill(enemy);

            _director.RefreshEnemyCount();  // 적 수를 다시 세서 HUD에 반영되게 함
            _lastAction = count + " enemies removed";
        }

        void AutoDeployKing()   // 배치 단계에서 킹을 자동으로 놓고 스테이지를 시작하는 함수
        // 체스 대결이라면 덱의 나머지 기물도 모두 자동으로 놓음
        {
            if (_turn.Phase != TurnPhase.Deploy)    // 배치 단계가 아니라면 실행하지 않음
            {
                _lastAction = "Not in deploy phase";
                return;
            }

            if (!_director.KingPlaced)  // 아직 킹을 놓지 않았다면
            {
                int kingIndex = -1; // 덱에서 킹 카드의 위치(못 찾으면 -1)
                for (int i = 0; i < _deploy.Deck.Count; i++)    // 덱을 처음부터 검사해서
                {
                    if (_deploy.Deck[i] != null && _deploy.Deck[i].isObjective) // 킹(목표 기물) 카드를 찾으면 위치를 기억하고 중단
                    {
                        kingIndex = i;
                        break;
                    }
                }

                if (kingIndex < 0)  // 덱에 킹이 없다면 실행하지 않음
                {
                    _lastAction = "No king in deck";
                    return;
                }

                _deploy.Select(kingIndex);  // 킹 카드를 선택하고
                if (!_deploy.TryDeployAt(new Coord(_grid.width / 2, 0)))    // 맨 아래 줄 가운데 칸에 놓아 봄 -> 실패하면 중단
                {
                    _lastAction = "Could not place king";
                    return;
                }
            }

            if (_director.IsChessDuel)  // 체스 대결이라면 남은 기물도 모두 배치
                PlaceRestOfDeck();

            _director.ConfirmDeploy();  // 배치 단계를 끝내고 1턴 시작
            _lastAction = "King placed, stage started";
        }

        void PlaceRestOfDeck()  // 덱에 남은 기물을 배치 가능한 빈 칸에 차례대로 놓는 함수(체스 대결용)
        {
            int rows = Mathf.Clamp(_deploy.deployRows, 1, _grid.height);   // 배치 가능한 줄 수
            for (int y = rows - 1; y >= 0 && _deploy.Deck.Count > 0; y--)  // 배치 가능한 가장 앞줄부터 아래로 내려가며(덱이 빌 때까지)
            {
                for (int x = 0; x < _grid.width && _deploy.Deck.Count > 0; x++)    // 왼쪽 칸부터 오른쪽으로
                {
                    var square = new Coord(x, y);
                    if (!_state.IsEmpty(square))    // 이미 기물이 있는 칸은 건너뛰고
                        continue;

                    _deploy.Select(0);  // 덱의 첫 번째 카드를 선택해서
                    _deploy.TryDeployAt(square);    // 그 칸에 놓음
                }
            }
        }

        void JumpToFinal()  // 최종전 스테이지로 바로 이동하는 함수
        {
            if (finalStage == null) // 최종전 스테이지가 지정되지 않았다면 실행하지 않음
            {
                _lastAction = "No final stage assigned on DevTools";
                return;
            }

            RunState.Instance.CurrentStage = finalStage;    // 현재 스테이지를 최종전으로 바꾸고
            Restart();  // 씬을 다시 불러옴 -> StageDirector가 시작할 때 최종전을 읽어 감
        }

        static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // 현재 씬을 처음부터 다시 불러오는 함수

        void OnGUI()    // 화면에 IMGUI 패널을 그릴 때 발생하는 이벤트(매 프레임 여러 번 호출됨)
        {
            if (!_visible)  // 패널이 닫혀 있다면 안내 문구만 표시
            {
                GUI.Label(new Rect(10f, 10f, 300f, 22f), "F1: dev tools");
                return;
            }

            const float width = 330f;   // 패널 너비
            GUILayout.BeginArea(new Rect(10f, 10f, width, 370f), GUI.skin.box);   // 왼쪽 위에 상자 모양 패널 영역 시작

            GUILayout.Label("DEV TOOLS   (F1 to hide)");
            GUILayout.Label(Status());  // 현재 상태 요약 표시
            GUILayout.Space(6f);

            // 각 버튼은 위 단축키와 같은 기능을 실행함

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

            if (!string.IsNullOrEmpty(_lastAction)) // 마지막 실행 결과가 있다면 패널 아래에 표시
            {
                GUILayout.Space(4f);
                GUILayout.Label("> " + _lastAction);
            }

            GUILayout.EndArea();    // 패널 영역 끝
        }

        string Status() // 패널에 보여 줄 현재 상태(단계, 턴, AP, 웨이브, 적 수, 골드, 킹 배치 여부) 문자열을 만드는 함수
        {
            if (_director == null || _turn == null) // 스테이지 컴포넌트가 없다면 안내 문구 반환
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
