using Promotion.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(BoardGrid))]
    public class BoardInput : MonoBehaviour   // 마우스/터치 입력을 읽어서 포인터가 보드의 어느 칸 위에 있는지 알려 주는 클래스
    // 격자가 규칙적이므로 Collider/Raycast 없이 좌표 계산만으로 칸을 찾음
    {
        [Tooltip("비워 두면 Camera.main을 사용한다.")]
        public Camera worldCamera;  // 화면 좌표를 월드 좌표로 바꿀 때 사용할 카메라

        public Coord HoveredCoord { get; private set; } = Coord.Invalid;    // 현재 포인터가 올라가 있는 칸(보드 밖이면 Invalid)
        public Vector3 PointerWorldPosition { get; private set; }   // 현재 포인터의 월드 좌표

        BoardGrid _grid;    // 월드 좌표 -> 칸 좌표 변환과 칸 강조 표시를 담당하는 컴포넌트
        InputAction _pointAction;   // 포인터 위치를 읽는 입력 액션(새 Input System)
        InputAction _pressAction;   // 클릭/터치 누름을 읽는 입력 액션

        public bool PressedThisFrame => _pressAction != null && _pressAction.WasPressedThisFrame();    // 이번 프레임에 눌렀는지 여부 -> 드래그 시작에 사용
        public bool ReleasedThisFrame => _pressAction != null && _pressAction.WasReleasedThisFrame();  // 이번 프레임에 뗐는지 여부 -> 드래그 끝에 사용
        public bool IsPressed => _pressAction != null && _pressAction.IsPressed();  // 지금 누르고 있는지 여부

        void Awake()    // 오브젝트가 생성될 때 한 번 실행
        {
            _grid = GetComponent<BoardGrid>();
            if (worldCamera == null)    // 카메라가 지정되지 않았다면 메인 카메라를 사용
                worldCamera = Camera.main;

            _pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");  // 마우스/터치 위치를 읽는 액션 생성
            _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");    // 마우스 클릭/터치 누름을 읽는 액션 생성
        }

        void OnEnable() // 컴포넌트가 켜질 때 발생하는 이벤트 -> 입력 액션을 활성화
        {
            _pointAction.Enable();
            _pressAction.Enable();
        }

        void OnDisable()    // 컴포넌트가 꺼질 때 발생하는 이벤트 -> 입력 액션을 비활성화
        {
            _pointAction.Disable();
            _pressAction.Disable();
        }

        void OnDestroy()    // 오브젝트가 파괴될 때 발생하는 이벤트 -> 직접 만든 입력 액션은 직접 정리해야 함
        {
            _pointAction?.Dispose();
            _pressAction?.Dispose();
        }

        void Update()   // 매 프레임 포인터 위치를 읽어서 올라가 있는 칸을 갱신
        {
            if (worldCamera == null)    // 카메라가 없으면 계산할 수 없으므로 종료
                return;

            if (!InputGate.WorldInputEnabled)   // 만약 상점/팝업 등 UI가 열려 있어 보드 입력이 막혀 있다면
            {
                HoveredCoord = Coord.Invalid;   // 선택된 칸을 없애고
                _grid.SetHover(Coord.Invalid);  // 칸 강조 표시도 지움
                return;
            }

            var screen = _pointAction.ReadValue<Vector2>(); // 화면상의 포인터 위치(픽셀)
            float depth = -worldCamera.transform.position.z;    // 카메라에서 보드 평면(z=0)까지의 거리
            PointerWorldPosition = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth)); // 화면 좌표 -> 월드 좌표 변환

            HoveredCoord = _grid.WorldToCoord(PointerWorldPosition);    // 월드 좌표가 어느 칸인지 계산하고
            _grid.SetHover(HoveredCoord);   // 그 칸을 강조 표시
        }
    }
}
