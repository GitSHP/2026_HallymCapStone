using System;
using System.Collections.Generic;
using Promotion.Data;
using Promotion.Map;
using UnityEngine;

namespace Promotion.Core
{
    public class RunState : MonoBehaviour   // 한 판(런) 동안 스테이지가 끝나도 유지되는 정보를 보관하는 클래스
    // -> 골드, 플레이어 덱의 기물, 보유한 유물, 맵 진행 상황을 저장함
    // -> 씬이 바뀌어도 사라지지 않으므로 맵 씬과 전투 씬이 같은 런 정보를 읽음
    {
        static RunState _instance;   // 게임 전체에 하나만 존재하는 RunState (싱글톤)

        public static RunState Instance   // 어디서든 RunState에 접근하기 위한 속성
        // -> 처음 접근할 때 없으면 자동으로 만들어 주므로 씬에 미리 배치해 둘 필요가 없음
        {
            get
            {
                if (_instance != null)   // 만약 이미 있다면 그대로 돌려줌
                    return _instance;

                _instance = FindAnyObjectByType<RunState>();   // 없다면 씬에 놓인 것이 있는지 찾아보고
                if (_instance == null)   // 그래도 없다면
                {
                    var go = new GameObject("RunState");   // 새 오브젝트를 만들어
                    _instance = go.AddComponent<RunState>();   // RunState를 붙임 -> 이때 Awake가 실행되며 초기화됨
                }
                return _instance;
            }
        }

        [Header("시작 값")]
        [SerializeField] int startingGold = 12;   // 런을 시작할 때 가지고 시작하는 골드

        [Header("시작 덱 (킹은 덱이 아니라 스테이지가 배치한다)")]
        [SerializeField] List<PieceDefinition> startingDeck = new();   // 런을 시작할 때 가지고 시작하는 기물 목록 (인스펙터에서 설정)
        readonly ArtifactManager _artifactEffects = new ArtifactManager();   
        // 보유 아티팩트의 효과를 켜고 끄는 관리자

        readonly List<PieceDefinition> _deck = new();   // 현재 플레이어가 보유한 기물 목록 (상점에서 사면 늘어남)
        readonly List<ItemData> _artifacts = new();   // 현재 플레이어가 보유한 유물(아티팩트) 목록 -> 상점에서 구매하면 여기에 쌓임

        bool _initialised;   // 초기화를 이미 했는지 여부 -> 중복 초기화로 골드·덱이 다시 채워지는 것을 방지

        public int Gold { get; private set; }   // 현재 보유 골드 (바꿀 때는 AddGold/TrySpend를 통해서만)
        public int StageIndex { get; private set; }   // 지금까지 몇 번째 스테이지까지 진행했는지

        public IReadOnlyList<PieceDefinition> Deck => _deck;   // 보유 기물 목록을 읽기 전용으로 공개
        public IReadOnlyList<ItemData> Artifacts => _artifacts;   // 보유 유물 목록을 읽기 전용으로 공개

        public MapData Map { get; set; }   // 이번 런의 맵 데이터 -> 맵 씬이 처음 열릴 때 생성됨

        public List<int> MapPath { get; } = new();   // 지금까지 들어간 맵 노드 번호를 순서대로 담은 목록 -> 마지막 값이 현재 위치

        public int CurrentMapNode => MapPath.Count > 0 ? MapPath[MapPath.Count - 1] : -1;   // 플레이어가 현재 서 있는 맵 노드 번호 (아직 출발 전이면 -1)

        public StageDefinition CurrentStage { get; set; }   // 맵에서 선택해 들어온 전투 스테이지 정보 -> 전투 씬을 직접 열었을 때는 null

        public event Action InventoryChanged;   // 보유 기물·유물 목록이 바뀔 때 발생하는 이벤트 -> 인벤토리 UI가 이 신호를 받아 슬롯을 다시 그림
        public event Action<int, int> GoldChanged;   // 골드가 바뀔 때 발생하는 이벤트 -> (이전 값, 현재 값)을 함께 보내므로 UI가 숫자를 부드럽게 바꿀 수 있음

        void Awake()   // 오브젝트가 생성될 때 한 번 실행
        {
            if (_instance != null && _instance != this)   // 만약 이미 다른 RunState가 있다면 -> 씬을 다시 불러와서 중복으로 생긴 경우
            {
                Destroy(gameObject);   // 새로 생긴 쪽을 삭제해 하나만 유지
                return;
            }

            _instance = this;   // 자신을 유일한 RunState로 등록하고
            transform.SetParent(null);   // 부모에서 떼어 최상위로 옮김 -> DontDestroyOnLoad는 최상위 오브젝트에만 동작하기 때문
            DontDestroyOnLoad(gameObject);   // 씬이 바뀌어도 삭제되지 않도록 설정
            Initialise();   // 시작 골드·덱 설정
        }

        void Initialise()   // 런 시작 값(골드, 시작 덱)을 채우는 함수
        {
            if (_initialised)   // 만약 이미 초기화했다면 다시 하지 않음
                return;

            _initialised = true;
            Gold = startingGold;   // 시작 골드를 넣고
            _deck.Clear();   // 덱을 비운 뒤
            foreach (var piece in startingDeck)   // 시작 덱의 기물을
                if (piece != null)   // 비어있는 항목은 빼고
                    _deck.Add(piece);   // 덱에 추가
        }

        public void AddGold(int amount)   // 골드를 더하거나(양수) 빼는(음수) 함수
        {
            if (amount == 0)   // 변화가 없다면 이벤트도 보내지 않음
                return;

            int previous = Gold;   // 바뀌기 전 값을 기억하고
            Gold = Mathf.Max(0, Gold + amount);   // 골드를 바꿈 (0 아래로는 내려가지 않음)
            GoldChanged?.Invoke(previous, Gold);   // 골드 변경 이벤트를 보내 UI 등에 알림
        }

        public bool CanAfford(int cost) => Gold >= cost;   // 그 가격을 낼 수 있는지 확인하는 함수

        public bool TrySpend(int cost)   // 골드를 지불하는 함수 -> 성공하면 true
        // -> 가격 전체를 낼 수 있을 때만 지불하므로 일부만 빠져나가는 일은 없음
        {
            if (cost < 0 || !CanAfford(cost))   // 만약 가격이 음수(잘못된 값)이거나 골드가 부족하다면 지불 실패
                return false;

            AddGold(-cost);   // 가격만큼 골드를 빼고
            return true;   // 성공을 알림
        }

        public void AddPiece(PieceDefinition piece)   // 보유 기물(인벤토리)에 기물을 하나 추가하는 함수 (상점 구매 등)
        {
            if (piece == null)
                return;
            _deck.Add(piece);
            InventoryChanged?.Invoke();   // 인벤토리 UI에 목록이 바뀌었음을 알림
        }

        public bool RemovePiece(PieceDefinition piece)   // 보유 기물에서 그 기물을 하나 지우는 함수 -> 지웠으면 true
        // -> 기물이 영구히 사라지는 경우(적에게 잡힘, 버리기)에만 호출함. 보드에 소환한 것만으로는 지우지 않음
        {
            if (piece == null || !_deck.Remove(piece))   // 만약 그 기물을 가지고 있지 않다면 실패
                return false;
            InventoryChanged?.Invoke();
            return true;
        }

        public int CountOf(PieceDefinition piece)   // 그 기물을 몇 개 보유하고 있는지 세는 함수
        {
            int count = 0;
            foreach (var owned in _deck)
                if (owned == piece)
                    count++;
            return count;
        }

        public void AddArtifact(ItemData artifact)   // 유물을 보유 목록에 추가하는 함수 -> 상점에서 구매가 성사됐을 때 호출 (효과 적용은 아직 연결되지 않음)
        {
            if (artifact == null)
            {
                return;
            }
            _artifacts.Add(artifact);
            _artifactEffects.Activate(artifact);    // 아티팩트이기 때문에 보유 목록에 넣은 순간 효과 발동 시작
            InventoryChanged?.Invoke();
        }

        public bool RemoveArtifact(ItemData artifact)   // 유물을 보유 목록에서 지우는 함수 (인벤토리에서 버리기) -> 지웠으면 true
        {
            if (artifact == null || !_artifacts.Remove(artifact)) 
            {
                return false;
            }
            _artifactEffects.Deactivate(artifact);  // 아티팩트 보유 목록에서 빠진 순간 효과 발동 종료
            InventoryChanged?.Invoke();
            return true;
        }

        public bool Owns(ItemData artifact)   // 그 유물을 이미 가지고 있는지 확인하는 함수 -> 중복 구매 방지에 사용
            => artifact != null && _artifacts.Contains(artifact);

        public void AdvanceStage() => StageIndex++;   // 스테이지를 하나 클리어했을 때 진행 번호를 1 올리는 함수

        public void ResetRun()   // 현재 런을 버리고 새 런을 시작하는 함수
        // -> 씬을 다시 불러오지 않아도 모든 진행 정보가 깨끗하게 초기화됨
        {
            _initialised = false;   // 초기화 표시를 풀어서 Initialise가 다시 동작하게 하고
            StageIndex = 0;   // 스테이지 진행을 처음으로
            _artifactEffects.DeactivateAll();   // 새 런이므로 모든 아티팩트 효과를 발동 종료하고
            _artifacts.Clear();   // 유물을 모두 비우고
            Map = null;   // 맵을 버려 다음에 새 맵이 생성되게 하고
            MapPath.Clear();   // 지나온 경로도 비우고
            CurrentStage = null;   // 선택된 스테이지도 비운 뒤
            Initialise();   // 시작 골드·덱을 다시 채움
            InventoryChanged?.Invoke();   // 인벤토리도 새 런 기준으로 다시 그리도록 알림
            GoldChanged?.Invoke(Gold, Gold);   // 골드 UI가 새 값으로 갱신되도록 이벤트를 보냄
        }
    }
}
