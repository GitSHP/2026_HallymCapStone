using UnityEngine;

namespace Promotion.Feel
{
    public class DangerPing : MonoBehaviour
    // 다음 적 턴에 적이 떨어질 칸에 뜨는 경고 표시 (붉게 깜빡이는 칸 + 퍼지는 테두리 + 경고 삼각형 + 떨어질 적의 그림자)
    // -> 모양은 DangerPing 프리팹이 원본이고, 움직임은 Fx.DangerWarnStart / DangerWarnStop이 담당함
    {
        [SerializeField] SpriteRenderer _flash;
        [SerializeField] SpriteRenderer[] _rings;
        [SerializeField] SpriteRenderer _mark;
        [SerializeField] SpriteRenderer _ghost;     // 떨어질 적의 반투명 그림

        public void Show(Sprite enemyArt)
        {
            if (_ghost != null)
            {
                _ghost.sprite = enemyArt;
                _ghost.enabled = enemyArt != null;
            }
            Fx.DangerWarnStart(_flash, _rings, _mark, _ghost);
        }

        public void Hide()  // 연출을 멈추고 사라짐
        {
            Fx.DangerWarnStop(_flash, _rings, _mark, _ghost);
            Destroy(gameObject);
        }

        void OnDestroy()    // 씬이 바뀌어 함께 사라질 때도 반복 연출이 남지 않게 멈춤
        {
            Fx.DangerWarnStop(_flash, _rings, _mark, _ghost);
        }
    }
}
