using Gambonanza.Data;

namespace Gambonanza.Core
{
    /// <summary>Runtime state of one piece on the board. Pure data — no Unity types.</summary>
    public class Piece
    {
        public PieceDefinition Def { get; }
        public Team Team { get; }
        public Coord Coord { get; set; }
        public int Hp { get; set; }

        public bool IsObjective => Def.isObjective;
        public bool IsAlive => IsObjective || Hp > 0;

        public Piece(PieceDefinition def, Team team, Coord coord)
        {
            Def = def;
            Team = team;
            Coord = coord;
            Hp = def.maxHp;
        }

        /// <summary>Returns true if this damage killed the piece. The objective never takes damage.</summary>
        public bool TakeDamage(int amount)
        {
            if (IsObjective)
                return false;

            Hp -= amount;
            return Hp <= 0;
        }
    }
}
