namespace Promotion.Tutorial
{
    public static class TutorialSession // "이번 전투는 튜토리얼이다"라는 정보를 타이틀 화면에서 전투 씬으로 전달하는 클래스
    // -> static이라 씬이 바뀌어도 값이 유지됨, 튜토리얼을 할지는 플레이어가 매번 선택함
    {
        public static TutorialScript Active { get; private set; } // 다음 전투 씬에서 실행할 튜토리얼 대본 (일반 전투라면 null)

        public static void Begin(TutorialScript script) => Active = script; // 타이틀에서 튜토리얼을 시작할 때 대본을 등록하는 함수

        public static void Complete() => Active = null; // 튜토리얼이 끝났을 때 등록을 지우는 함수 -> 이후 전투는 일반 전투로 진행
    }
}
