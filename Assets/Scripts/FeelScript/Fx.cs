using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace Promotion.Feel
{
    /// <summary>
    /// 게임플레이와 트윈 라이브러리를 잇는 유일한 접점이다.
    /// 게임플레이 코드는 PrimeTween을 직접 호출하지 않으므로, 나중에 라이브러리를
    /// 교체할 때는 이 파일만 다시 쓰면 된다.
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
                        // 에셋이 아직 없어도 게임이 돌아가도록 기본값으로 대체한다.
                        _presets = ScriptableObject.CreateInstance<TweenPresets>();
                        Debug.LogWarning("[Fx] TweenPresets asset not found in Resources — using defaults.");
                    }
                }
                return _presets;
            }
        }

        // 트랜스폼·채널마다 트윈을 하나만 유지해, 호버가 반복되어도 겹쳐 쌓이지 않게 한다.
        static readonly Dictionary<Transform, Tween> ScaleTweens = new();

        static void ReplaceScaleTween(Transform t, Tween tween)
        {
            if (ScaleTweens.TryGetValue(t, out var running) && running.isAlive)
                running.Stop();
            ScaleTweens[t] = tween;
        }

        // ---------- 호버 ----------

        public static void HoverIn(Transform t, float baseScale = 1f)
            => ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.hoverScale, P.hoverDuration, P.hoverEase));

        public static void HoverOut(Transform t, float baseScale = 1f)
            => ReplaceScaleTween(t, Tween.Scale(t, baseScale, P.hoverDuration, P.hoverEase));

        // ---------- 드래그 ----------

        public static void PickUp(Transform t, float baseScale = 1f)
        {
            ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.dragScale, P.dragLiftDuration, Ease.OutBack));
            Tween.LocalRotation(t, Quaternion.Euler(0f, 0f, P.dragTiltDegrees), P.dragLiftDuration, Ease.OutQuad);
        }

        /// <summary>기물을 칸에 내려놓는다. 자리를 잡고, 튕기듯 커졌다 돌아오며, 아래의 보드를 흔든다.</summary>
        public static Sequence Drop(Transform piece, Vector3 worldTarget, Transform boardRoot, float baseScale = 1f)
        {
            // 실제로 기울어진 기물만 바로 세운다. 같은 값으로 트윈하는 것은
            // 헛수고이고 PrimeTween이 경고를 띄운다.
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

        // ---------- 하이라이트 ----------

        /// <summary>
        /// 대상을 톡 튀어나오듯 등장시킨다. 인덱스만큼 시차를 두어 여러 개가 물결처럼 퍼진다.
        /// 작게 시작하되 절대 0에서 시작하지 않는다. 에디터 프레임 정체, timeScale 0,
        /// 재컴파일로 인한 트윈 취소 등으로 트윈이 아예 돌지 않더라도, 오브젝트가 영영
        /// 안 보이는 상태로 남지 않고 보여야 하기 때문이다. 연출은 실패해도 되지만,
        /// 연출이 나타내는 게임 상태는 실패하면 안 된다.
        /// </summary>
        public static void PopIn(Transform t, int index = 0, float targetScale = 1f)
        {
            t.localScale = Vector3.one * (targetScale * PopInStartScale);
            Tween.Scale(t, targetScale, P.highlightPopDuration, P.highlightEase,
                startDelay: index * P.highlightStagger);
        }

        const float PopInStartScale = 0.6f;

        // ---------- 전투 ----------

        /// <summary>공격자가 대상 쪽으로 돌진했다가 되돌아온다. 타격은 돌진 도중에 들어간다.</summary>
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
        /// 잡기와 전진을 하나의 동작으로 처리한다. 돌진해서 기물을 잡고 그 칸에 자리 잡는다.
        /// 하나의 시퀀스로 묶은 이유는, 한 트랜스폼에 위치 트윈 두 개가 걸리면 서로 충돌해
        /// 더 긴 쪽이 이기기 때문이다. 그 결과 공격자가 커서가 있던 자리에 멈춰 버리는 문제가 있었다.
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
        /// 맵 노드처럼 플레이어가 다음에 고를 수 있는 대상에 느린 숨쉬기 효과를 준다.
        /// 멈추거나 오브젝트가 파괴될 때까지 반복된다.
        /// </summary>
        public static Tween Breathe(Transform t, float baseScale = 1f)
        {
            t.localScale = Vector3.one * baseScale;
            return Tween.Scale(t, baseScale * P.mapPulseScale, P.mapPulseDuration, P.mapPulseEase,
                -1, CycleMode.Yoyo);
        }

        /// <summary>
        /// 그림을 뒤에 있는 것 위로 서서히 나타나게 한다. 0이 아니라 옅은 상태에서 시작하므로
        /// 트윈이 취소되어도 그림은 화면에 남는다.
        /// </summary>
        public static Tween FadeIn(UnityEngine.UI.Graphic graphic)
            => Tween.Alpha(graphic, StoryFadeStartAlpha, 1f, P.storyFadeDuration, P.storyFadeEase,
                useUnscaledTime: true);

        const float StoryFadeStartAlpha = 0.15f;

        /// <summary>그래픽의 불투명도를 지정한 값까지 부드럽게 바꾼다. 장면 뒤의 그늘이 짙어지는 연출 등에 쓴다.</summary>
        public static Tween FadeTo(UnityEngine.UI.Graphic graphic, float alpha)
            => Tween.Alpha(graphic, alpha, P.storyFadeDuration, P.storyFadeEase, useUnscaledTime: true);

        /// <summary>전체 화면 가림막을 걷어 낸 뒤 비활성화한다.</summary>
        public static Tween FadeOutCurtain(CanvasGroup curtain)
            => Tween.Alpha(curtain, 0f, P.curtainFadeDuration, P.curtainFadeEase, useUnscaledTime: true)
                .OnComplete(curtain, c => c.gameObject.SetActive(false));

        /// <summary>다른 칸으로 단순하게 미끄러져 이동한다. 시차를 두어 웨이브가 웨이브답게 보이게 한다.</summary>
        public static void StepTo(Transform t, Vector3 worldTarget, int index = 0)
            => Tween.Position(t, worldTarget, P.dropDuration * 1.6f, Ease.InOutQuad,
                startDelay: index * P.spawnStagger);

        // ---------- 등장 ----------

        public static Sequence SpawnDrop(Transform t, Vector3 worldTarget, Transform boardRoot, int index = 0)
        {
            t.position = worldTarget + Vector3.up * P.spawnDropHeight;

            return Sequence.Create()
                .ChainDelay(index * P.spawnStagger)
                .Chain(Tween.Position(t, worldTarget, P.spawnDuration, P.spawnEase))
                .ChainCallback(() => ShakeBoard(boardRoot, 0.6f));
        }

        // ---------- 전역 ----------

        /// <summary>타격감이 필요한 순간에 시간을 잠깐 멈춘다. unscaled time을 쓰므로 항상 원래대로 돌아온다.</summary>
        public static void HitStop(float scale = 0.05f)
        {
            Time.timeScale = scale;
            Tween.Delay(P.hitStopDuration, () => Time.timeScale = 1f, useUnscaledTime: true);
        }

        /// <summary>프레임레이트와 무관한 추적이다. 커서를 따라오는 드래그 잔상에 쓴다.</summary>
        public static Vector3 SpringFollow(Vector3 current, Vector3 target, float deltaTime)
            => Vector3.Lerp(current, target, 1f - Mathf.Exp(-P.dragFollowSharpness * deltaTime));
    }
}
