using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.Feel;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>Drops a wave of enemies onto free squares along the top of the board.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Tooltip("Ranks from the top edge that enemies may spawn on.")]
        public int spawnRows = 2;

        BoardGrid _grid;
        BoardState _state;
        PieceRegistry _registry;

        readonly List<Coord> _freeCells = new();

        void Awake() => Resolve();

        void Resolve()
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
        }

        public int Spawn(WaveDefinition wave)
        {
            Resolve();

            if (wave == null || wave.enemy == null || wave.count <= 0)
                return 0;

            CollectFreeSpawnCells();
            if (_freeCells.Count == 0)
                return 0;

            int spawned = 0;
            int wanted = Mathf.Min(wave.count, _freeCells.Count);

            for (int i = 0; i < wanted; i++)
            {
                int pick = Random.Range(i, _freeCells.Count);
                (_freeCells[i], _freeCells[pick]) = (_freeCells[pick], _freeCells[i]);

                var view = _registry.Spawn(wave.enemy, Team.Enemy, _freeCells[i]);
                if (view == null)
                    continue;

                Fx.SpawnDrop(view.transform, _grid.CoordToWorld(_freeCells[i]), transform, spawned);
                spawned++;
            }

            return spawned;
        }

        void CollectFreeSpawnCells()
        {
            _freeCells.Clear();

            int rows = Mathf.Clamp(spawnRows, 1, _grid.height);
            for (int y = _grid.height - rows; y < _grid.height; y++)
            for (int x = 0; x < _grid.width; x++)
            {
                var coord = new Coord(x, y);
                if (_state.IsEmpty(coord))
                    _freeCells.Add(coord);
            }
        }
    }
}
