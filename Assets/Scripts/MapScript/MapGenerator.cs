using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Map
{
    // 런 지도(갈래 길)를 무작위로 만들어 주는 클래스 (Slay the Spire 방식)
    // -> 격자 위로 여러 경로를 아래층에서 위층으로 올려 보내고, 한 걸음에 옆으로 최대 한 칸만 움직임
    // -> 이미 그려진 연결선과는 교차하지 않게 해서 지도가 꼬여 보이지 않도록 함
    // -> 경로가 같은 칸을 지나면 합쳐지고, 갈라지면 플레이어에게 선택지가 생김
    // -> 맨 위층은 보스 노드 하나로 마무리함
    public static class MapGenerator
    {
        public static MapData Generate(MapConfig config, int seed)
        // 지도 설정(config)과 시드 값으로 지도 전체를 만들어 돌려주는 함수
        // -> 같은 시드를 넣으면 항상 같은 지도가 나옴 (씬을 다시 불러와도 지도가 바뀌지 않음)
        {
            var rng = new System.Random(seed); // 시드 기반 난수 생성기 -> 지도 생성에 쓰는 모든 무작위 값은 여기서 뽑음

            int floors = Mathf.Max(2, config.floors);   // 보스 층을 뺀 일반 층 수 (최소 2층)
            int columns = Mathf.Max(2, config.columns); // 한 층의 가로 칸 수 (최소 2칸)
            int paths = Mathf.Max(1, config.paths);     // 아래에서 위로 올려 보낼 경로의 수 (최소 1개)

            var map = new MapData { seed = seed, floors = floors + 1, columns = columns }; // 만들어질 지도 -> 층 수는 보스 층까지 포함해서 +1
            var lookup = new Dictionary<(int floor, int column), int>(); // (층, 칸) 위치 -> 그 위치에 있는 노드 번호를 찾는 표
            var edges = new List<(int floor, int from, int to)>(); // 지금까지 그린 연결선 목록 (출발 층, 출발 칸, 도착 칸) -> 교차 검사에 사용

            int NodeAt(int floor, int column)
            // (층, 칸) 위치의 노드 번호를 돌려주는 함수 -> 아직 노드가 없다면 새로 만들어서 돌려줌
            // -> 두 경로가 같은 칸을 지나면 같은 노드를 공유하게 되어 경로가 자연스럽게 합쳐짐
            {
                if (lookup.TryGetValue((floor, column), out int index)) // 만약 그 위치에 이미 노드가 있다면
                    return index; // 기존 노드 번호를 그대로 돌려줌

                index = map.nodes.Count; // 새 노드의 번호는 현재 노드 개수 (리스트 맨 뒤에 추가되므로)
                map.nodes.Add(new MapNode { floor = floor, column = column }); // 새 노드를 만들어 지도에 추가하고
                lookup[(floor, column)] = index; // 다음에 같은 위치를 찾을 수 있도록 표에 기록함
                return index;
            }

            int firstStart = -1; // 첫 번째 경로가 출발한 칸 (두 번째 경로가 겹치지 않게 하기 위해 기억)
            for (int p = 0; p < paths; p++) // 경로 수만큼 반복해서 아래층부터 위층까지 길을 하나씩 그림
            {
                int column = rng.Next(columns); // 이번 경로가 출발할 칸을 무작위로 고름

                // 처음 두 경로는 서로 다른 칸에서 출발시킴 -> 시작부터 항상 선택지가 2개 이상 생김
                if (p == 0) // 첫 번째 경로라면 출발 칸을 기억하고
                    firstStart = column;
                else if (p == 1) // 두 번째 경로라면 첫 번째와 다른 칸이 나올 때까지 다시 뽑음
                    while (column == firstStart)
                        column = rng.Next(columns);

                int from = NodeAt(0, column); // 0층(맨 아래층)의 출발 노드
                for (int floor = 0; floor < floors - 1; floor++) // 한 층씩 위로 올라가면서
                {
                    int nextColumn = PickNextColumn(floor, column, columns, edges, rng); // 다음 층에서 갈 칸을 고르고 (왼쪽/직진/오른쪽 중 교차 안 하는 쪽)
                    edges.Add((floor, column, nextColumn)); // 방금 그은 연결선을 기록함 -> 이후 경로들이 이 선과 교차하지 않도록

                    int to = NodeAt(floor + 1, nextColumn); // 다음 층의 도착 노드 (없으면 새로 만듦)
                    if (!map.nodes[from].next.Contains(to)) // 만약 아직 연결되지 않은 노드라면 -> 같은 연결이 중복으로 들어가는 것을 방지
                        map.nodes[from].next.Add(to); // 출발 노드에서 도착 노드로 갈 수 있도록 연결함

                    from = to; // 도착 노드가 다음 걸음의 출발 노드가 됨
                    column = nextColumn;
                }
            }

            int boss = map.nodes.Count; // 보스 노드의 번호 (지금 리스트 맨 뒤에 추가될 예정)
            map.nodes.Add(new MapNode
            {
                floor = floors, column = columns / 2, type = MapNodeType.Boss, stage = config.bossStage
            }); // 맨 위층 가운데 칸에 보스 노드를 하나 만들고 보스전 스테이지를 연결함
            for (int i = 0; i < boss; i++) // 보스 노드를 제외한 모든 노드를 검사해서
                if (map.nodes[i].floor == floors - 1) // 만약 보스 바로 아래층(마지막 일반 층) 노드라면
                    map.nodes[i].next.Add(boss); // 보스 노드로 연결함 -> 모든 경로가 보스에서 끝남

            AssignTypes(map, config, rng); // 각 노드를 전투/상점 중 하나로 정함

            foreach (var node in map.nodes) // 모든 노드를 마무리 정리
            {
                node.next.Sort((a, b) => map.nodes[a].column.CompareTo(map.nodes[b].column)); // 연결된 다음 노드들을 왼쪽 칸부터 순서대로 정렬
                if (node.type != MapNodeType.Boss) // 보스가 아닌 노드라면
                    node.jitter = new Vector2(Signed(rng), Signed(rng)); // 위치를 살짝 흔들 오프셋을 줌 -> 지도가 딱딱한 격자처럼 보이지 않게 함
            }

            return map; // 완성된 지도를 돌려줌
        }

        static int PickNextColumn(int floor, int column, int columns,
            List<(int floor, int from, int to)> edges, System.Random rng)
        // 다음 층에서 갈 칸을 고르는 함수
        // -> 왼쪽 한 칸, 직진, 오른쪽 한 칸 중 기존 연결선과 교차하지 않는 쪽을 무작위로 고름
        {
            var options = new List<int>(3); // 후보 칸 목록 (최대 3개)
            for (int dx = -1; dx <= 1; dx++) // 왼쪽(-1), 직진(0), 오른쪽(+1)을 차례로 검사
            {
                int c = column + dx; // 후보 칸 번호
                if (c >= 0 && c < columns) // 만약 지도 가로 범위 안의 칸이라면
                    options.Add(c); // 후보에 추가함 (지도 밖으로 나가는 칸은 제외)
            }

            for (int i = options.Count - 1; i > 0; i--) // 후보 순서를 무작위로 섞음 (피셔-예이츠 셔플)
            {
                int j = rng.Next(i + 1); // 0 ~ i 사이 무작위 위치를 골라
                (options[i], options[j]) = (options[j], options[i]); // 두 후보의 자리를 바꿈
            }

            foreach (int candidate in options) // 섞인 순서대로 후보를 검사해서
                if (!Crosses(floor, column, candidate, edges)) // 만약 기존 연결선과 교차하지 않는다면
                    return candidate; // 그 칸을 선택함

            // 모든 후보가 교차한다면 곧장 위로 감
            // -> 직진은 이웃 연결선과 절대 교차하지 않으므로 이 선택지는 항상 안전함
            return column;
        }

        static bool Crosses(int floor, int from, int to, List<(int floor, int from, int to)> edges)
        // floor층의 from칸 -> 다음 층 to칸 연결선이 이미 그린 연결선과 X자로 교차하는지 검사하는 함수
        {
            foreach (var edge in edges) // 지금까지 그린 모든 연결선을 검사
            {
                if (edge.floor != floor) // 다른 층 사이의 연결선이라면 교차할 일이 없으므로 건너뜀
                    continue;

                // 만약 기존 선이 내 왼쪽에서 출발해 내 오른쪽에 도착하거나, 그 반대라면 -> 두 선이 X자로 교차함
                if ((edge.from < from && edge.to > to) || (edge.from > from && edge.to < to))
                    return true; // 교차함
            }
            return false; // 교차하는 선이 하나도 없음
        }

        static void AssignTypes(MapData map, MapConfig config, System.Random rng)
        // 각 노드의 종류(전투/상점)를 정하는 함수
        // -> firstShopFloor층보다 아래는 항상 전투, 그 위층의 노드는 shopChance 확률로 상점이 될 수 있음
        // -> 단, 같은 경로에서 상점 바로 다음에 또 상점이 오지는 않게 함
        {
            var parents = new List<int>[map.nodes.Count]; // 노드마다 "자신으로 연결되어 들어오는 아래층 노드" 목록
            for (int i = 0; i < map.nodes.Count; i++) // 모든 노드의 부모 목록을 빈 리스트로 만들고
                parents[i] = new List<int>();
            for (int i = 0; i < map.nodes.Count; i++) // 모든 연결(i -> child)을 거꾸로 기록함
                foreach (int child in map.nodes[i].next)
                    parents[child].Add(i); // child의 부모는 i

            var order = new List<int>(); // 노드를 처리할 순서
            for (int i = 0; i < map.nodes.Count; i++)
                order.Add(i);
            order.Sort((a, b) => map.nodes[a].floor.CompareTo(map.nodes[b].floor)); // 아래층부터 처리하도록 층 순서로 정렬 -> 부모의 종류가 먼저 정해져 있어야 연속 상점을 막을 수 있음

            foreach (int i in order) // 아래층 노드부터 차례로
            {
                var node = map.nodes[i]; // 이번에 종류를 정할 노드
                if (node.type == MapNodeType.Boss) // 보스 노드는 이미 종류가 정해져 있으므로 건너뜀
                    continue;

                bool parentIsShop = parents[i].Exists(p => map.nodes[p].type == MapNodeType.Shop); // 바로 아래층 부모 중 상점이 하나라도 있는지
                bool shop = node.floor >= config.firstShopFloor // 상점이 나올 수 있는 층 이상이고
                            && !parentIsShop // 부모 중 상점이 없고 (연속 상점 방지)
                            && rng.NextDouble() < config.shopChance; // 확률 검사를 통과했다면 -> 상점

                node.type = shop ? MapNodeType.Shop : MapNodeType.Battle; // 상점 또는 전투로 종류를 정하고
                if (!shop) // 만약 전투 노드라면
                    node.stage = PickStage(config, rng); // 치를 전투 스테이지를 무작위로 골라 연결함
            }
        }

        static StageDefinition PickStage(MapConfig config, System.Random rng)
        // 전투 스테이지 목록(battleStages)에서 하나를 무작위로 고르는 함수
        {
            var pool = config.battleStages; // 고를 수 있는 전투 스테이지 목록
            if (pool == null || pool.Length == 0) // 만약 목록이 비어 있다면 -> 고를 스테이지가 없음
                return null;

            return pool[rng.Next(pool.Length)]; // 목록 중 하나를 무작위로 돌려줌
        }

        static float Signed(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0); // -1 ~ 1 사이의 무작위 실수를 돌려주는 함수 (노드 흔들림 오프셋용)
    }
}
