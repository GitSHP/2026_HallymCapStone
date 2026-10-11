using Promotion.Core;
using Promotion.Feel;
using TMPro;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 기물의 시각적 본체다. 그리는 모든 것을 PieceDefinition에서 읽으므로
    /// 나중에 실제 스프라이트를 지정해도 여기를 고칠 필요가 없다.
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
            name = $"{piece.Team}_{piece.Def.itemName}";

            _body = gameObject.AddComponent<SpriteRenderer>();

            // 체스 대결의 흑 기물은 전용 이미지를 쓴다. 그 밖의 경우는
            // 아군 이미지에 진영 색을 입혀 대신 쓴다.
            bool enemyArt = piece.Team == Team.Enemy && piece.Def.enemySprite != null;
            var art = enemyArt ? piece.Def.enemySprite : piece.Def.itemImage;
            bool hasArt = art != null;
            _body.sprite = hasArt ? art : PlaceholderArt.Circle;
            _bodyColor = enemyArt ? Color.white
                : piece.Team == Team.Player ? piece.Def.playerColor : piece.Def.enemyColor;
            _body.color = _bodyColor;
            _body.sortingOrder = PieceSortingOrder;
            _body.sharedMaterial = PlaceholderArt.UnlitMaterial;

            // 글리프는 임시 표식이다. 실제 이미지가 있으면 필요 없다.
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

            // 픽셀 아트는 한 칸을 꽉 채우게 그려져 있다. 임시 원판은 여백이 필요하다.
            BaseScale = hasArt ? 1f : 0.82f;
            transform.localScale = Vector3.one * BaseScale;
            SnapToCoord();
        }

        public float BaseScale { get; private set; } = 0.82f;

        public void SnapToCoord()
            => transform.position = _grid.CoordToWorld(Piece.Coord);

        public void SetSortingBoost(bool lifted)
            => _body.sortingOrder = lifted ? PieceSortingOrder + 20 : PieceSortingOrder;

        public void Flash() => Fx.HitFlash(_body, _bodyColor);

        public void PlayDeath()
            => Fx.Die(transform, () => Destroy(gameObject));
    }
}
