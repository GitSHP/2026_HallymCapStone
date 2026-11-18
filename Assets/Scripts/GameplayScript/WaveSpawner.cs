using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class WaveSpawner : MonoBehaviour  // 웨이브(정해진 턴에 한꺼번에 나타나는 적 무리)를 보드 위쪽 줄에 소환하는 클래스
    // -> 착지 칸은 소환 한 턴 전(플레이어 턴 시작)에 미리 정해 둠(Plan) -> 플레이어가 경고 표시를 보고 피할 수 있게 함
    // -> 소환할 때 그 칸에 아군 기물이 있으면 떨어지는 적에게 깔려 잡힘, 킹이 있으면 패배(적이 킹 칸에 들어온 것과 같음)
    {
        public struct Landing   // 미리 정해 둔 착지 하나 -> 어떤 적이 어느 칸에 떨어질지
        {
            public PieceDefinition enemy;
            public Coord cell;
        }

        public event System.Action<PieceDefinition> EnemySpawned;   // 적이 하나 소환될 때마다 발생하는 이벤트 -> 기물 가이드가 처음 보는 적(중갑병)의 설명을 띄움
        public event System.Action PlansChanged;    // 미리 정해 둔 착지 칸이 바뀔 때 (계획 추가·소환·초기화) -> 위험 경고 표시(DangerPingView)가 구독

        [Tooltip("적이 생성될 수 있는, 위쪽 가장자리부터의 줄 수.")]
        public int spawnRows = 2;   // 적이 나타날 수 있는 위쪽 줄 수

        public bool KingCrushed { get; private set; }   // 마지막 소환에서 적이 킹이 서 있던 칸에 떨어졌는지 -> StageDirector가 패배로 처리

        BoardGrid _grid;    // 보드 크기와 칸 좌표 변환용
        BoardState _state;  // 칸 상태를 확인하는 보드 데이터
        PieceRegistry _registry;    // 적 기물을 생성하는 컴포넌트
        CombatResolver _combat;     // 착지 칸의 아군 기물을 잡는 처리

        readonly Dictionary<int, List<Landing>> _plans = new();    // 웨이브 번호 -> 미리 정해 둔 착지 목록
        readonly List<Coord> _candidates = new();   // 착지 후보 칸 목록(재사용)

        void Awake()  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠
        {
            Resolve();
        }

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
        }

        // ---------- 착지 계획 ----------

        public IEnumerable<Landing> PlannedLandings   // 아직 소환되지 않은 모든 착지 계획 (경고 표시용)
        {
            get
            {
                foreach (var plan in _plans.Values)
                {
                    foreach (var landing in plan)
                        yield return landing;
                }
            }
        }

        public void Plan(int waveIndex, WaveDefinition wave, int extra = 0, int stageNumber = 1)
        // 웨이브의 착지 칸을 미리 정하는 함수 -> 이미 정해 둔 웨이브는 다시 정하지 않음
        // extra: 런 진행(난이도 상승)으로 웨이브에 더할 적 수 / stageNumber: 섞여 나오는 적(중갑병)이 등장할 스테이지인지 판단
        {
            Resolve();
            if (wave == null || _plans.ContainsKey(waveIndex))
                return;

            CollectCandidates();
            var plan = new List<Landing>();
            AddLandings(plan, wave.enemy, wave.count + Mathf.Max(0, extra));
            if (wave.mixedEnemy != null && stageNumber >= wave.mixedFromStage)
                AddLandings(plan, wave.mixedEnemy, wave.mixedCount);

            _plans[waveIndex] = plan;
            PlansChanged?.Invoke();
        }

        public void ClearPlans()    // 계획을 모두 지우는 함수 (스테이지 시작·종료)
        {
            if (_plans.Count == 0)
                return;
            _plans.Clear();
            PlansChanged?.Invoke();
        }

        void AddLandings(List<Landing> plan, PieceDefinition enemy, int count)
        // 후보 칸 중 무작위로 count칸을 골라 계획에 넣는 함수 (같은 칸이 두 번 뽑히지 않음)
        {
            if (enemy == null)
                return;
            for (int n = 0; n < count && _candidates.Count > 0; n++)
            {
                int pick = Random.Range(0, _candidates.Count);
                plan.Add(new Landing { enemy = enemy, cell = _candidates[pick] });
                _candidates.RemoveAt(pick);
            }
        }

        void CollectCandidates()
        // 위쪽 소환 줄에서 착지할 수 있는 칸을 모으는 함수
        // -> 적이 있는 칸, 킹이 서 있는 칸, 다른 웨이브가 이미 정해 둔 칸은 제외. 아군 기물이 있는 칸은 후보에 넣음(경고를 보고 피해야 함)
        {
            _candidates.Clear();
            int rows = Mathf.Clamp(spawnRows, 1, _grid.height);
            for (int y = _grid.height - rows; y < _grid.height; y++)
            for (int x = 0; x < _grid.width; x++)
            {
                var coord = new Coord(x, y);
                var piece = _state.GetPiece(coord);
                if (piece != null && (piece.Team == Team.Enemy || piece.IsObjective))
                    continue;
                if (IsPlanned(coord))
                    continue;
                _candidates.Add(coord);
            }
        }

        bool IsPlanned(Coord coord)
        {
            foreach (var landing in PlannedLandings)
            {
                if (landing.cell == coord)
                    return true;
            }
            return false;
        }

        // ---------- 소환 ----------

        public int Spawn(int waveIndex, WaveDefinition wave, int extra = 0, int stageNumber = 1)
        // 웨이브를 정해 둔 칸에 소환하는 함수 -> 계획이 없다면(개발자 도구·튜토리얼 즉시 소환) 지금 정해서 소환. 소환한 적 수를 반환
        {
            Resolve();
            KingCrushed = false;
            if (wave == null)
                return 0;

            Plan(waveIndex, wave, extra, stageNumber);
            var plan = _plans[waveIndex];
            _plans.Remove(waveIndex);

            int spawned = 0;
            foreach (var landing in plan)
            {
                if (SpawnAt(landing, spawned))
                    spawned++;
            }
            PlansChanged?.Invoke();
            return spawned;
        }

        bool SpawnAt(Landing landing, int order)
        // 정해 둔 칸 하나에 적을 떨어뜨리는 함수 -> 그 칸의 아군 기물은 착지하는 순간 잡힘
        {
            var occupant = _state.GetPiece(landing.cell);
            if (occupant != null && occupant.Team == Team.Enemy)    // 그사이 적이 들어와 있다면 이 적은 건너뜀 (적끼리는 겹치지 않음)
                return false;
            if (occupant != null && occupant.IsObjective)   // 킹이 경고 칸에 서 있었다면 -> 적이 킹 칸에 들어온 것과 같으므로 패배
            {
                KingCrushed = true;
                return false;
            }

            PieceView victimView = null;
            if (occupant != null && _combat != null)
                victimView = _combat.FreeCellForLanding(occupant);  // 데이터에서 먼저 비워 적이 들어설 자리를 만듦 (그림은 착지 순간까지 남김)

            var view = _registry.Spawn(landing.enemy, Team.Enemy, landing.cell);
            if (view == null)
                return false;

            System.Action onImpact = null;
            if (occupant != null && _combat != null)
                onImpact = _combat.CrushOnLanding(view.Piece, occupant, victimView);   // 잡기를 확정하고, 착지 순간 실행할 처리를 받아 둠

            Fx.SpawnDrop(view.transform, _grid.CoordToWorld(landing.cell), transform, order, onImpact);
            // 위에서 떨어지며 등장하는 연출(소환 순서만큼 늦게 시작) -> 착지하는 순간 깔린 기물이 부서짐
            EnemySpawned?.Invoke(landing.enemy);
            return true;
        }
    }
}
