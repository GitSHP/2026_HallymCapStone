namespace Promotion.Core
{
    /// <summary>
    /// 월드 공간 입력(보드 호버, 기물 드래그)은 EventSystem을 거치지 않고 포인터를
    /// 직접 읽으므로, UI 오버레이만으로는 입력이 막히지 않는다. 보드를 가리는 화면은
    /// 떠 있는 동안 여기에 차단을 push한다. bool이 아닌 카운트로 관리하므로,
    /// 오버레이 두 개가 어떤 순서로 닫혀도 결국 보드는 조작 가능한 상태로 돌아온다.
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

        /// <summary>씬을 로드하면 차단을 push한 화면들이 사라지므로, 카운트도 함께 초기화해야 한다.</summary>
        public static void Reset() => _blockers = 0;
    }
}
