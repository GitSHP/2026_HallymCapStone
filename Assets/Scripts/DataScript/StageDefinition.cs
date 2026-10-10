using System;
using UnityEngine;

namespace Gambonanza.Data
{
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
    [CreateAssetMenu(menuName = "Gambonanza/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
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
    }
}
