using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Tutorial
{
    public class TutorialDirector : MonoBehaviour // 일반 전투 씬 안에서 튜토리얼을 진행하는 클래스
    // -> 이 클래스는 대사를 보여 주고 플레이어의 행동을 기다리기만 함
    // -> 킹 배치, 소환, 전투는 모두 실제 스테이지와 같은 시스템을 거치므로 튜토리얼에서 배우는 것이 곧 실제 게임
    // -> 타이틀 화면에서 튜토리얼을 시작하지 않았다면 동작하지 않음 (TutorialSession 참고)
    {
        [Tooltip("튜토리얼에서 승리한 뒤 런을 이어 갈 씬이다.")]
        public string nextScene = "Map"; // 튜토리얼을 끝낸 뒤 불러올 씬 이름

        TutorialScript _script; // 진행할 튜토리얼 대본 데이터
        StageDirector _director; // 스테이지 진행(승패 판정, 웨이브 소환 등)을 담당하는 컴포넌트
        TurnManager _turn; // 현재 턴 단계(배치/전투/승리/패배)를 알려 주는 컴포넌트
        BoardState _state; // 보드 위 기물 정보를 담은 컴포넌트
        DialogueView _view; // 대사·힌트를 화면에 보여 주는 컴포넌트

        readonly List<Piece> _scratch = new(); // 기물 목록을 잠시 받아 올 때 재사용하는 리스트 (매번 새로 만들지 않기 위해)

        void Awake()
        // 다른 컴포넌트의 Start보다 먼저 실행되어 튜토리얼 전투를 준비하는 함수
        {
            _script = TutorialSession.Active; // 타이틀에서 넘겨준 튜토리얼 대본을 가져옴
            if (_script == null) // 만약 튜토리얼이 아니라면 (일반 전투)
            {
                enabled = false; // 이 컴포넌트를 꺼서 아무 동작도 하지 않게 함
                return;
            }

            RunState.Instance.CurrentStage = _script.stage; // 이번 전투를 튜토리얼 스테이지로 지정함 -> Awake는 어떤 Start보다 먼저 실행되므로 전투가 튜토리얼 스테이지로 시작됨

            var debugSetup = FindAnyObjectByType<DebugStageSetup>(); // 디버그용 시작 배치가 씬에 있다면
            if (debugSetup != null)
                debugSetup.enabled = false; // 꺼서 튜토리얼 보드에 끼어들지 않게 함

            var shop = FindAnyObjectByType<ShopLauncher>(); // 승리 후 상점을 여는 컴포넌트가 있다면
            if (shop != null)
                shop.enabled = false; // 꺼 둠 -> 튜토리얼에서 승리하면 상점이 아니라 다음 씬(지도)으로 감
        }

        IEnumerator Start()
        // 튜토리얼 단계(step)를 처음부터 끝까지 순서대로 진행하는 코루틴
        // -> 각 단계마다: 동작 실행 -> 대사(클릭 대기) 또는 힌트(행동 대기) -> 다음 단계
        {
            _director = FindAnyObjectByType<StageDirector>(); // 씬에서 스테이지 진행 컴포넌트를 찾고
            _turn = _director.GetComponent<TurnManager>(); // 같은 오브젝트에 붙은 턴 관리자와
            _state = _director.GetComponent<BoardState>(); // 보드 상태도 가져옴
            _view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include); // 꺼져 있는 대사 화면까지 포함해서 찾음
            if (_view == null) // 만약 씬에 대사 화면이 없다면 -> 튜토리얼을 진행할 수 없음
            {
                Debug.LogError("[TutorialDirector] No DialogueView in the scene - add the TutorialDialogue prefab.");
                yield break; // 에러를 남기고 종료
            }
            _view.Init(_script); // 대사 화면을 켜고 화자 정보를 채움

            if (_script.steps.Length > 0 && _script.steps[0] != null && _script.steps[0].coverBoard) // 만약 첫 단계가 보드를 가리는 스토리 장면이라면
                _view.SetBoardCovered(true); // 보드가 잠깐이라도 비치지 않도록 즉시 가림

            // StageDirector는 자신의 Start 한 프레임 뒤에 스테이지를 적용함 -> 두 프레임 기다려서 보드가 준비된 뒤에 시작
            yield return null;
            yield return null;

            foreach (var step in _script.steps) // 대본의 단계를 하나씩 차례로 진행
            {
                if (step == null) // 비어 있는 단계는 건너뜀
                    continue;

                Perform(step.action); // 이 단계가 시작될 때 보드에 가할 동작을 실행 (예: 다음 웨이브 소환)

                if (step.wait == TutorialWait.Click) // 만약 대사 단계라면 (클릭해서 넘기는 대화)
                {
                    _view.SetBoardCovered(step.coverBoard); // 이 대사에 맞게 보드를 가리거나 드러내고
                    yield return _view.Say(step.text, step.portrait, step.storyFrame); // 대사를 보여 주고 플레이어가 넘길 때까지 기다림
                    continue; // 다음 단계로
                }

                // 대사가 아닌 단계라면 -> 짧은 힌트를 띄우고 플레이어가 직접 행동할 때까지 기다림
                _view.ShowHint(step.text);
                yield return new WaitUntil(() => IsMet(step.wait) || Lost); // 조건을 만족하거나 패배할 때까지 매 프레임 검사

                if (Lost) // 만약 패배했다면
                {
                    _view.HideAll(); // 대사 화면을 모두 숨기고 튜토리얼 진행을 멈춤 -> HUD의 재시작에 맡기고, 튜토리얼은 처음부터 다시 시작됨
                    yield break;
                }
            }

            Finish(); // 모든 단계를 마쳤다면 튜토리얼 종료
        }

        bool Lost => _turn.Phase == TurnPhase.Defeat; // 지금 패배 상태인지 여부

        bool IsMet(TutorialWait wait)
        // 단계가 기다리는 조건(wait)을 플레이어가 만족했는지 검사하는 함수
        {
            switch (wait)
            {
                case TutorialWait.KingPlaced: // 킹을 보드에 놓았는지
                    return _director.KingPlaced;
                case TutorialWait.StageStarted: // 시작 버튼을 눌러 배치 단계가 끝났는지
                    return _turn.Phase != TurnPhase.Deploy;
                case TutorialWait.PieceSummoned: // 킹 외의 기물을 하나 이상 소환했는지
                    _state.GetPiecesOf(Team.Player, _scratch); // 아군 기물 목록을 받아 와서
                    return _scratch.Exists(p => !p.IsObjective); // 킹(지켜야 할 목표 기물)이 아닌 기물이 있는지 검사
                case TutorialWait.Victory: // 스테이지에서 승리했는지
                    return _turn.Phase == TurnPhase.Victory;
                default: // 그 외(Click 등)는 기다릴 조건이 없으므로 바로 통과
                    return true;
            }
        }

        void Perform(TutorialAction action)
        // 단계가 시작될 때 튜토리얼이 보드에 가하는 동작을 실행하는 함수
        {
            if (action == TutorialAction.SpawnNextWave) // 만약 다음 웨이브 소환 동작이라면
                _director.SpawnNextWaveNow(); // 스테이지의 다음 웨이브를 즉시 보드에 내보냄
        }

        void Finish()
        // 튜토리얼을 마치고 다음 씬으로 넘어가는 함수
        {
            TutorialSession.Complete(); // 튜토리얼 표시를 지우고 -> 다음 전투는 일반 전투로 진행됨
            RunState.Instance.ResetRun(); // 런 정보를 초기화해서 새 런으로 시작하고
            InputGate.Reset(); // 남아 있을 수 있는 입력 막기를 모두 풀고
            SceneManager.LoadScene(nextScene); // 다음 씬(지도)을 불러옴
        }
    }
}
