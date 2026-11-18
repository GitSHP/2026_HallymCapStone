using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class ThreatView : MonoBehaviour
    // 다음 적 턴에 잡히거나 깔릴 아군 기물에 '!'를 띄우고 떨게 하는 클래스 (Board 오브젝트에 붙음)
    // -> 플레이어 턴 동안 짧은 간격으로 EnemyMover.PredictThreats(실제와 같은 규칙의 미리 보기)를 돌려 결과와 표시를 맞춤
    // -> 기물을 옮기거나 소환해 위협에서 벗어나면 표시가 사라지고, 새로 위험해지면 다시 튀어나옴
    // -> 체스 대결(최종전)에는 쓰지 않음 (적이 다른 규칙으로 움직임)
    {
        [Tooltip("위협받는 기물 위에 띄울 '!' 프리팹.")]
        [SerializeField] ThreatMark _prefab;

        [Tooltip("'!'를 놓을 위치(기물 중심 기준, 칸 크기 단위). 칸 안 오른쪽 위 -> 위쪽 칸의 기물과 겹치지 않음.")]
        [SerializeField] Vector2 _markOffset = new Vector2(0.3f, -0.02f);

        [Tooltip("미리 보기를 다시 계산하는 간격(초).")]
        [SerializeField] float _refreshInterval = 0.15f;

        TurnManager _turn;
        StageDirector _director;
        EnemyMover _mover;
        WaveSpawner _spawner;
        PieceRegistry _registry;
        DragController _drag;

        readonly HashSet<Piece> _threatened = new();
        readonly Dictionary<Piece, ThreatMark> _shown = new();
        readonly List<Piece> _gone = new();
        Piece _held;    // 지금 들고 있는 기물 -> 드는 동안은 표시하지 않음
        float _phaseTime;   // 플레이어 턴이 시작된 뒤 흐른 시간
        float _nextRefresh;

        void Awake()
        {
            _turn = GetComponent<TurnManager>();
            _director = GetComponent<StageDirector>();
            _mover = GetComponent<EnemyMover>();
            _spawner = GetComponent<WaveSpawner>();
            _registry = GetComponent<PieceRegistry>();
            _drag = GetComponent<DragController>();
        }

        void OnEnable()
        {
            if (_drag != null)
            {
                _drag.DragStarted += OnDragStarted;
                _drag.DragEnded += OnDragEnded;
            }
        }

        void OnDisable()
        {
            if (_drag != null)
            {
                _drag.DragStarted -= OnDragStarted;
                _drag.DragEnded -= OnDragEnded;
            }
            ClearAll();
        }

        void OnDragStarted(Piece piece, IReadOnlyList<MoveOption> options)
        {
            _held = piece;
            _nextRefresh = 0f;
        }

        void OnDragEnded(Piece piece, Coord cell, DragController.DragOutcome outcome)
        {
            _held = null;
            _nextRefresh = 0f;  // 놓자마자 다시 계산
        }

        void Update()
        {
            bool active = _prefab != null && _turn != null && _mover != null
                && _turn.Phase == TurnPhase.PlayerTurn && (_director == null || !_director.IsChessDuel);
            if (!active)    // 플레이어 턴이 아니라면 표시를 모두 지우고 다음 턴을 기다림
            {
                _phaseTime = 0f;
                ClearAll();
                return;
            }

            _phaseTime += Time.deltaTime;
            if (_phaseTime < Fx.P.threatDelay || Time.time < _nextRefresh)
                return;
            _nextRefresh = Time.time + _refreshInterval;

            _mover.PredictThreats(_spawner != null ? _spawner.PlannedLandings : null, _threatened);
            if (_held != null)
                _threatened.Remove(_held);
            Sync();
        }

        void Sync() // 미리 보기 결과와 화면 표시를 맞추는 함수 -> 새로 위험해진 기물은 띄우고, 벗어난 기물은 지움
        {
            _gone.Clear();
            foreach (var pair in _shown)
            {
                if (pair.Value == null || !_threatened.Contains(pair.Key))
                    _gone.Add(pair.Key);
            }
            foreach (var piece in _gone)
            {
                if (_shown[piece] != null)
                    _shown[piece].Hide();
                _shown.Remove(piece);
            }

            foreach (var piece in _threatened)
            {
                if (piece == null || _shown.ContainsKey(piece))
                    continue;
                var view = _registry.GetView(piece);
                if (view == null)
                    continue;

                var mark = Instantiate(_prefab, view.transform);
                mark.transform.localPosition = new Vector3(_markOffset.x, _markOffset.y, 0f);
                mark.Show(view.transform);
                _shown[piece] = mark;
            }
        }

        void ClearAll()
        {
            if (_shown.Count == 0)
                return;
            foreach (var mark in _shown.Values)
            {
                if (mark != null)
                    mark.Hide();
            }
            _shown.Clear();
        }
    }
}
