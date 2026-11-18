namespace Promotion.Tutorial
{
    public enum TutorialPhase // 튜토리얼이 지금 어느 씬의 단계를 진행 중인지
    {
        Battle, // 전투 씬 -> 전투와 그 뒤의 상점까지 (TutorialDirector 담당)
        Map     // 지도 씬 -> 첫 행선지를 고를 때까지 (MapTutorialDirector 담당)
    }

    public static class TutorialSession // "지금 튜토리얼 중이다"라는 정보를 씬 사이에 전달하는 클래스
    // -> static이라 씬이 바뀌어도 값이 유지됨, 튜토리얼을 할지는 플레이어가 매번 선택함
    // -> 단계(Phase)를 함께 들고 다니므로, 지도 단계 중에 전투 씬에 들어가도 튜토리얼 전투가 다시 시작되지 않음
    {
        public static TutorialScript Active { get; private set; } // 진행 중인 튜토리얼 대본 (튜토리얼이 아니라면 null)
        public static TutorialPhase Phase { get; private set; } // 지금 진행 중인 튜토리얼 단계

        public static bool IsIn(TutorialPhase phase) // 지금 그 단계의 튜토리얼을 진행 중인지 확인하는 함수
        {
            return Active != null && Phase == phase;
        }

        public static void Begin(TutorialScript script) // 타이틀에서 튜토리얼을 시작할 때 대본을 등록하는 함수 -> 전투 단계부터 시작
        {
            Active = script;
            Phase = TutorialPhase.Battle;
        }

        public static void AdvanceToMap() // 전투·상점 단계를 마치고 지도 단계로 넘어갈 때 호출하는 함수
        {
            Phase = TutorialPhase.Map;
        }

        public static void Complete() // 튜토리얼이 끝났을 때 등록을 지우는 함수 -> 이후 전투는 일반 전투로 진행
        {
            Active = null;
            Phase = TutorialPhase.Battle;
        }
    }
}
