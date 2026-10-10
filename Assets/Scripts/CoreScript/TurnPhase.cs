namespace Promotion.Core
{
    /// <summary>Stage lifecycle. Deploy happens once, then Player/Enemy alternate until an outcome.</summary>
    public enum TurnPhase
    {
        Deploy,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }
}
