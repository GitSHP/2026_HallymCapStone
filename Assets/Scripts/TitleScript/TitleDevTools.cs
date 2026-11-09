using Promotion.Core;
using Promotion.Data;
using Promotion.Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Promotion.Title
{
    public class TitleDevTools : MonoBehaviour  // 타이틀 화면에서 특수 기물 해금·뽑기를 빠르게 시험하기 위한 단축 기능 패널 -> 출시 전에 제거해야 함
    // 화면 왼쪽 위에 버튼 패널(IMGUI)을 띄우고, 같은 기능을 키보드 단축키로도 실행할 수 있음
    {
        [Tooltip("타이틀이 시작될 때 단축 기능 패널을 펼쳐 둔다.")]
        public bool visibleOnStart = false;

        bool _visible;
        string _lastAction = "";    // 마지막으로 실행한 기능의 결과 메시지

        void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)  // 정식(베타) 빌드에서는 개발자 도구를 통째로 없앰 -> 테스터가 F키로 승패·기록을 건드리지 못하게
            {                                                   // (에디터와 Development Build에서는 그대로 쓸 수 있음)
                Destroy(this);
                return;
            }
            _visible = visibleOnStart;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame) _visible = !_visible;   // F1: 패널 열기/닫기
            if (keyboard.f2Key.wasPressedThisFrame) AddCrown(); // F2: 검은 왕관 +1
            if (keyboard.f3Key.wasPressedThisFrame) SimulateBossClear();    // F3: 보스 클리어 기록 (+클리어, +왕관) 후 타이틀 다시 열기
            if (keyboard.f4Key.wasPressedThisFrame) ReplayUnlock(); // F4: 버튼이 풀리는 연출 다시 보기
            if (keyboard.f5Key.wasPressedThisFrame) UnlockAll();    // F5: 특수 기물 모두 해금
            if (keyboard.f6Key.wasPressedThisFrame) LockAll();  // F6: 특수 기물 모두 잠금 (룰렛 다시 돌려 보기용)
            if (keyboard.f10Key.wasPressedThisFrame) ResetToFirstRun(); // F10: 첫 실행 상태로 초기화
        }

        void AddCrown()
        {
            MetaProgress.AddCrowns(1);
            _lastAction = "Crowns: " + MetaProgress.Crowns;
        }

        void SimulateBossClear()    // 최종전을 이긴 것과 같은 기록을 남기고 타이틀을 다시 엶 -> 처음이면 풀리는 연출이 재생됨
        {
            MetaProgress.RecordBossClear();
            Reload();
        }

        void ReplayUnlock() // 풀리는 연출과 소개를 다시 보도록 기록을 되돌리고 타이틀을 다시 엶
        {
            if (!MetaProgress.FairyUnlocked)
                MetaProgress.RecordBossClear(); // 아직 보스를 이긴 적이 없다면 이긴 것으로 만들어 줌
            MetaProgress.MarkFairyIntroShown(false);
            Reload();
        }

        void UnlockAll()
        {
            var roulette = FairyRoulette.Instance;
            if (roulette == null)
            {
                _lastAction = "FairyRoulette asset missing";
                return;
            }
            foreach (var entry in roulette.entries)
                if (entry?.piece != null)
                    MetaProgress.Unlock(entry.piece.name);
            _lastAction = "All fairy pieces unlocked";
        }

        void LockAll()
        {
            MetaProgress.ClearUnlocks();
            _lastAction = "All fairy pieces locked";
        }

        void ResetToFirstRun()  // 영구 기록(보스 클리어·왕관·해금·풀리는 연출)과 기물 가이드 본 기록, 런 정보를 모두 지우고 타이틀을 다시 엶
        {
            MetaProgress.ResetAll();
            PieceGuideDirector.ResetSeen();
            RunState.Instance.ResetRun();
            TutorialSession.Complete();
            Reload();
        }

        static void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        string Status()
        {
            var unlocked = MetaProgress.UnlockedNames();
            return "boss clears " + MetaProgress.BossClears + "   crowns " + MetaProgress.Crowns +
                   "\nintro shown " + MetaProgress.FairyIntroShown +
                   "\nunlocked (" + unlocked.Count + "): " + string.Join(", ", unlocked);
        }

        void OnGUI()
        {
            if (!_visible)
            {
                GUI.Label(new Rect(10f, 10f, 300f, 22f), "F1: dev tools");
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, 360f, 330f), GUI.skin.box);
            GUILayout.Label("TITLE DEV TOOLS   (F1 to hide)");
            GUILayout.Label(Status());
            GUILayout.Space(6f);

            if (GUILayout.Button("F2  +1 black crown")) AddCrown();
            if (GUILayout.Button("F3  Record boss clear (+1 crown) & reload")) SimulateBossClear();
            if (GUILayout.Button("F4  Replay fairy unlock intro")) ReplayUnlock();
            if (GUILayout.Button("F5  Unlock all fairy pieces")) UnlockAll();
            if (GUILayout.Button("F6  Lock all fairy pieces")) LockAll();
            if (GUILayout.Button("F10 Reset to first run (all progress)")) ResetToFirstRun();

            if (_lastAction.Length > 0)
                GUILayout.Label("> " + _lastAction);
            GUILayout.EndArea();
        }
    }
}
