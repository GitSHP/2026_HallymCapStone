using UnityEngine;

namespace Promotion.Feel
{
    public class FireAura : MonoBehaviour   // 누적 킬로 불붙은 기물에 붙는 불꽃 입자 묶음을 켜고 끄는 클래스 (FireAura 프리팹에 붙어 있음)
    // -> 입자 모양·색·양은 전부 프리팹의 파티클 시스템 인스펙터에서 조정함. 이 스크립트는 켜기/끄기 순서만 담당
    // -> 기물이 픽셀 아트라서 둥근 입자 대신 작은 사각 입자(Square.png)를 씀
    // -> 입자는 월드 공간에서 움직이므로 기물을 드래그하면 불꼬리가 뒤로 남음
    {
        [SerializeField] ParticleSystem _flame;   // 기물 바닥에서 위로 피어오르는 불꽃 (몸체 뒤에 그림)
        [SerializeField] ParticleSystem _embers;  // 불꽃 위로 드문드문 튀어 오르는 불티 (몸체 앞에 그림)
        [SerializeField] ParticleSystem _burst;   // 점화 순간 사방으로 튀는 불티 (몸체 앞에 그림)
        [SerializeField] ParticleSystem _smoke;   // 불이 꺼질 때 피어오르는 회색 연기 (몸체 앞에 그림)
        [SerializeField] int _burstCount = 26;    // 점화 순간 튀는 불티 개수
        [SerializeField] int _smokeCount = 5;     // 꺼질 때 나오는 연기 입자 개수

        public bool Burning { get; private set; } // 지금 불이 타고 있는지 여부

        public void SetSortingOrder(int bodyOrder)
        // 기물 몸체의 그리기 순서를 기준으로 입자 층을 맞추는 함수 -> 불꽃은 몸체 바로 뒤, 불티·연기는 몸체 바로 앞
        // -> 기물 그림이 불꽃에 가려지지 않으면서도 불티가 몸체 위로 튀어 보이게 두 겹으로 나눔
        // bodyOrder: 기물 몸체(SpriteRenderer)의 sortingOrder (드래그 중에는 올라간 값)
        {
            SetOrder(_flame, bodyOrder - 1);
            SetOrder(_embers, bodyOrder + 2);
            SetOrder(_burst, bodyOrder + 2);
            SetOrder(_smoke, bodyOrder + 2);
        }

        public void Ignite()    // 불을 붙이는 함수 -> 불티가 한 번 터진 뒤 불꽃과 불티가 계속 피어오름
        {
            Burning = true;
            if (_burst != null)
                _burst.Emit(_burstCount);   // 점화 불티를 한 번에 터뜨리고
            if (_flame != null)
                _flame.Play();  // 계속 타는 불꽃과
            if (_embers != null)
                _embers.Play(); // 불티 방출을 시작함
        }

        public void PlayIdle()  // 점화 불티 없이 조용히 계속 타오르게 하는 함수 -> 처음부터 오라를 두르고 나오는 적(중갑병)용
        {
            Burning = true;
            if (_flame != null)
                _flame.Play();
            if (_embers != null)
                _embers.Play();
        }

        public void Extinguish()    // 불을 끄는 함수 -> 새 입자만 멈추고 이미 나온 입자는 자연스럽게 사라지게 둠 + 연기 몇 개
        {
            if (!Burning)
                return;

            Burning = false;
            if (_flame != null)
                _flame.Stop(true, ParticleSystemStopBehavior.StopEmitting);   // 방출만 멈춤 -> 남은 불꽃은 끝까지 타오르다 사라짐
            if (_embers != null)
                _embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_smoke != null)
                _smoke.Emit(_smokeCount);   // 꺼진 자리에서 연기가 피어오름
        }

        static void SetOrder(ParticleSystem system, int order)  // 파티클 시스템 하나의 그리기 순서를 바꾸는 함수
        {
            if (system == null)
                return;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.sortingOrder = order;
        }
    }
}
