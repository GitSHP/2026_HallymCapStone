using Promotion.Feel;
using Promotion.Gameplay;
using UnityEngine;

namespace Promotion.Core
{
    public class BoardGrid : MonoBehaviour   // 체스판 격자를 만들고, 칸 좌표(Coord)와 월드 좌표(Vector3)를 서로 바꿔주는 클래스
    // -> 좌표 변환은 이 클래스만 담당함. width/height/tileSize를 바꾸면 다른 코드 수정 없이 보드 전체가 다시 배치됨
    // -> 칸이 규칙적으로 놓여 있어서 Collider/Raycast 없이 계산만으로 마우스가 가리키는 칸을 찾음
    {
        [Header("크기")]
        public int width = 8;   // 보드의 가로 칸 수
        public int height = 8;   // 보드의 세로 칸 수
        public float tileSize = 1f;   // 칸 한 변의 길이 (월드 유닛)

        [Header("체크무늬 색상")]
        public Color lightColor = new Color(0.93f, 0.87f, 0.74f);   // 밝은 칸 색
        public Color darkColor = new Color(0.55f, 0.28f, 0.30f);   // 어두운 칸 색

        [Header("보드 아트")]
        [Tooltip("타일 아래에 그리는 선택적 보드 그림이다. 지정하면 타일은 투명해지고 " +
                 "하이라이트만 표시한다. 피벗은 8x8 칸의 중앙에 있어야 하고, " +
                 "한 칸이 1유닛이어야 한다(Pixels Per Unit = 칸 한 변의 픽셀 수).")]
        public Sprite boardArt;   // 타일 아래에 깔 보드 그림 (없으면 체크무늬 색으로 칸을 칠함)

        [Header("등장 연출")]
        public bool animateOnStart = true;   // 시작할 때 칸이 하나씩 튀어나오는 등장 연출을 재생할지 여부

        public const int TileSortingOrder = 0;   // 타일의 그리기 순서 (보드 그림은 이보다 1 낮게, 기물은 이보다 높게 그림)
        const string TileRootName = "Tiles";   // 타일들을 모아둘 자식 오브젝트 이름
        const string ArtName = "BoardArt";   // 보드 그림을 그릴 자식 오브젝트 이름

        TileView[,] _tiles;   // 칸 좌표 [x, y]로 타일을 바로 찾기 위한 2차원 배열
        Transform _tileRoot;   // 모든 타일의 부모 오브젝트
        SpriteRenderer _art;   // 보드 그림을 그리는 렌더러
        Coord _hoveredCoord = Coord.Invalid;   // 현재 마우스가 올라가 있는 칸 (없으면 Invalid)

        public bool IsBuilt => _tiles != null;   // 타일이 이미 만들어졌는지 여부

        void Awake()   // 오브젝트가 생성될 때 한 번 실행
        {
            if (!IsBuilt)   // 만약 아직 타일이 없다면 보드를 만듦 -> 다른 스크립트가 Awake에서 먼저 Build했다면 중복 생성하지 않음
                Build();
        }

        void Start()   // 첫 프레임 전에 한 번 실행
        {
            if (animateOnStart)   // 만약 등장 연출을 켜두었다면 재생
                PlayIntro();
        }

        public void Build()   // 보드 크기만큼 타일을 새로 만들어 배치하는 함수
        {
            EnsureTileRoot();   // 타일을 담을 부모 오브젝트를 준비하고
            ClearTiles();   // 예전 타일이 남아 있다면 모두 지우고
            EnsureArt();   // 보드 그림을 준비한 뒤
            _tiles = new TileView[width, height];   // 새 타일 배열을 만듦

            for (int y = 0; y < height; y++)   // 모든 행과
            for (int x = 0; x < width; x++)   // 모든 열을 돌면서 칸 하나씩 생성
            {
                var coord = new Coord(x, y);   // 이 칸의 좌표
                var go = new GameObject();   // 타일 오브젝트를 만들고
                go.transform.SetParent(_tileRoot, false);   // 타일 부모 아래에 넣고
                go.transform.localPosition = CoordToLocal(coord);   // 칸 좌표에 맞는 위치로 옮김

                var tile = go.AddComponent<TileView>();   // 칸 표시·하이라이트를 담당하는 TileView를 붙임
                bool isLight = (x + y) % 2 == 1;   // x+y가 홀수면 밝은 칸 -> 실제 체스판처럼 (0,0) 왼쪽 아래 칸이 어두운 칸이 됨
                var colour = boardArt != null ? Color.clear : (isLight ? lightColor : darkColor);   // 보드 그림이 있으면 타일은 투명, 없으면 체크무늬 색
                tile.Init(coord, colour, TileSortingOrder);   // 타일 초기화

                _tiles[x, y] = tile;   // 배열에 저장
            }
        }

        void EnsureTileRoot()   // 타일을 담을 부모 오브젝트("Tiles")를 찾거나 새로 만드는 함수
        // -> 타일을 전용 부모 아래에 두면 보드를 다시 만들 때 기물 등 다른 자식 오브젝트는 건드리지 않음
        {
            if (_tileRoot != null)   // 만약 이미 가지고 있다면 할 일 없음
                return;

            var existing = transform.Find(TileRootName);   // 씬에 이미 만들어져 있는지 찾아보고
            if (existing != null)   // 있다면 그것을 사용
            {
                _tileRoot = existing;
                return;
            }

            var go = new GameObject(TileRootName);   // 없다면 새로 만들어
            go.transform.SetParent(transform, false);   // 보드의 자식으로 넣음
            _tileRoot = go.transform;
        }

        public void SetBoardArt(Sprite art)   // 스테이지마다 다른 보드 그림으로 교체하는 함수
        {
            if (art == null || art == boardArt)   // 만약 그림이 없거나 이미 같은 그림이라면 바꿀 필요 없음
                return;

            boardArt = art;   // 새 그림을 저장하고
            EnsureArt();   // 렌더러에 반영한 뒤
            if (_tiles != null)   // 이미 타일이 있다면
                foreach (var tile in _tiles)
                    tile.SetBaseColor(Color.clear);   // 타일 바탕색을 투명하게 -> 아래 보드 그림이 보이고 타일은 하이라이트만 표시
        }

        void EnsureArt()   // 보드 그림 렌더러를 준비하고 현재 boardArt 설정에 맞게 켜고 끄는 함수
        {
            if (_art == null)   // 만약 렌더러를 아직 모른다면 씬에 있는 것을 찾아봄
            {
                var existing = transform.Find(ArtName);
                if (existing != null)
                    _art = existing.GetComponent<SpriteRenderer>();
            }

            if (boardArt == null)   // 만약 보드 그림을 쓰지 않는다면
            {
                if (_art != null)
                    _art.enabled = false;   // 렌더러를 꺼서 숨기고 종료
                return;
            }

            if (_art == null)   // 그림은 있는데 렌더러가 없다면 새로 만듦
            {
                var go = new GameObject(ArtName);
                go.transform.SetParent(transform, false);
                _art = go.AddComponent<SpriteRenderer>();
                _art.sharedMaterial = PlaceholderArt.UnlitMaterial;   // 조명 영향을 받지 않는 재질 -> 2D 조명 설정과 상관없이 원래 색으로 보이게 함
            }

            _art.enabled = true;   // 렌더러를 켜고
            _art.sprite = boardArt;   // 그림을 지정하고
            _art.sortingOrder = TileSortingOrder - 1;   // 타일보다 아래에 그리도록 순서를 한 단계 낮춤
            _art.transform.localPosition = Vector3.zero;   // 보드 중앙에 맞추고
            _art.transform.localScale = Vector3.one * tileSize;   // 칸 크기에 맞게 크기 조절
        }

        void ClearTiles()   // 기존 타일 오브젝트를 모두 삭제하는 함수
        {
            if (_tileRoot == null)
                return;

            for (int i = _tileRoot.childCount - 1; i >= 0; i--)   // 뒤에서부터 지움 -> 앞에서부터 지우면 인덱스가 당겨져 일부를 건너뛸 수 있음
            {
                var child = _tileRoot.GetChild(i).gameObject;
                if (Application.isPlaying)   // 만약 플레이 중이라면 일반 삭제
                    Destroy(child);
                else   // 에디터(플레이 전)에서는 Destroy가 동작하지 않으므로 즉시 삭제
                    DestroyImmediate(child);
            }
            _tiles = null;   // 타일 배열도 비움 -> IsBuilt가 false가 됨
        }

        public void PlayIntro()   // 보드 등장 연출을 재생하는 함수
        // -> 칸들이 한쪽 모서리부터 차례로 튀어나와 보드가 스스로 조립되는 것처럼 보이게 함
        {
            if (_art != null && _art.enabled)   // 만약 보드 그림을 쓰고 있다면 그림부터 튀어나오게 함
                Fx.PopIn(_art.transform, 0, tileSize);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                Fx.PopIn(_tiles[x, y].transform, x + y);   // x+y를 순서 값으로 넘김 -> 왼쪽 아래 모서리에서 대각선 물결처럼 퍼져나감
        }

        public bool IsInside(Coord c) => c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;   // 좌표가 보드 안쪽 칸인지 확인하는 함수

        Vector3 CoordToLocal(Coord c)   // 칸 좌표를 보드 기준 로컬 위치로 바꾸는 함수
        {
            float originX = -(width - 1) * 0.5f * tileSize;   // (0,0) 칸의 x 위치 -> 보드 전체가 이 오브젝트의 중앙에 오도록 절반만큼 왼쪽으로 이동
            float originY = -(height - 1) * 0.5f * tileSize;   // (0,0) 칸의 y 위치 -> 같은 이유로 절반만큼 아래로 이동
            return new Vector3(originX + c.x * tileSize, originY + c.y * tileSize, 0f);   // 시작점에서 칸 수 × 칸 크기만큼 떨어진 위치
        }

        public Vector3 CoordToWorld(Coord c) => transform.TransformPoint(CoordToLocal(c));   // 칸 좌표를 월드 위치로 바꾸는 함수 (기물을 칸 위에 놓을 때 사용)

        public Coord WorldToCoord(Vector3 world)   // 월드 위치(예: 마우스 위치)가 어느 칸인지 계산하는 함수 -> 보드 밖이면 Invalid
        {
            var local = transform.InverseTransformPoint(world);   // 월드 위치를 보드 기준 로컬 위치로 바꾸고
            float originX = -(width - 1) * 0.5f * tileSize;   // CoordToLocal과 같은 시작점을 구한 뒤
            float originY = -(height - 1) * 0.5f * tileSize;

            int x = Mathf.RoundToInt((local.x - originX) / tileSize);   // 시작점에서 몇 칸 떨어져 있는지 반올림 -> 칸 중앙 기준으로 가장 가까운 칸
            int y = Mathf.RoundToInt((local.y - originY) / tileSize);

            var coord = new Coord(x, y);
            return IsInside(coord) ? coord : Coord.Invalid;   // 보드 안쪽이면 그 칸을, 아니면 Invalid를 돌려줌
        }

        public TileView GetTile(Coord c) => IsInside(c) ? _tiles[c.x, c.y] : null;   // 칸 좌표의 타일을 돌려주는 함수 (보드 밖이면 null)

        public void ClearHighlights()   // 모든 칸의 하이라이트(이동 가능·잡기 표시)를 지우는 함수
        {
            if (!IsBuilt)   // 만약 타일이 아직 없다면 지울 것도 없음
                return;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _tiles[x, y].SetHighlight(TileHighlight.None);   // 하이라이트 없음으로 되돌림
        }

        public void SetHover(Coord coord)   // 마우스가 올라간 칸 표시를 새 칸으로 옮기는 함수
        {
            if (coord == _hoveredCoord)   // 만약 같은 칸이라면 바꿀 필요 없음 (매 프레임 호출되므로 불필요한 작업 방지)
                return;

            if (IsInside(_hoveredCoord))   // 이전 칸이 보드 안쪽이었다면
                _tiles[_hoveredCoord.x, _hoveredCoord.y].SetHovered(false);   // 이전 칸의 호버 표시를 끄고

            _hoveredCoord = coord;   // 새 칸을 기억한 뒤

            if (IsInside(coord))   // 새 칸이 보드 안쪽이라면
                _tiles[coord.x, coord.y].SetHovered(true);   // 호버 표시를 켬
        }
    }
}
