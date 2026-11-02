using System.Collections;
using System.Collections.Generic;
using Promotion.Data;
using Promotion.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Tutorial
{
    public class PieceGuideView : MonoBehaviour // 기물 가이드 창(이동 칸 그림, 설명 페이지)을 담당하는 클래스
    // -> 레이아웃은 PieceGuide 프리팹이 원본이고, 이 스크립트는 창을 켜고 끄며 내용만 채움
    // -> 게임이 멈춘 상태(timeScale 0)에서 열리므로 연출은 실제 시간(unscaled)으로 동작함
    {
        const int BoardSize = 7; // 이동 칸 그림의 한 변 칸 수 (7×7)
        const int Center = BoardSize / 2; // 기물이 서 있는 가운데 칸 번호 (3)

        static readonly Color LightCell = new Color(0.20f, 0.27f, 0.40f, 1f); // 밝은 칸 색
        static readonly Color DarkCell = new Color(0.13f, 0.18f, 0.28f, 1f); // 어두운 칸 색
        static readonly Color MoveCell = new Color(0.30f, 0.78f, 0.40f, 1f); // 이동할 수 있는 칸 색 (초록) -> 보드의 이동 가능 표시와 같은 계열
        static readonly Color CaptureCell = new Color(0.86f, 0.30f, 0.30f, 1f); // 잡을 때만 갈 수 있는 칸 색 (빨강) -> 폰의 대각선
        static readonly Color PieceCell = new Color(0.949f, 0.737f, 0.318f, 1f); // 기물이 서 있는 칸 색 (금색)

        [Header("창")]
        [SerializeField] CanvasGroup _group; // 창 전체(어두운 배경 포함)의 투명도 -> 나타나는 연출에 사용
        [SerializeField] RectTransform _panel; // 가운데 설명 상자 -> 커지며 나타나는 연출에 사용

        [Header("제목")]
        [SerializeField] Image _icon; // 기물 그림
        [SerializeField] TextMeshProUGUI _title; // 기물 이름
        [SerializeField] TextMeshProUGUI _cost; // 소환·이동 AP 비용 (기물 데이터에서 채움)

        [Header("이동 칸 그림")]
        [SerializeField] RectTransform _board; // 7×7 칸이 깔리는 부모 (GridLayoutGroup)
        [SerializeField] Image _cellTemplate; // 칸 하나의 원본 -> 꺼 둔 상태로 두고 복제해서 씀

        [Header("설명")]
        [SerializeField] TextMeshProUGUI _body; // 설명 본문
        [SerializeField] TextMeshProUGUI _pageLabel; // 페이지 표시 (예: 1 / 3)
        [SerializeField] Button _nextButton; // [다음] / [확인] 버튼
        [SerializeField] TextMeshProUGUI _nextLabel; // 버튼 글자

        readonly List<Image> _cells = new(); // 복제해서 만든 칸 목록 -> 다음 가이드에서 다시 씀
        PieceGuideData _guide; // 지금 보여 주는 가이드
        int _page; // 지금 보여 주는 페이지 번호 (0부터)
        bool _open; // 창이 열려 있는지 여부

        void Awake()
        {
            _nextButton.onClick.AddListener(Next); // 버튼을 누르면 다음 페이지로 넘기거나 창을 닫음
            _cellTemplate.gameObject.SetActive(false); // 원본 칸은 화면에 보이지 않게 함
        }

        public void Hide() // 창을 즉시 숨기는 함수 -> 씬이 시작될 때 PieceGuideDirector가 호출
        {
            _open = false;
            gameObject.SetActive(false);
        }

        public IEnumerator Show(PieceGuideData guide)
        // 가이드 창을 열고 플레이어가 마지막 페이지에서 [확인]을 누를 때까지 기다리는 코루틴
        // guide: 보여 줄 기물 가이드 (PieceGuideCatalog에서 찾은 것)
        {
            _guide = guide;
            _page = 0;
            _open = true;
            gameObject.SetActive(true); // 창을 켜고

            var piece = guide.piece;
            var art = ArtOf(guide);
            _title.text = piece.itemName; // 기물 이름과
            _icon.sprite = art; // 기물 그림을 채우고
            _icon.enabled = art != null; // 그림이 없다면 그림 칸을 비워 둠
            _cost.text = guide.enemy ? EnemyInfo(piece) // 적이라면 비용 대신 적 정보(전진 칸 수, 잡을 수 없는 기물)를 보여 줌
                : "소환 " + piece.deployApCost + " AP  ·  이동 " + piece.moveApCost + " AP"; // 비용은 기물 데이터를 그대로 보여 줌 -> 밸런스를 바꿔도 설명이 어긋나지 않음

            BuildBoard(guide); // 행마법 데이터로 이동 칸 그림을 그리고
            ShowPage(); // 첫 페이지를 보여 줌

            Fx.GuideOpen(_panel, _group); // 창이 커지며 나타나는 연출

            while (_open) // [확인]을 누를 때까지 기다림 (게임 시간이 멈춰 있어도 프레임은 계속 돌아감)
                yield return null;
        }

        void Next() // [다음] / [확인] 버튼을 눌렀을 때 실행되는 함수
        {
            if (_page < PageCount - 1) // 만약 아직 남은 페이지가 있다면
            {
                _page++; // 다음 페이지로 넘기고
                ShowPage();
                Fx.GuidePage(_body); // 본문이 살짝 떠오르며 나타나는 연출
                return;
            }

            Hide(); // 마지막 페이지라면 창을 닫음 -> Show 코루틴의 기다림이 끝남
        }

        int PageCount // 설명 페이지 수 (비어 있어도 1장으로 침 -> 이동 칸 그림만이라도 보여 주고 닫을 수 있게)
        {
            get { return Mathf.Max(1, _guide.pages.Length); }
        }

        void ShowPage() // 현재 페이지의 본문과 페이지 표시, 버튼 글자를 채우는 함수
        {
            _body.text = _guide.pages.Length > 0 ? _guide.pages[_page] : "";
            _pageLabel.text = (_page + 1) + " / " + PageCount;
            _pageLabel.gameObject.SetActive(PageCount > 1); // 한 장뿐이라면 페이지 표시는 숨김
            _nextLabel.text = _page < PageCount - 1 ? "다음" : "확인"; // 마지막 장이면 [확인]
        }

        // ---------- 이동 칸 그림 ----------

        static Sprite ArtOf(PieceGuideData guide)   // 가이드에 보여 줄 기물 그림 -> 적 가이드라면 적 전용 그림을 우선 씀
        {
            var piece = guide.piece;
            if (guide.enemy && piece.enemySprite != null)
                return piece.enemySprite;
            return piece.itemImage;
        }

        static string EnemyInfo(PieceDefinition piece)  // 적 가이드의 제목 아래 한 줄 정보를 만드는 함수 -> 데이터에서 읽으므로 수치를 바꿔도 설명이 맞음
        {
            string info = "적  ·  한 턴에 " + Mathf.Max(1, piece.enemyStepsPerTurn) + "칸 전진";
            if (piece.captureImmuneTo != null && piece.captureImmuneTo.Length > 0)
            {
                var names = new List<string>();
                foreach (var def in piece.captureImmuneTo)
                    if (def != null)
                        names.Add(def.itemName);
                string joined = string.Join("·", names);
                info += "  ·  " + joined + EuroOrRo(joined) + " 잡을 수 없음";
            }
            return info;
        }

        static string EuroOrRo(string word) // 앞 글자의 받침에 맞는 조사("으로"/"로")를 고르는 함수 -> 폰으로, 나이트로
        {
            if (string.IsNullOrEmpty(word))
                return "로";
            char last = word[word.Length - 1];
            if (last < '가' || last > '힣')   // 한글이 아니면 "로"
                return "로";
            int final = (last - '가') % 28; // 받침 번호 (0이면 받침 없음, 8이면 ㄹ)
            return final == 0 || final == 8 ? "로" : "으로";
        }

        void BuildBoard(PieceGuideData guide)
        // 기물의 행마법(MovePattern)을 읽어 7×7 칸에 이동할 수 있는 칸을 칠하는 함수
        // -> 빈 판 가운데에 기물 하나만 있다고 보고 계산하므로 막히는 칸은 없음
        // -> 새 기물을 에셋으로 추가해도 그림이 자동으로 맞게 나옴
        // -> 적 가이드라면 행마법 대신 한 턴에 다가올 수 있는 범위(전진 칸 수만큼 사방)를 칠함
        {
            var piece = guide.piece;
            var art = ArtOf(guide);
            var marks = new int[BoardSize, BoardSize]; // 칸마다 표시 종류 (0: 없음, 1: 이동, 2: 잡기만)

            if (guide.enemy)    // 적은 행마법이 아니라 킹을 향해 한 칸씩 걸어오므로, 한 턴에 닿는 범위를 칠함
            {
                int reach = Mathf.Max(1, piece.enemyStepsPerTurn);
                for (int x = 0; x < BoardSize; x++)
                    for (int y = 0; y < BoardSize; y++)
                        if (Mathf.Max(Mathf.Abs(x - Center), Mathf.Abs(y - Center)) <= reach)
                            marks[x, y] = 1;
            }

            foreach (var pattern in guide.enemy ? new MovePattern[0] : piece.patterns) // 기물의 이동 규칙을 하나씩 확인해서
            {
                if (pattern == null)
                    continue;

                int range = pattern.sliding ? pattern.maxDistance : 1; // 연속 이동이면 사거리만큼, 아니면 한 번만
                foreach (var dir in pattern.directions) // 규칙의 방향마다
                {
                    for (int step = 1; step <= range; step++) // 한 칸씩 나아가며
                    {
                        int x = Center + dir.x * step;
                        int y = Center + dir.y * step; // 위쪽(+y)이 플레이어 기준 '앞'
                        if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize) // 그림 밖으로 나가면 그 방향은 끝
                            break;

                        if (pattern.canMoveToEmpty) // 빈 칸으로 갈 수 있는 규칙이면 이동 칸 (이동 칸이 잡기만 칸보다 우선)
                            marks[x, y] = 1;
                        else if (pattern.canCapture && marks[x, y] == 0) // 잡을 때만 갈 수 있는 규칙이면 잡기만 칸
                            marks[x, y] = 2;
                    }
                }
            }

            EnsureCells(); // 칸 49개를 준비하고

            for (int row = 0; row < BoardSize; row++) // GridLayoutGroup은 왼쪽 위부터 채우므로 맨 윗줄(y = 6)부터 그림
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    int y = BoardSize - 1 - row;
                    var cell = _cells[row * BoardSize + x];
                    var icon = cell.transform.GetChild(0).GetComponent<Image>(); // 칸 위에 올리는 기물 그림 (원본 칸의 자식)

                    bool isPiece = x == Center && y == Center;
                    icon.gameObject.SetActive(isPiece && art != null); // 가운데 칸에만 기물 그림을 올림
                    if (isPiece)
                        icon.sprite = art;

                    if (isPiece)
                        cell.color = PieceCell;
                    else if (marks[x, y] == 1)
                        cell.color = MoveCell;
                    else if (marks[x, y] == 2)
                        cell.color = CaptureCell;
                    else
                        cell.color = (x + y) % 2 == 0 ? DarkCell : LightCell; // 표시가 없는 칸은 체스판 무늬
                }
            }
        }

        void EnsureCells() // 칸 49개를 처음 한 번만 원본에서 복제해 두는 함수
        {
            while (_cells.Count < BoardSize * BoardSize)
            {
                var cell = Instantiate(_cellTemplate, _board);
                cell.gameObject.SetActive(true);
                _cells.Add(cell);
            }
        }
    }
}
