using System;
using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.Feel;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Placing pieces from the deck onto the board: free during the deploy phase,
    /// paid for in action points once the fighting starts.
    /// </summary>
    public class DeployController : MonoBehaviour
    {
        [Tooltip("Ranks from the player's edge that pieces may be placed on.")]
        public int deployRows = 3;

        [Tooltip("Cards that are always in hand once the stage starts and are never used up. " +
                 "Summoning one still costs its action points.")]
        public PieceDefinition[] alwaysAvailable = Array.Empty<PieceDefinition>();

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

        /// <summary>A card that stays in hand after it is summoned.</summary>
        public bool IsUnlimited(int index) => index >= 0 && index < _unlimited.Count && _unlimited[index];

        /// <summary>
        /// The king leads the hand, the always-available cards follow, then the rest.
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

        /// <summary>Action point cost right now — deploying is free while setting up.</summary>
        public int CostOf(PieceDefinition def)
        {
            if (def == null || _turn == null)
                return 0;
            return _turn.Phase == TurnPhase.Deploy ? 0 : def.deployApCost;
        }

        /// <summary>
        /// Setting up the stage is only about choosing where the king stands.
        /// Everything else is summoned during play, for action points.
        /// </summary>
        public bool IsDeployableNow(PieceDefinition def)
        {
            if (def == null || _turn == null)
                return false;

            if (_turn.Phase == TurnPhase.Deploy)
                return def.isObjective;

            return _turn.Phase == TurnPhase.PlayerTurn;
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

        /// <summary>Places the selected card on a square. Returns false if it could not be placed.</summary>
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
