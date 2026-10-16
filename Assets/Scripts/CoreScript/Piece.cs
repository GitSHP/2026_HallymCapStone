using Promotion.Data;

namespace Promotion.Core
{
    public class Piece   // 보드 위에 놓인 기물 하나의 상태(무엇인지, 누구 편인지, 어디 있는지)를 담는 클래스
    // -> 화면 표시(PieceView)와 분리된 순수 데이터라서 규칙 계산용 복사 보드에서도 그대로 사용함
    {
        public PieceDefinition Def { get; }   // 기물의 원본 데이터(SO) -> 이름, 그림, 비용, 행마법이 들어있음
        public Team Team { get; }   // 이 기물이 속한 진영 (플레이어 / 적)
        public Coord Coord { get; set; }   // 현재 서 있는 칸의 좌표

        public bool Owned { get; set; }   // 런 인벤토리(RunState)에서 꺼낸 보유 기물인지 여부
        // -> true면 적에게 잡혔을 때 인벤토리에서도 영구히 지워짐. 킹·상시 폰·최종전 무료 폰 같은 임시 기물은 false

        public bool HasMoved { get; set; }   // 한 번이라도 움직였는지 여부 -> "첫 수에만 가능한 이동"(폰 2칸 전진 등) 판정에 사용

        public bool IsObjective => Def.isObjective;   // 지켜야 하는 핵심 기물(킹)인지 여부 -> 킹은 잡히지 않고, 적이 그 칸에 들어오면 패배

        public Piece(PieceDefinition def, Team team, Coord coord)   // 기물 데이터·진영·위치를 받아 새 기물을 만드는 생성자
        {
            Def = def;
            Team = team;
            Coord = coord;
        }
    }
}
