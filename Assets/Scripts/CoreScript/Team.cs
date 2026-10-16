namespace Promotion.Core
{
    public enum Team   // 기물이 어느 편인지 구분하는 열거형
    {
        Player,   // 플레이어 진영 (보드 아래쪽에서 위쪽으로 전진)
        Enemy   // 적 진영 (보드 위쪽에서 아래쪽으로 전진)
    }

    public static class TeamExtensions   // Team 값에 바로 붙여 쓸 수 있는 보조 함수 모음 (예: team.Opponent())
    {
        public static int ForwardY(this Team team) => team == Team.Player ? 1 : -1;   // 그 진영에게 "앞쪽"이 y축으로 +1인지 -1인지 돌려주는 함수
        // -> 폰처럼 앞으로만 가는 기물은 이 값을 곱해 진영별로 방향을 뒤집음

        public static Team Opponent(this Team team) => team == Team.Player ? Team.Enemy : Team.Player;   // 상대 진영을 돌려주는 함수
    }
}
