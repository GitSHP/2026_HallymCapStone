using Promotion.Data;

namespace Promotion.Core
{
    /// <summary>Runtime state of one piece on the board. Pure data — no Unity types.</summary>
    public class Piece
    {
        public PieceDefinition Def { get; }
        public Team Team { get; }
        public Coord Coord { get; set; }

        /// <summary>Set once the piece has moved; some patterns (the pawn's double step) only apply before that.</summary>
        public bool HasMoved { get; set; }

        public bool IsObjective => Def.isObjective;

        public Piece(PieceDefinition def, Team team, Coord coord)
        {
            Def = def;
            Team = team;
            Coord = coord;
        }
    }
}
