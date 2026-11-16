using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Analysis
{
    public class ReportLoadingView : MonoBehaviour
    // 리포트를 기다리는 동안 보여 주는 로딩 화면 -> 픽셀 로봇이 둥실거리며 눈을 깜빡이고, 점과 로딩 바가 움직임
    // -> 게임 시간이 멈춰 있어도 움직이도록 실제 시간(unscaledTime)으로 계산함
    {
        static readonly int[] FramePattern = { 0, 1, 0, 1, 0, 1, 2, 0, 1, 3, 3, 3, 0, 1 };  // 0 기본, 1 안테나 꺼짐, 2 눈 깜빡임, 3 옆 보기
        const float FrameSeconds = 0.22f;
        const float BobHeight = 8f;     // 위아래로 떠다니는 높이
        const float BobSpeed = 3.2f;
        const float BarSpeed = 1.1f;    // 로딩 바가 한 번 왕복하는 속도

        [SerializeField] Image _bot;
        [SerializeField] Sprite[] _frames;
        [SerializeField] TextMeshProUGUI _label;
        [SerializeField] RectTransform _barTrack;
        [SerializeField] RectTransform _barFill;

        RectTransform _botRect;
        Vector2 _botHome;
        string _message = "";
        float _startedAt;

        void Awake()
        {
            _botRect = _bot.rectTransform;
            _botHome = _botRect.anchoredPosition;
        }

        public void Show(string message)
        {
            _message = message;
            _startedAt = Time.unscaledTime;
            gameObject.SetActive(true);
            Tick();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            Tick();
        }

        void Tick()
        {
            if (_botRect == null)
                return;
            float t = Time.unscaledTime - _startedAt;

            int frame = FramePattern[Mathf.FloorToInt(t / FrameSeconds) % FramePattern.Length];
            if (frame < _frames.Length)
                _bot.sprite = _frames[frame];
            float bob = Mathf.Round(Mathf.Sin(t * BobSpeed) * BobHeight / 4f) * 4f;  // 4픽셀 단위로 움직여 픽셀 느낌을 살림
            _botRect.anchoredPosition = _botHome + new Vector2(0f, bob);

            int dots = Mathf.FloorToInt(t * 2.5f) % 4;
            _label.text = _message + new string('.', dots) + "<color=#00000000>" + new string('.', 3 - dots) + "</color>";  // 투명한 점으로 글자 폭을 고정

            float travel = _barTrack.rect.width - _barFill.rect.width;
            float pos = Mathf.PingPong(t * BarSpeed, 1f);
            pos = pos * pos * (3f - 2f * pos);  // 양 끝에서 살짝 느려지게
            _barFill.anchoredPosition = new Vector2(pos * travel, 0f);
        }
    }
}
