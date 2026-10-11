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
    /// 지도 씬이다. 런의 지도를 그리고, 플레이어가 다음 행선지를 고르면 그곳으로 보낸다.
    /// 틀(제목, 범례, 버튼)은 MapScreen 프리팹이고, 노드와 경로는 런마다
    /// MapNode·MapEdge 프리팹을 복제해 만든다.
    ///
    /// 전투 노드와 보스(최종 체스 대결)는 해당 스테이지로 전투 씬을 불러온다.
    /// 상점 노드는 임시로 그냥 지나가는 칸이다.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        public MapConfig config;

        [Header("레이아웃 (1920x1080 기준 픽셀)")]
        public float floorSpacing = 140f;
        public float columnSpacing = 210f;
        [Tooltip("지도가 표처럼 보이지 않도록 노드가 격자에서 벗어나는 정도다.")]
        public float jitter = 28f;
        [Tooltip("MapNode 프리팹이 그려지는 크기다. 다른 노드 크기는 이 값을 기준으로 배율을 정한다.")]
        public float nodeSize = 80f;
        public float bossSize = 150f;
        public float edgeWidth = 6f;

        [Tooltip("씬이 바뀌기 전에 누른 반응이 보이도록 선택 후 잠시 멈추는 시간이다.")]
        [Min(0f)] public float leaveDelay = 0.2f;

        const float Margin = 110f;

        const string PromptText = "다음으로 갈 곳을 고르세요";
        const string ShopPassText = "상점은 아직 준비 중입니다. 이 칸은 그냥 지나갑니다.";
        const string MapEndText = "지도의 끝입니다. 보스 스테이지는 아직 준비 중입니다.";

        [Header("구성 요소 (프리팹에서 지정)")]
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

        // ---------- 지도 (변경될 때마다 다시 만든다) ----------

        void Redraw()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            Map.GetSelectable(_run.CurrentMapNode, _selectable);

            _content.sizeDelta = new Vector2(0f,
                Margin * 2f + Map.BossFloor * floorSpacing + floorSpacing * 0.25f + bossSize * 0.5f);

            // 노드가 연결선 위에 놓이도록 연결선을 먼저 그린다.
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
            // 프리팹은 nodeSize 크기로 그려진다. 보스는 같은 노드를 더 크게 키운 것이다.
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

        // ---------- 선택 ----------

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

            // 임시 노드는 밟고 지나가며, 그 자리에서 계속 진행한다.
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

        // ---------- 외형 ----------

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
