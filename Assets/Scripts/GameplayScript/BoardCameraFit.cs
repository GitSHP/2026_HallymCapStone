using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 보드를 화면 전체가 아니라 HUD 배치에 맞춰 화면에 담는다.
    /// 예약 영역은 UI가 차지할 띠(위쪽 웨이브 바, 아래쪽 AP와 덱, 양옆 유물·정보 패널)를
    /// 나타내며, 보드는 어떤 화면비에서든 남은 공간 안에 크기를 맞춰 가운데 놓인다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class BoardCameraFit : MonoBehaviour
    {
        public BoardGrid board;

        [Header("UI용으로 예약할 화면 비율")]
        [Tooltip("웨이브 진행 바.")]
        [Range(0f, 0.4f)] public float topReserve = 0.085f;
        [Tooltip("AP 바와 소환 덱.")]
        [Range(0f, 0.4f)] public float bottomReserve = 0.14f;
        [Tooltip("유물 인벤토리와 기물 정보 패널. 한쪽 기준.")]
        [Range(0f, 0.4f)] public float sideReserve = 0.16f;

        [Header("화면 구성")]
        [Tooltip("보드 주위 여백. 월드 단위.")]
        public float padding = 0.45f;

        Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
        }

        void Start() => Fit();

        void Update()
        {
            // Game 뷰 크기를 바꾸거나 값을 편집하는 동안에도 화면 구성을 올바르게 유지한다.
            if (!Application.isPlaying || _lastAspect != _camera.aspect)
                Fit();
        }

        float _lastAspect;

        public void Fit()
        {
            if (board == null)
                return;

            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
                _camera.orthographic = true;
            }

            _lastAspect = _camera.aspect;

            float usableVertical = Mathf.Max(0.1f, 1f - topReserve - bottomReserve);
            float usableHorizontal = Mathf.Max(0.1f, 1f - sideReserve * 2f);

            float halfBoardHeight = board.height * board.tileSize * 0.5f + padding;
            float halfBoardWidth = board.width * board.tileSize * 0.5f + padding;

            // 직교 크기는 월드 단위로 나타낸 화면 높이의 절반이다.
            float sizeForHeight = halfBoardHeight / usableVertical;
            float sizeForWidth = halfBoardWidth / (usableHorizontal * Mathf.Max(0.1f, _camera.aspect));

            float size = Mathf.Max(sizeForHeight, sizeForWidth);
            _camera.orthographicSize = size;

            // 위아래 예약 영역이 다르면 빈 영역이 중앙에서 벗어난다. 보드가 화면이 아니라
            // 그 영역의 가운데에 오도록 카메라를 옮긴다.
            float boardCentreFraction = bottomReserve + usableVertical * 0.5f;
            float offsetFraction = boardCentreFraction - 0.5f;
            float yOffset = offsetFraction * size * 2f;

            var boardPos = board.transform.position;
            transform.position = new Vector3(boardPos.x, boardPos.y - yOffset, -10f);
        }
    }
}
