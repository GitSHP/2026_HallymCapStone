using System.Collections.Generic;

namespace Promotion.Core
{
    public interface IBoardView   // 보드를 "읽기만" 하는 공통 창구 인터페이스
    // -> 실제 보드(BoardState)와 계산용 복사 보드(SearchBoard)가 둘 다 구현하므로
    //    행마법 계산(MoveResolver)·체스 규칙(ChessRules)이 어느 보드든 같은 코드로 동작함
    {
        int Width { get; }   // 보드의 가로 칸 수
        int Height { get; }   // 보드의 세로 칸 수
        IReadOnlyList<Piece> AllPieces { get; }   // 보드 위에 있는 모든 기물 목록 (아군·적 모두)
        bool IsInside(Coord c);   // 좌표가 보드 안쪽 칸인지 확인하는 함수
        Piece GetPiece(Coord c);   // 그 칸에 서 있는 기물을 돌려주는 함수 (없으면 null)
    }
}
