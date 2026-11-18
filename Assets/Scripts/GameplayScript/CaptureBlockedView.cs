using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class CaptureBlockedView : MonoBehaviour // 잡을 수 없는 상대(중갑병)에게 기물을 놓았을 때 "막힘!"을 띄우는 클래스 (Board 오브젝트에 붙어 있음)
    // -> DragController의 CaptureBlocked 이벤트를 구독함. 놓은 기물은 DragController가 원래 칸으로 되돌림
    // -> 화면에서는 상대 기물 위에 "막힘!"이 톡 튀어나왔다 사라지고, 상대 기물이 '팅' 하고 흔들리며 쇠 부딪히는 소리가 남
    {
        [Tooltip("막힘 글자 프리팹.")]
        [SerializeField] WorldPopup _popupPrefab;
        [Tooltip("글자가 뜨는 높이(상대 기물 칸 중심 기준, 월드 단위).")]
        [SerializeField] float _offsetY = 0.55f;

        DragController _drag;   // 막힘 신호(CaptureBlocked)를 보내는 컴포넌트
        PieceRegistry _registry;    // 상대 기물의 화면 오브젝트를 찾는 데 사용
        BoardGrid _grid;    // 칸 좌표 -> 월드 좌표 변환용

        void Awake()
        {
            _drag = GetComponent<DragController>();
            _registry = GetComponent<PieceRegistry>();
            _grid = GetComponent<BoardGrid>();
        }

        void OnEnable()
        {
            if (_drag != null)
                _drag.CaptureBlocked += OnCaptureBlocked;
        }

        void OnDisable()
        {
            if (_drag != null)
                _drag.CaptureBlocked -= OnCaptureBlocked;
        }

        void OnCaptureBlocked(Piece attacker, Piece blocker)
        // 잡기가 막혔을 때 실행되는 함수
        // attacker: 놓은 기물(폰), blocker: 막아 낸 상대(중갑병)
        {
            if (blocker == null)
                return;

            var view = _registry.GetView(blocker);
            if (view != null)
                view.PlayBlocked();
            Sfx.Play(SfxId.Lock);   // 쇠가 부딪히는 소리

            if (_popupPrefab == null)
                return;
            var popup = Instantiate(_popupPrefab, transform);   // Board 자식으로 만들어 보드와 함께 움직이게 함
            popup.transform.position = _grid.CoordToWorld(blocker.Coord) + Vector3.up * _offsetY;
            popup.Play();
        }
    }
}
