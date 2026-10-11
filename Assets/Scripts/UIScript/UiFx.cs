using System;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// 트윈 경계 중 UI 쪽 절반이다. 화면은 "이 카드가 거절됐다", "이 패널이 들어온다" 같은
    /// 의도만 전달하고 지속 시간이나 이징을 직접 지정하지 않는다. 그래서 인터페이스 전체를
    /// TweenPresets 에셋 하나로 다시 조정할 수 있다.
    /// 보드에서 같은 역할을 맡는 <see cref="Promotion.Feel.Fx"/>와 짝을 이룬다.
    /// </summary>
    public static class UiFx
    {
        static Feel.TweenPresets P => Feel.Fx.P;

        // ---------- 화면 ----------

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

        // ---------- 카드 ----------

        /// <summary>카드가 하나씩 차례로 떠올라 자리를 잡아, 진열이 왼쪽에서 오른쪽으로 읽히게 한다.</summary>
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

        // ---------- 상호작용 ----------

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

        // ---------- 피드백 ----------

        /// <summary>확인 연출이다. 여기서 좋은 일이 방금 일어났음을 알린다.</summary>
        public static void Punch(Transform target)
            => Tween.PunchScale(target, new ShakeSettings(
                P.uiPunchStrength, P.uiPunchDuration, P.uiPunchFrequency));

        /// <summary>거절 연출이다. 누구나 "안 된다"로 읽는 좌우 고개 젓기 동작이다.</summary>
        public static void Deny(Transform target)
            => Tween.ShakeLocalPosition(target, new ShakeSettings(
                P.uiDenyShake, P.uiDenyDuration, P.uiDenyFrequency));

        /// <summary>
        /// 숫자를 한 번에 바꾸지 않고 굴려서 바꾼다. 플레이어가 전후 두 값을 비교할 필요 없이
        /// 골드가 빠져나가는 것을 직접 보게 된다.
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
