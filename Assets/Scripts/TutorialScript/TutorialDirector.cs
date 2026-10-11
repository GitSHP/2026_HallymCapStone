using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Promotion.Tutorial
{
    /// <summary>
    /// 일반 전투 씬 안에서 튜토리얼을 진행한다. 이 클래스는 말하고 기다리기만 한다.
    /// 킹 배치, 소환, 전투는 모두 실제 스테이지와 같은 시스템을 거치므로,
    /// 플레이어가 여기서 배우는 것이 곧 실제 게임이다.
    ///
    /// 타이틀 화면에서 튜토리얼을 시작하지 않았다면 동작하지 않는다(<see cref="TutorialSession"/> 참고).
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [Tooltip("튜토리얼에서 승리한 뒤 런을 이어 갈 씬이다.")]
        public string nextScene = "Map";

        TutorialScript _script;
        StageDirector _director;
        TurnManager _turn;
        BoardState _state;
        DialogueView _view;

        readonly List<Piece> _scratch = new();

        void Awake()
        {
            _script = TutorialSession.Active;
            if (_script == null)
            {
                enabled = false;
                return;
            }

            // 여기 있는 모든 처리는 어떤 Start보다도 먼저 실행되므로, 전투가 튜토리얼 전투로 시작된다.
            RunState.Instance.CurrentStage = _script.stage;

            var debugSetup = FindAnyObjectByType<DebugStageSetup>();
            if (debugSetup != null)
                debugSetup.enabled = false;

            // 튜토리얼에서 승리하면 상점이 아니라 스토리로 돌아간다.
            var shop = FindAnyObjectByType<ShopLauncher>();
            if (shop != null)
                shop.enabled = false;
        }

        IEnumerator Start()
        {
            _director = FindAnyObjectByType<StageDirector>();
            _turn = _director.GetComponent<TurnManager>();
            _state = _director.GetComponent<BoardState>();
            _view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include);
            if (_view == null)
            {
                Debug.LogError("[TutorialDirector] No DialogueView in the scene - add the TutorialDialogue prefab.");
                yield break;
            }
            _view.Init(_script);

            // 튜토리얼이 스토리로 시작하면 보드가 잠깐이라도 비치지 않도록 즉시 가린다.
            if (_script.steps.Length > 0 && _script.steps[0] != null && _script.steps[0].coverBoard)
                _view.SetBoardCovered(true);

            // StageDirector는 자신의 Start 한 프레임 뒤에 스테이지를 적용한다.
            yield return null;
            yield return null;

            foreach (var step in _script.steps)
            {
                if (step == null)
                    continue;

                Perform(step.action);

                if (step.wait == TutorialWait.Click)
                {
                    _view.SetBoardCovered(step.coverBoard);
                    yield return _view.Say(step.text, step.portrait, step.storyFrame);
                    continue;
                }

                _view.ShowHint(step.text);
                yield return new WaitUntil(() => IsMet(step.wait) || Lost);

                // 패배하면 HUD의 재시작에 맡기고, 튜토리얼은 처음부터 다시 시작한다.
                if (Lost)
                {
                    _view.HideAll();
                    yield break;
                }
            }

            Finish();
        }

        bool Lost => _turn.Phase == TurnPhase.Defeat;

        bool IsMet(TutorialWait wait)
        {
            switch (wait)
            {
                case TutorialWait.KingPlaced:
                    return _director.KingPlaced;
                case TutorialWait.StageStarted:
                    return _turn.Phase != TurnPhase.Deploy;
                case TutorialWait.PieceSummoned:
                    _state.GetPiecesOf(Team.Player, _scratch);
                    return _scratch.Exists(p => !p.IsObjective);
                case TutorialWait.Victory:
                    return _turn.Phase == TurnPhase.Victory;
                default:
                    return true;
            }
        }

        void Perform(TutorialAction action)
        {
            if (action == TutorialAction.SpawnNextWave)
                _director.SpawnNextWaveNow();
        }

        void Finish()
        {
            TutorialSession.Complete();
            RunState.Instance.ResetRun();
            InputGate.Reset();
            SceneManager.LoadScene(nextScene);
        }
    }
}
