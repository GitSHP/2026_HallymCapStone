using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(BoardGrid))]
    public class DragController : MonoBehaviour   // 플레이어가 기물을 마우스로 집어서(드래그) 다른 칸에 내려놓아 움직이는 것을 담당하는 클래스
    // 자체 규칙은 없음 -> 어디로 갈 수 있는지는 MoveResolver/ChessRules에 묻고, 어떻게 보여야 하는지는 Fx에 맡김
    {
        [Tooltip("로컬 플레이어가 조종하는 진영.")]
        public Team controlledTeam = Team.Player;   // 플레이어가 움직일 수 있는 진영(아군)

        [Tooltip("완전한 체스 합법성 판정. 자기 킹을 체크 상태로 두는 수는 제시하지 않는다(체스 대결).")]
        public bool chessLegality;  // true면 실제 체스 규칙으로 갈 수 있는 칸을 계산(체스 대결용)

        public event System.Action<Piece, Coord> MoveCommitted; // 드래그로 이동이 확정됐을 때 발생하는 이벤트 -> 움직인 기물과 도착 칸을 넘겨줌

        BoardGrid _grid;    // 칸 좌표 변환과 칸 강조 표시용
        BoardState _state;  // 보드 위 기물 위치 데이터
        BoardInput _input;  // 마우스 누름/뗌과 포인터 위치를 알려 주는 컴포넌트
        PieceRegistry _registry;    // 기물 데이터와 화면 오브젝트를 짝지어 관리하는 컴포넌트
        TurnManager _turn;  // 이동에 필요한 AP를 확인/소모하는 컴포넌트
        CombatResolver _combat; // 잡기를 처리하는 컴포넌트

        readonly List<MoveOption> _options = new(); // 지금 집은 기물이 갈 수 있는 칸 목록(잡기 여부 포함)

        PieceView _dragged; // 지금 드래그 중인 기물의 화면 오브젝트(드래그 중이 아니면 null)
        Coord _dragOrigin;  // 드래그를 시작한 칸 -> 취소하면 이 칸으로 돌아감
        Vector3 _ghostTarget;   // 드래그 중인 기물이 따라가야 할 목표 위치(포인터 위치)

        public bool IsDragging => _dragged != null; // 지금 드래그 중인지 여부

        void Awake() => Resolve();  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_input == null) _input = GetComponent<BoardInput>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_turn == null) _turn = GetComponent<TurnManager>();
            if (_combat == null) _combat = GetComponent<CombatResolver>();
        }

        void Update()   // 매 프레임 드래그 시작/진행/끝을 처리
        {
            Resolve();

            if (!InputGate.WorldInputEnabled)   // 만약 UI 창이 열려 보드 입력이 막혔다면
            {
                if (_dragged != null)   // 드래그 중이던 기물을 제자리로 돌려놓음 -> 기물이 창 아래에 방치되지 않도록
                    CancelDrag();
                return;
            }

            if (_input.PressedThisFrame)    // 이번 프레임에 눌렀다면 드래그 시작 시도
                TryBeginDrag();

            if (_dragged != null)   // 드래그 중이라면
            {
                UpdateDragVisual(); // 기물이 포인터를 따라오게 하고

                if (_input.ReleasedThisFrame)   // 이번 프레임에 뗐다면 드래그 끝 처리
                    EndDrag();
            }
        }

        void TryBeginDrag() // 포인터 아래의 기물을 집어 드는 함수 -> 집을 수 없는 경우엔 아무것도 하지 않음
        {
            var coord = _input.HoveredCoord;    // 누른 칸
            if (!_grid.IsInside(coord)) // 보드 밖이라면 종료
                return;

            var piece = _state.GetPiece(coord); // 그 칸의 기물
            if (piece == null || piece.Team != controlledTeam)  // 기물이 없거나 아군이 아니라면 종료
                return;

            if (_turn != null && !_turn.CanAfford(piece.Def.moveApCost))
            // 이동은 턴 행동 -> 플레이어 턴이고 이동 AP를 낼 수 있을 때만 집을 수 있음
                return;

            var view = _registry.GetView(piece);    // 기물의 화면 오브젝트
            if (view == null)
                return;

            _dragged = view;    // 드래그 상태 기록
            _dragOrigin = coord;
            _ghostTarget = view.transform.position;

            if (chessLegality)  // 체스 대결이라면 실제 체스 규칙으로 갈 수 있는 칸을 계산
            {
                var copy = SearchBoard.CopyOf(_state);  // 계산용 복사 보드를 만들고
                ChessRules.GetLegalMoves(copy, copy.Mirror(piece), _options);
                // 복사 보드에서 같은 칸의 기물을 찾아 합법수 계산 -> 자기 킹을 체크 상태로 두는 수는 빠짐
            }
            else    // 일반 스테이지라면 기물의 행마법만으로 갈 수 있는 칸을 계산
            {
                MoveResolver.GetLegalMoves(_state, piece, _options);
            }
            ShowOptions();  // 갈 수 있는 칸을 강조 표시

            view.SetSortingBoost(true); // 드래그 중인 기물이 다른 기물 위에 그려지도록 순서를 올리고
            Fx.PickUp(view.transform, view.BaseScale);  // 집어 드는 연출(살짝 커지고 기울어짐)
        }

        void UpdateDragVisual() // 드래그 중인 기물이 포인터를 따라오게 하는 함수
        {
            _ghostTarget = _input.PointerWorldPosition; // 목표 위치 = 포인터 위치
            _ghostTarget.z = 0f;    // 2D 평면 위로 고정

            _dragged.transform.position = Fx.SpringFollow(
                _dragged.transform.position, _ghostTarget, Time.deltaTime);
            // 포인터에 딱 붙지 않고 살짝 늦게 뒤따라가게 함 -> 드래그에 무게감을 줌
        }

        void CancelDrag()   // 드래그를 취소하고 기물을 원래 칸으로 돌려놓는 함수
        {
            var view = _dragged;
            _dragged = null;    // 드래그 상태 해제
            _grid.ClearHighlights();    // 칸 강조 표시 지우고
            view.SetSortingBoost(false);    // 그리기 순서를 원래대로
            Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);   // 원래 칸으로 내려놓는 연출
        }

        void EndDrag()  // 마우스를 뗐을 때 이동을 확정하거나 취소하는 함수
        {
            var view = _dragged;
            var piece = view.Piece; // 움직이려는 기물 데이터
            var target = _input.HoveredCoord;   // 놓으려는 칸

            _dragged = null;    // 드래그 상태 해제
            _grid.ClearHighlights();
            view.SetSortingBoost(false);

            bool legal = _grid.IsInside(target) // 보드 안이고
                         && target != _dragOrigin   // 원래 칸이 아니고
                         && MoveResolver.IsLegalTarget(_options, target, out _);    // 갈 수 있는 칸 목록에 있어야 이동 가능

            if (!legal) // 갈 수 없는 칸이라면 원래 자리로 되돌림 -> 보드 밖에 놓는 것이 자연스러운 "취소"
            {
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            MoveResolver.IsLegalTarget(_options, target, out var option);   // 놓은 칸의 이동 정보(잡기인지 등)를 가져옴

            if (_turn != null && !_turn.TrySpend(piece.Def.moveApCost))
            // 이동 AP를 소모 -> 부족하다면 원래 자리로 되돌림
            {
                Fx.Drop(view.transform, _grid.CoordToWorld(_dragOrigin), transform, view.BaseScale);
                return;
            }

            if (option.isCapture && _combat != null)    // 잡기 이동이라면
            {
                _combat.CaptureInto(piece, _state.GetPiece(target), target);
                // CombatResolver가 기물 이동과 잡기 연출을 모두 처리함 -> 여기서 이동 연출을 따로 시작하면 서로 충돌함
                MoveCommitted?.Invoke(piece, target);   // 이동이 확정됐음을 알림
                return;
            }

            if (option.isCapture)   // 잡기인데 CombatResolver가 없다면 연출 없이 바로 대상을 제거
                KillOutright(_state.GetPiece(target));

            _state.Move(piece, target); // 보드 데이터에서 기물을 옮기고
            Fx.Drop(view.transform, _grid.CoordToWorld(target), transform, view.BaseScale); // 새 칸에 내려놓는 연출
            MoveCommitted?.Invoke(piece, target);   // 이동이 확정됐음을 알림
        }

        bool KillOutright(Piece victim) // CombatResolver 없이 대상을 바로 제거하는 함수 -> 제거했으면 true
        {
            if (victim == null)
                return false;

            var victimView = _registry.GetView(victim);
            if (victimView != null) // 대상 오브젝트가 있다면 번쩍이는 피격 연출
                victimView.Flash();

            _registry.Kill(victim); // 보드와 화면에서 제거하고
            Fx.HitStop();   // 타격감을 위해 아주 잠깐 화면을 멈춤
            return true;
        }

        void ShowOptions()  // 집은 기물이 갈 수 있는 칸을 강조 표시하는 함수
        {
            _grid.ClearHighlights();    // 이전 강조 표시를 지우고

            var originTile = _grid.GetTile(_dragOrigin);
            if (originTile != null) // 출발 칸은 선택 표시
                originTile.SetHighlight(TileHighlight.Selected);

            _options.Sort((a, b) =>
                Coord.KingDistance(_dragOrigin, a.target)
                    .CompareTo(Coord.KingDistance(_dragOrigin, b.target)));
            // 출발 칸에서 가까운 칸부터 정렬 -> 표시가 기물에서 바깥쪽으로 물결처럼 퍼지게 함

            for (int i = 0; i < _options.Count; i++)    // 갈 수 있는 칸을 하나씩
            {
                var tile = _grid.GetTile(_options[i].target);
                if (tile == null)
                    continue;

                tile.SetHighlight(_options[i].isCapture ? TileHighlight.Attack : TileHighlight.Move, i);
                // 잡기 칸은 공격 색, 빈 칸은 이동 색으로 표시(i번째 순서만큼 늦게 켜짐)
            }
        }
    }
}
