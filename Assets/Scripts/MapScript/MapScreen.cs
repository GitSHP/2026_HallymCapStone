using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Promotion.Map
{
    /// <summary>
    /// The map scene: draws the run's map, lets the player pick where to go next and
    /// sends them there. The frame (title, legend, buttons) is the MapScreen prefab;
    /// nodes and paths are copies of the MapNode and MapEdge prefabs, made per run.
    ///
    /// Battle nodes and the boss (the final chess duel) load the battle scene with
    /// their stage. Shop nodes are placeholders that are passed through.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        public MapConfig config;

        [Header("Layout (reference pixels at 1920x1080)")]
        public float floorSpacing = 140f;
        public float columnSpacing = 210f;
        [Tooltip("How far nodes stray from the grid, so the map does not read as a table.")]
        public float jitter = 28f;
        [Tooltip("The size the MapNode prefab is drawn at; other node sizes scale it.")]
        public float nodeSize = 80f;
        public float bossSize = 150f;
        public float edgeWidth = 6f;

        [Tooltip("Pause after a pick so the press is seen before the scene changes.")]
        [Min(0f)] public float leaveDelay = 0.2f;

        const float Margin = 110f;

        const string PromptText = "다음으로 갈 곳을 고르세요";
        const string ShopPassText = "상점은 아직 준비 중입니다. 이 칸은 그냥 지나갑니다.";
        const string MapEndText = "지도의 끝입니다. 보스 스테이지는 아직 준비 중입니다.";

        [Header("Parts (set in the prefab)")]
        [SerializeField] ScrollRect _scroll;
        [SerializeField] RectTransform _content;
        [SerializeField] TextMeshProUGUI _status;
        [SerializeField] TextMeshProUGUI _message;
        [SerializeField] UiButton _newRun;
        [SerializeField] MapNodeView _nodePrefab;
        [SerializeField] Image _edgePrefab;

        RunState _run;

        readonly List<int> _selectable = new();
        bool _leaving;

        MapData Map => _run.Map;

        void Start()
        {
            _run = RunState.Instance;
            EnsureMap();
            _message.text = PromptText;
            _newRun.Clicked += StartNewRun;
            Redraw();
        }

        void EnsureMap()
        {
            if (_run.Map == null)
                _run.Map = MapGenerator.Generate(config, System.Environment.TickCount);
        }

        // ---------- Map (rebuilt on every change) ----------

        void Redraw()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            Map.GetSelectable(_run.CurrentMapNode, _selectable);

            _content.sizeDelta = new Vector2(0f,
                Margin * 2f + Map.BossFloor * floorSpacing + floorSpacing * 0.25f + bossSize * 0.5f);

            // Edges first so the nodes sit on top of them.
            for (int i = 0; i < Map.nodes.Count; i++)
                foreach (int next in Map.nodes[i].next)
                    DrawEdge(i, next);

            for (int i = 0; i < Map.nodes.Count; i++)
                DrawNode(i);

            RefreshStatus();
            ScrollToCurrent();
        }

        void RefreshStatus()
        {
            int current = _run.CurrentMapNode;
            string floor = current < 0
                ? "출발 전"
                : "층 " + (Map.nodes[current].floor + 1) + " / " + Map.floors;
            _status.text = floor + "   ·   골드 " + _run.Gold;

            bool atEnd = current >= 0 && _selectable.Count == 0;
            _newRun.gameObject.SetActive(atEnd);
            if (atEnd)
                _message.text = MapEndText;
        }

        Vector2 PositionOf(MapNode node)
        {
            if (node.type == MapNodeType.Boss)
                return new Vector2(0f, Margin + node.floor * floorSpacing + floorSpacing * 0.25f);

            float x = (node.column - (Map.columns - 1) * 0.5f) * columnSpacing + node.jitter.x * jitter;
            float y = Margin + node.floor * floorSpacing + node.jitter.y * jitter * 0.5f;
            return new Vector2(x, y);
        }

        void DrawEdge(int from, int to)
        {
            var a = PositionOf(Map.nodes[from]);
            var b = PositionOf(Map.nodes[to]);
            var delta = b - a;

            var line = Instantiate(_edgePrefab, _content);
            line.name = "Edge_" + from + "_" + to;
            line.color = EdgeColor(from, to);

            var rect = line.rectTransform;
            rect.sizeDelta = new Vector2(delta.magnitude, edgeWidth);
            rect.anchoredPosition = a;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        Color EdgeColor(int from, int to)
        {
            var path = _run.MapPath;
            for (int i = 1; i < path.Count; i++)
                if (path[i - 1] == from && path[i] == to)
                    return UiTheme.Gold;

            if (from == _run.CurrentMapNode && _selectable.Contains(to))
                return WithAlpha(UiTheme.Ink, 0.6f);

            return WithAlpha(UiTheme.InkFaint, 0.55f);
        }

        void DrawNode(int index)
        {
            var node = Map.nodes[index];
            float size = node.type == MapNodeType.Boss ? bossSize : nodeSize;
            var accent = AccentOf(node.type);

            bool selectable = !_leaving && _selectable.Contains(index);
            bool current = index == _run.CurrentMapNode;
            bool visited = _run.MapPath.Contains(index);

            var view = Instantiate(_nodePrefab, _content);
            view.name = "Node_" + index;
            view.Rect.anchoredPosition = PositionOf(node);
            // The prefab is drawn at nodeSize; the boss is the same node, larger.
            view.Rect.localScale = Vector3.one * (size / nodeSize);

            view.halo.gameObject.SetActive(selectable || current);
            if (selectable || current)
            {
                view.halo.color = current ? WithAlpha(UiTheme.Gold, 0.45f) : WithAlpha(accent, 0.22f);
                if (selectable)
                    Fx.Breathe(view.halo.transform);
            }

            view.icon.sprite = UiSprites.Get(IconOf(node.type));
            view.icon.color = selectable ? accent
                : visited ? Color.Lerp(accent, UiTheme.InkMuted, 0.4f)
                : UiTheme.InkMuted;

            view.group.alpha = selectable || visited ? 1f : 0.6f;
            view.visited.SetActive(visited && !current);

            view.button.enabled = selectable;
            if (selectable)
                view.button.Clicked += () => OnNodeClicked(index, view.button.transform);
        }

        void ScrollToCurrent()
        {
            Canvas.ForceUpdateCanvases();

            float viewHeight = _scroll.viewport.rect.height;
            float contentHeight = _content.rect.height;
            if (contentHeight <= viewHeight)
            {
                _scroll.verticalNormalizedPosition = 0f;
                return;
            }

            int current = _run.CurrentMapNode;
            float focusY = current < 0 ? 0f : PositionOf(Map.nodes[current]).y;
            _scroll.verticalNormalizedPosition =
                Mathf.Clamp01((focusY - viewHeight * 0.3f) / (contentHeight - viewHeight));
        }

        // ---------- Choices ----------

        void OnNodeClicked(int index, Transform view)
        {
            if (_leaving || !_selectable.Contains(index))
                return;

            var node = Map.nodes[index];
            _run.MapPath.Add(index);
            UiFx.Punch(view);

            if (node.type == MapNodeType.Battle || (node.type == MapNodeType.Boss && node.stage != null))
            {
                _leaving = true;
                _run.CurrentStage = node.stage;
                StartCoroutine(LoadAfterDelay(config.battleScene));
                return;
            }

            // Placeholder nodes: step onto them and carry on from there.
            _message.text = node.type == MapNodeType.Shop ? ShopPassText : MapEndText;
            Redraw();
        }

        IEnumerator LoadAfterDelay(string sceneName)
        {
            yield return new WaitForSecondsRealtime(leaveDelay);
            SceneManager.LoadScene(sceneName);
        }

        void StartNewRun()
        {
            _run.ResetRun();
            EnsureMap();
            _message.text = PromptText;
            Redraw();
        }

        // ---------- Look ----------

        public static IconId IconOf(MapNodeType type) => type switch
        {
            MapNodeType.Shop => IconId.Bag,
            MapNodeType.Boss => IconId.Crown,
            _ => IconId.Sword
        };

        public static Color AccentOf(MapNodeType type) => type switch
        {
            MapNodeType.Shop => UiTheme.Gold,
            MapNodeType.Boss => UiTheme.ArtifactAccent,
            _ => UiTheme.Deny
        };

        static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
