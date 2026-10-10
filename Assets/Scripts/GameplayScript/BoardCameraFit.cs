using Gambonanza.Core;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Frames the board for the HUD layout rather than for the raw screen.
    /// The reserves describe the strips the UI will occupy (wave bar on top,
    /// AP + deck below, relic and inspector panels at the sides); the board is
    /// sized and centred inside what is left, at any aspect ratio.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class BoardCameraFit : MonoBehaviour
    {
        public BoardGrid board;

        [Header("Screen fractions reserved for UI")]
        [Tooltip("Wave progress bar.")]
        [Range(0f, 0.4f)] public float topReserve = 0.085f;
        [Tooltip("AP bar and summon deck.")]
        [Range(0f, 0.4f)] public float bottomReserve = 0.14f;
        [Tooltip("Relic inventory and piece inspector, each side.")]
        [Range(0f, 0.4f)] public float sideReserve = 0.16f;

        [Header("Framing")]
        [Tooltip("Breathing room around the board, in world units.")]
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
            // Keeps the framing correct while resizing the Game view or editing values.
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

            // Orthographic size is half the screen height in world units.
            float sizeForHeight = halfBoardHeight / usableVertical;
            float sizeForWidth = halfBoardWidth / (usableHorizontal * Mathf.Max(0.1f, _camera.aspect));

            float size = Mathf.Max(sizeForHeight, sizeForWidth);
            _camera.orthographicSize = size;

            // Unequal top/bottom reserves push the free area off-centre; shift the
            // camera so the board sits in the middle of that area, not the screen.
            float boardCentreFraction = bottomReserve + usableVertical * 0.5f;
            float offsetFraction = boardCentreFraction - 0.5f;
            float yOffset = offsetFraction * size * 2f;

            var boardPos = board.transform.position;
            transform.position = new Vector3(boardPos.x, boardPos.y - yOffset, -10f);
        }
    }
}
