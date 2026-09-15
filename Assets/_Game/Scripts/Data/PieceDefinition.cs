using Gambonanza.Core;
using UnityEngine;

namespace Gambonanza.Data
{
    /// <summary>
    /// Everything that makes a piece what it is: art, cost, stats, and how it moves.
    /// Swapping in real art later means assigning <see cref="sprite"/> here — no code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Gambonanza/Piece Definition", fileName = "Piece")]
    public class PieceDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Pawn";
        [Tooltip("Placeholder glyph shown until real art is assigned.")]
        public string glyph = "P";

        [Header("Art (leave empty to use the placeholder)")]
        public Sprite sprite;
        public Color playerColor = new Color(0.95f, 0.95f, 0.92f);
        public Color enemyColor = new Color(0.25f, 0.22f, 0.28f);

        [Header("Cost")]
        [Tooltip("Action points spent to move this piece.")]
        public int moveApCost = 1;
        [Tooltip("Action points spent to deploy it from the deck.")]
        public int deployApCost = 1;

        [Header("Combat")]
        public int maxHp = 3;
        public int attack = 1;

        [Header("Role")]
        [Tooltip("The king: it has no HP and is never attacked. Enemies win by stepping onto its tile.")]
        public bool isObjective;

        [Header("Movement")]
        public MovePattern[] patterns = new MovePattern[0];
    }
}
