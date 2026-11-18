using UnityEngine;

namespace Promotion.Feel
{
    [DefaultExecutionOrder(-1000)]  // 다른 스크립트·트윈보다 먼저 지난 프레임의 떨림을 되돌림 (LateUpdate에서 다시 더함)
    public class ThreatMark : MonoBehaviour
    // 다음 적 턴에 잡힐 아군 기물 머리 위의 '!' 표시 + 기물 떨림
    // -> 모양은 ThreatMark 프리팹이 원본이고, '!'의 튀어나옴·옅어짐은 Fx.ThreatPop이 담당함
    // -> 떨림은 기물 위치에 작은 흔들림을 '더했다가 빼는' 방식이라 이동 연출·드래그와 겹쳐도 위치가 틀어지지 않음
    // -> 위치는 부모(보드) 기준 좌표로 다룸: 착지 때 보드 전체가 흔들려도 그걸 '다른 코드가 옮겼다'고 착각해 흔들림이 쌓이지 않게 함
    {
        [SerializeField] SpriteRenderer _sprite;    // '!' 그림

        Transform _target;  // 떨게 할 기물
        Vector3 _offset;    // 지금 더해 둔 흔들림
        Vector3 _leftAt;    // 흔들림을 더한 직후의 기물 위치(부모 기준) -> 그사이 다른 코드가 위치를 바꿨는지 확인용
        bool _applied;      // 흔들림을 더해 둔 상태인지
        float _nextJitter;  // 다음에 흔들림 방향을 바꿀 시각

        public void Show(Transform target)  // 기물에 붙어 '!'를 띄우고 떨기 시작
        {
            _target = target;
            Fx.ThreatPop(_sprite.transform, _sprite);
        }

        public void Hide()  // 떨림을 되돌리고 사라짐
        {
            RemoveOffset();
            Fx.ThreatStop(_sprite.transform, _sprite);
            Destroy(gameObject);
        }

        void Update()
        {
            RemoveOffset();
        }

        void LateUpdate()
        {
            if (_target == null)
                return;

            if (Time.time >= _nextJitter)   // 짧은 간격마다 떨리는 위치를 새로 고름
            {
                var p = Fx.P;
                _nextJitter = Time.time + p.threatShakeInterval;
                var r = Random.insideUnitCircle * p.threatShake;
                _offset = new Vector3(r.x, r.y, 0f);
            }

            _target.localPosition += _offset;
            _leftAt = _target.localPosition;
            _applied = true;
        }

        void RemoveOffset() // 지난 프레임에 더한 흔들림을 빼는 함수 -> 다른 코드가 이미 위치를 새로 정했다면 건드리지 않음
        {
            if (!_applied || _target == null)
            {
                _applied = false;
                return;
            }
            if (_target.localPosition == _leftAt)
                _target.localPosition -= _offset;
            _applied = false;
        }

        void OnDestroy()    // 기물과 함께 사라질 때도 반복 연출이 남지 않게 멈춤
        {
            if (_sprite != null)
                Fx.ThreatStop(_sprite.transform, _sprite);
        }
    }
}
