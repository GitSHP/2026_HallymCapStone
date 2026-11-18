namespace Promotion.Core
{
    public enum TurnPhase   // 스테이지가 지금 어느 단계에 있는지 나타내는 열거형
    {
        Deploy,   // 배치 단계 -> 전투 시작 전 킹을 보드에 놓는 단계
        PlayerTurn,   // 플레이어 턴 -> AP를 써서 기물을 소환·이동하는 단계
        EnemyTurn,   // 적 턴 -> 적들이 킹 쪽으로 한 칸씩 다가오는 단계
        Victory,   // 승리로 스테이지가 끝난 상태
        Defeat   // 패배로 스테이지가 끝난 상태
    }
}
