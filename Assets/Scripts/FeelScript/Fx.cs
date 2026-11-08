using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Promotion.Feel
{
    public static class Fx  // 게임 코드와 트윈(애니메이션) 라이브러리(PrimeTween)를 잇는 유일한 연결 지점
    // -> 게임 코드는 PrimeTween을 직접 부르지 않고 이 파일의 함수만 부름 -> 나중에 라이브러리를 바꿔도 이 파일만 고치면 됨
    // 연출 수치(시간, 세기)는 전부 TweenPresets 에셋(P)에서 가져옴
    {
        static TweenPresets _presets;   // 불러온 연출 수치 에셋을 저장해 두는 변수 -> 한 번만 불러오고 재사용함

        public static TweenPresets P    // 연출 수치 에셋에 접근하는 속성 (Fx.P.hoverScale 처럼 사용)
        {
            get
            {
                if (_presets == null)   // 만약 아직 에셋을 불러오지 않았다면
                {
                    _presets = Resources.Load<TweenPresets>("TweenPresets");    // Resources 폴더에서 TweenPresets 에셋을 불러오고
                    if (_presets == null)   // 그래도 없다면 -> 에셋이 없어도 게임이 돌아가도록 기본값으로 만든 임시 에셋을 씀
                    {
                        _presets = ScriptableObject.CreateInstance<TweenPresets>();
                        Debug.LogWarning("[Fx] TweenPresets asset not found in Resources — using defaults.");
                    }
                }
                return _presets;
            }
        }

        static readonly Dictionary<Transform, Tween> ScaleTweens = new();   // 오브젝트마다 현재 실행 중인 크기 트윈을 기억하는 목록

        static void ReplaceScaleTween(Transform t, Tween tween) // 오브젝트의 크기 트윈을 새 것으로 교체하는 함수
        // -> 마우스를 빠르게 올렸다 내렸다 해도 크기 트윈이 겹쳐 쌓이지 않고 항상 하나만 돌게 함
        {
            if (ScaleTweens.TryGetValue(t, out var running) && running.isAlive)    // 만약 이 오브젝트에 아직 돌고 있는 크기 트윈이 있다면
                running.Stop(); // 그 트윈을 멈추고
            ScaleTweens[t] = tween; // 새 트윈을 기억함
        }

        // ---------- 타이틀 화면 연출 ----------
        public static void TileIn(Transform tile, UnityEngine.UI.Graphic graphic, float targetAlpha, float delay)
        // 타이틀 배경의 체스판 칸 하나를 나타나게 하는 함수
        // -> 화면에서는 칸이 반쯤 작은 크기 + 투명한 상태에서 톡 튀어나오듯 커지며 서서히 진해짐 (delay를 칸마다 다르게 줘서 물결처럼 깔림)
        {
            tile.localScale = Vector3.one * P.titleTileFromScale;   // 시작 크기를 작게 맞추고
            var c = graphic.color;
            graphic.color = new Color(c.r, c.g, c.b, 0f);   // 시작 투명도를 0(완전 투명)으로 맞춤

            Tween.Scale(tile, P.titleTileFromScale, 1f, P.titleTileDuration, Ease.OutBack,
                startDelay: delay, useUnscaledTime: true);  // 작은 크기 -> 원래 크기로 살짝 지나쳤다 돌아오며 커짐
            Tween.Alpha(graphic, 0f, targetAlpha, P.titleTileDuration, Ease.OutQuad,
                startDelay: delay, useUnscaledTime: true);  // 동시에 투명 -> 목표 투명도까지 서서히 진해짐
            // useUnscaledTime: true -> 게임 시간이 멈춰 있어도(timeScale 0) 연출이 재생됨
        }

        public static Tween DropOnto(RectTransform target, float delay, Action onLand)
        // UI 오브젝트(킹 로고)를 화면 위쪽에서 원래 자리로 떨어뜨리는 함수 -> 착지하는 순간 onLand를 호출함
        {
            float home = target.anchoredPosition.y; // 원래 있어야 할 높이를 기억하고
            var p = target.anchoredPosition;
            target.anchoredPosition = new Vector2(p.x, home + P.titleLogoDropHeight);  // 그보다 위쪽(화면 밖)으로 옮겨 둠

            return Tween.UIAnchoredPositionY(target, home + P.titleLogoDropHeight, home, P.titleLogoDropDuration,
                    Ease.InQuad, startDelay: delay, useUnscaledTime: true)  // 위에서 원래 자리까지 점점 빨라지며 떨어짐
                .OnComplete(onLand);    // 다 떨어지면 착지 처리(흔들림 등)를 실행
        }

        public static void TitleImpact(Transform landed, Transform shakeRoot)   // 킹 로고가 착지했을 때의 충격 연출 함수
        {
            Tween.PunchScale(landed, new ShakeSettings(P.titleLandSquash, P.titleShakeDuration, 8f,
                useUnscaledTime: true));    // 로고가 옆으로 퍼지고 위아래로 눌렸다가 말랑하게 원래 모양으로 돌아옴
            Tween.ShakeLocalPosition(shakeRoot, new ShakeSettings(P.titleShakeStrength, P.titleShakeDuration,
                P.titleShakeFrequency, useUnscaledTime: true)); // 동시에 타이틀 화면 전체가 '쿵' 하고 흔들림
        }

        public static Tween LettersRise(TMP_Text label, float delay)
        // 제목 글자를 한 글자씩 차례로 아래에서 솟아오르게 하는 함수
        // -> 화면에서는 앞 글자부터 하나씩 아래쪽에서 투명하게 올라오며 살짝 튀었다가 제자리에 멈춤
        // -> 글자 오브젝트를 따로 만들지 않고 TextMeshPro 글자의 꼭짓점(정점)을 직접 움직여 구현함
        {
            label.ForceMeshUpdate();    // 글자 모양 정보를 최신 상태로 만들고
            var info = label.textInfo;  // 글자별 위치, 색 정보를 가져옴
            int count = info.characterCount;    // 전체 글자 수

            var original = new Vector3[info.meshInfo.Length][];    // 글자 꼭짓점의 원래 위치를 보관할 배열
            var alpha = new byte[info.meshInfo.Length][];   // 글자 꼭짓점의 원래 투명도를 보관할 배열
            for (int m = 0; m < info.meshInfo.Length; m++)  // 글자 메쉬(재질)마다
            {
                original[m] = (Vector3[])info.meshInfo[m].vertices.Clone(); // 원래 꼭짓점 위치를 복사해 두고
                var colors = info.meshInfo[m].colors32;
                alpha[m] = new byte[colors.Length];
                for (int v = 0; v < colors.Length; v++) // 원래 투명도도 복사해 둠 -> 매 프레임 원래 값 기준으로 계산하기 위해
                    alpha[m][v] = colors[v].a;
            }

            float stagger = P.titleLetterStagger;   // 글자마다의 시차
            float each = P.titleLetterDuration; // 글자 하나가 올라오는 시간
            float rise = P.titleLetterRise; // 올라오는 거리(픽셀)
            float total = Mathf.Max(0, count - 1) * stagger + each; // 마지막 글자까지 다 올라오는 데 걸리는 전체 시간

            void Apply(float elapsed)   // 경과 시간에 맞춰 모든 글자의 위치와 투명도를 계산해 적용하는 내부 함수
            {
                if (label == null)  // 만약 그 사이 글자 오브젝트가 삭제됐다면 아무것도 하지 않음
                    return;

                for (int i = 0; i < count; i++) // 글자 하나씩
                {
                    var ch = info.characterInfo[i];
                    if (!ch.isVisible)  // 띄어쓰기처럼 보이지 않는 글자는 건너뜀
                        continue;

                    float t = Mathf.Clamp01((elapsed - i * stagger) / each);    // 이 글자의 진행도(0~1) -> 뒤 글자일수록 늦게 시작함
                    float eased = EaseOutBack(t);   // 진행도에 '살짝 지나쳤다 돌아오는' 곡선을 적용
                    float shown = Mathf.Clamp01(t * 2f);    // 투명도는 진행도의 절반 시점에 이미 다 보이도록 빠르게 올림

                    int m = ch.materialReferenceIndex;  // 이 글자가 속한 메쉬 번호
                    int vi = ch.vertexIndex;    // 이 글자의 첫 꼭짓점 번호
                    var verts = info.meshInfo[m].vertices;
                    var colors = info.meshInfo[m].colors32;
                    for (int k = 0; k < 4; k++) // 글자 하나는 사각형(꼭짓점 4개)으로 그려지므로 4개를 모두 옮김
                    {
                        verts[vi + k] = original[m][vi + k] + new Vector3(0f, -(1f - eased) * rise, 0f);    // 처음엔 rise만큼 아래, 끝나면 원래 자리
                        colors[vi + k].a = (byte)(alpha[m][vi + k] * shown);    // 투명 -> 원래 진하기로
                    }
                }

                label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);   // 바꾼 위치와 색을 화면에 반영
            }

            Apply(0f);  // 시작 전에 한 번 적용해 글자들을 숨겨 둠 -> 대기 시간 동안 글자가 미리 보이지 않게 함
            return Tween.Custom(0f, total, total, Apply, Ease.Linear, startDelay: delay, useUnscaledTime: true)    // 0 ~ total 시간 동안 매 프레임 Apply를 호출
                .OnComplete(() =>
                {
                    if (label != null)  // 끝나면 글자 메쉬를 원래 상태로 다시 만들어 정확한 최종 모습을 보장함
                        label.ForceMeshUpdate();
                });
        }

        static float EaseOutBack(float t)   // 0~1 진행도를 '목표를 살짝 지나쳤다가 돌아오는' 곡선 값으로 바꾸는 함수 (Ease.OutBack과 같은 공식)
        {
            const float c1 = 1.70158f;  // 지나치는 정도를 정하는 표준 상수
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static Tween FadeInGroup(CanvasGroup group, float delay) // UI 묶음(크레딧 등)을 투명한 상태에서 서서히 나타나게 하는 함수
        {
            group.alpha = 0f;   // 먼저 완전히 투명하게 만들고
            return Tween.Alpha(group, 0f, 1f, P.titleCreditFade, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);    // 대기 후 서서히 보이게 함
        }

        public static Tween IdleBreathe(Transform t)    // 타이틀 연출이 끝난 뒤 킹 로고가 계속 천천히 커졌다 작아지는(숨 쉬는) 함수
        {
            t.localScale = Vector3.one; // 원래 크기에서 시작해
            return Tween.Scale(t, P.titleLogoBreatheScale, P.titleLogoBreatheDuration, Ease.InOutSine,
                -1, CycleMode.Yoyo, useUnscaledTime: true); // -1 = 무한 반복, Yoyo = 커졌다가 다시 원래대로 왕복
        }

        // ---------- 공통 ----------
        public static void CompleteAll()    // 실행 중인 모든 트윈을 즉시 끝 상태로 만드는 함수 (테스트/검증용)
        {
            Tween.CompleteAll();
        }

        // ---------- 호버 ----------
        public static void HoverIn(Transform t, float baseScale = 1f)   // 마우스를 기물 위에 올렸을 때 살짝 커지게 하는 함수
        {
            ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.hoverScale, P.hoverDuration, P.hoverEase));
        }

        public static void HoverOut(Transform t, float baseScale = 1f)  // 마우스가 기물에서 벗어났을 때 원래 크기로 돌아가게 하는 함수
        {
            ReplaceScaleTween(t, Tween.Scale(t, baseScale, P.hoverDuration, P.hoverEase));
        }

        // ---------- 드래그 ----------
        public static void PickUp(Transform t, float baseScale = 1f)    // 기물을 집어 들었을 때의 연출 함수
        {
            ReplaceScaleTween(t, Tween.Scale(t, baseScale * P.dragScale, P.dragLiftDuration, Ease.OutBack));    // 기물이 톡 튀며 커지고 -> 손에 들린 느낌
            Tween.LocalRotation(t, Quaternion.Euler(0f, 0f, P.dragTiltDegrees), P.dragLiftDuration, Ease.OutQuad); // 동시에 살짝 기울어짐
        }

        public static Sequence Drop(Transform piece, Vector3 worldTarget, Transform boardRoot, float baseScale = 1f)
        // 기물을 칸에 내려놓는 연출 함수
        // -> 화면에서는 기물이 칸으로 내려앉으며 원래 크기로 돌아오고, 착지 순간 통 튀었다 돌아오며, 체스판이 살짝 흔들림
        {
            if (piece.localRotation != Quaternion.identity) // 만약 기물이 기울어져 있다면 바로 세움
                Tween.LocalRotation(piece, Quaternion.identity, P.dropDuration, Ease.OutQuad);
            // -> 이미 똑바른 기물에 같은 값으로 트윈하면 헛수고이고 PrimeTween이 경고를 띄우므로 조건을 검사함

            var seq = Sequence.Create() // 여러 트윈을 순서대로 묶은 시퀀스를 만듦
                .Group(Tween.LocalPosition(piece, ToLocal(piece, worldTarget), P.dropDuration, P.dropEase))  // 목표 칸으로 이동하면서 (보드 기준 위치로 -> 보드가 흔들려도 칸에 정확히 내려앉음)
                .Group(Tween.Scale(piece, baseScale, P.dropDuration, P.dropEase))   // 동시에(Group) 원래 크기로 줄어들고
                .ChainCallback(() => Sfx.Play(SfxId.Move))  // 착지하는 순간 '탁' 소리
                .Chain(Tween.PunchScale(piece, new ShakeSettings(
                    P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency)));  // 도착한 뒤(Chain) 통 튀는 착지 효과

            if (boardRoot != null)  // 체스판이 전달됐다면 판도 흔듦
                ShakeBoard(boardRoot);

            return seq;
        }

        public static void ShakeBoard(Transform boardRoot, float intensity = 1f)    // 체스판 전체를 짧게 흔드는 함수 -> intensity로 세기 배율 조절
        {
            Tween.CompleteAll(boardRoot);   // 앞의 흔들림이 아직 남아 있다면 끝 상태(제자리)로 마무리한 뒤 새로 흔듦
            // -> 흔들림 도중 새 흔들림이 시작되면 어긋난 위치를 '제자리'로 착각해 판이 조금씩 밀리는 문제를 막음
            Tween.ShakeLocalPosition(boardRoot, new ShakeSettings(
                P.boardShakeStrength * intensity, P.boardShakeDuration, P.boardShakeFrequency));
        }

        static Vector3 ToLocal(Transform t, Vector3 world)
        // 월드 위치를 기물 부모(보드의 Pieces) 기준 위치로 바꾸는 함수
        // -> 기물 이동을 보드 기준으로 해야, 착지 때 보드가 흔들리는 중에 이동이 끝나도 기물이 칸에서 어긋나지 않음
        {
            return t.parent != null ? t.parent.InverseTransformPoint(world) : world;
        }

        // ---------- 하이라이트 ----------
        public static void PopIn(Transform t, int index = 0, float targetScale = 1f)
        // 대상(이동 가능 칸 표시 등)을 톡 튀어나오듯 등장시키는 함수 -> index만큼 시차를 둬 여러 개가 물결처럼 퍼짐
        {
            t.localScale = Vector3.one * (targetScale * PopInStartScale);   // 작은 크기에서 시작하되 절대 0에서 시작하지 않음
            // -> 에디터 프레임 정체, timeScale 0, 재컴파일 등으로 트윈이 돌지 않아도 오브젝트가 영영 안 보이는 상태로 남지 않게 함
            // -> 연출은 실패해도 되지만 연출이 보여 주는 게임 상태는 실패하면 안 됨
            Tween.Scale(t, targetScale, P.highlightPopDuration, P.highlightEase,
                startDelay: index * P.highlightStagger);    // 순번만큼 늦게 시작해 원래 크기로 튀어나옴
        }

        const float PopInStartScale = 0.6f; // PopIn이 시작하는 크기 비율 (목표 크기의 60%)

        // ---------- 전투 ----------
        public static Sequence Lunge(Transform attacker, Vector3 towardWorld, Action onImpact = null, Action onDone = null)
        // 공격자가 대상 쪽으로 돌진했다가 제자리로 돌아오는 연출 함수 -> 가장 앞으로 나간 순간 onImpact(타격 처리)를, 제자리에 돌아오면 onDone을 호출함
        {
            var origin = attacker.localPosition; // 출발 위치를 보드 기준으로 기억하고
            var dir = (towardWorld - attacker.position).normalized;    // 대상 쪽 방향을 구한 뒤
            var peak = ToLocal(attacker, attacker.position + dir * P.lungeDistance);  // 그 방향으로 돌진 거리만큼 나간 지점을 계산

            return Sequence.Create()
                .Chain(Tween.LocalPosition(attacker, peak, P.lungeDuration, Ease.OutQuad))   // 앞으로 빠르게 튀어나간 뒤
                .ChainCallback(() => onImpact?.Invoke())    // 타격 처리를 하고
                .Chain(Tween.LocalPosition(attacker, origin, P.lungeDuration * 1.4f, Ease.OutBack)) // 조금 더 천천히 제자리로 돌아온 뒤
                .ChainCallback(() => onDone?.Invoke());    // 잡기 동작이 모두 끝났음을 알림
        }

        public static Sequence CaptureAdvance(Transform attacker, Vector3 victimWorld, Vector3 destination,
            Transform boardRoot, float baseScale, Action onImpact = null, Action onLanded = null)
        // 잡기와 전진을 하나의 동작으로 처리하는 연출 함수 -> 돌진해서 상대 기물을 잡고 그대로 그 칸에 자리 잡음
        // -> 부딪히는 순간 onImpact(타격 처리)를, 잡은 칸에 내려앉는 순간 onLanded(잡기 동작 끝)를 호출함
        // -> 하나의 시퀀스로 묶은 이유: 한 오브젝트에 위치 트윈 두 개가 걸리면 서로 충돌해 공격자가 엉뚱한 자리에 멈추는 문제가 있었음
        {
            var dir = (victimWorld - attacker.position).normalized; // 잡힐 기물 쪽 방향을 구하고
            var peak = ToLocal(attacker, attacker.position + dir * P.lungeDistance);   // 돌진 지점을 계산 (보드 기준)
            var landing = ToLocal(attacker, destination);   // 잡은 칸 (보드 기준)

            if (attacker.localRotation != Quaternion.identity)  // 드래그로 기울어져 있다면 바로 세움
                Tween.LocalRotation(attacker, Quaternion.identity, P.lungeDuration, Ease.OutQuad);

            return Sequence.Create()
                .Chain(Tween.LocalPosition(attacker, peak, P.lungeDuration, Ease.OutQuad))   // 1. 상대 쪽으로 돌진하고
                .ChainCallback(() => onImpact?.Invoke())    // 2. 부딪히는 순간 잡기 처리(상대 기물 제거 등)를 한 뒤
                .Chain(Tween.LocalPosition(attacker, landing, P.dropDuration, P.dropEase))   // 3. 잡은 칸으로 내려앉으면서
                .Group(Tween.Scale(attacker, baseScale, P.dropDuration, P.dropEase))    //    동시에 원래 크기로 돌아오고
                .ChainCallback(() =>    // 4. 착지하는 순간
                {
                    Tween.PunchScale(attacker, new ShakeSettings(
                        P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency));   // 통 튀는 착지 효과와
                    if (boardRoot != null)
                        ShakeBoard(boardRoot);  // 체스판 흔들림을 줌
                    onLanded?.Invoke(); // 잡기 동작이 모두 끝났음을 알림
                });
        }

        public static Sequence CastleSwap(Transform first, Vector3 firstTarget, float firstScale,
            Transform second, Vector3 secondTarget, float secondScale, Transform boardRoot, Action onLanded)
        // 캐슬링 연출 함수 -> 두 기물이 동시에 살짝 커지며 서로의 칸으로 휙 건너간 뒤 '쾅' 착지함
        // -> 착지하는 순간 onLanded(이펙트·글자·소리)를 호출함
        {
            float half = P.castleSwapDuration * 0.5f;
            if (first.localRotation != Quaternion.identity) // 드래그로 기울어진 기물을 바로 세움 (CaptureAdvance와 같은 방식)
                Tween.LocalRotation(first, Quaternion.identity, half, Ease.OutQuad);
            return Sequence.Create()
                .Group(Tween.LocalPosition(first, ToLocal(first, firstTarget), P.castleSwapDuration, Ease.InOutCubic))
                .Group(Tween.LocalPosition(second, ToLocal(second, secondTarget), P.castleSwapDuration, Ease.InOutCubic))
                .Group(Sequence.Create(Tween.Scale(first, firstScale * P.castleHopScale, half, Ease.OutQuad))  // 건너가는 앞 절반 동안 커졌다가
                    .Chain(Tween.Scale(first, firstScale, half, Ease.InQuad)))    // 뒤 절반 동안 원래 크기로 돌아오며 착지
                .Group(Sequence.Create(Tween.Scale(second, secondScale * P.castleHopScale, half, Ease.OutQuad))
                    .Chain(Tween.Scale(second, secondScale, half, Ease.InQuad)))
                .ChainCallback(() =>    // 착지하는 순간
                {
                    Tween.PunchScale(first, new ShakeSettings(P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency));
                    Tween.PunchScale(second, new ShakeSettings(P.dropPunchStrength, P.dropPunchDuration, P.dropPunchFrequency));
                    if (boardRoot != null)
                        ShakeBoard(boardRoot, P.castleShake);   // 일반 착지보다 세게 흔들고
                    onLanded?.Invoke();
                });
        }

        public static void HitFlash(SpriteRenderer sr, Color baseColor) // 맞은 기물이 빨갛게 번쩍이며 부르르 떨리는 연출 함수
        {
            Tween.Color(sr, P.hitFlashColor, P.hitFlashDuration, Ease.OutQuad, cycles: 2,
                cycleMode: CycleMode.Yoyo); // 빨간색으로 바뀌었다가 원래 색으로 돌아옴 (2번 = 갔다 오기 한 번)
            Tween.ShakeLocalPosition(sr.transform, new ShakeSettings(
                P.hitShakeStrength, P.hitShakeDuration));   // 동시에 제자리에서 떨림
        }

        public static Sequence Die(Transform t, Action onComplete = null)
        // 잡힌 기물이 빙글 돌면서 작아져 사라지는 연출 함수 -> 다 사라지면 onComplete(오브젝트 삭제 등)를 호출함
        {
            return Sequence.Create()
                .Group(Tween.Scale(t, 0f, P.deathDuration, Ease.InBack))    // 살짝 커졌다가 0까지 줄어들고
                .Group(Tween.LocalRotation(t, Quaternion.Euler(0f, 0f, P.deathSpinDegrees), P.deathDuration, Ease.InQuad))  // 동시에 회전함
                .ChainCallback(() => onComplete?.Invoke());
        }

        // ---------- 누적 킬 불꽃 ----------
        public static void FireIgnite(Transform piece, Transform boardRoot, float baseScale, Action onIgnite)
        // 기물에 불이 붙는 순간의 연출 함수 -> 잡기 연출이 끝날 때까지 잠깐 기다렸다가 onIgnite(불꽃 입자 켜기)를 호출함
        // -> 화면에서는 불티가 사방으로 튀면서 기물이 '화르륵' 커졌다 돌아오고 체스판이 약하게 흔들림
        {
            Tween.Delay(P.fireIgniteDelay, () => IgniteWhenIdle(piece, boardRoot, baseScale, onIgnite, 0));
        }

        public static bool IsAnimating(Transform t)  // 그 오브젝트에 아직 재생 중인 연출이 있는지 확인하는 함수 (연출이 끝난 뒤에 할 일을 기다릴 때 사용)
        {
            return t != null && Tween.GetTweensCount(t) > 0;
        }

        static void IgniteWhenIdle(Transform piece, Transform boardRoot, float baseScale, Action onIgnite, int tries)
        // 기물에 걸린 연출(돌진·착지 튕김)이 모두 끝났을 때 점화하는 함수 -> 아직 움직이는 중이면 잠깐 뒤에 다시 확인함
        // -> 크기 트윈 두 개가 한 기물에 겹치면 기물 크기가 틀어진 채로 남으므로, 끝나길 기다렸다가 점화 펀치를 줌
        {
            if (piece == null)  // 기다리는 사이 기물이 잡혀 사라졌다면 취소
                return;

            if (Tween.GetTweensCount(piece) > 0 && tries < FireIgniteMaxRetries)    // 아직 연출 중이라면 조금 뒤에 다시 확인
            {
                Tween.Delay(FireIgniteRetryInterval, () => IgniteWhenIdle(piece, boardRoot, baseScale, onIgnite, tries + 1));
                return;
            }

            if (Tween.GetTweensCount(piece) == 0)   // 끝까지 기다려도 연출이 남아 있다면 펀치는 생략하고 불만 붙임
            {
                piece.localScale = Vector3.one * baseScale;
                Tween.PunchScale(piece, new ShakeSettings(P.fireIgnitePunch, P.fireIgnitePunchDuration, 10f));
            }
            if (boardRoot != null)
                ShakeBoard(boardRoot, P.fireIgniteShake);
            onIgnite?.Invoke();
        }

        const float FireIgniteRetryInterval = 0.05f;    // 점화를 다시 확인하는 간격(초)
        const int FireIgniteMaxRetries = 20;    // 최대 재확인 횟수 (약 1초)

        public static void FireGlow(SpriteRenderer body)
        // 불붙은 기물의 몸체가 주황빛으로 천천히 물들었다 돌아오기를 반복하는 함수 -> FireGlowStop을 부를 때까지 계속됨
        {
            Tween.StopAll(body);
            Tween.Color(body, P.fireGlowColor, P.fireGlowPeriod, Ease.InOutSine, -1, CycleMode.Yoyo);
        }

        public static void FireGlowStop(SpriteRenderer body, Color baseColor)  // 몸체 깜빡임을 멈추고 원래 색으로 부드럽게 되돌리는 함수
        {
            Tween.StopAll(body);
            if (body.color != baseColor)    // 이미 원래 색이면 같은 값으로 트윈하지 않음 (PrimeTween 경고 방지)
                Tween.Color(body, baseColor, P.fireGlowFadeOut, Ease.OutQuad);
        }

        // ---------- 처치 배너 ----------
        static Sequence _callout;   // 지금 재생 중인 처치 배너 연출 -> 새 처치가 나오면 멈추고 처음부터 다시 재생

        public static void CalloutBanner<T>(RectTransform body, CanvasGroup group, float homeY, float scale, T owner, Action<T> onHidden)
            where T : class
        // 처치 배너를 화면 가운데 아래쪽에서 상단 자리까지 '치고 올라오게' 띄웠다가 사라지게 하는 함수
        // -> 화면에서는 배너가 살짝 크고 투명한 상태로 아래에서 튀어 올라와 자리에 '탁' 박히고, 잠깐 머문 뒤 위로 살짝 떠오르며 사라짐
        // homeY: 배너가 자리 잡는 높이 / scale: 배너 크기 배율 (불붙은 기물이면 조금 더 크게)
        // owner·onHidden: 다 사라졌을 때 owner로 호출할 함수(배너 끄기) -> owner가 씬 전환으로 사라졌다면 호출하지 않음
        // -> 게임 시간이 멈춰 있어도(기물 가이드, 히트 스톱) 배너는 제 속도로 재생되도록 실제 시간을 씀
        {
            if (_callout.isAlive)   // 앞 배너가 아직 떠 있다면 쌓지 않고 멈춘 뒤 새 배너로 다시 시작
                _callout.Stop();

            var p = body.anchoredPosition;
            body.anchoredPosition = new Vector2(p.x, homeY - P.calloutEnterRise);  // 자리보다 아래에서 시작하고
            body.localScale = Vector3.one * (scale * P.calloutPopScale);    // 살짝 크게 시작 (0에서 시작하지 않음)
            group.alpha = CalloutStartAlpha;    // 완전 투명에서 시작하지 않음 -> 연출이 끊겨도 배너는 보임

            _callout = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPositionY(body, homeY, P.calloutPopDuration, Ease.OutBack, useUnscaledTime: true))    // 위로 치고 올라와 자리에 박히면서
                .Group(Tween.Scale(body, scale, P.calloutPopDuration, Ease.OutBack, useUnscaledTime: true)) // 원래 크기로 줄어들고
                .Group(Tween.Alpha(group, 1f, P.calloutPopDuration * 0.5f, Ease.OutQuad, useUnscaledTime: true))  // 빠르게 진해짐
                .ChainDelay(P.calloutHold)  // 잠깐 그대로 보여 준 뒤
                .Chain(Tween.UIAnchoredPositionY(body, homeY + P.calloutExitRise, P.calloutFadeDuration, Ease.InQuad, useUnscaledTime: true))  // 위로 살짝 떠오르면서
                .Group(Tween.Alpha(group, 0f, P.calloutFadeDuration, Ease.InQuad, useUnscaledTime: true))    // 동시에 투명해짐
                .ChainCallback(owner, onHidden, warnIfTargetDestroyed: false);
        }

        const float CalloutStartAlpha = 0.3f;   // 배너가 나타나기 시작하는 투명도

        // ---------- 최종전 AP 잠김 ----------

        public static void ApLock(RectTransform panel, UnityEngine.UI.Image panelImage, Color lockedColor, Action onLock)

        // 최종전에서 AP 패널이 잠기는 연출 함수 -> 체스판이 드러나고 잠깐 뒤 '철컥' 하고 잠김

        // -> 화면에서는 패널이 붉게 번쩍이며 툭 커졌다 돌아오고 좌우로 덜컹 흔들린 뒤 어두운 붉은색으로 가라앉음

        // onLock: 잠기는 순간 호출 (글자를 "AP 사용 불가"로 바꾸고 소리를 냄)

        // -> 대화가 끝난 직후라 게임 시간이 멈춰 있을 수도 있으므로 실제 시간으로 재생

        {

            Tween.StopAll(panel);

            Tween.StopAll(panelImage);

            Sequence.Create(useUnscaledTime: true)

                .ChainDelay(P.apLockDelay)  // 체스판이 드러난 것을 잠깐 보여 준 뒤

                .ChainCallback(() => onLock?.Invoke())  // 잠김 (글자 교체 + 소리)

                .Chain(Tween.Color(panelImage, P.apLockFlashColor, lockedColor, P.apLockDuration, Ease.OutQuad, useUnscaledTime: true))    // 밝은 붉은색으로 번쩍였다가 어두운 붉은색으로 가라앉으면서

                .Group(Tween.PunchScale(panel, new ShakeSettings(P.apLockPunch, P.apLockDuration, 10f, useUnscaledTime: true))) // 툭 커졌다 돌아오고

                .Group(Tween.ShakeLocalPosition(panel, new ShakeSettings(P.apLockShake, P.apLockDuration, 18f, useUnscaledTime: true)));  // 좌우로 덜컹 흔들림

        }



        // ---------- 잡기 막힘 (중갑병) ----------
        public static void Blocked(Transform piece)
        // 잡기를 막아 낸 기물이 제자리에서 '팅' 하고 좌우로 흔들리는 함수 (위치는 흔들림이 끝나면 원래대로)
        {
            Tween.ShakeLocalPosition(piece, new ShakeSettings(P.blockedShake, P.blockedShakeDuration, 22f));
        }

        public static void PopupRise<T>(Transform t, T owner, Action<T, float> setAlpha, Action<T> onDone) where T : class
        // 보드 위에 잠깐 뜨는 글자(예: "막힘!")를 '톡' 튀어나오게 했다가 위로 떠오르며 사라지게 하는 함수
        // owner·setAlpha: 글자·배경 투명도를 바꾸는 대상과 함수 / onDone: 다 사라졌을 때 호출(오브젝트 지우기)
        {
            var y = t.localPosition.y;
            t.localScale = Vector3.one * BubbleStartScale;   // 조금 작게 시작 (0에서 시작하지 않음)
            setAlpha(owner, 1f);
            Sequence.Create()
                .Chain(Tween.Scale(t, 1f, P.blockedPopIn, Ease.OutBack))   // 톡 튀어나오고
                .ChainDelay(P.blockedHold)  // 잠깐 보여 준 뒤
                .Chain(Tween.LocalPositionY(t, y + P.blockedRise, P.blockedFade, Ease.OutQuad))   // 위로 떠오르면서
                .Group(Tween.Custom(owner, 1f, 0f, P.blockedFade, setAlpha, Ease.InQuad))   // 투명해짐
                .ChainCallback(owner, onDone, warnIfTargetDestroyed: false);
        }

        // ---------- 보스 말풍선 ----------
        public static void BubblePop(RectTransform bubble, CanvasGroup group)
        // 보스 말풍선이 '톡' 튀어나오듯 나타나는 함수 -> 이미 떠 있던 말풍선이라도 처음부터 다시 튀어나옴
        // -> 게임 시간이 멈춰 있어도(히트 스톱) 제 속도로 재생되도록 실제 시간을 씀
        {
            Tween.StopAll(bubble);
            Tween.StopAll(group);
            bubble.localScale = Vector3.one * BubbleStartScale;    // 조금 작게 시작해서 (0에서 시작하지 않음)
            group.alpha = BubbleStartAlpha;
            Tween.Scale(bubble, 1f, P.uiPanelIn, Ease.OutBack, useUnscaledTime: true);
            Tween.Alpha(group, 1f, P.uiPanelIn * 0.5f, Ease.OutQuad, useUnscaledTime: true);
        }

        public static void BubbleHide(RectTransform bubble, CanvasGroup group)  // 보스 말풍선이 서서히 사라진 뒤 꺼지는 함수
        {
            Tween.StopAll(group);
            Tween.Alpha(group, 0f, P.uiPanelOut, Ease.InQuad, useUnscaledTime: true)
                .OnComplete(bubble, b => b.gameObject.SetActive(false), warnIfTargetDestroyed: false);
        }

        const float BubbleStartScale = 0.8f;    // 말풍선이 나타나기 시작하는 크기 비율
        const float BubbleStartAlpha = 0.3f;    // 말풍선이 나타나기 시작하는 투명도

        // ---------- 맵 / 스토리 ----------
        public static Tween Breathe(Transform t, float baseScale = 1f)
        // 맵 노드처럼 플레이어가 다음에 고를 수 있는 대상이 천천히 커졌다 작아지게(숨 쉬게) 하는 함수
        // -> 멈추거나 오브젝트가 파괴될 때까지 계속 반복됨
        {
            t.localScale = Vector3.one * baseScale; // 원래 크기에서 시작해
            return Tween.Scale(t, baseScale * P.mapPulseScale, P.mapPulseDuration, P.mapPulseEase,
                -1, CycleMode.Yoyo);    // 무한 반복(-1)으로 커졌다 원래대로 왕복(Yoyo)
        }

        public static Tween FadeIn(UnityEngine.UI.Graphic graphic)  // 그림/글을 옅은 상태에서 서서히 선명하게 나타나게 하는 함수
        {
            return Tween.Alpha(graphic, StoryFadeStartAlpha, 1f, P.storyFadeDuration, P.storyFadeEase,
                useUnscaledTime: true);
        }

        const float StoryFadeStartAlpha = 0.15f;    // FadeIn이 시작하는 투명도 -> 0이 아니라 옅은 상태에서 시작해 트윈이 취소돼도 그림이 화면에 남음

        public static Tween FadeTo(UnityEngine.UI.Graphic graphic, float alpha) // 그래픽의 투명도를 지정한 값까지 부드럽게 바꾸는 함수 (배경 그늘이 짙어지는 연출 등)
        {
            return Tween.Alpha(graphic, alpha, P.storyFadeDuration, P.storyFadeEase, useUnscaledTime: true);
        }

        public static Tween FadeOutCurtain(CanvasGroup curtain) // 화면 전체를 가린 막을 서서히 걷어 낸 뒤 비활성화하는 함수
        {
            return Tween.Alpha(curtain, 0f, P.curtainFadeDuration, P.curtainFadeEase, useUnscaledTime: true)
                .OnComplete(curtain, c => c.gameObject.SetActive(false));   // 완전히 투명해지면 막 오브젝트를 꺼서 클릭을 가로막지 않게 함
        }

        // ---------- 등장 / 이동 ----------
        public static void StepTo(Transform t, Vector3 worldTarget, int index = 0)
        // 기물을 다른 칸으로 부드럽게 미끄러지듯 이동시키는 함수 (적이 한 칸씩 다가올 때 사용)
        // -> index만큼 시차를 둬서 적 무리가 한꺼번에가 아니라 차례로 걸어오는 것처럼 보이게 함
        {
            Tween.LocalPosition(t, ToLocal(t, worldTarget), P.dropDuration * 1.6f, Ease.InOutQuad,
                startDelay: index * P.spawnStagger)
                .OnComplete(t, _ => Sfx.Play(SfxId.Move), warnIfTargetDestroyed: false);   // 칸에 도착하는 순간 '탁' 소리 -> 시차 덕분에 적들이 차례로 '탁탁탁' 내려앉음
        }

        public static void StepTo(Transform t, Vector3 fromWorld, Vector3 worldTarget, int index, int stepIndex)
        // 한 턴에 여러 칸 걷는 적(중갑병)용 이동 함수 -> stepIndex번째 걸음은 앞 걸음들이 끝난 뒤 fromWorld 칸에서 이어서 출발
        {
            if (stepIndex <= 0) // 첫 걸음은 기존 이동과 같음
            {
                StepTo(t, worldTarget, index);
                return;
            }

            float duration = P.dropDuration * 1.6f;
            Tween.LocalPosition(t, ToLocal(t, fromWorld), ToLocal(t, worldTarget), duration, Ease.InOutQuad,
                startDelay: index * P.spawnStagger + stepIndex * duration)   // 출발 칸을 직접 지정 -> 앞 걸음이 끝난 자리에서 매끄럽게 이어짐
                .OnComplete(t, _ => Sfx.Play(SfxId.Move), warnIfTargetDestroyed: false);
        }

        public static Sequence SpawnDrop(Transform t, Vector3 worldTarget, Transform boardRoot, int index = 0)
        // 적이 위에서 칸으로 떨어지며 등장하는 연출 함수
        {
            var landing = ToLocal(t, worldTarget);  // 착지할 칸 (보드 기준)
            t.position = worldTarget + Vector3.up * P.spawnDropHeight;  // 목표 칸보다 위쪽에 먼저 놓고

            return Sequence.Create()
                .ChainDelay(index * P.spawnStagger) // 순번만큼 기다렸다가 -> 여러 적이 차례로 떨어짐
                .Chain(Tween.LocalPosition(t, landing, P.spawnDuration, P.spawnEase))    // 점점 빨라지며 칸으로 떨어지고
                .ChainCallback(() =>
                {
                    ShakeBoard(boardRoot, 0.6f);    // 착지 순간 체스판을 약하게(60%) 흔들고
                    Sfx.Play(SfxId.Move);   // '탁' 소리
                });
        }

        // ---------- 기물 가이드 ----------
        public static void GuideOpen(Transform panel, CanvasGroup group)
        // 기물 가이드 창이 살짝 작은 크기에서 커지며 서서히 나타나는 함수
        // -> 가이드가 떠 있는 동안 게임 시간이 멈춰 있으므로(timeScale 0) 실제 시간(unscaled)으로 재생함
        {
            panel.localScale = Vector3.one * P.guideOpenScale;  // 0이 아니라 90% 크기에서 시작 -> 트윈이 멈춰도 창은 보임
            group.alpha = 0.35f;    // 완전히 투명하게 시작하지 않음 (같은 이유)
            Tween.Scale(panel, 1f, P.guideOpenDuration, Ease.OutBack, useUnscaledTime: true);
            Tween.Alpha(group, 1f, P.guideOpenDuration, Ease.OutQuad, useUnscaledTime: true);
        }

        public static void GuidePage(TMP_Text body)    // 가이드의 페이지를 넘겼을 때 본문 글이 살짝 떠오르며 나타나는 함수
        {
            body.alpha = 0.35f;
            Tween.Custom(body, 0.35f, 1f, P.guideOpenDuration, (t, a) => t.alpha = a, Ease.OutQuad, useUnscaledTime: true);
        }

        // ---------- 특수 기물 해금·룰렛 (타이틀) ----------
        public static void FairyUnlock(Transform button, Transform lockIcon, UnityEngine.UI.Graphic lockGraphic, Action onDone)
        // 잠겨 있던 특수 기물 버튼의 자물쇠가 풀리는 연출 함수 -> 자물쇠가 덜컹덜컹 흔들리다 위로 튕겨 날아가며 사라지고, 버튼이 '톡' 튐
        // -> 다 끝나면 onDone 호출 (설명 대사 시작)
        {
            Sequence.Create(useUnscaledTime: true)
                .ChainDelay(P.fairyUnlockDelay) // 화면이 다 나타난 뒤 잠깐 기다렸다가
                .Chain(Tween.ShakeLocalPosition(lockIcon, new ShakeSettings(P.fairyLockShake, P.fairyLockShakeDuration, 18f)))  // 자물쇠가 덜컹덜컹 흔들리고
                .Chain(Tween.PunchScale(lockIcon, new ShakeSettings(Vector3.one * 0.35f, 0.2f, 8f)))   // '철컥' 하고 부풀었다가
                .Chain(Tween.LocalPositionY(lockIcon, lockIcon.localPosition.y + P.fairyLockFlyHeight, P.fairyLockFlyDuration, Ease.OutCubic))  // 위로 튕겨 날아가며
                .Group(Tween.LocalRotation(lockIcon, new Vector3(0f, 0f, P.fairyLockSpin), P.fairyLockFlyDuration, Ease.OutCubic))   // 빙글 돌고
                .Group(Tween.Alpha(lockGraphic, 0f, P.fairyLockFlyDuration, Ease.InQuad))  // 서서히 사라짐
                .Group(Tween.PunchScale(button, new ShakeSettings(Vector3.one * P.fairyUnlockPunch, P.fairyLockFlyDuration, 7f)))  // 동시에 버튼이 출렁 튐
                .ChainCallback(() => onDone?.Invoke());
        }

        public static void ReelTick(Transform word)
        // 룰렛 글자가 다음 단어로 바뀔 때마다 위에서 툭 내려오는 연출 함수 -> 칸이 굴러가는 느낌을 줌
        {
            Tween.StopAll(word);
            Tween.LocalPositionY(word, P.reelTickOffset, 0f, P.reelTickDuration, Ease.OutQuad, useUnscaledTime: true);
        }

        public static void ReelStop(Transform reel)    // 룰렛 한 칸이 멈추는 순간 '탁' 하고 튀는 연출 함수
        {
            Tween.PunchScale(reel, new ShakeSettings(Vector3.one * P.reelStopPunch, P.reelStopDuration, 9f, useUnscaledTime: true));
        }

        public static void RevealPiece(Transform art, CanvasGroup group)
        // 뽑힌 특수 기물 그림이 커지며 나타나는 연출 함수 -> 0이 아니라 60% 크기·옅은 상태에서 시작 (트윈이 멈춰도 그림은 보이게)
        {
            art.localScale = Vector3.one * 0.6f;
            group.alpha = 0.3f;
            Tween.Scale(art, 0.6f, 1f, P.reelRevealDuration, Ease.OutBack, useUnscaledTime: true);
            Tween.Alpha(group, 0.3f, 1f, P.reelRevealDuration * 0.6f, Ease.OutQuad, useUnscaledTime: true);
        }

        public static void GlowPulse(UnityEngine.UI.Graphic glow)
        // 기물 뒤의 파란 빛이 천천히 밝아졌다 어두워지기를 반복하는 연출 함수 -> 멈출 때는 GlowStop
        {
            Tween.StopAll(glow);
            Tween.Alpha(glow, P.fairyGlowMin, P.fairyGlowMax, P.fairyGlowPeriod, Ease.InOutSine, -1, CycleMode.Yoyo, useUnscaledTime: true);
        }

        public static void GlowStop(UnityEngine.UI.Graphic glow)   // 파란 빛 깜빡임을 멈추는 함수
        {
            Tween.StopAll(glow);
        }

        // ---------- 전역 ----------
        public static void HitStop(float scale = 0.05f)
        // 타격감이 필요한 순간 게임 시간을 아주 잠깐 거의 멈추는(히트 스톱) 함수
        {
            Time.timeScale = scale; // 게임 속도를 5%로 낮추고
            Tween.Delay(P.hitStopDuration, () => Time.timeScale = 1f, useUnscaledTime: true);
            // 실제 시간 기준(unscaled)으로 잠시 뒤 원래 속도로 되돌림 -> 게임 시간이 느려져도 항상 정상 속도로 돌아옴
        }

        public static Vector3 SpringFollow(Vector3 current, Vector3 target, float deltaTime)
        // 현재 위치를 목표 위치 쪽으로 부드럽게 끌어당긴 다음 위치를 계산하는 함수 (드래그 중 기물이 커서를 따라올 때 사용)
        // -> 지수 감쇠 공식을 써서 프레임레이트가 달라도 따라오는 속도가 같음
        {
            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-P.dragFollowSharpness * deltaTime));
        }
    }
}
