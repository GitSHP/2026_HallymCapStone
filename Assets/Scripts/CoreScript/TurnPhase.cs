namespace Promotion.Core
{
    /// <summary>스테이지 생명주기다. Deploy는 한 번만 일어나고, 이후 승패가 날 때까지 Player/Enemy가 번갈아 진행된다.</summary>
    public enum TurnPhase
    {
        Deploy,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }
}
