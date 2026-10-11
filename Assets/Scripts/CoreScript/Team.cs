namespace Promotion.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public static class TeamExtensions
    {
        /// <summary>플레이어는 보드 위쪽(+y)으로 전진하고, 적은 킹을 향해 아래로 내려온다.</summary>
        public static int ForwardY(this Team team) => team == Team.Player ? 1 : -1;

        public static Team Opponent(this Team team) => team == Team.Player ? Team.Enemy : Team.Player;
    }
}
