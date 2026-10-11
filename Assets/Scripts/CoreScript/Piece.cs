using Promotion.Data;

namespace Promotion.Core
{
    /// <summary>보드 위 기물 하나의 런타임 상태다. 순수 데이터이며 Unity 타입을 쓰지 않는다.</summary>
    public class Piece
    {
        public PieceDefinition Def { get; }
        public Team Team { get; }
        public Coord Coord { get; set; }

        /// <summary>기물이 한 번이라도 움직이면 설정된다. 일부 패턴(폰의 두 칸 전진)은 그 전에만 적용된다.</summary>
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
