namespace Promotion.Tutorial
{
    /// <summary>
    /// "이번 전투는 튜토리얼이다"라는 정보를 타이틀 화면에서 전투 씬으로 전달한다.
    /// 튜토리얼을 할지는 플레이어가 매번 선택한다.
    /// </summary>
    public static class TutorialSession
    {
        /// <summary>다음 전투 씬에서 실행할 튜토리얼이다. 일반 전투라면 null이다.</summary>
        public static TutorialScript Active { get; private set; }

        public static void Begin(TutorialScript script) => Active = script;

        public static void Complete() => Active = null;
    }
}
