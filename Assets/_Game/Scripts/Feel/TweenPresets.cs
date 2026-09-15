using PrimeTween;
using UnityEngine;

namespace Gambonanza.Feel
{
    /// <summary>
    /// Every timing, strength and ease used by game feel lives here so the whole
    /// look can be retuned from one inspector without touching gameplay code.
    /// Expected at Assets/_Game/Resources/TweenPresets.asset
    /// </summary>
    [CreateAssetMenu(menuName = "Gambonanza/Tween Presets", fileName = "TweenPresets")]
    public class TweenPresets : ScriptableObject
    {
        [Header("Hover")]
        public float hoverScale = 1.08f;
        public float hoverDuration = 0.12f;
        public Ease hoverEase = Ease.OutQuad;

        [Header("Drag")]
        public float dragScale = 1.18f;
        public float dragTiltDegrees = 6f;
        public float dragFollowSharpness = 18f;   // higher = snappier cursor follow
        public float dragLiftDuration = 0.1f;

        [Header("Drop / Landing")]
        public float dropDuration = 0.14f;
        public Ease dropEase = Ease.InQuad;
        public Vector3 dropPunchStrength = new Vector3(0.35f, 0.35f, 0f);
        public float dropPunchDuration = 0.3f;
        public float dropPunchFrequency = 12f;

        [Header("Board Shake (on landing)")]
        public Vector3 boardShakeStrength = new Vector3(0.08f, 0.08f, 0f);
        public float boardShakeDuration = 0.25f;
        public float boardShakeFrequency = 14f;

        [Header("Move Highlight")]
        public float highlightPopDuration = 0.18f;
        public float highlightStagger = 0.02f;
        public Ease highlightEase = Ease.OutBack;

        [Header("Combat")]
        public float lungeDistance = 0.35f;
        public float lungeDuration = 0.12f;
        public Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        public float hitFlashDuration = 0.1f;
        public Vector3 hitShakeStrength = new Vector3(0.12f, 0.05f, 0f);
        public float hitShakeDuration = 0.22f;

        [Header("Death")]
        public float deathDuration = 0.28f;
        public float deathSpinDegrees = 200f;

        [Header("Spawn")]
        public float spawnDropHeight = 3f;
        public float spawnDuration = 0.3f;
        public Ease spawnEase = Ease.InQuad;
        public float spawnStagger = 0.06f;

        [Header("Global")]
        public float hitStopDuration = 0.08f;
    }
}
