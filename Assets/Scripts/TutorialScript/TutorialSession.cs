namespace Promotion.Tutorial
{
    /// <summary>
    /// Carries "this battle is the tutorial" from the title screen into the battle
    /// scene. The player chooses each time whether to play it.
    /// </summary>
    public static class TutorialSession
    {
        /// <summary>The tutorial to run in the next battle scene. Null for a normal battle.</summary>
        public static TutorialScript Active { get; private set; }

        public static void Begin(TutorialScript script) => Active = script;

        public static void Complete() => Active = null;
    }
}
