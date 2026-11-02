using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class WaveSpawner : MonoBehaviour  // 웨이브(정해진 턴에 한꺼번에 나타나는 적 무리)를 보드 위쪽 줄에 소환하는 클래스
    {
        [Tooltip("적이 생성될 수 있는, 위쪽 가장자리부터의 줄 수.")]
        public event System.Action<PieceDefinition> EnemySpawned;   // 적이 하나 소환될 때마다 발생하는 이벤트 -> 기물 가이드가 처음 보는 적(중갑병)의 설명을 띄움

        public int spawnRows = 2;   // 적이 나타날 수 있는 위쪽 줄 수

        BoardGrid _grid;    // 보드 크기와 칸 좌표 변환용
        BoardState _state;  // 칸이 비어 있는지 확인하는 보드 데이터
        PieceRegistry _registry;    // 적 기물을 생성하는 컴포넌트

        readonly List<Coord> _freeCells = new();    // 적을 놓을 수 있는 빈 칸 목록(재사용)

        void Awake()  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠
        {
            Resolve();
        }

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
        }

        public int Spawn(WaveDefinition wave, int extra = 0, int stageNumber = 1)   // 웨이브 하나의 적들을 위쪽 빈 칸에 무작위로 소환하는 함수 -> 실제로 소환한 수를 반환
        // extra: 런 진행(난이도 상승)으로 웨이브에 더할 적 수 -> 웨이브 데이터(에셋)는 바꾸지 않고 소환할 때만 더함
        // stageNumber: 지금 몇 번째 스테이지인지 (1부터) -> 섞여 나오는 적(중갑병)이 등장할 스테이지인지 판단
        {
            Resolve();
            if (wave == null)
                return 0;

            CollectFreeSpawnCells();    // 소환 가능한 빈 칸을 모으고 -> 같은 웨이브의 적들이 같은 칸을 고르지 않도록 한 번만 모음
            int used = 0;   // 지금까지 쓴 빈 칸 수 (목록 앞쪽부터 씀)
            int spawned = 0;    // 실제로 소환한 적의 수

            spawned += SpawnSome(wave.enemy, wave.count + Mathf.Max(0, extra), ref used, spawned);   // 기본 적

            if (wave.mixedEnemy != null && stageNumber >= wave.mixedFromStage)  // 섞여 나오는 적이 등장할 스테이지라면 함께 소환
                spawned += SpawnSome(wave.mixedEnemy, wave.mixedCount, ref used, spawned);

            return spawned;
        }

        int SpawnSome(PieceDefinition enemy, int count, ref int used, int order)
        // 모아 둔 빈 칸 중 아직 쓰지 않은 칸에 적을 count마리 무작위로 소환하는 함수 -> 실제로 소환한 수를 반환
        // order: 등장 연출 순번의 시작값 (앞서 소환한 적 다음 순서로 떨어지게)
        {
            if (enemy == null || count <= 0)
                return 0;

            int spawned = 0;
            int wanted = Mathf.Min(count, _freeCells.Count - used);   // 남은 빈 칸보다 많이 소환할 수는 없음
            for (int n = 0; n < wanted; n++)
            {
                int i = used++;
                int pick = Random.Range(i, _freeCells.Count);   // 아직 고르지 않은 칸 중 하나를 무작위로 골라서
                (_freeCells[i], _freeCells[pick]) = (_freeCells[pick], _freeCells[i]);
                // i번째 자리와 바꿈 -> 같은 칸이 두 번 뽑히지 않는 무작위 선택(셔플)

                var view = _registry.Spawn(enemy, Team.Enemy, _freeCells[i]);  // 그 칸에 적을 생성
                if (view == null)   // 생성에 실패했다면 건너뜀
                    continue;

                Fx.SpawnDrop(view.transform, _grid.CoordToWorld(_freeCells[i]), transform, order + spawned);
                EnemySpawned?.Invoke(enemy);
                // 위에서 떨어지며 등장하는 연출(소환 순서만큼 늦게 시작)
                spawned++;
            }
            return spawned;
        }

        void CollectFreeSpawnCells()    // 위쪽 소환 줄에서 비어 있는 칸을 모두 모으는 함수
        {
            _freeCells.Clear(); // 이전 목록을 비우고

            int rows = Mathf.Clamp(spawnRows, 1, _grid.height); // 소환 줄 수(최소 1줄, 최대 보드 높이)
            for (int y = _grid.height - rows; y < _grid.height; y++)    // 맨 위에서 rows줄 안의
            for (int x = 0; x < _grid.width; x++)   // 모든 칸을 검사해서
            {
                var coord = new Coord(x, y);
                if (_state.IsEmpty(coord))  // 비어 있는 칸만 목록에 추가
                    _freeCells.Add(coord);
            }
        }
    }
}
