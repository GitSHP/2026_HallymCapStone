using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// How the chess AI plays. Difficulty is tuned here, not in code: a beginner
    /// opponent looks one move ahead, picks loosely among good moves and now and
    /// then makes a believable mistake, but never misses a checkmate.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/AI Profile", fileName = "AIProfile")]
    public class AIProfile : ScriptableObject
    {
        [Header("Search")]
        [Tooltip("Moves looked ahead. 1 only sees what each move wins or loses right away.")]
        [Range(1, 4)] public int maxDepth = 1;

        [Header("Looseness")]
        [Tooltip("How freely it picks moves that are not the best, in pawns. 0 always plays its top move.")]
        [Min(0f)] public float temperature = 1f;
        [Tooltip("Chance of a deliberate mistake: a plausible move picked at random.")]
        [Range(0f, 1f)] public float blunderRate = 0.25f;
        [Tooltip("A mistake may give away at most this much, in pawns (3 = a bishop; the queen is never thrown away).")]
        [Min(0f)] public float maxBlunderLoss = 3f;

        [Header("Mercy")]
        [Tooltip("When the AI leads by this many pawns, it starts making more mistakes.")]
        [Min(0f)] public float mercyLead = 5f;
        [Tooltip("Mistake chance once the mercy lead is reached.")]
        [Range(0f, 1f)] public float mercyBlunderRate = 0.4f;

        [Header("Must do")]
        [Tooltip("Chance of playing a checkmate when one is available. 1 means it always finishes the game.")]
        [Range(0f, 1f)] public float mateFindRate = 1f;
        [Tooltip("Opening moves played sensibly (centre pawns, developing pieces) before looseness kicks in.")]
        [Min(0)] public int sensibleOpeningMoves = 4;

        [Header("Presentation")]
        [Tooltip("Minimum seconds the AI appears to think before moving.")]
        [Min(0f)] public float minThinkSeconds = 0.6f;
    }
}
