using Promotion.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 포인터 위치를 보드 좌표로 바꾼다.
    /// &lt;Mouse&gt;가 아니라 &lt;Pointer&gt;에 바인딩해서 추가 코드 없이 터치도 동작한다.
    /// 격자가 규칙적이라 호버 판정은 계산만으로 충분하다. 콜라이더도 레이캐스트도 쓰지 않는다.
    /// </summary>
    [RequireComponent(typeof(BoardGrid))]
    public class BoardInput : MonoBehaviour
    {
        [Tooltip("비워 두면 Camera.main을 사용한다.")]
        public Camera worldCamera;

        public Coord HoveredCoord { get; private set; } = Coord.Invalid;
        public Vector3 PointerWorldPosition { get; private set; }

        BoardGrid _grid;
        InputAction _pointAction;
        InputAction _pressAction;

        public bool PressedThisFrame => _pressAction != null && _pressAction.WasPressedThisFrame();
        public bool ReleasedThisFrame => _pressAction != null && _pressAction.WasReleasedThisFrame();
        public bool IsPressed => _pressAction != null && _pressAction.IsPressed();

        void Awake()
        {
            _grid = GetComponent<BoardGrid>();
            if (worldCamera == null)
                worldCamera = Camera.main;

            _pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
            _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        }

        void OnEnable()
        {
            _pointAction.Enable();
            _pressAction.Enable();
        }

        void OnDisable()
        {
            _pointAction.Disable();
            _pressAction.Disable();
        }

        void OnDestroy()
        {
            _pointAction?.Dispose();
            _pressAction?.Dispose();
        }

        void Update()
        {
            if (worldCamera == null)
                return;

            // 오버레이 화면이 떠 있다. 플레이어가 실제로 보고 있는 메뉴 아래에서
            // 보드가 강조된 채로 남지 않도록 호버를 해제한다.
            if (!InputGate.WorldInputEnabled)
            {
                HoveredCoord = Coord.Invalid;
                _grid.SetHover(Coord.Invalid);
                return;
            }

            var screen = _pointAction.ReadValue<Vector2>();
            // 카메라에서 보드 평면까지의 거리(2D 씬에서는 z = 0).
            float depth = -worldCamera.transform.position.z;
            PointerWorldPosition = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

            HoveredCoord = _grid.WorldToCoord(PointerWorldPosition);
            _grid.SetHover(HoveredCoord);
        }
    }
}
