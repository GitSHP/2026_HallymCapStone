using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace Gambonanza.Feel
{
    /// <summary>
    /// The single seam between gameplay and the tween library.
    /// Gameplay code never calls PrimeTween directly — swapping the library
    /// later means rewriting this file and nothing else.
    /// </summary>
    public static class Fx
    {
        static TweenPresets _presets;

        public static TweenPresets P
        {
            get
            {
                if (_presets == null)
                {
                    _presets = Resources.Load<TweenPresets>("TweenPresets");
                    if (_presets == null)
                    {
                        // Falls back to defaults so the game still runs before the asset exists.
                        _presets = ScriptableObject.CreateInstance<TweenPresets>();
                        Debug.LogWarning("[Fx] TweenPresets asset not found in Resources — using defaults.");
                    }
                }
                return _presets;
            }
        }

        // Keeps one tween per transform per channel so repeated hovers don't stack.
        static readonly Dictionary<Transform, Tween> ScaleTweens = new();

        static void ReplaceScaleTween(Transform t, Tween tween)
        {
            if (ScaleTweens.TryGetValue(t, out var running) && running.isAlive)
                running.Stop();
            ScaleTweens[t] = tween;
        }

        // ---------- Hover ----------

        public static void HoverIn(Transform t, float baseScale = 1f)
            => ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.hoverScale, P.hoverDuration, P.hoverEase));

        public static void HoverOut(Transform t, float baseScale = 1f)
            => ReplaceScaleTween(t, Tween.Scale(t, baseScale, P.hoverDuration, P.hoverEase));

        // ---------- Drag ----------

        public static void PickUp(Transform t, float baseScale = 1f)
        {
            ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.dragScale, P.dragLiftDuration, Ease.OutBack));
            Tween.LocalRotation(t, Quaternion.Euler(0f, 0f, P.dragTiltDegrees), P.dragLiftDuration, Ease.OutQuad);
        }

        /// <summary>Drops a piece onto a square: settle, punch, and shake the board underneath it.</summary>
        public static Sequence Drop(Transform piece, Vector3 worldTarget, Transform boardRoot, float baseScale = 1f)
        {
            // Only straighten a piece that was actually tilted; tweening a value
            // to itself is wasted work and PrimeTween warns about it.
            if (piece.localRotation != Quaternion.identity)
                Tween.LocalRotation(piece, Quaternion.identity, P.dropDuration, Ease.OutQuad);

            var seq = Sequence.Create()
                .Group(Tween.Position(piece, worldTarget, P.dropDuration, P.dropEase))
                .Group(Tween.Scale(piece, baseScale, P.dropDuration, P.dropEase))
                .Chain(Tween.PunchScale(piece, new ShakeSettings(
                    P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency)));

            if (boardRoot != null)
                ShakeBoard(boardRoot);

            return seq;
        }

        public static void ShakeBoard(Transform boardRoot, float intensity = 1f)
            => Tween.ShakeLocalPosition(boardRoot, new ShakeSettings(
                P.boardShakeStrength * intensity, P.boardShakeDuration, P.boardShakeFrequency));

        // ---------- Highlights ----------

        /// <summary>
        /// Pops something in, staggered by its index so a set ripples outward.
        /// Starts small but never at zero: if the tween never runs — editor frame
        /// stalls, timeScale 0, tweens cancelled by a recompile — the object must
        /// still be visible rather than stuck invisible forever. Presentation is
        /// allowed to fail; the game state it represents is not.
        /// </summary>
        public static void PopIn(Transform t, int index = 0, float targetScale = 1f)
        {
            t.localScale = Vector3.one * (targetScale * PopInStartScale);
            Tween.Scale(t, targetScale, P.highlightPopDuration, P.highlightEase,
                startDelay: index * P.highlightStagger);
        }

        const float PopInStartScale = 0.6f;

        // ---------- Combat ----------

        /// <summary>Attacker lunges toward the target and snaps back — the hit lands mid-lunge.</summary>
        public static Sequence Lunge(Transform attacker, Vector3 towardWorld, Action onImpact = null)
        {
            var origin = attacker.position;
            var dir = (towardWorld - origin).normalized;
            var peak = origin + dir * P.lungeDistance;

            return Sequence.Create()
                .Chain(Tween.Position(attacker, peak, P.lungeDuration, Ease.OutQuad))
                .ChainCallback(() => onImpact?.Invoke())
                .Chain(Tween.Position(attacker, origin, P.lungeDuration * 1.4f, Ease.OutBack));
        }

        /// <summary>
        /// Capture and advance as one motion: lunge in, take the piece, settle on
        /// the square. Kept in a single sequence because two position tweens on
        /// one transform fight, and the longer one wins — which parked the
        /// attacker wherever the cursor happened to be.
        /// </summary>
        public static Sequence CaptureAdvance(Transform attacker, Vector3 victimWorld, Vector3 destination,
            Transform boardRoot, float baseScale, Action onImpact = null)
        {
            var dir = (victimWorld - attacker.position).normalized;
            var peak = attacker.position + dir * P.lungeDistance;

            if (attacker.localRotation != Quaternion.identity)
                Tween.LocalRotation(attacker, Quaternion.identity, P.lungeDuration, Ease.OutQuad);

            return Sequence.Create()
                .Chain(Tween.Position(attacker, peak, P.lungeDuration, Ease.OutQuad))
                .ChainCallback(() => onImpact?.Invoke())
                .Chain(Tween.Position(attacker, destination, P.dropDuration, P.dropEase))
                .Group(Tween.Scale(attacker, baseScale, P.dropDuration, P.dropEase))
                .ChainCallback(() =>
                {
                    Tween.PunchScale(attacker, new ShakeSettings(
                        P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency));
                    if (boardRoot != null)
                        ShakeBoard(boardRoot);
                });
        }

        public static void HitFlash(SpriteRenderer sr, Color baseColor)
        {
            Tween.Color(sr, P.hitFlashColor, P.hitFlashDuration, Ease.OutQuad, cycles: 2,
                cycleMode: CycleMode.Yoyo);
            Tween.ShakeLocalPosition(sr.transform, new ShakeSettings(
                P.hitShakeStrength, P.hitShakeDuration));
        }

        public static Sequence Die(Transform t, Action onComplete = null)
        {
            return Sequence.Create()
                .Group(Tween.Scale(t, 0f, P.deathDuration, Ease.InBack))
                .Group(Tween.LocalRotation(t, Quaternion.Euler(0f, 0f, P.deathSpinDegrees), P.deathDuration, Ease.InQuad))
                .ChainCallback(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Slow breathing on something the player can pick next, such as a map node.
        /// Loops until stopped or until the object is destroyed.
        /// </summary>
        public static Tween Breathe(Transform t, float baseScale = 1f)
        {
            t.localScale = Vector3.one * baseScale;
            return Tween.Scale(t, baseScale * P.mapPulseScale, P.mapPulseDuration, P.mapPulseEase,
                -1, CycleMode.Yoyo);
        }

        /// <summary>
        /// Fades a picture in over whatever is behind it. Starts faint rather than at
        /// zero so a cancelled tween still leaves the picture on screen.
        /// </summary>
        public static Tween FadeIn(UnityEngine.UI.Graphic graphic)
            => Tween.Alpha(graphic, StoryFadeStartAlpha, 1f, P.storyFadeDuration, P.storyFadeEase,
                useUnscaledTime: true);

        const float StoryFadeStartAlpha = 0.15f;

        /// <summary>Eases a graphic's opacity to a level, such as a shade deepening behind a scene.</summary>
        public static Tween FadeTo(UnityEngine.UI.Graphic graphic, float alpha)
            => Tween.Alpha(graphic, alpha, P.storyFadeDuration, P.storyFadeEase, useUnscaledTime: true);

        /// <summary>Lifts a full-screen cover, then switches it off.</summary>
        public static Tween FadeOutCurtain(CanvasGroup curtain)
            => Tween.Alpha(curtain, 0f, P.curtainFadeDuration, P.curtainFadeEase, useUnscaledTime: true)
                .OnComplete(curtain, c => c.gameObject.SetActive(false));

        /// <summary>A plain slide to another square, staggered so a wave reads as a wave.</summary>
        public static void StepTo(Transform t, Vector3 worldTarget, int index = 0)
            => Tween.Position(t, worldTarget, P.dropDuration * 1.6f, Ease.InOutQuad,
                startDelay: index * P.spawnStagger);

        // ---------- Spawn ----------

        public static Sequence SpawnDrop(Transform t, Vector3 worldTarget, Transform boardRoot, int index = 0)
        {
            t.position = worldTarget + Vector3.up * P.spawnDropHeight;

            return Sequence.Create()
                .ChainDelay(index * P.spawnStagger)
                .Chain(Tween.Position(t, worldTarget, P.spawnDuration, P.spawnEase))
                .ChainCallback(() => ShakeBoard(boardRoot, 0.6f));
        }

        // ---------- Global ----------

        /// <summary>Brief time freeze on impactful moments. Uses unscaled time so it always recovers.</summary>
        public static void HitStop(float scale = 0.05f)
        {
            Time.timeScale = scale;
            Tween.Delay(P.hitStopDuration, () => Time.timeScale = 1f, useUnscaledTime: true);
        }

        /// <summary>Frame-rate independent follow, for the cursor-trailing drag ghost.</summary>
        public static Vector3 SpringFollow(Vector3 current, Vector3 target, float deltaTime)
            => Vector3.Lerp(current, target, 1f - Mathf.Exp(-P.dragFollowSharpness * deltaTime));
    }
}
