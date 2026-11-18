using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class CaptureEffectView : MonoBehaviour  // 기물 잡기 동작이 끝나면 잡힌 자리에 타격 이펙트를 띄우는 클래스 (Board 오브젝트에 붙어 있음)
    // -> CombatResolver의 CaptureFinished 이벤트를 구독함 -> 잡기 동작(돌진·착지)이 다 끝난 뒤 터짐. CombatResolver는 이펙트가 있는지 모름
    // -> 웨이브 스테이지와 최종전 모두, 누가 잡든(아군이 적을 / 적이 아군을) 이펙트가 뜸
    {
        [Tooltip("잡히는 순간 터지는 타격 이펙트 프리팹.")]
        [SerializeField] CaptureBurst _prefab;

        CombatResolver _combat; // 잡기 동작이 끝났다는 신호(CaptureFinished)를 보내는 컴포넌트

        void Awake()
        {
            _combat = GetComponent<CombatResolver>();
        }

        void OnEnable()
        {
            if (_combat != null)
                _combat.CaptureFinished += OnCaptureFinished;  // 잡기 동작이 끝날 때마다 OnCaptureFinished가 실행되게 연결
        }

        void OnDisable()
        {
            if (_combat != null)
                _combat.CaptureFinished -= OnCaptureFinished;
        }

        void OnCaptureFinished(Piece victim, Vector3 where)
        // 잡기 동작이 끝난 순간 실행되는 함수 -> 잡힌 기물이 있던 자리에 이펙트를 하나 만들어 재생 (재생이 끝나면 스스로 사라짐)
        // victim: 잡힌 기물, where: 잡힌 기물이 있던 월드 위치
        {
            if (_prefab == null || victim == null)
                return;

            var burst = Instantiate(_prefab, where, Quaternion.identity, transform);    // Board 자식으로 만들어 보드 흔들림과 함께 움직이게 함
            burst.Play(victim.Team == Team.Enemy);
            Sfx.Play(SfxId.Shatter);    // 파편이 튀는 순간 기물이 부서지는 소리
        }
    }
}
