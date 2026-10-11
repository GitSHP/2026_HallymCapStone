using System;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 덱의 기물을 보드에 놓는다. 배치 단계에서는 무료이고, 전투가 시작되면 AP를 소모한다.
    /// </summary>
    public class DeployController : MonoBehaviour
    {
        [Tooltip("기물을 놓을 수 있는, 플레이어 쪽 가장자리부터의 줄 수.")]
        public int deployRows = 3;

        [Tooltip("스테이지가 시작되면 항상 손에 있고 소모되지 않는 카드. " +
                 "소환할 때는 여전히 AP를 소모한다.")]
        public PieceDefinition[] alwaysAvailable = Array.Empty<PieceDefinition>();

        [Tooltip("배치 단계에서 킹뿐 아니라 모든 카드를 놓을 수 있다(체스 대결).")]
        public bool deployAllAtSetup;

        [Tooltip("전투가 시작된 뒤 AP를 소모해 카드를 소환할 수 있다.")]
        public bool summonDuringBattle = true;

        public event Action DeckChanged;

        readonly List<PieceDefinition> _deck = new();
        readonly List<bool> _unlimited = new();

        public IReadOnlyList<PieceDefinition> Deck => _deck;
        public int SelectedIndex { get; private set; } = -1;

        public PieceDefinition Selected =>
            SelectedIndex >= 0 && SelectedIndex < _deck.Count ? _deck[SelectedIndex] : null;

        BoardGrid _grid;
        BoardState _state;
        PieceRegistry _registry;
        BoardInput _input;
        DragController _drag;
        TurnManager _turn;

        void Awake() => Resolve();

        void Resolve()
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_input == null) _input = GetComponent<BoardInput>();
            if (_drag == null) _drag = GetComponent<DragController>();
            if (_turn == null) _turn = GetComponent<TurnManager>();
        }

        /// <summary>소환한 뒤에도 손에 남는 카드인지 확인한다.</summary>
        public bool IsUnlimited(int index) => index >= 0 && index < _unlimited.Count && _unlimited[index];

        /// <summary>
        /// 손패는 킹이 맨 앞에 오고, 그 뒤에 항상 사용 가능한 카드, 그다음 나머지가 온다.
        /// </summary>
        public void LoadDeck(IEnumerable<PieceDefinition> definitions)
        {
            _deck.Clear();
            _unlimited.Clear();

            var rest = new List<PieceDefinition>();
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def == null)
                        continue;

                    if (def.isObjective)
                        Add(def, false);
                    else
                        rest.Add(def);
                }
            }

            if (summonDuringBattle)
                foreach (var def in alwaysAvailable)
                    if (def != null)
                        Add(def, true);

            foreach (var def in rest)
                Add(def, false);

            SelectedIndex = -1;
            DeckChanged?.Invoke();
        }

        public void Select(int index)
        {
            SelectedIndex = index == SelectedIndex ? -1 : index;
            ShowDeployableTiles();
            DeckChanged?.Invoke();
        }

        /// <summary>현재 시점의 AP 비용이다. 배치 단계에서는 무료다.</summary>
        public int CostOf(PieceDefinition def)
        {
            if (def == null || _turn == null)
                return 0;
            return _turn.Phase == TurnPhase.Deploy ? 0 : def.deployApCost;
        }

        /// <summary>
        /// 스테이지 배치 단계에서는 킹을 어디에 세울지만 정한다.
        /// 나머지는 모두 플레이 중에 AP를 소모해 소환한다.
        /// </summary>
        public bool IsDeployableNow(PieceDefinition def)
        {
            if (def == null || _turn == null)
                return false;

            if (_turn.Phase == TurnPhase.Deploy)
                return def.isObjective || deployAllAtSetup;

            return summonDuringBattle && _turn.Phase == TurnPhase.PlayerTurn;
        }

        public bool CanDeploy(PieceDefinition def, Coord coord)
        {
            if (!IsDeployableNow(def))
                return false;

            return _grid.IsInside(coord)
                   && coord.y < Mathf.Clamp(deployRows, 1, _grid.height)
                   && _state.IsEmpty(coord);
        }

        public bool CanDeployAt(Coord coord) => CanDeploy(Selected, coord);

        void Update()
        {
            Resolve();

            if (_turn == null || _input == null || Selected == null)
                return;

            if (!InputGate.WorldInputEnabled)
                return;

            if (_drag != null && _drag.IsDragging)
                return;

            if (!_input.PressedThisFrame)
                return;

            TryDeployAt(_input.HoveredCoord);
        }

        /// <summary>선택한 카드를 칸에 놓는다. 놓지 못했으면 false를 반환한다.</summary>
        public bool TryDeployAt(Coord coord)
        {
            Resolve();

            var def = Selected;
            if (def == null || !CanDeployAt(coord))
                return false;

            int cost = CostOf(def);
            if (cost > 0 && !_turn.TrySpend(cost))
                return false;

            var view = _registry.Spawn(def, Team.Player, coord);
            if (view == null)
                return false;

            Fx.Drop(view.transform, _grid.CoordToWorld(coord), transform, view.BaseScale);

            if (!IsUnlimited(SelectedIndex))
            {
                _deck.RemoveAt(SelectedIndex);
                _unlimited.RemoveAt(SelectedIndex);
            }

            SelectedIndex = -1;
            _grid.ClearHighlights();
            DeckChanged?.Invoke();
            return true;
        }

        void Add(PieceDefinition def, bool unlimited)
        {
            _deck.Add(def);
            _unlimited.Add(unlimited);
        }

        void ShowDeployableTiles()
        {
            _grid.ClearHighlights();
            if (Selected == null)
                return;

            int rows = Mathf.Clamp(deployRows, 1, _grid.height);
            int index = 0;

            for (int y = 0; y < rows; y++)
            for (int x = 0; x < _grid.width; x++)
            {
                var coord = new Coord(x, y);
                if (!CanDeploy(Selected, coord))
                    continue;

                var tile = _grid.GetTile(coord);
                if (tile != null)
                    tile.SetHighlight(TileHighlight.Move, index++);
            }
        }
    }
}
