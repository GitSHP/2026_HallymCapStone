using System.Collections.Generic;
using Gambonanza.Core;
using Gambonanza.Data;
using Gambonanza.Feel;
using UnityEngine;

namespace Gambonanza.Gameplay
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

        public void ClearAll()
        {
            foreach (var view in _views.Values)
                if (view != null)
                    Destroy(view.gameObject);

            _views.Clear();
        }
    }
}
