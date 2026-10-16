using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(BoardState))]
    public class PieceRegistry : MonoBehaviour  // 기물 데이터(Piece)와 화면에 보이는 오브젝트(PieceView)를 짝지어 관리하는 클래스
    // 기물을 만들거나 없앨 때는 반드시 여기를 거침 -> 보드 데이터와 화면이 서로 어긋나지 않게 함
    {
        readonly Dictionary<Piece, PieceView> _views = new();  // 기물 데이터 -> 화면 오브젝트를 찾는 사전

        BoardState _state;  // 보드 위 기물 위치 데이터
        BoardGrid _grid;    // 칸 좌표 -> 월드 좌표 변환용
        Transform _pieceRoot;   // 생성한 기물 오브젝트들을 모아 둘 부모 오브젝트("Pieces")

        void Awake()    // 오브젝트가 생성될 때 한 번 실행
        {
            _state = GetComponent<BoardState>();
            _grid = GetComponent<BoardGrid>();

            var rootGo = new GameObject("Pieces");  // 기물들을 담을 빈 오브젝트를 만들어서
            rootGo.transform.SetParent(transform, false);   // 보드의 자식으로 붙임 -> 하이어라키가 정리됨
            _pieceRoot = rootGo.transform;
        }

        public PieceView GetView(Piece piece)   // 기물 데이터에 해당하는 화면 오브젝트를 찾아 주는 함수(없으면 null)
            => piece != null && _views.TryGetValue(piece, out var view) ? view : null;

        public PieceView Spawn(PieceDefinition def, Team team, Coord coord) // 기물을 보드 데이터에 놓고 화면 오브젝트도 만드는 함수 -> 만든 오브젝트를 반환
        {
            var piece = _state.Place(def, team, coord); // 보드 데이터에 기물을 놓음
            if (piece == null)  // 놓지 못했다면(칸이 차 있거나 보드 밖) 경고를 남기고 null 반환
            {
                Debug.LogWarning($"[PieceRegistry] Could not place {def.itemName} at {coord}");
                return null;
            }

            var go = new GameObject();  // 빈 오브젝트를 만들어
            go.transform.SetParent(_pieceRoot, false);  // "Pieces" 아래에 붙이고

            var view = go.AddComponent<PieceView>();    // 기물 화면 컴포넌트를 추가해서
            view.Init(piece, _grid);    // 그림, 색, 위치를 설정
            _views[piece] = view;   // 데이터와 화면 오브젝트를 짝지어 기록
            return view;
        }

        public void Kill(Piece piece)   // 잡힌 기물을 제거하는 함수 -> 데이터는 즉시 지우고 화면은 사라지는 연출 후 파괴
        {
            var view = GetView(piece);
            _state.Remove(piece);   // 보드 데이터에서 제거
            _views.Remove(piece);   // 짝 기록에서 제거

            if (view != null)   // 화면 오브젝트가 있다면 사라지는 연출 재생(끝나면 스스로 파괴됨)
                view.PlayDeath();
        }

        public void RemoveAll() // 보드 위의 모든 기물을 연출 없이 즉시 제거하는 함수(체스 대결 보드 준비용)
        {
            var pieces = new List<Piece>(_state.AllPieces); // 제거하면서 목록이 바뀌므로 복사본을 만들어 순회
            foreach (var piece in pieces)
            {
                var view = GetView(piece);
                _state.Remove(piece);   // 데이터에서 제거
                _views.Remove(piece);
                if (view != null)   // 화면 오브젝트는 바로 파괴
                    Destroy(view.gameObject);
            }
        }

        public PieceView Replace(Piece piece, PieceDefinition into) // 기물을 다른 종류의 기물로 바꾸는 함수(프로모션 등) -> 새 오브젝트를 반환
        {
            if (piece == null || into == null)
                return null;

            var coord = piece.Coord;    // 원래 기물의 칸과 진영을 기억해 두고
            var team = piece.Team;
            var oldView = GetView(piece);
            _state.Remove(piece);   // 원래 기물을 데이터와 화면에서 제거한 뒤
            _views.Remove(piece);
            if (oldView != null)
                Destroy(oldView.gameObject);

            var view = Spawn(into, team, coord);    // 같은 칸에 새 기물을 생성
            if (view != null)
            {
                view.Piece.HasMoved = true; // 새 기물은 이미 움직인 것으로 표시 -> 첫 수 전용 규칙(2칸 전진 등)이 다시 적용되지 않게
                Fx.PopIn(view.transform, 0, view.BaseScale);    // 톡 튀어나오는 등장 연출
            }
            return view;
        }

        public void ClearAll()  // 화면 오브젝트를 모두 파괴하고 짝 기록을 비우는 함수(보드 데이터는 건드리지 않음)
        {
            foreach (var view in _views.Values)
                if (view != null)
                    Destroy(view.gameObject);

            _views.Clear();
        }
    }
}
