using Promotion.Feel;
using Promotion.Gameplay;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// 격자 좌표가 월드 공간에 어떻게 대응하는지 아는 유일한 클래스다.
    /// 타일을 생성하고, Coord와 Vector3 사이를 변환하며, 범위 검사에 답한다.
    /// width/height/tileSize를 바꾸면 다른 수정 없이 보드 전체가 다시 배치된다.
    /// </summary>
    public class BoardGrid : MonoBehaviour
    {
        [Header("크기")]
        public int width = 8;
        public int height = 8;
        public float tileSize = 1f;

        [Header("체크무늬 색상")]
        public Color lightColor = new Color(0.93f, 0.87f, 0.74f);
        public Color darkColor = new Color(0.55f, 0.28f, 0.30f);

        [Header("보드 아트")]
        [Tooltip("타일 아래에 그리는 선택적 보드 그림이다. 지정하면 타일은 투명해지고 " +
                 "하이라이트만 표시한다. 피벗은 8x8 칸의 중앙에 있어야 하고, " +
                 "한 칸이 1유닛이어야 한다(Pixels Per Unit = 칸 한 변의 픽셀 수).")]
        public Sprite boardArt;

        [Header("등장 연출")]
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
                // 표준 체스판 배색이다. a1 (0,0)은 어두운 칸이다.
                bool isLight = (x + y) % 2 == 1;
                var colour = boardArt != null ? Color.clear : (isLight ? lightColor : darkColor);
                tile.Init(coord, colour, TileSortingOrder);

                _tiles[x, y] = tile;
            }
        }

        /// <summary>
        /// 타일은 전용 루트 아래에 두므로, 보드를 다시 만들어도 기물 컨테이너 같은
        /// 형제 오브젝트는 건드리지 않는다.
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

        /// <summary>자체 보드 그림을 가진 스테이지를 위해 보드 그림을 교체한다.</summary>
        public void SetBoardArt(Sprite art)
        {
            if (art == null || art == boardArt)
                return;

            boardArt = art;
            EnsureArt();
            if (_tiles != null)
                foreach (var tile in _tiles)
                    tile.SetBaseColor(Color.clear);
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

        /// <summary>타일이 먼 모서리부터 차례로 튀어나와, 진입 시 보드가 스스로 조립되는 것처럼 보이게 한다.</summary>
        public void PlayIntro()
        {
            if (_art != null && _art.enabled)
                Fx.PopIn(_art.transform, 0, tileSize);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                Fx.PopIn(_tiles[x, y].transform, x + y);
        }

        // ---------- 좌표 변환 ----------

        public bool IsInside(Coord c) => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

        Vector3 CoordToLocal(Coord c)
        {
            // 크기와 상관없이 보드를 이 transform의 중앙에 맞춘다.
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

        // ---------- 타일 접근 ----------

        public TileView GetTile(Coord c) => IsInside(c) ? _tiles[c.x, c.y] : null;

        public void ClearHighlights()
        {
            if (!IsBuilt)
                return;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _tiles[x, y].SetHighlight(TileHighlight.None);
        }

        /// <summary>호버를 새 칸으로 옮기고 이전 칸의 호버는 해제한다.</summary>
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
