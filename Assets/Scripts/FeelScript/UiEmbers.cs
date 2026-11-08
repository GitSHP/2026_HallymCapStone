using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Feel
{
    public class UiEmbers : MonoBehaviour   // UI 위에서 작은 사각 입자가 아래에서 피어올라 사라지는 효과 (특수 기물의 파란 오라를 UI에서 흉내 냄)
    // -> 타이틀 캔버스는 화면 위에 바로 그리는 방식(Overlay)이라 파티클 시스템이 UI 위에 보이지 않음 -> Image 입자를 직접 움직임
    // -> 트윈이 아니라 매 프레임 계산으로 움직이므로 PrimeTween을 쓰지 않음 (게임 시간이 멈춰도 실제 시간으로 움직임)
    {
        [SerializeField] Sprite _sprite;    // 입자 그림 (사각형)
        [SerializeField] Gradient _colors = DefaultColors();   // 입자가 태어나서 사라질 때까지 바뀌는 색 (밝은 하늘색 -> 파랑 -> 남색, 끝에 투명)
        [SerializeField] float _rate = 26f; // 1초에 생기는 입자 수
        [SerializeField] Vector2 _size = new Vector2(6f, 12f);  // 입자 한 변의 길이 범위(픽셀)
        [SerializeField] Vector2 _speed = new Vector2(60f, 130f);   // 위로 올라가는 속도 범위(픽셀/초)
        [SerializeField] Vector2 _life = new Vector2(0.6f, 1.1f);   // 입자가 살아 있는 시간 범위(초)
        [SerializeField] float _spreadX = 70f;  // 입자가 생기는 가로 범위(가운데 기준 ±픽셀)
        [SerializeField] float _baseY = -60f;   // 입자가 생기는 높이(가운데 기준 픽셀)

        class Ember // 입자 하나의 상태
        {
            public RectTransform rect;
            public Image image;
            public float age;
            public float life;
            public float speed;
            public float drift;
        }

        readonly List<Ember> _alive = new();    // 살아 있는 입자
        readonly List<Ember> _pool = new(); // 다 쓴 입자 (다시 씀)
        float _spawnBudget; // 이번 프레임까지 쌓인 '만들어야 할 입자 수'
        bool _playing;

        public void Play()  // 입자를 계속 피워 올리기 시작하는 함수
        {
            _playing = true;
        }

        public void Stop()  // 새 입자를 멈추고 남은 입자도 모두 지우는 함수
        {
            _playing = false;
            foreach (var ember in _alive)
            {
                ember.rect.gameObject.SetActive(false);
                _pool.Add(ember);
            }
            _alive.Clear();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (_playing)
            {
                _spawnBudget += _rate * dt;
                while (_spawnBudget >= 1f)
                {
                    _spawnBudget -= 1f;
                    Spawn();
                }
            }

            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                var ember = _alive[i];
                ember.age += dt;
                float t = ember.age / ember.life;
                if (t >= 1f)    // 수명이 다하면 숨기고 다시 쓰도록 모아 둠
                {
                    ember.rect.gameObject.SetActive(false);
                    _alive.RemoveAt(i);
                    _pool.Add(ember);
                    continue;
                }
                ember.rect.anchoredPosition += new Vector2(ember.drift, ember.speed) * dt;
                ember.image.color = _colors.Evaluate(t);
                ember.rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, t);  // 올라갈수록 작아짐
            }
        }

        void Spawn()    // 입자 하나를 아래쪽 무작위 위치에 만드는 함수
        {
            Ember ember;
            if (_pool.Count > 0)
            {
                ember = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                ember.rect.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                ember = new Ember { rect = (RectTransform)go.transform, image = go.GetComponent<Image>() };
                ember.image.sprite = _sprite;
                ember.image.raycastTarget = false;  // 입자가 클릭을 가로채지 않게
            }

            float size = Random.Range(_size.x, _size.y);
            ember.rect.sizeDelta = new Vector2(size, size);
            ember.rect.anchoredPosition = new Vector2(Random.Range(-_spreadX, _spreadX), _baseY + Random.Range(-10f, 10f));
            ember.age = 0f;
            ember.life = Random.Range(_life.x, _life.y);
            ember.speed = Random.Range(_speed.x, _speed.y);
            ember.drift = Random.Range(-15f, 15f);
            ember.image.color = _colors.Evaluate(0f);
            _alive.Add(ember);
        }

        static Gradient DefaultColors() // 기본 색 변화 -> 파란 오라(BlueAura)와 같은 색
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 0.96f, 1f), 0f),
                    new GradientColorKey(new Color(0.24f, 0.65f, 1f), 0.4f),
                    new GradientColorKey(new Color(0.07f, 0.22f, 0.66f), 0.8f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
    }
}
