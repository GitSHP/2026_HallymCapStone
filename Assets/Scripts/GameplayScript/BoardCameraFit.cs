using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class BoardCameraFit : MonoBehaviour   // 화면 크기에 맞춰 카메라가 보드 전체를 보여 주도록 크기와 위치를 맞추는 클래스
    // 화면 위/아래/양옆은 UI(웨이브 바, AP 바, 유물 패널 등)가 차지하므로 그만큼 비워 두고 남은 영역에 보드를 맞춤
    {
        public BoardGrid board; // 화면에 맞출 대상 보드

        [Header("UI용으로 예약할 화면 비율")]
        [Tooltip("웨이브 진행 바.")]
        [Range(0f, 0.4f)] public float topReserve = 0.085f;    // 화면 위쪽에서 UI용으로 비워 둘 비율
        [Tooltip("AP 바와 소환 덱.")]
        [Range(0f, 0.4f)] public float bottomReserve = 0.14f;  // 화면 아래쪽에서 UI용으로 비워 둘 비율
        [Tooltip("유물 인벤토리와 기물 정보 패널. 한쪽 기준.")]
        [Range(0f, 0.4f)] public float sideReserve = 0.16f;    // 화면 양옆에서 UI용으로 비워 둘 비율(한쪽 기준)

        [Header("화면 구성")]
        [Tooltip("보드 주위 여백. 월드 단위.")]
        public float padding = 0.45f;   // 보드 가장자리와 화면 사이에 둘 여백

        Camera _camera; // 이 오브젝트에 붙어 있는 카메라

        void Awake()    // 오브젝트가 생성될 때 한 번 실행
        {
            _camera = GetComponent<Camera>();   // 카메라를 찾고
            _camera.orthographic = true;    // 2D 게임이므로 원근감 없는 직교 카메라로 설정
        }

        void Start() => Fit();  // 첫 프레임에 화면을 한 번 맞춤

        void Update()   // 매 프레임 실행
        {
            if (!Application.isPlaying || _lastAspect != _camera.aspect)
            // 에디터 편집 중이거나(값을 바꾸면 바로 보이도록) 화면 비율이 바뀌었다면 다시 맞춤
                Fit();
        }

        float _lastAspect;  // 마지막으로 맞췄을 때의 화면 비율 -> 비율이 바뀌었는지 비교하는 데 사용

        public void Fit()   // 카메라 크기와 위치를 계산해서 보드가 UI를 피해 화면에 꽉 차게 하는 함수
        {
            if (board == null)  // 맞출 보드가 없다면 종료
                return;

            if (_camera == null)    // 에디터 모드에서는 Awake가 아직 안 불렸을 수 있으므로 카메라를 다시 찾음
            {
                _camera = GetComponent<Camera>();
                _camera.orthographic = true;
            }

            _lastAspect = _camera.aspect;   // 현재 화면 비율 기억

            float usableVertical = Mathf.Max(0.1f, 1f - topReserve - bottomReserve);   // UI를 빼고 남은 세로 비율(최소 0.1)
            float usableHorizontal = Mathf.Max(0.1f, 1f - sideReserve * 2f);  // UI를 빼고 남은 가로 비율(최소 0.1)

            float halfBoardHeight = board.height * board.tileSize * 0.5f + padding;    // 여백을 포함한 보드 높이의 절반
            float halfBoardWidth = board.width * board.tileSize * 0.5f + padding;  // 여백을 포함한 보드 너비의 절반

            float sizeForHeight = halfBoardHeight / usableVertical; // 세로가 들어가려면 필요한 카메라 크기
            float sizeForWidth = halfBoardWidth / (usableHorizontal * Mathf.Max(0.1f, _camera.aspect));    // 가로가 들어가려면 필요한 카메라 크기

            float size = Mathf.Max(sizeForHeight, sizeForWidth);    // 둘 중 큰 값을 써야 가로/세로 모두 화면 안에 들어옴
            _camera.orthographicSize = size;

            float boardCentreFraction = bottomReserve + usableVertical * 0.5f; // 남은 영역의 세로 중앙이 화면 아래에서 몇 % 위치인지
            float offsetFraction = boardCentreFraction - 0.5f;  // 화면 정중앙과의 차이
            float yOffset = offsetFraction * size * 2f; // 그 차이를 월드 단위 거리로 바꿈

            var boardPos = board.transform.position;
            transform.position = new Vector3(boardPos.x, boardPos.y - yOffset, -10f);
            // 보드가 남은 영역의 중앙에 오도록 카메라를 위아래로 옮김(z = -10은 2D 카메라 기본 위치)
        }
    }
}
