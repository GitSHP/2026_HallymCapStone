using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// Spawns pieces and keeps the link between logical Piece and its PieceView.
    /// Anything that needs to animate a piece looks up its view here.
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
                Debug.LogWarning($"[PieceRegistry] Could not place {def.displayName} at {coord}");
                return null;
            }

            var go = new GameObject();
            go.transform.SetParent(_pieceRoot, false);

            var view = go.AddComponent<PieceView>();
            view.Init(piece, _grid);
            _views[piece] = view;
            return view;
        }

        /// <summary>Removes a piece from the board and plays its death animation.</summary>
        public void Kill(Piece piece)
        {
            var view = GetView(piece);
            _state.Remove(piece);
            _views.Remove(piece);

            if (view != null)
                view.PlayDeath();
        }

        /// <summary>Takes every piece off the board at once, with no death animation.</summary>
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
        /// Swaps a piece for another kind on the same square — promotion. The old
        /// body disappears and the new one pops in.
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
