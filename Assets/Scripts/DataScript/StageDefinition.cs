using System;
using UnityEngine;

namespace Promotion.Data
{
    public enum StageMode
    {
        /// <summary>Hold off waves of enemies that walk toward the king.</summary>
        Waves,
        /// <summary>A game of chess against the AI, won by checkmate.</summary>
        ChessDuel
    }

    /// <summary>A piece standing on a square at the start of a stage.</summary>
    [Serializable]
    public class PiecePlacement
    {
        public PieceDefinition piece;
        [Tooltip("x = file (0 = a), y = rank (0 = the player's back rank).")]
        public Vector2Int square;
    }

    [Serializable]
    public class WaveDefinition
    {
        [Tooltip("Player turn at whose end this wave marches in.")]
        public int spawnOnTurn = 1;
        public PieceDefinition enemy;
        public int count = 3;
    }

    /// <summary>
    /// One stage: how much the player can do per turn, what they may deploy,
    /// and what comes at them. Authoring a new stage means a new asset, not new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Mode")]
        public StageMode mode = StageMode.Waves;

        [Header("Look")]
        [Tooltip("Board picture for this stage. Empty keeps the scene's own board.")]
        public Sprite boardArt;

        [Header("Action points")]
        public int apPerTurn = 5;

        [Header("Deploy")]
        [Tooltip("Ranks from the player's edge that pieces may be deployed onto.")]
        public int deployRows = 3;

        [Tooltip("The king. It must be placed before the stage can start.")]
        public PieceDefinition kingDefinition;

        [Tooltip("Anything else the player brings in besides the king.")]
        public PieceDefinition[] startingDeck = Array.Empty<PieceDefinition>();

        [Header("Waves")]
        public WaveDefinition[] waves = Array.Empty<WaveDefinition>();
        [Tooltip("Ranks from the top edge that enemies spawn on.")]
        public int spawnRows = 2;

        [Header("Rules")]
        [Tooltip("Enemies may step diagonally toward the king.")]
        public bool enemiesMoveDiagonally = true;

        [Header("Chess duel")]
        [Tooltip("The enemy (black) army and where it stands.")]
        public PiecePlacement[] enemyLayout = Array.Empty<PiecePlacement>();
        [Tooltip("Pawn handed out for free at deploy, on top of what the run collected.")]
        public PieceDefinition freePawn;
        [Min(0)] public int maxFreePawns = 8;
        [Tooltip("Player moves allowed before the duel counts as lost.")]
        [Min(1)] public int moveLimit = 100;
        [Tooltip("What a pawn may become on the far rank. The AI always takes the first entry.")]
        public PieceDefinition[] promotionChoices = Array.Empty<PieceDefinition>();
        public AIProfile aiProfile;

        public bool IsChessDuel => mode == StageMode.ChessDuel;
    }
}
