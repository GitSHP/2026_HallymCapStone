using System;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// The UI half of the tween seam. Screens describe intent — "this card was
    /// denied", "this panel is entering" — and never name a duration or an ease,
    /// so the whole interface can be retuned from the TweenPresets asset alone.
    /// Mirrors <see cref="Promotion.Feel.Fx"/>, which owns the same job on the board.
    /// </summary>
    public static class UiFx
    {
        static Feel.TweenPresets P => Feel.Fx.P;

        // ---------- Screen ----------

        public static Tween BackdropIn(CanvasGroup group, float targetAlpha = 1f)
        {
            group.alpha = 0f;
            return Tween.Alpha(group, targetAlpha, P.uiBackdropFade, Ease.OutQuad);
        }

        public static Tween BackdropOut(CanvasGroup group)
            => Tween.Alpha(group, 0f, P.uiBackdropFade, Ease.InQuad);

        public static Sequence PanelIn(RectTransform panel, CanvasGroup group)
        {
            panel.localScale = P.uiPanelFromScale;
            group.alpha = 0f;

            return Sequence.Create()
                .Group(Tween.Scale(panel, Vector3.one, P.uiPanelIn, P.uiPanelInEase))
                .Group(Tween.Alpha(group, 1f, P.uiPanelIn * 0.6f, Ease.OutQuad));
        }

        public static Sequence PanelOut(RectTransform panel, CanvasGroup group, Action onComplete = null)
        {
            return Sequence.Create()
                .Group(Tween.Scale(panel, P.uiPanelFromScale, P.uiPanelOut, P.uiPanelOutEase))
                .Group(Tween.Alpha(group, 0f, P.uiPanelOut, Ease.InQuad))
                .ChainCallback(() => onComplete?.Invoke());
        }

        // ---------- Cards ----------

        /// <summary>Cards rise into place one after another so the offer reads left to right.</summary>
        public static void CardIn(RectTransform card, CanvasGroup group, int index)
        {
            var home = card.anchoredPosition;
            card.anchoredPosition = home + new Vector2(0f, P.uiCardFromOffsetY);
            card.localScale = Vector3.one * 0.9f;
            group.alpha = 0f;

            float delay = index * P.uiCardStagger;
            Tween.UIAnchoredPosition(card, home, P.uiCardIn, P.uiCardInEase, startDelay: delay);
            Tween.Scale(card, Vector3.one, P.uiCardIn, P.uiCardInEase, startDelay: delay);
            Tween.Alpha(group, 1f, P.uiCardIn * 0.7f, Ease.OutQuad, startDelay: delay);
        }

        // ---------- Interaction ----------

        public static void HoverIn(RectTransform target, Vector2 homePosition)
        {
            Tween.Scale(target, Vector3.one * P.uiHoverScale, P.uiHoverDuration, Ease.OutQuad);
            Tween.UIAnchoredPosition(target, homePosition + new Vector2(0f, P.uiHoverLift),
                P.uiHoverDuration, Ease.OutQuad);
        }

        public static void HoverOut(RectTransform target, Vector2 homePosition)
        {
            Tween.Scale(target, Vector3.one, P.uiHoverDuration, Ease.OutQuad);
            Tween.UIAnchoredPosition(target, homePosition, P.uiHoverDuration, Ease.OutQuad);
        }

        public static void Press(Transform target)
            => Tween.Scale(target, Vector3.one * P.uiPressScale, P.uiPressDuration, Ease.OutQuad);

        public static void Release(Transform target, bool hovered)
            => Tween.Scale(target, Vector3.one * (hovered ? P.uiHoverScale : 1f),
                P.uiPressDuration, Ease.OutQuad);

        // ---------- Feedback ----------

        /// <summary>Confirmation: something good just happened here.</summary>
        public static void Punch(Transform target)
            => Tween.PunchScale(target, new ShakeSettings(
                P.uiPunchStrength, P.uiPunchDuration, P.uiPunchFrequency));

        /// <summary>Refusal: a horizontal head-shake, the gesture everyone already reads as "no".</summary>
        public static void Deny(Transform target)
            => Tween.ShakeLocalPosition(target, new ShakeSettings(
                P.uiDenyShake, P.uiDenyDuration, P.uiDenyFrequency));

        /// <summary>
        /// Rolls a number instead of snapping it, so the player sees the gold leave
        /// rather than having to compare two stills.
        /// </summary>
        public static Tween CountTo(TMP_Text label, int from, int to, string format = "{0}")
            => Tween.Custom(from, to, P.uiCountDuration, value =>
            {
                if (label != null)
                    label.text = string.Format(format, Mathf.RoundToInt(value));
            }, Ease.OutQuad);

        public static Tween FlashColor(TMP_Text label, Color flash, Color back)
        {
            label.color = flash;
            return Tween.Color(label, back, P.uiPunchDuration, Ease.OutQuad);
        }
    }
}
