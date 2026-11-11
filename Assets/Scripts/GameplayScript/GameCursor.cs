using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Promotion.Gameplay
{
    public class GameCursor : MonoBehaviour
    // 게임 전체의 마우스 커서 모양을 바꾸는 클래스 (픽셀 체스 에셋의 손 커서)
    // -> 평소: 가리키는 손 / 집을 수 있는 아군 기물 위: 편 손 / 기물을 끄는 중: 쥔 손
    // -> 게임 시작 시 스스로 만들어져 씬이 바뀌어도 유지됨. 씬에 따로 놓을 필요가 없음
    // -> 커서 그림은 Resources/Cursors 폴더 (원본 16x16을 4배로 키운 64x64)
    {
        enum Kind { None, Point, Open, Grab }

        static readonly Vector2 PointHotspot = new(26f, 8f);    // 클릭 지점 = 집게손가락 끝
        static readonly Vector2 HandHotspot = new(32f, 32f);    // 편 손·쥔 손은 손 가운데

        Texture2D _point;
        Texture2D _open;
        Texture2D _grab;
        Kind _current = Kind.None;
        DragController _drag;   // 지금 씬의 드래그 담당 (타이틀·지도에는 없음)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("GameCursor");
            DontDestroyOnLoad(go);
            go.AddComponent<GameCursor>();
        }

        void Awake()
        {
            _point = Resources.Load<Texture2D>("Cursors/Cursor_Point");
            _open = Resources.Load<Texture2D>("Cursors/Cursor_Open");
            _grab = Resources.Load<Texture2D>("Cursors/Cursor_Grab");
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindDrag();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene _scene, LoadSceneMode _mode)
        {
            FindDrag();
        }

        void FindDrag()
        {
            _drag = FindAnyObjectByType<DragController>();
        }

        void Update()
        {
            Apply(Decide());
        }

        Kind Decide()   // 지금 상황에 맞는 커서 종류를 고르는 함수
        {
            if (_drag == null)
                return Kind.Point;
            if (_drag.IsDragging)
                return Kind.Grab;
            if (IsOverUi())   // UI 위에서는 보드 기물을 집을 수 없으므로 가리키는 손
                return Kind.Point;
            if (_drag.IsHoveringPickable)
                return Kind.Open;
            return Kind.Point;
        }

        static bool IsOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void Apply(Kind kind)   // 커서가 바뀔 때만 실제로 교체함
        {
            if (kind == _current)
                return;
            _current = kind;

            if (kind == Kind.Grab && _grab != null)
                Cursor.SetCursor(_grab, HandHotspot, CursorMode.ForceSoftware);
            else if (kind == Kind.Open && _open != null)
                Cursor.SetCursor(_open, HandHotspot, CursorMode.ForceSoftware);
            else if (_point != null)
                Cursor.SetCursor(_point, PointHotspot, CursorMode.ForceSoftware);
        }
    }
}
