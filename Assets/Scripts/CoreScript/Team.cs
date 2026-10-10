namespace Gambonanza.Core
{
    public enum Team
    {
        Player,
        Enemy
    }

    public static class TeamExtensions
    {
        /// <summary>Player advances up the board (+y); enemies march down toward the king.</summary>
        public static int ForwardY(this Team team) => team == Team.Player ? 1 : -1;

        public static Team Opponent(this Team team) => team == Team.Player ? Team.Enemy : Team.Player;
    }
}
