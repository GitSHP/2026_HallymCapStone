using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Feel
{
    public class KillCallout : MonoBehaviour   // 화면 상단 가운데에 뜨는 처치 배너 한 개를 보여 주는 클래스 (HUD 안의 KillBanner 프리팹에 붙어 있음)
    // -> 역사다리꼴 배경 위에 [잡은 기물 그림 + 누적 킬 수] [칼 아이콘 + 문구] [잡힌 적 그림]
    // -> 배치·크기·색은 프리팹 인스펙터에서 조정함. 이 스크립트는 그림·글자·색을 채우고 켜고 끄기만 함
    {
        [SerializeField] RectTransform _body;   // 연출로 움직이고 커졌다 줄어드는 배너 본체
        [SerializeField] CanvasGroup _group;    // 배너 전체 투명도 -> 나타나고 사라지는 연출에 사용
        [SerializeField] TMP_Text _label;   // 문구 ("처치!", "연속처치!")
        [SerializeField] TMP_Text _killCount;   // 잡은 기물 그림 아래의 누적 킬 수 ("3킬")
        [SerializeField] Image _attacker;   // 잡은 기물(가해자) 그림
        [SerializeField] Image _victim; // 잡힌 적(피해자) 그림
        [Tooltip("배너 전체 크기 배율. 연출 크기(불붙은 기물이면 더 크게)는 이 값에 곱해짐.")]
        [SerializeField] float _scale = 0.72f;  // 배너 기본 크기 배율 -> 화면에서 너무 크면 이 값만 줄이면 됨
        [SerializeField] Color _victimTint = new Color(0.55f, 0.5f, 0.5f, 1f);  // 잡힌 적 그림에 곱하는 색 -> 어둡게 해서 쓰러진 느낌

        float _homeY = float.NaN;   // 배너가 자리 잡는 원래 높이 (프리팹에 놓인 위치)

        public RectTransform Body
        {
            get { return _body; }
        }

        public float Scale  // 배너 기본 크기 배율
        {
            get { return _scale; }
        }

        public CanvasGroup Group
        {
            get { return _group; }
        }

        public float HomeY  // 배너가 최종적으로 자리 잡는 높이 -> 연출이 중간에 끊겨도 다음 연출은 이 높이를 기준으로 시작
        {
            get
            {
                if (float.IsNaN(_homeY))    // 처음 한 번 프리팹에 놓인 높이를 기억
                    _homeY = _body.anchoredPosition.y;
                return _homeY;
            }
        }

        void Awake()    // 배너는 HUD 안에 꺼진 채로 놓여 있음 -> 처음 Show로 켜질 때 한 번 실행
        {
            _ = HomeY;  // 연출이 위치를 바꾸기 전에 원래 높이를 먼저 기억해 둠
            _group.blocksRaycasts = false;  // 배너가 떠 있어도 아래 보드·버튼 클릭을 막지 않음
            _group.interactable = false;
        }

        public void Show(string text, Color color, int kills, Sprite attacker, Color attackerColor, Sprite victim, Color victimColor)
        // 배너를 켜고 그림·글자·색을 채우는 함수
        // text·color: 문구와 글자 색 / kills: 잡은 기물의 이번 스테이지 누적 킬 수
        // attacker·attackerColor, victim·victimColor: 잡은 기물과 잡힌 적의 그림과 색 (보드 위 모습 그대로)
        {
            _ = HomeY;
            gameObject.SetActive(true);
            _label.text = text;
            _label.color = color;
            if (_killCount != null)
                _killCount.text = kills + "킬";

            SetPortrait(_attacker, attacker, attackerColor);
            SetPortrait(_victim, victim, victimColor * _victimTint);    // 잡힌 쪽은 어둡게
        }

        static void SetPortrait(Image target, Sprite sprite, Color color)   // 기물 그림 한 칸을 채우는 함수 -> 그림이 없으면 그 칸은 비워 둠
        {
            if (target == null)
                return;

            target.sprite = sprite;
            target.color = color;
            target.enabled = sprite != null;
        }

        public void Hide()  // 다 사라진 배너를 끄는 함수
        {
            gameObject.SetActive(false);
        }
    }
}
