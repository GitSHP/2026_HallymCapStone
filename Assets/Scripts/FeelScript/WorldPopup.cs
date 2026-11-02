using TMPro;
using UnityEngine;

namespace Promotion.Feel
{
    public class WorldPopup : MonoBehaviour // 보드 위에 잠깐 떴다 사라지는 글자 하나(예: "막힘!")를 보여 주는 클래스 (BlockedPopup 프리팹에 붙어 있음)
    // -> 글꼴·색·배경 모양은 프리팹에서 조정함. 이 스크립트는 투명도만 바꾸고 다 사라지면 스스로 지움
    {
        [SerializeField] TMP_Text _label;   // 글자
        [SerializeField] SpriteRenderer _background;    // 글자 뒤 배경 (없어도 됨)

        float _backgroundAlpha = 1f;    // 배경의 원래 투명도

        void Awake()
        {
            if (_background != null)
                _backgroundAlpha = _background.color.a;
        }

        public void Play()  // 떠오르며 사라지는 연출을 재생하는 함수 -> 끝나면 오브젝트가 스스로 지워짐
        {
            Fx.PopupRise(transform, this, (popup, alpha) => popup.SetAlpha(alpha), popup => Destroy(popup.gameObject));
        }

        void SetAlpha(float alpha)  // 글자와 배경의 투명도를 함께 바꾸는 함수
        {
            _label.alpha = alpha;
            if (_background != null)
            {
                var c = _background.color;
                c.a = _backgroundAlpha * alpha;
                _background.color = c;
            }
        }
    }
}
