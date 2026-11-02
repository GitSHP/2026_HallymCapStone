using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Tutorial
{
    public class BossReactionView : MonoBehaviour   // 최종전 동안 화면 오른쪽 아래에 보스 초상화를 두고, 대사를 말풍선으로 잠깐 띄우는 화면 (BossReaction 프리팹에 붙어 있음)
    // -> 배치·크기·색은 프리팹이 원본. 이 스크립트는 켜고 끄기와 그림·글자 채우기만 함
    {
        [SerializeField] GameObject _root;  // 초상화와 말풍선을 묶은 전체 (대화창이 떠 있을 땐 숨김)
        [SerializeField] Image _portrait;   // 보스 초상화
        [SerializeField] RectTransform _bubble; // 말풍선 상자
        [SerializeField] CanvasGroup _bubbleGroup;  // 말풍선 투명도 (나타나고 사라지는 연출)
        [SerializeField] TextMeshProUGUI _bubbleName;   // 말풍선 위쪽의 화자 이름
        [SerializeField] TextMeshProUGUI _bubbleText;   // 말풍선 본문

        float _hideAt = -1f;    // 말풍선을 숨길 시각(실제 시간) -> -1이면 말풍선이 떠 있지 않음

        public void Setup(string speakerName, Sprite portrait)  // 이름과 초상화를 채우는 함수 -> 최종전이 시작될 때 한 번 호출
        {
            _bubbleName.text = speakerName;
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            _bubble.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)    // 초상화 전체를 보이거나 숨기는 함수 -> 대화창이 떠 있을 때는 겹치지 않게 숨김
        {
            _root.SetActive(visible);
        }

        public void Say(string text, float seconds) // 말풍선에 대사를 띄우는 함수 -> seconds초 뒤 저절로 사라짐. 떠 있는 중에 또 부르면 새 대사로 바꿈
        {
            if (string.IsNullOrEmpty(text))
                return;

            _bubbleText.text = text;
            _bubble.gameObject.SetActive(true);
            Feel.Fx.BubblePop(_bubble, _bubbleGroup);
            _hideAt = Time.unscaledTime + seconds;
        }

        void Update()   // 정해 둔 시간이 지나면 말풍선을 숨김
        {
            if (_hideAt < 0f || Time.unscaledTime < _hideAt)
                return;

            _hideAt = -1f;
            Feel.Fx.BubbleHide(_bubble, _bubbleGroup);
        }
    }
}
