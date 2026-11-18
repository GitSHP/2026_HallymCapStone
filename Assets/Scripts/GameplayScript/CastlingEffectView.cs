using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class CastlingEffectView : MonoBehaviour // 캐슬링이 착지하는 순간 파란 이펙트·"캐슬링!" 글자·종소리를 띄우는 클래스 (Board 오브젝트에 붙임)
    // -> DragController의 Castled 이벤트를 구독함. DragController는 이펙트가 있는지 모름
    // -> 화면에서는 킹과 룩 두 칸에서 파란 입자가 팍 튀고, 킹 위에 "캐슬링!"이 톡 튀어나왔다 떠오르며 사라짐
    {
        [Tooltip("착지한 두 칸에서 터지는 파란 입자 이펙트 프리팹.")]
        [SerializeField] CaptureBurst _burstPrefab;
        [Tooltip("킹 위에 뜨는 \"캐슬링!\" 글자 프리팹.")]
        [SerializeField] WorldPopup _popupPrefab;
        [Tooltip("글자가 뜨는 높이(킹 칸 중심 기준, 월드 단위).")]
        [SerializeField] float _offsetY = 0.6f;

        DragController _drag;   // 캐슬링 신호(Castled)를 보내는 컴포넌트
        BoardGrid _grid;    // 칸 좌표 -> 월드 좌표 변환용

        void Awake()
        {
            _drag = GetComponent<DragController>();
            _grid = GetComponent<BoardGrid>();
        }

        void OnEnable()
        {
            if (_drag != null)
                _drag.Castled += OnCastled;
        }

        void OnDisable()
        {
            if (_drag != null)
                _drag.Castled -= OnCastled;
        }

        void OnCastled(Piece king, Piece rook)  // 캐슬링 연출이 착지한 순간 실행되는 함수
        {
            Sfx.Play(SfxId.Castle); // 기물 소리와 다른 맑은 종소리
            Fx.HitStop();   // 아주 잠깐 멈춰 '쾅' 하는 타격감을 줌

            Burst(king);
            Burst(rook);

            if (_popupPrefab == null || king == null)
                return;
            var popup = Instantiate(_popupPrefab, transform);   // Board 자식으로 만들어 보드 흔들림과 함께 움직이게 함
            popup.transform.position = _grid.CoordToWorld(king.Coord) + Vector3.up * _offsetY;
            popup.Play();
        }

        void Burst(Piece piece) // 그 기물의 칸에서 파란 입자를 한 번 터뜨리는 함수
        {
            if (_burstPrefab == null || piece == null)
                return;
            var burst = Instantiate(_burstPrefab, _grid.CoordToWorld(piece.Coord), Quaternion.identity, transform);
            burst.Play(false);  // 파편 색은 프리팹의 아군 색(파란색)을 씀
        }
    }
}
