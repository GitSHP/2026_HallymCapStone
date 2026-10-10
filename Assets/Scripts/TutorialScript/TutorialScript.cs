using System;
using Gambonanza.Data;
using UnityEngine;

namespace Gambonanza.Tutorial
{
    /// <summary>What a step waits for before the next one starts.</summary>
    public enum TutorialWait
    {
        /// <summary>A line of dialogue: the player clicks to continue.</summary>
        Click,
        KingPlaced,
        /// <summary>The player pressed the start button and turn 1 began.</summary>
        StageStarted,
        /// <summary>At least one piece besides the king has been summoned.</summary>
        PieceSummoned,
        Victory
    }

    /// <summary>Something the tutorial does to the board as a step begins.</summary>
    public enum TutorialAction
    {
        None,
        /// <summary>Drops the stage's next wave onto the board right away.</summary>
        SpawnNextWave
    }

    [Serializable]
    public class TutorialStep
    {
        [TextArea(2, 5)]
        public string text;

        [Tooltip("Click shows a dialogue box the player reads; anything else shows a small hint " +
                 "and waits until the player has done it.")]
        public TutorialWait wait = TutorialWait.Click;

        public TutorialAction action = TutorialAction.None;

        [Tooltip("Expression for this line. Empty shows the default portrait. Talk lines only.")]
        public Sprite portrait;

        [Tooltip("Comic page shown above the dialogue while this line is up. Empty hides it. " +
                 "Talk lines only.")]
        public Sprite storyFrame;

        [Tooltip("Hide the board behind a solid backdrop while this line is up, for story scenes. " +
                 "The first line without it lifts the cover. Talk lines only.")]
        public bool coverBoard;
    }

    /// <summary>
    /// The whole tutorial as data: who speaks, what they say and what the player
    /// must do between lines. Rewriting the story means editing this asset only.
    /// </summary>
    [CreateAssetMenu(menuName = "Gambonanza/Tutorial Script", fileName = "TutorialScript")]
    public class TutorialScript : ScriptableObject
    {
        [Header("Speaker")]
        public string speakerName = "안내인";
        [Tooltip("Default upper-body illustration, used by any line without its own expression. " +
                 "Leave empty to show the placeholder frame.")]
        public Sprite portrait;

        [Header("Story scenes")]
        [Tooltip("Shown behind the dialogue on lines that cover the board. Empty uses plain black.")]
        public Sprite storyBackground;
        [Tooltip("How dark the background turns while a comic page is up. It fades in with the " +
                 "first panel and back out after the last, so lines without the comic show it clear.")]
        [Range(0f, 1f)] public float storyBackgroundShade = 0.8f;

        [Header("Battle")]
        [Tooltip("The stage fought during the tutorial.")]
        public StageDefinition stage;

        [Header("Text")]
        [Tooltip("Characters revealed per second.")]
        [Min(1f)] public float charactersPerSecond = 40f;

        public TutorialStep[] steps = Array.Empty<TutorialStep>();
    }
}
