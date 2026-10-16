using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class EnemyMover : MonoBehaviour   // 적 턴에 적들을 움직이는 클래스
    // 적은 체스 행마법을 쓰지 않음 -> 매 턴 킹까지의 최단 경로를 따라 한 칸씩 다가옴
    // 경로 위의 아군 기물은 잡고 지나가며, 킹 칸에 들어서면 플레이어 패배. 적끼리는 공격하지 않고 막히면 기다림
    {
        [Tooltip("적이 킹을 향해 대각선으로 이동할 수 있다.")]
        public bool moveDiagonally = true;  // true면 8방향, false면 상하좌우 4방향으로만 이동

        static readonly Vector2Int[] Orthogonal =   // 상하좌우 4방향
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        static readonly Vector2Int[] AllEight = // 상하좌우 + 대각선 8방향
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        BoardGrid _grid;    // 보드 크기와 칸 좌표 변환용
        BoardState _state;  // 보드 위 기물 위치 데이터
        PieceRegistry _registry;    // 기물 데이터와 화면 오브젝트를 짝지어 관리하는 컴포넌트
        CombatResolver _combat; // 잡기를 처리하는 컴포넌트

        readonly List<Piece> _enemies = new();  // 이번 턴에 움직일 적 목록
        readonly Queue<Coord> _frontier = new();    // 최단 거리 탐색(BFS)에서 다음에 살펴볼 칸을 담는 대기열
        int[,] _dist;   // 각 칸에서 킹까지의 최단 거리(칸 수)를 저장하는 2차원 배열

        const int Unreachable = int.MaxValue;   // 킹까지 갈 수 없는 칸을 나타내는 값

        void Awake() => Resolve();  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
        }

        Vector2Int[] Directions => moveDiagonally ? AllEight : Orthogonal;  // 설정에 따라 사용할 이동 방향 목록

        public bool ResolveEnemyTurn()  // 모든 적을 한 번씩 움직이는 함수 -> 적이 킹 칸에 도달했다면 true 반환
        {
            Resolve();

            var king = _state.FindObjective(Team.Player);   // 플레이어 킹을 찾음
            if (king == null)   // 킹이 없다면 이미 진 것으로 보고 true 반환
                return true;

            BuildDistanceField(king.Coord); // 모든 칸에서 킹까지의 최단 거리를 계산

            _state.GetPiecesOf(Team.Enemy, _enemies);   // 적을 모두 모으고
            _enemies.Sort((a, b) => DistanceAt(a.Coord).CompareTo(DistanceAt(b.Coord)));
            // 킹에 가까운 적부터 처리 -> 앞줄이 먼저 비켜야 뒷줄이 전진할 수 있어서 대열이 막히지 않음

            for (int i = 0; i < _enemies.Count; i++)    // 적을 하나씩 한 칸 이동
            {
                if (StepEnemy(_enemies[i], i))  // 만약 이 적이 킹 칸에 들어갔다면 즉시 true 반환(패배)
                    return true;
            }

            return false;
        }

        void BuildDistanceField(Coord target)   // 킹 칸(target)에서 시작해 모든 칸까지의 최단 거리를 BFS(너비 우선 탐색)로 계산하는 함수
        // 적이 있는 칸은 막다른 곳 -> 그 적이 자기 거리를 알 수 있도록 거리는 기록하지만 그 칸을 거쳐서 더 퍼져 나가지는 않음
        {
            if (_dist == null || _dist.GetLength(0) != _grid.width || _dist.GetLength(1) != _grid.height)
            // 배열이 없거나 보드 크기가 바뀌었다면 새로 만듦
                _dist = new int[_grid.width, _grid.height];

            for (int y = 0; y < _grid.height; y++)  // 모든 칸의 거리를 "갈 수 없음"으로 초기화
            for (int x = 0; x < _grid.width; x++)
                _dist[x, y] = Unreachable;

            _frontier.Clear();
            _dist[target.x, target.y] = 0;  // 킹 칸의 거리는 0
            _frontier.Enqueue(target);  // 킹 칸부터 탐색 시작

            while (_frontier.Count > 0) // 살펴볼 칸이 남아 있는 동안 반복
            {
                var current = _frontier.Dequeue();  // 대기열에서 칸을 하나 꺼내고
                int next = _dist[current.x, current.y] + 1; // 이웃 칸의 거리는 현재 칸 + 1

                foreach (var dir in Directions) // 이동 가능한 방향의 이웃 칸을 하나씩 확인
                {
                    var neighbour = current + dir;
                    if (!_grid.IsInside(neighbour) || _dist[neighbour.x, neighbour.y] != Unreachable)
                    // 보드 밖이거나 이미 거리를 기록한 칸이라면 건너뜀
                        continue;

                    _dist[neighbour.x, neighbour.y] = next; // 이웃 칸의 거리를 기록

                    var occupant = _state.GetPiece(neighbour);
                    if (occupant != null && occupant.Team == Team.Enemy)
                    // 적이 있는 칸이라면 거리는 기록하되 이 칸을 거쳐 더 퍼져 나가지는 않음 -> 적끼리는 길을 막음
                        continue;

                    _frontier.Enqueue(neighbour);   // 다음에 이 칸에서 다시 퍼져 나가도록 대기열에 추가
                }
            }
        }

        int DistanceAt(Coord c) => _grid.IsInside(c) ? _dist[c.x, c.y] : Unreachable; // 칸의 킹까지 거리를 돌려주는 함수(보드 밖이면 갈 수 없음)

        bool StepEnemy(Piece enemy, int order)  // 적 하나를 킹 쪽으로 한 칸 움직이는 함수 -> 킹 칸에 들어갔다면 true 반환
        // order는 이번 턴에 몇 번째로 움직이는 적인지 -> 이동 연출에 시차를 줌
        {
            var best = Coord.Invalid;   // 이동할 가장 좋은 칸(아직 없음)
            int bestDistance = DistanceAt(enemy.Coord); // 지금 칸의 거리 -> 이보다 가까운 칸만 후보가 됨

            foreach (var dir in Directions) // 이동 가능한 방향의 이웃 칸을 하나씩 확인
            {
                var candidate = enemy.Coord + dir;
                if (!_grid.IsInside(candidate)) // 보드 밖이라면 건너뜀
                    continue;

                int distance = DistanceAt(candidate);
                if (distance >= bestDistance)   // 지금까지 찾은 칸보다 킹에 가깝지 않다면 건너뜀
                    continue;

                var occupant = _state.GetPiece(candidate);
                if (occupant != null && occupant.Team == Team.Enemy)    // 다른 적이 있는 칸이라면 건너뜀 -> 적끼리는 공격하지 않고 기다리기만 함
                    continue;

                best = candidate;   // 더 가까운 칸을 찾았으므로 기록
                bestDistance = distance;
            }

            if (!best.IsValid)  // 만약 다가갈 칸이 없다면(갇힌 상태) -> 옆에 있는 아군 기물을 공격하거나, 없으면 그냥 대기
            {
                AttackAdjacentPlayerPiece(enemy);
                return false;
            }

            var target = _state.GetPiece(best); // 이동할 칸에 있는 기물
            if (target != null && target.Team == Team.Player)   // 만약 이동할 칸에 아군 기물이 있다면
            {
                if (target.IsObjective) // 그 기물이 킹이라면 -> 킹 칸으로 들어가고 true 반환(플레이어 패배)
                {
                    StepInto(enemy, best, order);
                    return true;
                }

                _combat.CaptureInto(enemy, target, best);
                // 킹이 아니라면 잡고 그 칸을 차지함 -> 잡기와 이동을 하나의 동작으로 처리
                return false;
            }

            StepInto(enemy, best, order);   // 빈 칸이라면 그냥 한 칸 이동
            return false;
        }

        void AttackAdjacentPlayerPiece(Piece enemy) // 갇힌 적이 바로 옆의 아군 기물 하나를 제자리에서 공격하는 함수
        {
            foreach (var dir in Directions) // 이웃 칸을 하나씩 확인해서
            {
                var neighbour = enemy.Coord + dir;
                if (!_grid.IsInside(neighbour)) // 보드 밖은 건너뜀
                    continue;

                var occupant = _state.GetPiece(neighbour);
                if (occupant == null || occupant.Team != Team.Player || occupant.IsObjective)
                // 기물이 없거나, 아군이 아니거나, 킹이라면 건너뜀
                    continue;

                _combat.Capture(enemy, occupant);   // 처음 찾은 아군 기물을 잡고(적은 제자리에 남음) 종료
                return;
            }
        }

        void StepInto(Piece enemy, Coord destination, int order)    // 적을 목적지 칸으로 옮기는 함수
        {
            _state.Move(enemy, destination);    // 보드 데이터에서 옮기고

            var view = _registry.GetView(enemy);
            if (view != null)   // 화면 오브젝트가 있다면 미끄러지듯 이동하는 연출(order만큼 늦게 시작)
                Fx.StepTo(view.transform, _grid.CoordToWorld(destination), order);
        }
    }
}
