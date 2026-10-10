using System.Collections;
using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gambonanza.Tutorial
{
    /// <summary>
    /// Runs the tutorial inside the ordinary battle scene. It only talks and waits:
    /// placing the king, summoning and fighting all go through the same systems
    /// as a real stage, so what the player learns here is the real game.
    ///
    /// Idle unless the title screen started a tutorial (see <see cref="TutorialSession"/>).
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [Tooltip("Scene the run continues in once the tutorial is won.")]
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

            // Everything here runs before any Start, so the battle comes up as the tutorial's.
            RunState.Instance.CurrentStage = _script.stage;

            var debugSetup = FindAnyObjectByType<DebugStageSetup>();
            if (debugSetup != null)
                debugSetup.enabled = false;

            // The tutorial's victory leads back into the story, not into the shop.
            var shop = FindAnyObjectByType<ShopLauncher>();
            if (shop != null)
                shop.enabled = false;
        }

        IEnumerator Start()
        {
            _director = FindAnyObjectByType<StageDirector>();
            _turn = _director.GetComponent<TurnManager>();
            _state = _director.GetComponent<BoardState>();
            _view = DialogueView.Create(_script);

            // Cover straight away if the story opens the tutorial, so the board never flashes.
            if (_script.steps.Length > 0 && _script.steps[0] != null && _script.steps[0].coverBoard)
                _view.SetBoardCovered(true);

            // StageDirector applies the stage a frame after its own Start.
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

                // A loss hands over to the HUD's restart; the tutorial starts again from the top.
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
