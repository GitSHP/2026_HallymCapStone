using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// Pick up a piece, see where it may go, drop it there.
    /// Owns no rules of its own — it asks MoveResolver what is legal and
    /// asks Fx how things should look.
    /// </summary>
    [RequireComponent(typeof(BoardGrid))]
    public class DragController : MonoBehaviour
    {
        [Tooltip("Which side the local player controls.")]
        public Team controlledTeam = Team.Player;

        BoardGrid _grid;
        BoardState _state;
        BoardInput _input;
        PieceRegistry _registry;
        TurnManager _turn;
        CombatResolver _combat;

        readonly List<MoveOption> _options = new();

        PieceView _dragged;
        Coord _dragOrigin;
        Vector3 _ghostTarget;

        public bool IsDragging => _dragged != null;

        void Awake() => Resolve();

        void Resolve()
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_input == null) _input = GetComponent<BoardInput>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_turn == null) _turn = GetComponent<TurnManager>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
        }

        void Update()
        {
            Resolve();

            if (!InputGate.WorldInputEnabled)
            {
                // A screen opened mid-drag: put the piece back rather than leaving
                // it stranded under the overlay.
                if (_dragged != null)
                    CancelDrag();
                return;
            }

            if (_input.PressedThisFrame)
                TryBeginDrag();

            if (_dragged != null)
            {
                UpdateDragVisual();

                if (_input.ReleasedThisFrame)
                    EndDrag();
            }
        }

        void TryBeginDrag()
        {
            var coord = _input.HoveredCoord;
            if (!_grid.IsInside(coord))
                return;

            var piece = _state.GetPiece(coord);
            if (piece == null || piece.Team != controlledTeam)
                return;

            // Moving is a turn action: only on the player's turn, only if affordable.
            if (_turn != null && !_turn.CanAfford(piece.Def.moveApCost))
                return;

            var view = _registry.GetView(piece);
            if (view == null)
                return;

            _dragged = view;
            _dragOrigin = coord;
            _ghostTarget = view.transform.position;

            MoveResolver.GetLegalMoves(_state, piece, _options);
            ShowOptions();

            view.SetSortingBoost(true);
            Fx.PickUp(view.transform, view.BaseScale);
        }

        void UpdateDragVisual()
        {
            // Trailing the cursor instead of sticking to it is what gives the drag weight.
            _ghostTarget = _input.PointerWorldPosition;
            _ghostTarget.z = 0f;

            _dragged.transform.position = Fx.SpringFollow(
                _dragged.transform.position, _ghostTarget, Time.deltaTime);
        }

        void CancelDrag()
        {
            var view = _dragged;
            _dragged = null;
            _grid.ClearHighlights();
            view.SetSortingBoost(false);
            Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
        }

        void EndDrag()
        {
            var view = _dragged;
            var piece = view.Piece;
            var target = _input.HoveredCoord;

            _dragged = null;
            _grid.ClearHighlights();
            view.SetSortingBoost(false);

            bool legal = _grid.IsInside(target)
                         && target != _dragOrigin
                         && MoveResolver.IsLegalTarget(_options, target, out _);

            if (!legal)
            {
                // Snap home — dropping outside the board is the natural "cancel".
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            MoveResolver.IsLegalTarget(_options, target, out var option);

            // The action is paid for whether or not the blow finishes the target.
            if (_turn != null && !_turn.TrySpend(piece.Def.moveApCost))
            {
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            if (option.isCapture && _combat != null)
            {
                // The resolver moves the piece and animates the whole capture,
                // so the drag must not start a competing move of its own.
                _combat.CaptureInto(piece, _state.GetPiece(target), target);
                return;
            }

            if (option.isCapture)
                KillOutright(_state.GetPiece(target));

            _state.Move(piece, target);
            Fx.Drop(view.transform, _grid.CoordToWorld(target), transform, view.BaseScale);
        }

        bool KillOutright(Piece victim)
        {
            if (victim == null)
                return false;

            var victimView = _registry.GetView(victim);
            if (victimView != null)
                victimView.Flash();

            _registry.Kill(victim);
            Fx.HitStop();
            return true;
        }

        void ShowOptions()
        {
            _grid.ClearHighlights();

            var originTile = _grid.GetTile(_dragOrigin);
            if (originTile != null)
                originTile.SetHighlight(TileHighlight.Selected);

            // Nearer squares pop first so the set ripples outward from the piece.
            _options.Sort((a, b) =>
                Coord.KingDistance(_dragOrigin, a.target)
                    .CompareTo(Coord.KingDistance(_dragOrigin, b.target)));

            for (int i = 0; i < _options.Count; i++)
            {
                var tile = _grid.GetTile(_options[i].target);
                if (tile == null)
                    continue;

                tile.SetHighlight(_options[i].isCapture ? TileHighlight.Attack : TileHighlight.Move, i);
            }
        }
    }
}
