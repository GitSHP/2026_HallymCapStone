using System;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class DeployController : MonoBehaviour  // 기물 카드(덱)에서 기물을 골라 보드에 놓는 것(배치/소환)을 담당하는 클래스
    // 배치 단계: 전투 시작 전 단계 -> 일반 스테이지에서는 킹만 놓을 수 있고 비용이 들지 않음
    // 전투 중 소환: 플레이어 턴에 AP(한 턴에 쓸 수 있는 행동력)를 써서 나머지 기물을 보드에 놓음
    {
        [Tooltip("기물을 놓을 수 있는, 플레이어 쪽 가장자리부터의 줄 수.")]
        public int deployRows = 3;  // 기물을 놓을 수 있는 아래쪽 줄 수(0번 줄부터)

        [Tooltip("스테이지가 시작되면 항상 손에 있고 소모되지 않는 카드. " +
                 "소환할 때는 여전히 AP를 소모한다.")]
        public PieceDefinition[] alwaysAvailable = Array.Empty<PieceDefinition>();  // 써도 사라지지 않는 기본 카드 목록(예: 폰)

        [Tooltip("배치 단계에서 킹뿐 아니라 모든 카드를 놓을 수 있다(체스 대결).")]
        public bool deployAllAtSetup;   // true면 배치 단계에서 모든 카드를 놓을 수 있음(체스 대결용)

        [Tooltip("전투가 시작된 뒤 AP를 소모해 카드를 소환할 수 있다.")]
        public bool summonDuringBattle = true;  // true면 전투 중 AP를 써서 소환 가능(일반 스테이지용)

        public event Action DeckChanged;    // 덱 내용이나 선택한 카드가 바뀔 때 발생하는 이벤트 -> HUD의 카드 목록이 갱신됨

        readonly List<PieceDefinition> _deck = new();   // 현재 손에 있는 카드(기물 종류) 목록
        readonly List<bool> _unlimited = new(); // 같은 순서로 각 카드가 무제한(써도 안 사라짐)인지 저장하는 목록

        public IReadOnlyList<PieceDefinition> Deck => _deck;    // 외부(HUD)에서 덱을 읽기만 할 수 있게 열어 둔 속성
        public int SelectedIndex { get; private set; } = -1;    // 현재 선택한 카드의 순번(-1이면 선택 안 함)

        public PieceDefinition Selected =>
            SelectedIndex >= 0 && SelectedIndex < _deck.Count ? _deck[SelectedIndex] : null;
        // 현재 선택한 카드의 기물 종류(선택 안 했거나 범위를 벗어나면 null)

        BoardGrid _grid;    // 칸 좌표 변환과 칸 강조 표시용
        BoardState _state;  // 칸이 비어 있는지 확인하는 보드 데이터
        PieceRegistry _registry;    // 기물을 실제로 생성하는 컴포넌트
        BoardInput _input;  // 마우스 클릭과 포인터가 올라간 칸을 알려 주는 컴포넌트
        DragController _drag;   // 기물 드래그 중인지 확인용 -> 드래그 중에는 소환하지 않음
        TurnManager _turn;  // 현재 단계와 AP를 확인/소모하는 컴포넌트

        void Awake() => Resolve();  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
            if (_input == null) _input = GetComponent<BoardInput>();
            if (_drag == null) _drag = GetComponent<DragController>();
            if (_turn == null) _turn = GetComponent<TurnManager>();
        }

        public bool IsUnlimited(int index) => index >= 0 && index < _unlimited.Count && _unlimited[index];
        // index번째 카드가 무제한 카드(써도 사라지지 않음)인지 확인하는 함수

        public void LoadDeck(IEnumerable<PieceDefinition> definitions)  // 스테이지 시작 시 덱을 새로 채우는 함수
        // 순서: 킹 -> 무제한 기본 카드 -> 나머지 카드 (킹을 맨 앞에 둬서 배치 단계에서 바로 찾을 수 있게 함)
        {
            _deck.Clear();  // 이전 덱을 비우고
            _unlimited.Clear();

            var rest = new List<PieceDefinition>(); // 킹이 아닌 카드를 잠시 모아 둘 리스트
            if (definitions != null)
            {
                foreach (var def in definitions)    // 넘겨받은 카드를 하나씩 확인
                {
                    if (def == null)    // 비어 있는 항목은 건너뜀
                        continue;

                    if (def.isObjective)    // 킹(목표 기물)이라면 바로 덱 앞쪽에 추가
                        Add(def, false);
                    else    // 나머지는 나중에 추가하려고 따로 모아 둠
                        rest.Add(def);
                }
            }

            if (summonDuringBattle) // 전투 중 소환이 가능한 스테이지라면 무제한 기본 카드를 추가
                foreach (var def in alwaysAvailable)
                    if (def != null)
                        Add(def, true);

            foreach (var def in rest)   // 모아 둔 나머지 카드를 뒤에 추가
                Add(def, false);

            SelectedIndex = -1; // 선택 해제
            DeckChanged?.Invoke();  // HUD에 덱이 바뀌었음을 알림
        }

        public void Select(int index)   // HUD에서 카드를 클릭했을 때 그 카드를 선택하는 함수
        {
            SelectedIndex = index == SelectedIndex ? -1 : index;    // 이미 선택한 카드를 다시 누르면 선택 해제, 아니면 그 카드 선택
            ShowDeployableTiles();  // 놓을 수 있는 칸을 강조 표시
            DeckChanged?.Invoke();
        }

        public int CostOf(PieceDefinition def)  // 지금 이 기물을 놓을 때 드는 AP를 돌려주는 함수
        {
            if (def == null || _turn == null)
                return 0;
            return _turn.Phase == TurnPhase.Deploy ? 0 : def.deployApCost;  // 배치 단계에서는 무료, 전투 중에는 기물에 정해진 AP 비용
        }

        public bool IsDeployableNow(PieceDefinition def)    // 지금 단계에서 이 기물을 놓을 수 있는지 검사하는 함수(칸은 따지지 않음)
        {
            if (def == null || _turn == null)
                return false;

            if (_turn.Phase == TurnPhase.Deploy)    // 배치 단계라면 킹이거나, 모든 카드 배치가 허용된 경우(체스 대결)만 가능
                return def.isObjective || deployAllAtSetup;

            return summonDuringBattle && _turn.Phase == TurnPhase.PlayerTurn;   // 전투 중이라면 소환이 허용되고 플레이어 턴일 때만 가능
        }

        public bool CanDeploy(PieceDefinition def, Coord coord) // 이 기물을 이 칸에 놓을 수 있는지 검사하는 함수
        {
            if (!IsDeployableNow(def))  // 지금 단계에서 놓을 수 없는 기물이라면 불가
                return false;

            return _grid.IsInside(coord)    // 보드 안의 칸이고
                   && coord.y < Mathf.Clamp(deployRows, 1, _grid.height)   // 배치 가능한 아래쪽 줄 안에 있고
                   && _state.IsEmpty(coord);    // 비어 있는 칸이어야 함
        }

        public bool CanDeployAt(Coord coord) => CanDeploy(Selected, coord); // 현재 선택한 카드를 이 칸에 놓을 수 있는지 검사하는 함수

        void Update()   // 매 프레임 실행 -> 카드를 선택한 상태에서 칸을 클릭하면 그 칸에 기물을 놓음
        {
            Resolve();

            if (_turn == null || _input == null || Selected == null)    // 필요한 컴포넌트가 없거나 선택한 카드가 없다면 종료
                return;

            if (!InputGate.WorldInputEnabled)   // UI 창이 열려 있어 보드 입력이 막혀 있다면 종료
                return;

            if (_drag != null && _drag.IsDragging)  // 기물을 드래그하는 중이라면 종료 -> 드래그와 소환이 겹치지 않게
                return;

            if (!_input.PressedThisFrame)   // 이번 프레임에 클릭하지 않았다면 종료
                return;

            TryDeployAt(_input.HoveredCoord);   // 클릭한 칸에 기물 놓기 시도
        }

        public bool TryDeployAt(Coord coord)    // 선택한 카드의 기물을 이 칸에 놓아 보는 함수 -> 성공하면 true
        {
            Resolve();

            var def = Selected; // 선택한 카드의 기물 종류
            if (def == null || !CanDeployAt(coord)) // 선택한 카드가 없거나 놓을 수 없는 칸이라면 실패
                return false;

            int cost = CostOf(def); // 필요한 AP
            if (cost > 0 && !_turn.TrySpend(cost))  // AP가 필요한데 남은 AP가 부족하다면 실패(충분하면 여기서 AP가 차감됨)
                return false;

            var view = _registry.Spawn(def, Team.Player, coord);    // 그 칸에 아군 기물을 생성
            if (view == null)
                return false;

            Fx.Drop(view.transform, _grid.CoordToWorld(coord), transform, view.BaseScale);
            // 기물이 칸에 탁 내려앉는 연출(보드도 살짝 흔들림)

            if (!IsUnlimited(SelectedIndex))    // 무제한 카드가 아니라면 사용한 카드를 덱에서 제거
            {
                _deck.RemoveAt(SelectedIndex);
                _unlimited.RemoveAt(SelectedIndex);
            }

            SelectedIndex = -1; // 선택 해제하고
            _grid.ClearHighlights();    // 칸 강조 표시를 지우고
            DeckChanged?.Invoke();  // HUD에 덱이 바뀌었음을 알림
            return true;
        }

        void Add(PieceDefinition def, bool unlimited)   // 덱에 카드 한 장을 추가하는 함수 -> 무제한 여부도 같은 순서로 저장
        {
            _deck.Add(def);
            _unlimited.Add(unlimited);
        }

        void ShowDeployableTiles()  // 선택한 카드를 놓을 수 있는 칸을 모두 강조 표시하는 함수
        {
            _grid.ClearHighlights();    // 이전 강조 표시를 지우고
            if (Selected == null)   // 선택한 카드가 없다면 여기서 종료
                return;

            int rows = Mathf.Clamp(deployRows, 1, _grid.height);   // 배치 가능한 줄 수(최소 1줄, 최대 보드 높이)
            int index = 0;  // 강조 표시 순번 -> 칸마다 조금씩 늦게 켜져서 물결처럼 보이게 함

            for (int y = 0; y < rows; y++)  // 배치 가능한 줄의
            for (int x = 0; x < _grid.width; x++)   // 모든 칸을 검사해서
            {
                var coord = new Coord(x, y);
                if (!CanDeploy(Selected, coord))    // 놓을 수 없는 칸은 건너뛰고
                    continue;

                var tile = _grid.GetTile(coord);
                if (tile != null)   // 놓을 수 있는 칸은 이동 가능 색으로 강조 표시
                    tile.SetHighlight(TileHighlight.Move, index++);
            }
        }
    }
}
