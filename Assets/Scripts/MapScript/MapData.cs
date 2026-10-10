using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Map
{
    public enum MapNodeType
    {
        Battle,
        /// <summary>Placeholder: passed through without doing anything yet.</summary>
        Shop,
        /// <summary>Placeholder: the end of the map, no boss fight yet.</summary>
        Boss
    }

    /// <summary>One stop on the run map. Edges only point upward, one floor at a time.</summary>
    [Serializable]
    public class MapNode
    {
        public int floor;
        public int column;
        public MapNodeType type;

        [Tooltip("The battle fought here. Battle nodes only.")]
        public StageDefinition stage;

        [Tooltip("Small random offset in [-1, 1] so the map does not read as a grid.")]
        public Vector2 jitter;

        public List<int> next = new();
    }

    /// <summary>
    /// The whole map for one run: plain data, no Unity objects besides the stage
    /// references, so it survives scene loads inside RunState.
    /// </summary>
    [Serializable]
    public class MapData
    {
        public int seed;
        /// <summary>Floor count including the boss floor.</summary>
        public int floors;
        public int columns;
        public List<MapNode> nodes = new();

        public int BossFloor => floors - 1;

        /// <summary>
        /// Where the player may go from <paramref name="current"/>: the first floor
        /// before anything has been entered, the nodes it links to afterwards.
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
