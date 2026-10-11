using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Map
{
    /// <summary>
    /// Slay the Spire 방식으로 갈래 지도를 만든다. 격자 위로 여러 경로를 올려 보내되,
    /// 한 걸음에 옆으로 최대 한 칸만 움직이고 이미 그려진 연결선과는 교차하지 않는다.
    /// 경로가 같은 칸을 지나면 합쳐지고, 갈라지면 플레이어에게 선택지가 생긴다.
    /// 맨 위는 보스 노드 하나로 마무리한다.
    /// </summary>
    public static class MapGenerator
    {
        public static MapData Generate(MapConfig config, int seed)
        {
            var rng = new System.Random(seed);

            int floors = Mathf.Max(2, config.floors);
            int columns = Mathf.Max(2, config.columns);
            int paths = Mathf.Max(1, config.paths);

            var map = new MapData { seed = seed, floors = floors + 1, columns = columns };
            var lookup = new Dictionary<(int floor, int column), int>();
            var edges = new List<(int floor, int from, int to)>();

            int NodeAt(int floor, int column)
            {
                if (lookup.TryGetValue((floor, column), out int index))
                    return index;

                index = map.nodes.Count;
                map.nodes.Add(new MapNode { floor = floor, column = column });
                lookup[(floor, column)] = index;
                return index;
            }

            int firstStart = -1;
            for (int p = 0; p < paths; p++)
            {
                int column = rng.Next(columns);

                // 처음 두 경로는 서로 다른 칸에서 출발시켜 시작부터 항상 선택지가 있게 한다.
                if (p == 0)
                    firstStart = column;
                else if (p == 1)
                    while (column == firstStart)
                        column = rng.Next(columns);

                int from = NodeAt(0, column);
                for (int floor = 0; floor < floors - 1; floor++)
                {
                    int nextColumn = PickNextColumn(floor, column, columns, edges, rng);
                    edges.Add((floor, column, nextColumn));

                    int to = NodeAt(floor + 1, nextColumn);
                    if (!map.nodes[from].next.Contains(to))
                        map.nodes[from].next.Add(to);

                    from = to;
                    column = nextColumn;
                }
            }

            int boss = map.nodes.Count;
            map.nodes.Add(new MapNode
            {
                floor = floors, column = columns / 2, type = MapNodeType.Boss, stage = config.bossStage
            });
            for (int i = 0; i < boss; i++)
                if (map.nodes[i].floor == floors - 1)
                    map.nodes[i].next.Add(boss);

            AssignTypes(map, config, rng);

            foreach (var node in map.nodes)
            {
                node.next.Sort((a, b) => map.nodes[a].column.CompareTo(map.nodes[b].column));
                if (node.type != MapNodeType.Boss)
                    node.jitter = new Vector2(Signed(rng), Signed(rng));
            }

            return map;
        }

        /// <summary>왼쪽 한 칸, 직진, 오른쪽 한 칸 중 기존 연결선과 교차하지 않는 쪽을 고른다.</summary>
        static int PickNextColumn(int floor, int column, int columns,
            List<(int floor, int from, int to)> edges, System.Random rng)
        {
            var options = new List<int>(3);
            for (int dx = -1; dx <= 1; dx++)
            {
                int c = column + dx;
                if (c >= 0 && c < columns)
                    options.Add(c);
            }

            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (options[i], options[j]) = (options[j], options[i]);
            }

            foreach (int candidate in options)
                if (!Crosses(floor, column, candidate, edges))
                    return candidate;

            // 곧장 위로 가면 이웃 연결선과 절대 교차하지 않으므로 이 선택지는 항상 존재한다.
            return column;
        }

        static bool Crosses(int floor, int from, int to, List<(int floor, int from, int to)> edges)
        {
            foreach (var edge in edges)
            {
                if (edge.floor != floor)
                    continue;

                if ((edge.from < from && edge.to > to) || (edge.from > from && edge.to < to))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 첫 층은 항상 전투다. 그 위층의 노드는 상점이 될 수 있지만,
        /// 같은 경로에서 상점 바로 다음에 또 상점이 오지는 않는다.
        /// </summary>
        static void AssignTypes(MapData map, MapConfig config, System.Random rng)
        {
            var parents = new List<int>[map.nodes.Count];
            for (int i = 0; i < map.nodes.Count; i++)
                parents[i] = new List<int>();
            for (int i = 0; i < map.nodes.Count; i++)
                foreach (int child in map.nodes[i].next)
                    parents[child].Add(i);

            var order = new List<int>();
            for (int i = 0; i < map.nodes.Count; i++)
                order.Add(i);
            order.Sort((a, b) => map.nodes[a].floor.CompareTo(map.nodes[b].floor));

            foreach (int i in order)
            {
                var node = map.nodes[i];
                if (node.type == MapNodeType.Boss)
                    continue;

                bool parentIsShop = parents[i].Exists(p => map.nodes[p].type == MapNodeType.Shop);
                bool shop = node.floor >= config.firstShopFloor
                            && !parentIsShop
                            && rng.NextDouble() < config.shopChance;

                node.type = shop ? MapNodeType.Shop : MapNodeType.Battle;
                if (!shop)
                    node.stage = PickStage(config, rng);
            }
        }

        static StageDefinition PickStage(MapConfig config, System.Random rng)
        {
            var pool = config.battleStages;
            if (pool == null || pool.Length == 0)
                return null;

            return pool[rng.Next(pool.Length)];
        }

        static float Signed(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);
    }
}
