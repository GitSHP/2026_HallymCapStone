using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class DangerPingView : MonoBehaviour
    // 다음 적 턴에 웨이브가 떨어질 칸에 경고 표시를 띄우는 클래스 (Board 오브젝트에 붙음)
    // -> WaveSpawner가 착지 칸을 미리 정할 때마다(PlansChanged) 표시를 맞춰 줌 -> 소환되면 표시가 사라짐
    // -> 플레이어는 플레이어 턴 동안 이 칸을 보고, 기물을 두면 깔려 잡힌다는 것을 알 수 있음
    {
        [Tooltip("착지 예정 칸에 띄울 경고 표시 프리팹.")]
        [SerializeField] DangerPing _prefab;

        WaveSpawner _spawner;
        BoardGrid _grid;
        readonly Dictionary<Coord, DangerPing> _shown = new();
        readonly HashSet<Coord> _planned = new();

        void Awake()
        {
            _spawner = GetComponent<WaveSpawner>();
            _grid = GetComponent<BoardGrid>();
        }

        void OnEnable()
        {
            if (_spawner != null)
                _spawner.PlansChanged += Sync;
        }

        void OnDisable()
        {
            if (_spawner != null)
                _spawner.PlansChanged -= Sync;
        }

        void Sync() // 계획된 착지 칸과 화면의 경고 표시를 맞추는 함수 -> 새 칸은 띄우고, 사라진 칸은 지움
        {
            if (_prefab == null)
                return;

            _planned.Clear();
            foreach (var landing in _spawner.PlannedLandings)
            {
                _planned.Add(landing.cell);
                if (_shown.ContainsKey(landing.cell))
                    continue;
                var ping = Instantiate(_prefab, _grid.CoordToWorld(landing.cell), Quaternion.identity, transform);
                ping.Show(Art(landing.enemy));
                _shown[landing.cell] = ping;
            }

            var gone = new List<Coord>();
            foreach (var pair in _shown)
            {
                if (!_planned.Contains(pair.Key))
                    gone.Add(pair.Key);
            }
            foreach (var cell in gone)
            {
                if (_shown[cell] != null)
                    _shown[cell].Hide();
                _shown.Remove(cell);
            }
        }

        static Sprite Art(Promotion.Data.PieceDefinition def)
        {
            if (def == null)
                return null;
            return def.enemySprite != null ? def.enemySprite : def.itemImage;
        }
    }
}
