using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Map
{
    public enum MapNodeType
    {
        Battle,
        /// <summary>임시 노드다. 아직 아무 일 없이 지나간다.</summary>
        Shop,
        /// <summary>임시 노드다. 지도의 끝이며 아직 보스전은 없다.</summary>
        Boss
    }

    /// <summary>런 지도의 지점 하나다. 연결은 항상 위쪽으로, 한 층씩만 이어진다.</summary>
    [Serializable]
    public class MapNode
    {
        public int floor;
        public int column;
        public MapNodeType type;

        [Tooltip("이 노드에서 치르는 전투다. 전투 노드에서만 쓴다.")]
        public StageDefinition stage;

        [Tooltip("지도가 격자처럼 보이지 않도록 주는 [-1, 1] 범위의 작은 무작위 오프셋이다.")]
        public Vector2 jitter;

        public List<int> next = new();
    }

    /// <summary>
    /// 런 하나의 지도 전체다. 스테이지 참조 외에는 Unity 오브젝트가 없는 순수 데이터라서
    /// RunState 안에서 씬을 불러와도 유지된다.
    /// </summary>
    [Serializable]
    public class MapData
    {
        public int seed;
        /// <summary>보스 층을 포함한 층 수다.</summary>
        public int floors;
        public int columns;
        public List<MapNode> nodes = new();

        public int BossFloor => floors - 1;

        /// <summary>
        /// <paramref name="current"/>에서 플레이어가 갈 수 있는 곳을 구한다. 아직 아무 곳에도
        /// 들어가지 않았다면 첫 층이고, 그 뒤로는 현재 노드와 연결된 노드들이다.
        /// </summary>
        public void GetSelectable(int current, List<int> results)
        {
            results.Clear();

            if (current < 0 || current >= nodes.Count)
            {
                for (int i = 0; i < nodes.Count; i++)
                    if (nodes[i].floor == 0)
                        results.Add(i);
                return;
            }

            results.AddRange(nodes[current].next);
        }
    }
}
