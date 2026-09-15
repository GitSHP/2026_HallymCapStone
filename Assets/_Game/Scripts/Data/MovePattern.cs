using System;
using UnityEngine;

namespace Gambonanza.Data
{
    /// <summary>
    /// One movement rule. Every classic chess piece — and every variant piece
    /// added later — is a combination of these, so new pieces are authored as
    /// ScriptableObject data rather than new code.
    /// </summary>
    [Serializable]
    public class MovePattern
    {
        [Tooltip("Offsets from the piece. For sliding pieces these are directions, repeated until blocked.")]
        public Vector2Int[] directions = Array.Empty<Vector2Int>();

        [Tooltip("Repeat the direction until blocked (rook/bishop/queen) instead of a single step.")]
        public bool sliding;

        [Tooltip("Range limit for sliding patterns.")]
        public int maxDistance = 8;

        [Tooltip("May move onto an empty square (false for the pawn's diagonal capture).")]
        public bool canMoveToEmpty = true;

        [Tooltip("May capture an enemy (false for the pawn's forward push).")]
        public bool canCapture = true;

        [Tooltip("Mirror the Y axis for the enemy team so 'forward' means the right way for both sides.")]
        public bool relativeToFacing;
    }
}
