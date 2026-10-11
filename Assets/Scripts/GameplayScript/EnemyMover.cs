using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 적 턴 로직이다. 적은 체스 행마법을 쓰지 않는다. 각 적은 킹까지의 최단 경로를 따라
    /// 한 칸 이동하고, 그 경로를 막는 아군 기물을 제거하며, 킹의 칸에 들어서면 승리한다.
    /// </summary>
    public class EnemyMover : MonoBehaviour
    {
        [Tooltip("적이 킹을 향해 대각선으로 이동할 수 있다.")]
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

        /// <summary>모든 적을 한 번씩 이동시킨다. 킹의 칸에 도달했으면 true를 반환한다.</summary>
        public bool ResolveEnemyTurn()
        {
            Resolve();

            var king = _state.FindObjective(Team.Player);
            if (king == null)
                return true;

            BuildDistanceField(king.Coord);

            _state.GetPiecesOf(Team.Enemy, _enemies);
            // 가까운 적부터 처리한다. 앞줄이 먼저 칸을 비워야 뒷줄이 전진할 수 있어서
            // 대열이 스스로 정체되지 않는다.
            _enemies.Sort((a, b) => DistanceAt(a.Coord).CompareTo(DistanceAt(b.Coord)));

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (StepEnemy(_enemies[i], i))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 킹까지의 너비 우선 탐색 거리다. 적이 차지한 칸은 막다른 곳이다.
        /// 그 칸에 있는 적이 자기 위치를 알 수 있도록 거리는 기록하지만,
        /// 탐색 경로는 그 칸을 통과하지 않는다.
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
                        continue;   // 기록은 하지만 이 칸을 거쳐 확장하지 않는다

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
                    continue;   // 같은 적은 절대 공격하지 않고 기다리기만 한다

                best = candidate;
                bestDistance = distance;
            }

            if (!best.IsValid)
            {
                // 갇힌 상태다. 가능하면 인접한 기물을 공격하고, 아니면 대기한다.
                AttackAdjacentPlayerPiece(enemy);
                return false;
            }

            var target = _state.GetPiece(best);
            if (target != null && target.Team == Team.Player)
            {
                if (target.IsObjective)
                {
                    StepInto(enemy, best, order);
                    return true;        // 킹에 도달했다. 런은 패배한다
                }

                // 잡기는 위치 기반이다. 기물을 잡고 그 칸을 차지하는 동작을
                // 서로 경합하는 두 동작이 아니라 하나의 동작으로 처리한다.
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
