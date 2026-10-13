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
    public class MapScreen : MonoBehaviour // 지도 씬을 담당하는 클래스
    // -> 런의 지도를 화면에 그리고, 플레이어가 다음 행선지(노드)를 고르면 그곳으로 보냄
    // -> 틀(제목, 범례, 버튼)은 MapScreen 프리팹이고, 노드와 연결선은 런마다 MapNode·MapEdge 프리팹을 복제해서 만듦
    // -> 전투 노드와 보스 노드(최종 체스 대결)는 해당 스테이지로 전투 씬을 불러오고, 상점 노드는 임시로 그냥 지나감
    {
        public MapConfig config; // 지도 생성 설정 (층 수, 칸 수, 경로 수, 상점 확률, 전투 씬 이름 등)

        [Header("레이아웃 (1920x1080 기준 픽셀)")]
        public float floorSpacing = 140f;  // 지도에서 층과 층 사이의 세로 간격(픽셀)
        public float columnSpacing = 210f; // 지도에서 칸과 칸 사이의 가로 간격(픽셀)
        [Tooltip("지도가 표처럼 보이지 않도록 노드가 격자에서 벗어나는 정도다.")]
        public float jitter = 28f; // 노드를 격자 위치에서 흔드는 최대 거리(픽셀) -> MapNode.jitter(-1~1)에 곱해서 사용
        [Tooltip("MapNode 프리팹이 그려지는 크기다. 다른 노드 크기는 이 값을 기준으로 배율을 정한다.")]
        public float nodeSize = 80f;  // 일반 노드의 크기(픽셀) = 프리팹 원래 크기
        public float bossSize = 150f; // 보스 노드의 크기(픽셀) -> 같은 프리팹을 bossSize / nodeSize 배로 키워서 그림
        public float edgeWidth = 6f;  // 노드 사이 연결선의 두께(픽셀)

        [Tooltip("씬이 바뀌기 전에 누른 반응이 보이도록 선택 후 잠시 멈추는 시간이다.")]
        [Min(0f)] public float leaveDelay = 0.2f; // 노드를 누른 뒤 전투 씬으로 넘어가기 전까지 기다리는 시간(초)

        const float Margin = 110f; // 지도 위아래 여백(픽셀)

        const string PromptText = "다음으로 갈 곳을 고르세요"; // 평소 안내 문구
        const string ShopPassText = "상점은 아직 준비 중입니다. 이 칸은 그냥 지나갑니다."; // 상점 노드를 밟았을 때 안내 문구
        const string MapEndText = "지도의 끝입니다. 보스 스테이지는 아직 준비 중입니다."; // 더 갈 곳이 없을 때 안내 문구

        [Header("구성 요소 (프리팹에서 지정)")]
        [SerializeField] ScrollRect _scroll; // 지도를 위아래로 스크롤하는 스크롤 뷰
        [SerializeField] RectTransform _content; // 스크롤 뷰 안쪽 내용물 -> 노드와 연결선이 모두 이 아래에 생성됨
        [SerializeField] TextMeshProUGUI _status; // 현재 층과 골드를 보여 주는 상단 텍스트
        [SerializeField] TextMeshProUGUI _message; // 안내 문구를 보여 주는 텍스트
        [SerializeField] UiButton _newRun; // 지도 끝에 도달했을 때 나타나는 "새 런 시작" 버튼
        [SerializeField] MapNodeView _nodePrefab; // 노드 하나를 그릴 때 복제하는 프리팹
        [SerializeField] Image _edgePrefab; // 연결선 하나를 그릴 때 복제하는 프리팹 (가로로 긴 막대 이미지)
        [Tooltip("지도를 따라 위아래로 움직이는 세로형 배경이다. 출발 지점에서는 그림의 아래쪽, 보스에 가까울수록 위쪽이 보인다.")]
        [SerializeField] RectTransform _scrollingBackground; // 스크롤에 맞춰 함께 움직이는 세로로 긴 배경 그림

        RunState _run; // 런 전체 진행 정보 (지도, 현재 노드, 지나온 길, 골드 등)

        readonly List<int> _selectable = new(); // 지금 선택할 수 있는 노드 번호 목록
        bool _leaving; // 전투 씬으로 넘어가는 중인지 여부 -> true면 더 이상 노드를 누를 수 없음 (중복 클릭 방지)

        MapData Map => _run.Map; // 현재 런의 지도 (짧게 쓰기 위한 속성)

        void Start() // 지도 씬이 시작될 때 지도를 준비하고 화면을 그리는 함수
        {
            _run = RunState.Instance; // 런 정보를 가져오고
            EnsureMap(); // 지도가 없다면 새로 만듦
            _message.text = PromptText; // 기본 안내 문구를 띄우고
            _newRun.Clicked += StartNewRun; // 새 런 버튼을 누르면 StartNewRun이 실행되도록 연결
            _scroll.onValueChanged.AddListener(_ => SyncBackground()); // 스크롤할 때마다 배경 위치도 맞춰 움직이도록 연결
            Redraw(); // 지도 전체를 그림
        }

        void SyncBackground()
        // 스크롤 위치에 맞춰 세로형 배경을 움직이는 함수
        // -> 지도 맨 아래(출발 지점)를 볼 때는 배경 그림의 아래쪽, 맨 위(보스)를 볼 때는 배경 그림의 위쪽이 보이게 함
        {
            if (_scrollingBackground == null) // 배경이 지정되지 않았다면 할 일이 없음
                return;

            float travel = Mathf.Max(0f, _scrollingBackground.rect.height - ((RectTransform)transform).rect.height); // 배경이 움직일 수 있는 최대 거리 = 배경 높이 - 화면 높이 (배경이 화면보다 작으면 0)
            float t = Mathf.Clamp01(_scroll.verticalNormalizedPosition); // 현재 스크롤 위치 (0 = 맨 아래, 1 = 맨 위)
            _scrollingBackground.anchoredPosition = new Vector2(0f, -t * travel); // 위로 스크롤할수록 배경을 아래로 내림 -> 그림의 위쪽 부분이 화면에 들어옴
        }

        void EnsureMap() // 런에 지도가 없을 때만 새 지도를 만드는 함수 -> 이미 있다면 그대로 유지 (전투 후 돌아와도 같은 지도)
        {
            if (_run.Map == null) // 만약 아직 지도가 없다면
                _run.Map = MapGenerator.Generate(config, System.Environment.TickCount); // 현재 시각 값을 시드로 써서 매번 다른 지도를 만듦
        }

        void Redraw()
        // 지도 화면 전체를 처음부터 다시 그리는 함수 -> 지도 상태가 바뀔 때마다 호출
        {
            for (int i = _content.childCount - 1; i >= 0; i--) // 이전에 그린 노드와 연결선을 모두 삭제하고 (뒤에서부터 지움)
                Destroy(_content.GetChild(i).gameObject);

            Map.GetSelectable(_run.CurrentMapNode, _selectable); // 현재 위치에서 갈 수 있는 노드 목록을 새로 구함

            _content.sizeDelta = new Vector2(0f,
                Margin * 2f + Map.BossFloor * floorSpacing + floorSpacing * 0.25f + bossSize * 0.5f); // 스크롤 내용물의 높이를 지도 전체(위아래 여백 + 모든 층 + 보스 노드)가 들어가도록 맞춤

            // 노드가 연결선 위에 그려지도록 연결선을 먼저 생성함 (나중에 생성된 UI가 위에 보임)
            for (int i = 0; i < Map.nodes.Count; i++) // 모든 노드의
                foreach (int next in Map.nodes[i].next) // 모든 연결에 대해
                    DrawEdge(i, next); // 연결선을 그림

            for (int i = 0; i < Map.nodes.Count; i++) // 그다음 모든 노드를 그림
                DrawNode(i);

            RefreshStatus(); // 상단 상태 텍스트(층, 골드)를 갱신하고
            ScrollToCurrent(); // 현재 위치가 잘 보이도록 스크롤을 옮기고
            SyncBackground(); // 바뀐 스크롤 위치에 맞춰 배경도 맞춤
        }

        void RefreshStatus() // 상단 상태 텍스트(층, 골드)와 새 런 버튼 표시 여부를 갱신하는 함수
        {
            int current = _run.CurrentMapNode; // 플레이어가 지금 서 있는 노드 번호 (-1 = 아직 출발 전)
            string floor = current < 0
                ? "출발 전"
                : "층 " + (Map.nodes[current].floor + 1) + " / " + Map.floors; // 출발 전이면 "출발 전", 아니면 "층 현재/전체" (층 번호는 1부터 표시)
            _status.text = floor + "   ·   골드 " + _run.Gold; // 층 정보와 골드를 함께 표시

            bool atEnd = current >= 0 && _selectable.Count == 0; // 출발은 했는데 더 갈 곳이 없다면 -> 지도의 끝에 도달함
            _newRun.gameObject.SetActive(atEnd); // 지도 끝에서만 새 런 버튼을 보여 줌
            if (atEnd) // 만약 지도 끝이라면 끝 안내 문구를 띄움
                _message.text = MapEndText;
        }

        Vector2 PositionOf(MapNode node)
        // 노드가 화면(스크롤 내용물)에서 그려질 위치를 계산하는 함수 -> 가로는 가운데 기준, 세로는 아래에서부터 위로 쌓임
        {
            if (node.type == MapNodeType.Boss) // 만약 보스 노드라면 흔들림 없이 가로 가운데, 맨 위층보다 조금 더 위에 둠
                return new Vector2(0f, Margin + node.floor * floorSpacing + floorSpacing * 0.25f);

            float x = (node.column - (Map.columns - 1) * 0.5f) * columnSpacing + node.jitter.x * jitter; // 칸 번호를 가운데 기준으로 바꿔 가로 위치를 구하고 흔들림을 더함
            float y = Margin + node.floor * floorSpacing + node.jitter.y * jitter * 0.5f; // 층 번호로 세로 위치를 구하고 흔들림을 더함 (세로 흔들림은 절반만 -> 층 구분이 흐려지지 않게)
            return new Vector2(x, y);
        }

        void DrawEdge(int from, int to)
        // from 노드와 to 노드를 잇는 연결선 하나를 그리는 함수
        // -> 가로로 긴 막대 이미지를 두 노드 사이 길이만큼 늘리고, 두 노드를 향하도록 회전시킴
        {
            var a = PositionOf(Map.nodes[from]); // 시작 노드 위치
            var b = PositionOf(Map.nodes[to]);   // 끝 노드 위치
            var delta = b - a; // 시작 -> 끝 방향과 거리

            var line = Instantiate(_edgePrefab, _content); // 연결선 프리팹을 복제해서 내용물 아래에 생성하고
            line.name = "Edge_" + from + "_" + to; // 하이어라키에서 알아보기 쉽게 이름을 붙이고
            line.color = EdgeColor(from, to); // 상태에 맞는 색을 칠함

            var rect = line.rectTransform;
            rect.sizeDelta = new Vector2(delta.magnitude, edgeWidth); // 길이는 두 노드 사이 거리, 두께는 edgeWidth
            rect.anchoredPosition = a; // 시작 노드 위치에 놓고
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg); // 끝 노드 방향으로 회전시킴 (막대의 기준점이 왼쪽 끝이라 시작점에서 뻗어 나감)
        }

        Color EdgeColor(int from, int to)
        // 연결선의 상태에 따라 색을 정하는 함수 -> 지나온 길은 금색, 지금 갈 수 있는 길은 진하게, 나머지는 흐리게
        {
            var path = _run.MapPath; // 플레이어가 지금까지 지나온 노드 순서
            for (int i = 1; i < path.Count; i++) // 지나온 길의 연속된 두 노드를 검사해서
                if (path[i - 1] == from && path[i] == to) // 만약 이 연결선이 실제로 지나온 길이라면
                    return UiTheme.Gold; // 금색

            if (from == _run.CurrentMapNode && _selectable.Contains(to)) // 만약 현재 노드에서 다음으로 갈 수 있는 길이라면
                return WithAlpha(UiTheme.Ink, 0.6f); // 진한 색

            return WithAlpha(UiTheme.InkFaint, 0.55f); // 그 외의 길은 흐린 색
        }

        void DrawNode(int index)
        // index번 노드 하나를 그리는 함수 -> 상태(선택 가능/현재 위치/지나옴/아직 못 감)에 따라 후광·아이콘 색·투명도·버튼을 다르게 설정
        {
            var node = Map.nodes[index]; // 그릴 노드 데이터
            float size = node.type == MapNodeType.Boss ? bossSize : nodeSize; // 보스는 크게, 나머지는 기본 크기
            var accent = AccentOf(node.type); // 노드 종류별 강조 색

            bool selectable = !_leaving && _selectable.Contains(index); // 지금 누를 수 있는 노드인지 (씬 이동 중이면 전부 불가)
            bool current = index == _run.CurrentMapNode; // 플레이어가 지금 서 있는 노드인지
            bool visited = _run.MapPath.Contains(index); // 이미 지나온 노드인지

            var view = Instantiate(_nodePrefab, _content); // 노드 프리팹을 복제해서 생성하고
            view.name = "Node_" + index; // 하이어라키에서 알아보기 쉽게 이름을 붙이고
            view.Rect.anchoredPosition = PositionOf(node); // 계산한 위치에 놓음
            view.Rect.localScale = Vector3.one * (size / nodeSize); // 프리팹은 nodeSize 크기로 그려짐 -> 보스는 같은 노드를 배율만큼 키운 것

            view.halo.gameObject.SetActive(selectable || current); // 선택 가능하거나 현재 위치인 노드에만 후광을 켬
            if (selectable || current)
            {
                view.halo.color = current ? WithAlpha(UiTheme.Gold, 0.45f) : WithAlpha(accent, 0.22f); // 현재 위치는 금색 후광, 선택 가능한 노드는 종류별 색 후광
                if (selectable) // 만약 선택 가능한 노드라면
                    Fx.Breathe(view.halo.transform); // 후광이 숨 쉬듯 커졌다 작아지게 해서 누를 수 있다는 걸 알려 줌
            }

            view.icon.sprite = UiSprites.Get(IconOf(node.type)); // 노드 종류에 맞는 아이콘을 넣고
            view.icon.color = selectable ? accent
                : visited ? Color.Lerp(accent, UiTheme.InkMuted, 0.4f)
                : UiTheme.InkMuted; // 아이콘 색: 선택 가능 = 강조 색, 지나옴 = 강조 색을 조금 흐리게, 그 외 = 흐린 보조 색

            view.group.alpha = selectable || visited ? 1f : 0.6f; // 선택 가능하거나 지나온 노드는 선명하게, 나머지는 흐리게
            view.visited.SetActive(visited && !current); // 지나왔지만 지금 서 있지는 않은 노드에만 체크 표시

            view.button.enabled = selectable; // 선택 가능한 노드에서만 버튼을 켜고
            if (selectable) // 누르면 OnNodeClicked가 실행되도록 연결함
                view.button.Clicked += () => OnNodeClicked(index, view.button.transform);
        }

        void ScrollToCurrent()
        // 플레이어의 현재 위치가 화면 아래쪽(약 30% 높이)에 오도록 스크롤을 옮기는 함수 -> 앞으로 갈 길이 위쪽에 넓게 보임
        {
            Canvas.ForceUpdateCanvases(); // 방금 바꾼 내용물 크기가 바로 반영되도록 UI 레이아웃을 강제로 갱신

            float viewHeight = _scroll.viewport.rect.height; // 화면에 보이는 영역의 높이
            float contentHeight = _content.rect.height; // 지도 전체 높이
            if (contentHeight <= viewHeight) // 만약 지도 전체가 한 화면에 다 들어간다면 -> 스크롤할 필요 없음
            {
                _scroll.verticalNormalizedPosition = 0f; // 맨 아래로 맞추고 끝냄
                return;
            }

            int current = _run.CurrentMapNode; // 현재 노드 번호
            float focusY = current < 0 ? 0f : PositionOf(Map.nodes[current]).y; // 보여 줄 기준 높이 (출발 전이면 맨 아래)
            _scroll.verticalNormalizedPosition =
                Mathf.Clamp01((focusY - viewHeight * 0.3f) / (contentHeight - viewHeight)); // 기준 높이가 화면 30% 지점에 오도록 스크롤 위치(0~1)를 계산해서 적용
        }

        void OnNodeClicked(int index, Transform view)
        // 플레이어가 노드를 눌렀을 때 실행되는 함수
        // -> 전투/보스 노드면 해당 스테이지로 전투 씬을 불러오고, 상점 노드면 그냥 지나간 뒤 지도를 다시 그림
        {
            if (_leaving || !_selectable.Contains(index)) // 이미 씬 이동 중이거나 갈 수 없는 노드라면 무시
                return;

            var node = Map.nodes[index]; // 누른 노드 데이터
            _run.MapPath.Add(index); // 지나온 길에 추가함 -> 현재 위치가 이 노드로 바뀜
            UiFx.Punch(view); // 눌린 노드가 통 튀는 반응 연출

            if (node.type == MapNodeType.Battle || (node.type == MapNodeType.Boss && node.stage != null)) // 만약 전투 노드이거나, 스테이지가 연결된 보스 노드라면
            {
                _leaving = true; // 씬 이동 중으로 표시해서 추가 클릭을 막고
                _run.CurrentStage = node.stage; // 전투 씬에서 치를 스테이지를 런 정보에 넘겨주고
                StartCoroutine(LoadAfterDelay(config.battleScene)); // 잠시 기다린 뒤 전투 씬을 불러옴
                return;
            }

            // 상점(임시 노드)이나 스테이지가 없는 보스는 밟고 지나가며, 그 자리에서 계속 진행함
            _message.text = node.type == MapNodeType.Shop ? ShopPassText : MapEndText; // 노드 종류에 맞는 안내 문구를 띄우고
            Redraw(); // 바뀐 현재 위치로 지도를 다시 그림
        }

        IEnumerator LoadAfterDelay(string sceneName)
        // leaveDelay만큼 기다린 뒤 씬을 불러오는 것을 순서대로 진행하는 코루틴 -> 누른 반응 연출이 보일 시간을 줌
        {
            yield return new WaitForSecondsRealtime(leaveDelay); // 실제 시간 기준으로 기다리고 (게임 속도 조절의 영향을 받지 않음)
            SceneManager.LoadScene(sceneName); // 전투 씬을 불러옴
        }

        void StartNewRun() // 새 런 버튼을 눌렀을 때 런을 초기화하고 새 지도로 다시 시작하는 함수
        {
            _run.ResetRun(); // 런 정보(지도, 지나온 길, 골드 등)를 초기화하고
            EnsureMap(); // 지도가 비었으므로 새 지도를 만들고
            _message.text = PromptText; // 안내 문구를 기본으로 되돌리고
            Redraw(); // 새 지도를 그림
        }

        public static IconId IconOf(MapNodeType type) => type switch // 노드 종류에 맞는 아이콘을 돌려주는 함수
        {
            MapNodeType.Shop => IconId.Bag,   // 상점 = 가방
            MapNodeType.Boss => IconId.Crown, // 보스 = 왕관
            _ => IconId.Sword                 // 그 외(전투) = 칼
        };

        public static Color AccentOf(MapNodeType type) => type switch // 노드 종류에 맞는 강조 색을 돌려주는 함수
        {
            MapNodeType.Shop => UiTheme.Gold,           // 상점 = 금색
            MapNodeType.Boss => UiTheme.ArtifactAccent, // 보스 = 유물 강조 색
            _ => UiTheme.Deny                           // 그 외(전투) = 빨강
        };

        static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha); // 색은 그대로 두고 투명도만 바꾼 색을 돌려주는 함수
    }
}
