using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 기물을 생성하고, 논리 객체인 Piece와 그 PieceView의 연결을 관리한다.
    /// 기물을 애니메이션해야 하는 곳은 모두 여기서 뷰를 찾는다.
    /// </summary>
    [RequireComponent(typeof(BoardState))]
    public class PieceRegistry : MonoBehaviour
    {
        readonly Dictionary<Piece, PieceView> _views = new();

        BoardState _state;
        BoardGrid _grid;
        Transform _pieceRoot;

        void Awake()
        {
            _state = GetComponent<BoardState>();
            _grid = GetComponent<BoardGrid>();

            var rootGo = new GameObject("Pieces");
            rootGo.transform.SetParent(transform, false);
            _pieceRoot = rootGo.transform;
        }

        public PieceView GetView(Piece piece)
            => piece != null && _views.TryGetValue(piece, out var view) ? view : null;

        public PieceView Spawn(PieceDefinition def, Team team, Coord coord)
        {
            var piece = _state.Place(def, team, coord);
            if (piece == null)
            {
                Debug.LogWarning($"[PieceRegistry] Could not place {def.itemName} at {coord}");
                return null;
            }

            var go = new GameObject();
            go.transform.SetParent(_pieceRoot, false);

            var view = go.AddComponent<PieceView>();
            view.Init(piece, _grid);
            _views[piece] = view;
            return view;
        }

        /// <summary>기물을 보드에서 제거하고 사망 연출을 재생한다.</summary>
        public void Kill(Piece piece)
        {
            var view = GetView(piece);
            _state.Remove(piece);
            _views.Remove(piece);

            if (view != null)
                view.PlayDeath();
        }

        /// <summary>사망 연출 없이 모든 기물을 한 번에 보드에서 치운다.</summary>
        public void RemoveAll()
        {
            var pieces = new List<Piece>(_state.AllPieces);
            foreach (var piece in pieces)
            {
                var view = GetView(piece);
                _state.Remove(piece);
                _views.Remove(piece);
                if (view != null)
                    Destroy(view.gameObject);
            }
        }

        /// <summary>
        /// 같은 칸에서 기물을 다른 종류로 바꾼다. 즉 승급이다. 기존 본체는
        /// 사라지고 새 본체가 튀어나오듯 나타난다.
        /// </summary>
        public PieceView Replace(Piece piece, PieceDefinition into)
        {
            if (piece == null || into == null)
                return null;

            var coord = piece.Coord;
            var team = piece.Team;
            var oldView = GetView(piece);
            _state.Remove(piece);
            _views.Remove(piece);
            if (oldView != null)
                Destroy(oldView.gameObject);

            var view = Spawn(into, team, coord);
            if (view != null)
            {
                view.Piece.HasMoved = true;
                Fx.PopIn(view.transform, 0, view.BaseScale);
            }
            return view;
        }

        public void ClearAll()
        {
            foreach (var view in _views.Values)
                if (view != null)
                    Destroy(view.gameObject);

            _views.Clear();
        }
    }
}
