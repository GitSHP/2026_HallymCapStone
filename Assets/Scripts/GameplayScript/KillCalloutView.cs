using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class KillCalloutView : MonoBehaviour   // 플레이어 기물이 적을 잡는 순간 화면 상단 가운데에 처치 배너를 띄웠다가 사라지게 하는 클래스 (Board 오브젝트에 붙어 있음)
    // -> 역사다리꼴 배너에 [잡은 기물 그림 + 누적 킬 수] [칼 아이콘 + 문구] [잡힌 적 그림]
    // -> 이번 스테이지에서 그 기물의 첫 처치면 "처치!", 두 번째부터는 "연속처치!"
    // -> 배너는 잡기 동작과 타격 이펙트가 끝난 뒤에 뜸 (킬이 세어진 순간 내용을 저장해 두었다가 잡기 동작이 끝나면 잠깐 뒤 띄움)
    // -> 문구는 한 번에 하나만 뜸. 떠 있는 동안 또 잡으면 쌓지 않고 새 문구로 바꿔 다시 재생
    // -> KillStreakTracker의 KillCounted 이벤트를 구독함 -> 최종전에서는 킬을 세지 않으므로 문구도 뜨지 않음
    // -> 배너 모양은 HUD 안의 KillBanner 프리팹이 원본. 이 스크립트는 켜고 끄기와 그림·글자·색 채우기만 함
    {
        [Tooltip("HUD 안의 처치 배너. 비워 두면 씬에서 찾는다(꺼져 있어도 찾음).")]
        [SerializeField] KillCallout _callout;

        KillStreakTracker _tracker; // 킬이 세어질 때 신호(KillCounted)를 보내는 컴포넌트
        CombatResolver _combat; // 잡기 동작이 끝났다는 신호(CaptureFinished)를 보내는 컴포넌트 -> 이 신호를 받은 뒤 배너를 띄움

        struct Pending  // 잡기 동작이 끝나길 기다리는 배너 한 장의 내용 -> 킬이 세어진 순간(잡힌 기물이 아직 보일 때) 그림을 미리 저장해 둠
        {
            public string text;
            public Color color;
            public int kills;
            public bool fire;
            public Sprite attacker, victim;
            public Color attackerColor, victimColor;
        }

        readonly Dictionary<Piece, Pending> _pending = new();   // 잡힌 기물 -> 띄울 배너 내용
        PieceRegistry _registry;    // 기물 데이터 -> 화면 오브젝트(PieceView)를 찾아 그림을 가져오는 데 사용

        void Awake()
        {
            _tracker = GetComponent<KillStreakTracker>();
            _combat = GetComponent<CombatResolver>();
            _registry = GetComponent<PieceRegistry>();
        }

        void OnEnable()
        {
            if (_tracker != null)
                _tracker.KillCounted += OnKillCounted;  // 킬이 세어질 때마다 OnKillCounted가 실행되게 연결
            if (_combat != null)
                _combat.CaptureFinished += OnCaptureFinished;   // 잡기 동작이 끝날 때마다 OnCaptureFinished가 실행되게 연결
        }

        void OnDisable()
        {
            if (_tracker != null)
                _tracker.KillCounted -= OnKillCounted;
            if (_combat != null)
                _combat.CaptureFinished -= OnCaptureFinished;
        }

        void OnKillCounted(Piece attacker, Piece target, int kills)
        // 킬이 세어질 때마다 실행되는 함수 -> 배너 내용을 저장만 해 둠 (잡힌 기물이 사라지기 전에 그림을 가져와야 하므로 지금 저장)
        // kills: 잡은 기물의 이번 스테이지 누적 킬 수 (1이면 첫 처치)
        {
            if (target == null)
                return;

            bool fire = kills >= Fx.P.killStreakThreshold;  // 불이 붙는 킬 수 이상인지
            var attackerView = _registry.GetView(attacker);
            var targetView = _registry.GetView(target);
            _pending[target] = new Pending
            {
                text = kills <= 1 ? "처치!" : "연속처치!",
                color = fire ? Fx.P.calloutFireColor : kills <= 1 ? Fx.P.calloutFirstColor : Fx.P.calloutChainColor,
                kills = kills,
                fire = fire,
                attacker = attackerView != null ? attackerView.Art : null,
                attackerColor = attackerView != null ? attackerView.ArtColor : Color.white,
                victim = targetView != null ? targetView.Art : null,
                victimColor = targetView != null ? targetView.ArtColor : Color.white,
            };
        }

        void OnCaptureFinished(Piece victim, Vector3 where)
        // 잡기 동작이 끝나 타격 이펙트가 터진 순간 실행되는 함수 -> 저장해 둔 배너가 있다면 이펙트를 잠깐 보여 준 뒤 띄움
        {
            if (victim == null || !_pending.TryGetValue(victim, out var pending))
                return;
            _pending.Remove(victim);
            StartCoroutine(ShowAfterBurst(pending));
        }

        IEnumerator ShowAfterBurst(Pending pending) // 타격 이펙트가 재생되는 동안 기다렸다가 배너를 띄우는 코루틴
        {
            yield return new WaitForSecondsRealtime(Fx.P.calloutAfterBurstDelay);

            if (_callout == null)   // 연결이 비어 있다면 처음 쓸 때 HUD에서 찾음 -> 배너는 평소 꺼져 있으므로 꺼진 오브젝트까지 포함해서 찾음
                _callout = FindAnyObjectByType<KillCallout>(FindObjectsInactive.Include);
            if (_callout == null)
                yield break;

            _callout.Show(pending.text, pending.color, pending.kills,
                pending.attacker, pending.attackerColor, pending.victim, pending.victimColor);
            Fx.CalloutBanner(_callout.Body, _callout.Group, _callout.HomeY, _callout.Scale * (pending.fire ? Fx.P.calloutFireScale : 1f),
                _callout, callout => callout.Hide());
        }
    }
}
