using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 기물을 집어 들고, 갈 수 있는 곳을 확인하고, 그곳에 내려놓는다.
    /// 자체 규칙은 갖지 않는다. 무엇이 합법인지는 MoveResolver에 묻고,
    /// 어떻게 보여야 하는지는 Fx에 묻는다.
    /// </summary>
    [RequireComponent(typeof(BoardGrid))]
    public class DragController : MonoBehaviour
    {
        [Tooltip("로컬 플레이어가 조종하는 진영.")]
        public Team controlledTeam = Team.Player;

        [Tooltip("완전한 체스 합법성 판정. 자기 킹을 체크 상태로 두는 수는 제시하지 않는다(체스 대결).")]
        public bool chessLegality;

        /// <summary>드래그로 수가 확정되면 한 번 발생한다. 기물과 그 기물이 이동한 칸을 전달한다.</summary>
        public event System.Action<Piece, Coord> MoveCommitted;

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
                // 드래그 도중 화면이 열렸다. 기물이 오버레이 아래에 방치되지 않도록
                // 제자리로 돌려놓는다.
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

            // 이동은 턴 행동이다. 플레이어 턴이고 비용을 낼 수 있을 때만 가능하다.
            if (_turn != null && !_turn.CanAfford(piece.Def.moveApCost))
                return;

            var view = _registry.GetView(piece);
            if (view == null)
                return;

            _dragged = view;
            _dragOrigin = coord;
            _ghostTarget = view.transform.position;

            if (chessLegality)
            {
                var copy = SearchBoard.CopyOf(_state);
                ChessRules.GetLegalMoves(copy, copy.Mirror(piece), _options);
            }
            else
            {
                MoveResolver.GetLegalMoves(_state, piece, _options);
            }
            ShowOptions();

            view.SetSortingBoost(true);
            Fx.PickUp(view.transform, view.BaseScale);
        }

        void UpdateDragVisual()
        {
            // 커서에 딱 붙지 않고 뒤따라가게 해서 드래그에 무게감을 준다.
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
                // 원래 자리로 되돌린다. 보드 밖에 놓는 것이 자연스러운 "취소"다.
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            MoveResolver.IsLegalTarget(_options, target, out var option);

            // 공격이 대상을 끝내는지와 관계없이 행동 비용은 지불한다.
            if (_turn != null && !_turn.TrySpend(piece.Def.moveApCost))
            {
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            if (option.isCapture && _combat != null)
            {
                // 리졸버가 기물을 옮기고 잡기 전체를 연출하므로,
                // 드래그 쪽에서 경합하는 이동을 따로 시작하면 안 된다.
                _combat.CaptureInto(piece, _state.GetPiece(target), target);
                MoveCommitted?.Invoke(piece, target);
                return;
            }

            if (option.isCapture)
                KillOutright(_state.GetPiece(target));

            _state.Move(piece, target);
            Fx.Drop(view.transform, _grid.CoordToWorld(target), transform, view.BaseScale);
            MoveCommitted?.Invoke(piece, target);
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

            // 가까운 칸부터 나타나게 해서 표시가 기물에서 바깥쪽으로 물결처럼 퍼진다.
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
