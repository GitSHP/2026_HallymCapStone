using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Feel;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Enemy turn logic. Enemies do not use chess movement — each one walks a
    /// single step along the shortest route to the king, clears player pieces
    /// blocking that route, and wins by stepping onto the king's tile.
    /// </summary>
    public class EnemyMover : MonoBehaviour
    {
        [Tooltip("Enemies may step diagonally toward the king.")]
        public bool moveDiagonally = true;

        static readonly Vector2Int[] Orthogonal =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        static readonly Vector2Int[] AllEight =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        BoardGrid _grid;
        BoardState _state;
        PieceRegistry _registry;
        CombatResolver _combat;

        readonly List<Piece> _enemies = new();
        readonly Queue<Coord> _frontier = new();
        int[,] _dist;

        const int Unreachable = int.MaxValue;

        void Awake() => Resolve();

        void Resolve()
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
        }

        Vector2Int[] Directions => moveDiagonally ? AllEight : Orthogonal;

        /// <summary>Moves every enemy once. Returns true if the king's tile was reached.</summary>
        public bool ResolveEnemyTurn()
        {
            Resolve();

            var king = _state.FindObjective(Team.Player);
            if (king == null)
                return true;

            BuildDistanceField(king.Coord);

            _state.GetPiecesOf(Team.Enemy, _enemies);
            // Closest first: the front rank vacates its square before the rank
            // behind tries to advance, so a column does not freeze itself.
            _enemies.Sort((a, b) => DistanceAt(a.Coord).CompareTo(DistanceAt(b.Coord)));

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (StepEnemy(_enemies[i], i))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Breadth-first distance to the king. Enemy-held squares are dead ends —
        /// they get a distance so their occupant knows where it stands, but the
        /// search never routes through them.
        /// </summary>
        void BuildDistanceField(Coord target)
        {
            if (_dist == null || _dist.GetLength(0) != _grid.width || _dist.GetLength(1) != _grid.height)
                _dist = new int[_grid.width, _grid.height];

            for (int y = 0; y < _grid.height; y++)
            for (int x = 0; x < _grid.width; x++)
                _dist[x, y] = Unreachable;

            _frontier.Clear();
            _dist[target.x, target.y] = 0;
            _frontier.Enqueue(target);

            while (_frontier.Count > 0)
            {
                var current = _frontier.Dequeue();
                int next = _dist[current.x, current.y] + 1;

                foreach (var dir in Directions)
                {
                    var neighbour = current + dir;
                    if (!_grid.IsInside(neighbour) || _dist[neighbour.x, neighbour.y] != Unreachable)
                        continue;

                    _dist[neighbour.x, neighbour.y] = next;

                    var occupant = _state.GetPiece(neighbour);
                    if (occupant != null && occupant.Team == Team.Enemy)
                        continue;   // recorded, but not expanded through

                    _frontier.Enqueue(neighbour);
                }
            }
        }

        int DistanceAt(Coord c) => _grid.IsInside(c) ? _dist[c.x, c.y] : Unreachable;

        bool StepEnemy(Piece enemy, int order)
        {
            var best = Coord.Invalid;
            int bestDistance = DistanceAt(enemy.Coord);

            foreach (var dir in Directions)
            {
                var candidate = enemy.Coord + dir;
                if (!_grid.IsInside(candidate))
                    continue;

                int distance = DistanceAt(candidate);
                if (distance >= bestDistance)
                    continue;

                var occupant = _state.GetPiece(candidate);
                if (occupant != null && occupant.Team == Team.Enemy)
                    continue;   // fellow enemies are never attacked, only waited on

                best = candidate;
                bestDistance = distance;
            }

            if (!best.IsValid)
            {
                // Boxed in: hit something adjacent if possible, otherwise hold.
                AttackAdjacentPlayerPiece(enemy);
                return false;
            }

            var target = _state.GetPiece(best);
            if (target != null && target.Team == Team.Player)
            {
                if (target.IsObjective)
                {
                    StepInto(enemy, best, order);
                    return true;        // reached the king — the run is lost
                }

                // Capture is positional: take the piece and occupy its square,
                // as one motion rather than two competing ones.
                _combat.CaptureInto(enemy, target, best);
                return false;
            }

            StepInto(enemy, best, order);
            return false;
        }

        void AttackAdjacentPlayerPiece(Piece enemy)
        {
            foreach (var dir in Directions)
            {
                var neighbour = enemy.Coord + dir;
                if (!_grid.IsInside(neighbour))
                    continue;

                var occupant = _state.GetPiece(neighbour);
                if (occupant == null || occupant.Team != Team.Player || occupant.IsObjective)
                    continue;

                _combat.Capture(enemy, occupant);
                return;
            }
        }

        void StepInto(Piece enemy, Coord destination, int order)
        {
            _state.Move(enemy, destination);

            var view = _registry.GetView(enemy);
            if (view != null)
                Fx.StepTo(view.transform, _grid.CoordToWorld(destination), order);
        }
    }
}
