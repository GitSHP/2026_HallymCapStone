using Gambonanza.Core;
using Gambonanza.Feel;
using TMPro;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Visual body of a piece. Reads everything it draws from the PieceDefinition,
    /// so assigning real sprites later needs no change here.
    /// </summary>
    public class PieceView : MonoBehaviour
    {
        public const int PieceSortingOrder = 10;

        public Piece Piece { get; private set; }

        BoardGrid _grid;
        SpriteRenderer _body;
        TextMeshPro _label;
        Color _bodyColor;

        public void Init(Piece piece, BoardGrid grid)
        {
            Piece = piece;
            _grid = grid;
            name = $"{piece.Team}_{piece.Def.displayName}";

            _body = gameObject.AddComponent<SpriteRenderer>();
            bool hasArt = piece.Def.sprite != null;
            _body.sprite = hasArt ? piece.Def.sprite : PlaceholderArt.Circle;
            _bodyColor = piece.Team == Team.Player ? piece.Def.playerColor : piece.Def.enemyColor;
            _body.color = _bodyColor;
            _body.sortingOrder = PieceSortingOrder;
            _body.sharedMaterial = PlaceholderArt.UnlitMaterial;

            // The glyph is placeholder signage; real art makes it redundant.
            if (!hasArt)
            {
                var labelGo = new GameObject("Glyph");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);

                _label = labelGo.AddComponent<TextMeshPro>();
                _label.text = piece.Def.glyph;
                _label.fontSize = 5f;
                _label.alignment = TextAlignmentOptions.Center;
                _label.color = piece.Team == Team.Player
                    ? new Color(0.1f, 0.1f, 0.12f)
                    : new Color(0.95f, 0.9f, 0.85f);
                _label.rectTransform.sizeDelta = new Vector2(1f, 1f);
                _label.GetComponent<MeshRenderer>().sortingOrder = PieceSortingOrder + 1;
            }

            transform.localScale = Vector3.one * 0.82f;
            SnapToCoord();
        }

        public float BaseScale => 0.82f;

        public void SnapToCoord()
            => transform.position = _grid.CoordToWorld(Piece.Coord);

        public void SetSortingBoost(bool lifted)
            => _body.sortingOrder = lifted ? PieceSortingOrder + 20 : PieceSortingOrder;

        public void Flash() => Fx.HitFlash(_body, _bodyColor);

        public void PlayDeath()
            => Fx.Die(transform, () => Destroy(gameObject));
    }
}
