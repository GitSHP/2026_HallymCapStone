using Gambonanza.Feel;
using Gambonanza.Gameplay;
using UnityEngine;

namespace Gambonanza.Core
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

        [Header("Intro")]
        public bool animateOnStart = true;

        public const int TileSortingOrder = 0;
        const string TileRootName = "Tiles";

        TileView[,] _tiles;
        Transform _tileRoot;
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
                tile.Init(coord, isLight ? lightColor : darkColor, TileSortingOrder);

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
