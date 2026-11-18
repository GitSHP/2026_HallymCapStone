using UnityEngine;

namespace Promotion.Feel
{
    public class CaptureBurst : MonoBehaviour   // 기물이 잡히는 순간 그 자리에서 터지는 타격 이펙트 한 번을 재생하는 클래스 (CaptureBurst 프리팹에 붙어 있음)
    // -> 세 겹: 마름모 섬광(번쩍) + 스파크 선(팍) + 잡힌 기물 색의 사각 파편(튀었다 떨어짐)
    // -> 입자 모양·양·속도는 프리팹의 파티클 시스템 인스펙터에서 조정함. 이 스크립트는 파편 색을 정하고 재생만 함
    // -> 재생이 끝나면 오브젝트가 스스로 사라짐 (루트 파티클 시스템의 Stop Action = Destroy)
    {
        [SerializeField] ParticleSystem _flash; // 맞는 순간 커졌다 사라지는 흰 마름모 섬광
        [SerializeField] ParticleSystem _sparks;    // 사방으로 빠르게 뻗는 가는 스파크 선
        [SerializeField] ParticleSystem _shards;    // 잡힌 기물 색의 사각 파편
        [SerializeField] int _sparkCount = 10;  // 스파크 선 개수
        [SerializeField] int _shardCount = 22;  // 파편 개수

        [Header("파편 색 (두 색 사이에서 무작위)")]
        [SerializeField] Color _enemyShardA = new Color(0.32f, 0.2f, 0.42f, 1f);    // 적이 잡혔을 때 파편 색 (검보라)
        [SerializeField] Color _enemyShardB = new Color(0.1f, 0.08f, 0.14f, 1f);
        [SerializeField] Color _playerShardA = new Color(0.95f, 0.95f, 0.9f, 1f);   // 아군이 잡혔을 때 파편 색 (흰·회색)
        [SerializeField] Color _playerShardB = new Color(0.6f, 0.62f, 0.68f, 1f);

        public void Play(bool victimIsEnemy)    // 이펙트를 한 번 재생하는 함수
        // victimIsEnemy: 잡힌 기물이 적 진영이면 true -> 파편 색을 진영에 맞춤
        {
            if (_shards != null)
            {
                var main = _shards.main;
                main.startColor = victimIsEnemy
                    ? new ParticleSystem.MinMaxGradient(_enemyShardA, _enemyShardB)
                    : new ParticleSystem.MinMaxGradient(_playerShardA, _playerShardB);
                _shards.Emit(_shardCount);
            }
            if (_flash != null)
                _flash.Emit(1);
            if (_sparks != null)
                _sparks.Emit(_sparkCount);
        }
    }
}
