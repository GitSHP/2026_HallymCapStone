using PrimeTween;
using UnityEngine;

namespace Promotion.Feel
{
    /// <summary>
    /// 게임 연출에 쓰이는 모든 시간·세기·Ease 값을 이곳에 모아, 게임플레이 코드를
    /// 건드리지 않고 인스펙터 하나에서 전체 느낌을 다시 조정할 수 있게 한다.
    /// 에셋 위치는 Assets/_Game/Resources/TweenPresets.asset 으로 가정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Tween Presets", fileName = "TweenPresets")]
    public class TweenPresets : ScriptableObject
    {
        [Header("호버")]
        public float hoverScale = 1.08f;
        public float hoverDuration = 0.12f;
        public Ease hoverEase = Ease.OutQuad;

        [Header("드래그")]
        public float dragScale = 1.18f;
        public float dragTiltDegrees = 6f;
        public float dragFollowSharpness = 18f;   // 클수록 커서를 더 빠릿하게 따라간다
        public float dragLiftDuration = 0.1f;

        [Header("내려놓기 / 착지")]
        public float dropDuration = 0.14f;
        public Ease dropEase = Ease.InQuad;
        public Vector3 dropPunchStrength = new Vector3(0.35f, 0.35f, 0f);
        public float dropPunchDuration = 0.3f;
        public float dropPunchFrequency = 12f;

        [Header("보드 흔들림 (착지 시)")]
        public Vector3 boardShakeStrength = new Vector3(0.08f, 0.08f, 0f);
        public float boardShakeDuration = 0.25f;
        public float boardShakeFrequency = 14f;

        [Header("이동 하이라이트")]
        public float highlightPopDuration = 0.18f;
        public float highlightStagger = 0.02f;
        public Ease highlightEase = Ease.OutBack;

        [Header("전투")]
        public float lungeDistance = 0.35f;
        public float lungeDuration = 0.12f;
        public Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        public float hitFlashDuration = 0.1f;
        public Vector3 hitShakeStrength = new Vector3(0.12f, 0.05f, 0f);
        public float hitShakeDuration = 0.22f;

        [Header("사망")]
        public float deathDuration = 0.28f;
        public float deathSpinDegrees = 200f;

        [Header("등장")]
        public float spawnDropHeight = 3f;
        public float spawnDuration = 0.3f;
        public Ease spawnEase = Ease.InQuad;
        public float spawnStagger = 0.06f;

        [Header("UI — 화면")]
        public float uiBackdropFade = 0.22f;
        public float uiPanelIn = 0.34f;
        public float uiPanelOut = 0.18f;
        public Vector3 uiPanelFromScale = new Vector3(0.92f, 0.92f, 1f);
        public Ease uiPanelInEase = Ease.OutBack;
        public Ease uiPanelOutEase = Ease.InQuad;

        [Header("UI — 카드")]
        public float uiCardIn = 0.3f;
        public float uiCardStagger = 0.05f;
        public float uiCardFromOffsetY = -40f;
        public Ease uiCardInEase = Ease.OutBack;

        [Header("UI — 상호작용")]
        public float uiHoverScale = 1.05f;
        public float uiHoverLift = 10f;
        public float uiHoverDuration = 0.14f;
        public float uiPressScale = 0.95f;
        public float uiPressDuration = 0.07f;

        [Header("UI — 피드백")]
        public Vector3 uiPunchStrength = new Vector3(0.14f, 0.14f, 0f);
        public float uiPunchDuration = 0.35f;
        public float uiPunchFrequency = 10f;
        public Vector3 uiDenyShake = new Vector3(14f, 0f, 0f);
        public float uiDenyDuration = 0.3f;
        public float uiDenyFrequency = 18f;
        public float uiCountDuration = 0.4f;

        [Header("맵")]
        public float mapPulseScale = 1.18f;
        public float mapPulseDuration = 0.6f;
        public Ease mapPulseEase = Ease.InOutSine;

        [Header("스토리")]
        public float storyFadeDuration = 0.5f;
        public Ease storyFadeEase = Ease.OutQuad;
        public float curtainFadeDuration = 0.8f;
        public Ease curtainFadeEase = Ease.InOutQuad;

        [Header("전역")]
        public float hitStopDuration = 0.08f;
    }
}
