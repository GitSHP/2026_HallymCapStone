using Promotion.Feel;
using Promotion.Gameplay;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// The only class that knows how grid coordinates map to world space.
    /// Builds the tiles, converts between Coord and Vector3, and answers bounds checks.
    /// Changing width/height/tileSize re-lays the whole board with no other edits.
    /// </summary>
    public class BoardGrid : MonoBehaviour
    {
        [Header("Dimensions")]
        public int width = 8;
        public int height = 8;
        public float tileSize = 1f;

        [Header("Checker colours")]
        public Color lightColor = new Color(0.93f, 0.87f, 0.74f);
        public Color darkColor = new Color(0.55f, 0.28f, 0.30f);

        [Header("Board art")]
        [Tooltip("Optional board picture drawn under the tiles. With it the tiles turn transparent " +
                 "and only carry highlights. Its pivot must sit at the centre of the 8x8 squares, " +
                 "and one square must be one unit (Pixels Per Unit = square size in pixels).")]
        public Sprite boardArt;

        [Header("Intro")]
        public bool animateOnStart = true;

        public const int TileSortingOrder = 0;
        const string TileRootName = "Tiles";
        const string ArtName = "BoardArt";

        TileView[,] _tiles;
        Transform _tileRoot;
        SpriteRenderer _art;
        Coord _hoveredCoord = Coord.Invalid;

        public bool IsBuilt => _tiles != null;

        void Awake()
        {
            if (!IsBuilt)
                Build();
        }

        void Start()
        {
            if (animateOnStart)
                PlayIntro();
        }

        public void Build()
        {
            EnsureTileRoot();
            ClearTiles();
            EnsureArt();
            _tiles = new TileView[width, height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var coord = new Coord(x, y);
                var go = new GameObject();
                go.transform.SetParent(_tileRoot, false);
                go.transform.localPosition = CoordToLocal(coord);

                var tile = go.AddComponent<TileView>();
                // Standard chessboard parity: a1 (0,0) is a dark square.
                bool isLight = (x + y) % 2 == 1;
                var colour = boardArt != null ? Color.clear : (isLight ? lightColor : darkColor);
                tile.Init(coord, colour, TileSortingOrder);

                _tiles[x, y] = tile;
            }
        }

        /// <summary>
        /// Tiles live under their own root so rebuilding the board never touches
        /// siblings such as the piece container.
        /// </summary>
        void EnsureTileRoot()
        {
            if (_tileRoot != null)
                return;

            var existing = transform.Find(TileRootName);
            if (existing != null)
            {
                _tileRoot = existing;
                return;
            }

            var go = new GameObject(TileRootName);
            go.transform.SetParent(transform, false);
            _tileRoot = go.transform;
        }

        void EnsureArt()
        {
            if (_art == null)
            {
                var existing = transform.Find(ArtName);
                if (existing != null)
                    _art = existing.GetComponent<SpriteRenderer>();
            }

            if (boardArt == null)
            {
                if (_art != null)
                    _art.enabled = false;
                return;
            }

            if (_art == null)
            {
                var go = new GameObject(ArtName);
                go.transform.SetParent(transform, false);
                _art = go.AddComponent<SpriteRenderer>();
                _art.sharedMaterial = PlaceholderArt.UnlitMaterial;
            }

            _art.enabled = true;
            _art.sprite = boardArt;
            _art.sortingOrder = TileSortingOrder - 1;
            _art.transform.localPosition = Vector3.zero;
            _art.transform.localScale = Vector3.one * tileSize;
        }

        void ClearTiles()
        {
            if (_tileRoot == null)
                return;

            for (int i = _tileRoot.childCount - 1; i >= 0; i--)
            {
                var child = _tileRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
            _tiles = null;
        }

        /// <summary>Tiles pop in from the far corner so the board assembles itself on entry.</summary>
        public void PlayIntro()
        {
            if (_art != null && _art.enabled)
                Fx.PopIn(_art.transform, 0, tileSize);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                Fx.PopIn(_tiles[x, y].transform, x + y);
        }

        // ---------- Coordinate conversion ----------

        public bool IsInside(Coord c) => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

        Vector3 CoordToLocal(Coord c)
        {
            // Centre the board on this transform regardless of dimensions.
            float originX = -(width - 1) * 0.5f * tileSize;
            float originY = -(height - 1) * 0.5f * tileSize;
            return new Vector3(originX + c.x * tileSize, originY + c.y * tileSize, 0f);
        }

        public Vector3 CoordToWorld(Coord c) => transform.TransformPoint(CoordToLocal(c));

        public Coord WorldToCoord(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            float originX = -(width - 1) * 0.5f * tileSize;
            float originY = -(height - 1) * 0.5f * tileSize;

            int x = Mathf.RoundToInt((local.x - originX) / tileSize);
            int y = Mathf.RoundToInt((local.y - originY) / tileSize);

            var coord = new Coord(x, y);
            return IsInside(coord) ? coord : Coord.Invalid;
        }

        // ---------- Tile access ----------

        public TileView GetTile(Coord c) => IsInside(c) ? _tiles[c.x, c.y] : null;

        public void ClearHighlights()
        {
            if (!IsBuilt)
                return;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _tiles[x, y].SetHighlight(TileHighlight.None);
        }

        /// <summary>Moves the hover to a new square, clearing the previous one.</summary>
        public void SetHover(Coord coord)
        {
            if (coord == _hoveredCoord)
                return;

            if (IsInside(_hoveredCoord))
                _tiles[_hoveredCoord.x, _hoveredCoord.y].SetHovered(false);

            _hoveredCoord = coord;

            if (IsInside(coord))
                _tiles[coord.x, coord.y].SetHovered(true);
        }
    }
}
