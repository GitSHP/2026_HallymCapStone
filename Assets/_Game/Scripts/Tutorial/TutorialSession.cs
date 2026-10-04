using UnityEngine;

namespace Gambonanza.Tutorial
{
    /// <summary>
    /// Carries "this battle is the tutorial" from the title screen into the battle
    /// scene, and remembers across launches whether it has been finished.
    /// </summary>
    public static class TutorialSession
    {
        const string DoneKey = "gambonanza.tutorial_done";

        /// <summary>The tutorial to run in the next battle scene. Null for a normal battle.</summary>
        public static TutorialScript Active { get; private set; }

        public static bool IsDone => PlayerPrefs.GetInt(DoneKey, 0) == 1;

        public static void Begin(TutorialScript script) => Active = script;

        public static void Complete()
        {
            Active = null;
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Makes the next game start play the tutorial again.</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.Save();
        }
    }
}
