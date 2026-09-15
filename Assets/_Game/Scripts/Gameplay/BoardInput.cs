using Gambonanza.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Turns pointer position into board coordinates.
    /// Bound to &lt;Pointer&gt; rather than &lt;Mouse&gt; so touch works without extra code.
    /// The grid is regular, so hover needs arithmetic — no colliders, no raycasts.
    /// </summary>
    [RequireComponent(typeof(BoardGrid))]
    public class BoardInput : MonoBehaviour
    {
        [Tooltip("Leave empty to use Camera.main.")]
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

            var screen = _pointAction.ReadValue<Vector2>();
            // Distance from the camera to the board plane (z = 0 in a 2D scene).
            float depth = -worldCamera.transform.position.z;
            PointerWorldPosition = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

            HoveredCoord = _grid.WorldToCoord(PointerWorldPosition);
            _grid.SetHover(HoveredCoord);
        }
    }
}
