namespace Promotion.Core
{
    /// <summary>
    /// World-space input (board hover, piece dragging) reads the pointer directly
    /// rather than through the EventSystem, so a UI overlay would not block it on
    /// its own. Screens that cover the board push a block here while they are up.
    /// Counted rather than boolean: two overlays closing in any order still end
    /// with the board playable.
    /// </summary>
    public static class InputGate
    {
        static int _blockers;

        public static bool WorldInputEnabled => _blockers <= 0;

        public static void Push() => _blockers++;

        public static void Pop()
        {
            _blockers--;
            if (_blockers < 0)
                _blockers = 0;
        }

        /// <summary>Scene loads wipe the screens that pushed blocks; the count must go with them.</summary>
        public static void Reset() => _blockers = 0;
    }
}
