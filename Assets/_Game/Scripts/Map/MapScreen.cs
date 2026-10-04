using System.Collections;
using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.Feel;
using Gambonanza.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gambonanza.Map
{
    /// <summary>
    /// The map scene: draws the run's map, lets the player pick where to go next and
    /// sends them there. Built in code like the HUD, so the scene only needs this
    /// component and a MapConfig.
    ///
    /// Only battle nodes do anything yet. Shop and boss nodes are placeholders that
    /// are passed through, so the route logic can be exercised before they exist.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        public MapConfig config;

        [Header("Layout (reference pixels at 1920x1080)")]
        public float floorSpacing = 140f;
        public float columnSpacing = 210f;
        [Tooltip("How far nodes stray from the grid, so the map does not read as a table.")]
        public float jitter = 28f;
        public float nodeSize = 80f;
        public float bossSize = 150f;
        public float edgeWidth = 6f;

        [Tooltip("Pause after a pick so the press is seen before the scene changes.")]
        [Min(0f)] public float leaveDelay = 0.2f;

        const float Margin = 110f;

        const string PromptText = "다음으로 갈 곳을 고르세요";
        const string ShopPassText = "상점은 아직 준비 중입니다. 이 칸은 그냥 지나갑니다.";
        const string MapEndText = "지도의 끝입니다. 보스 스테이지는 아직 준비 중입니다.";

        RunState _run;
        RectTransform _root;
        ScrollRect _scroll;
        RectTransform _content;
        TextMeshProUGUI _status;
        TextMeshProUGUI _message;
        UiButton _newRun;

        readonly List<int> _selectable = new();
        bool _leaving;

        MapData Map => _run.Map;

        void Start()
        {
            _run = RunState.Instance;
            EnsureMap();
            BuildFrame();
            Redraw();
        }

        void EnsureMap()
        {
            if (_run.Map == null)
                _run.Map = MapGenerator.Generate(config, System.Environment.TickCount);
        }

        // ---------- Frame (built once) ----------

        void BuildFrame()
        {
            var canvas = UiBuilder.OverlayCanvas("MapCanvas", 0);
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;

            var background = UiBuilder.Panel("Background", _root, UiTheme.Panel, false);
            UiBuilder.Stretch(background.rectTransform);

            var title = UiBuilder.Label("Title", _root, "지도", 40f, UiTheme.Ink,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UiBuilder.Place(title.rectTransform, UiBuilder.TopLeft, UiBuilder.TopLeft,
                new Vector2(60f, -36f), new Vector2(300f, 56f));

            _status = UiBuilder.Label("Status", _root, "", 26f, UiTheme.InkMuted, TextAlignmentOptions.Right);
            UiBuilder.Place(_status.rectTransform, UiBuilder.TopRight, UiBuilder.TopRight,
                new Vector2(-60f, -42f), new Vector2(700f, 44f));

            BuildLegend();
            BuildScrollArea();

            _message = UiBuilder.Label("Message", _root, PromptText, 26f, UiTheme.Ink);
            UiBuilder.Place(_message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-160f, 120f), new Vector2(1200f, 40f));

            _newRun = UiButton.CreateAction(_root, "NewRun", IconId.Arrow, "새 런",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 34f),
                new Vector2(240f, 70f), UiTheme.Confirm, UiTheme.InkOnAccent);
            _newRun.Clicked += StartNewRun;
        }

        void BuildLegend()
        {
            var panel = UiBuilder.Panel("Legend", _root, UiTheme.Card);
            UiBuilder.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-60f, 0f), new Vector2(280f, 250f));

            var heading = UiBuilder.Label("Heading", panel.transform, "범례", 24f, UiTheme.InkMuted,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UiBuilder.Place(heading.rectTransform, UiBuilder.TopLeft, UiBuilder.TopLeft,
                new Vector2(24f, -16f), new Vector2(230f, 36f));

            LegendRow(panel.transform, 0, MapNodeType.Battle, "전투");
            LegendRow(panel.transform, 1, MapNodeType.Shop, "상점 (준비 중)");
            LegendRow(panel.transform, 2, MapNodeType.Boss, "보스 (준비 중)");
        }

        void LegendRow(Transform parent, int row, MapNodeType type, string text)
        {
            float y = -78f - row * 56f;

            var icon = UiBuilder.Icon("Icon", parent, IconOf(type), AccentOf(type));
            UiBuilder.Place(icon.rectTransform, UiBuilder.TopLeft, new Vector2(0f, 0.5f),
                new Vector2(24f, y), Vector2.one * 38f);

            var label = UiBuilder.Label("Label", parent, text, 22f, UiTheme.Ink, TextAlignmentOptions.Left);
            UiBuilder.Place(label.rectTransform, UiBuilder.TopLeft, new Vector2(0f, 0.5f),
                new Vector2(76f, y), new Vector2(190f, 36f));
        }

        void BuildScrollArea()
        {
            var viewport = UiBuilder.Panel("Viewport", _root, Color.clear, false);
            var rect = viewport.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(60f, 170f);
            rect.offsetMax = new Vector2(-380f, -110f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiBuilder.Node("Content", rect);
            _content.anchorMin = Vector2.zero;
            _content.anchorMax = new Vector2(1f, 0f);
            _content.pivot = new Vector2(0.5f, 0f);
            _content.anchoredPosition = Vector2.zero;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = rect;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
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

            var line = UiBuilder.Panel("Edge_" + from + "_" + to, _content, EdgeColor(from, to), false);
            line.raycastTarget = false;

            var rect = line.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0f, 0.5f);
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
            var position = PositionOf(node);
            var anchor = new Vector2(0.5f, 0f);
            var accent = AccentOf(node.type);

            bool selectable = !_leaving && _selectable.Contains(index);
            bool current = index == _run.CurrentMapNode;
            bool visited = _run.MapPath.Contains(index);

            if (selectable || current)
            {
                var halo = UiBuilder.Icon("Halo_" + index, _content, PlaceholderArt.Circle,
                    current ? WithAlpha(UiTheme.Gold, 0.45f) : WithAlpha(accent, 0.22f));
                UiBuilder.Place(halo.rectTransform, anchor, UiBuilder.Centre, position, Vector2.one * size * 1.35f);
                if (selectable)
                    Fx.Breathe(halo.transform);
            }

            Image face;
            if (selectable)
            {
                var button = UiButton.CreateIcon(_content, "Node_" + index, IconOf(node.type),
                    anchor, UiBuilder.Centre, position, size, UiTheme.CardWell, accent);
                button.Clicked += () => OnNodeClicked(index, button.transform);
                face = button.GetComponent<Image>();
            }
            else
            {
                face = UiBuilder.Panel("Node_" + index, _content, UiTheme.CardWell, false);
                UiBuilder.Place(face.rectTransform, anchor, UiBuilder.Centre, position, Vector2.one * size);

                var iconColor = visited ? Color.Lerp(accent, UiTheme.InkMuted, 0.4f) : UiTheme.InkMuted;
                var icon = UiBuilder.Icon("Icon", face.transform, IconOf(node.type), iconColor);
                UiBuilder.Place(icon.rectTransform, UiBuilder.Centre, UiBuilder.Centre, Vector2.zero,
                    Vector2.one * size * 0.5f);

                if (!visited)
                    UiBuilder.Group(face.gameObject).alpha = 0.6f;
            }

            face.sprite = PlaceholderArt.Circle;
            face.type = Image.Type.Simple;

            if (visited && !current)
            {
                var tick = UiBuilder.Icon("Visited", face.transform, IconId.Check, UiTheme.Gold);
                UiBuilder.Place(tick.rectTransform, new Vector2(1f, 0f), UiBuilder.Centre,
                    new Vector2(-size * 0.12f, size * 0.12f), Vector2.one * size * 0.36f);
            }
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

            if (node.type == MapNodeType.Battle)
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

        static IconId IconOf(MapNodeType type) => type switch
        {
            MapNodeType.Shop => IconId.Bag,
            MapNodeType.Boss => IconId.Crown,
            _ => IconId.Sword
        };

        static Color AccentOf(MapNodeType type) => type switch
        {
            MapNodeType.Shop => UiTheme.Gold,
            MapNodeType.Boss => UiTheme.ArtifactAccent,
            _ => UiTheme.Deny
        };

        static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
