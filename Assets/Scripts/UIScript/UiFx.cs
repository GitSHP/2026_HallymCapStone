using System;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Promotion.UI
{
    public static class UiFx  // UI 전용 연출(트윈) 함수 모음 -> UI 스크립트는 PrimeTween을 직접 쓰지 않고 이 함수들을 호출함
    // -> 연출 시간·세기 같은 수치는 TweenPresets 에셋에 있어서 인스펙터에서 조정 가능
    {
        static Feel.TweenPresets P => Feel.Fx.P;  // 연출 수치가 들어 있는 TweenPresets 에셋을 짧게 부르기 위한 속성

        public static Tween BackdropIn(CanvasGroup group, float targetAlpha = 1f)  // 어두운 배경을 서서히 나타나게 하는 함수
        {
            group.alpha = 0f;  // 완전히 투명한 상태에서 시작해서
            return Tween.Alpha(group, targetAlpha, P.uiBackdropFade, Ease.OutQuad);  // 목표 투명도까지 서서히 진해짐
        }

        public static Tween BackdropOut(CanvasGroup group)  // 어두운 배경을 서서히 사라지게 하는 함수
            => Tween.Alpha(group, 0f, P.uiBackdropFade, Ease.InQuad);

        public static Sequence PanelIn(RectTransform panel, CanvasGroup group)  // 패널(창)이 커지면서 나타나는 연출 함수
        {
            panel.localScale = P.uiPanelFromScale;  // 살짝 작은 크기에서 시작하고 (크기 0에서 시작하지 않음 -> 연출이 끊겨도 창이 보이도록)
            group.alpha = 0f;  // 투명한 상태에서 시작

            return Sequence.Create()  // 아래 두 연출을 동시에 재생
                .Group(Tween.Scale(panel, Vector3.one, P.uiPanelIn, P.uiPanelInEase))  // 원래 크기로 커지고
                .Group(Tween.Alpha(group, 1f, P.uiPanelIn * 0.6f, Ease.OutQuad));  // 크기 연출보다 조금 빨리 불투명해짐
        }

        public static Sequence PanelOut(RectTransform panel, CanvasGroup group, Action onComplete = null)  // 패널이 작아지면서 사라지는 연출 함수
        // -> 연출이 끝나면 onComplete 함수를 실행함 (예: 창 오브젝트 끄기)
        {
            return Sequence.Create()
                .Group(Tween.Scale(panel, P.uiPanelFromScale, P.uiPanelOut, P.uiPanelOutEase))  // 작아지면서
                .Group(Tween.Alpha(group, 0f, P.uiPanelOut, Ease.InQuad))  // 동시에 투명해지고
                .ChainCallback(() => onComplete?.Invoke());  // 모두 끝난 뒤 완료 함수 실행
        }

        public static void CardIn(RectTransform card, CanvasGroup group, int index)  // 카드·버튼이 아래에서 올라오며 나타나는 연출 함수
        // -> index가 클수록 늦게 시작해서 여러 장이 차례대로 등장함
        {
            var home = card.anchoredPosition;  // 최종 도착 위치(원래 위치)를 기억하고
            card.anchoredPosition = home + new Vector2(0f, P.uiCardFromOffsetY);  // 그보다 아래쪽에서 시작
            card.localScale = Vector3.one * 0.9f;  // 90% 크기에서 시작
            group.alpha = 0f;  // 투명한 상태에서 시작

            float delay = index * P.uiCardStagger;  // 순서에 따라 시작을 늦출 시간
            Tween.UIAnchoredPosition(card, home, P.uiCardIn, P.uiCardInEase, startDelay: delay);  // 원래 위치로 이동
            Tween.Scale(card, Vector3.one, P.uiCardIn, P.uiCardInEase, startDelay: delay);  // 원래 크기로 커짐
            Tween.Alpha(group, 1f, P.uiCardIn * 0.7f, Ease.OutQuad, startDelay: delay);  // 불투명해짐
        }

        public static void HoverIn(RectTransform target, Vector2 homePosition)  // 마우스를 올렸을 때 살짝 커지고 떠오르는 연출 함수
        {
            Tween.Scale(target, Vector3.one * P.uiHoverScale, P.uiHoverDuration, Ease.OutQuad);  // 조금 커지고
            Tween.UIAnchoredPosition(target, homePosition + new Vector2(0f, P.uiHoverLift),
                P.uiHoverDuration, Ease.OutQuad);  // 원래 위치보다 위로 떠오름
        }

        public static void HoverOut(RectTransform target, Vector2 homePosition)  // 마우스가 벗어났을 때 원래 크기·위치로 돌아오는 연출 함수
        {
            Tween.Scale(target, Vector3.one, P.uiHoverDuration, Ease.OutQuad);
            Tween.UIAnchoredPosition(target, homePosition, P.uiHoverDuration, Ease.OutQuad);
        }

        public static void Press(Transform target)  // 눌렀을 때 작아지는 연출 함수
            => Tween.Scale(target, Vector3.one * P.uiPressScale, P.uiPressDuration, Ease.OutQuad);

        public static void Release(Transform target, bool hovered)  // 뗐을 때 크기를 되돌리는 연출 함수
            => Tween.Scale(target, Vector3.one * (hovered ? P.uiHoverScale : 1f),
                P.uiPressDuration, Ease.OutQuad);  // 마우스가 아직 위에 있으면 떠오른 크기, 아니면 원래 크기로

        public static void Punch(Transform target)  // 클릭했을 때 톡 튀는(크기가 출렁이는) 연출 함수
            => Tween.PunchScale(target, new ShakeSettings(
                P.uiPunchStrength, P.uiPunchDuration, P.uiPunchFrequency));

        public static void Deny(Transform target)  // 거절 연출 함수 -> 좌우로 흔들어서 "할 수 없음"을 알림 (예: 골드 부족)
            => Tween.ShakeLocalPosition(target, new ShakeSettings(
                P.uiDenyShake, P.uiDenyDuration, P.uiDenyFrequency));

        public static Tween CountTo(TMP_Text label, int from, int to, string format = "{0}")  // 숫자가 from에서 to까지 차르륵 바뀌는 연출 함수 (골드 표시 등)
            => Tween.Custom(from, to, P.uiCountDuration, value =>  // 시간에 따라 from → to 사이 값을 계속 받아서
            {
                if (label != null)  // 텍스트 UI가 아직 남아 있다면 (화면이 닫혀 사라졌을 수 있음)
                    label.text = string.Format(format, Mathf.RoundToInt(value));  // 정수로 반올림해서 형식에 맞춰 표시
            }, Ease.OutQuad);

        public static Tween FlashColor(TMP_Text label, Color flash, Color back)  // 글자 색을 잠깐 번쩍 바꿨다가 원래 색으로 돌리는 연출 함수
        {
            label.color = flash;  // 번쩍일 색으로 바로 바꾸고
            return Tween.Color(label, back, P.uiPunchDuration, Ease.OutQuad);  // 원래 색으로 서서히 돌아옴
        }
    }
}
