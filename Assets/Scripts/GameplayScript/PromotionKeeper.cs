using System.Collections.Generic;
using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class PromotionKeeper : MonoBehaviour   // 일반 스테이지에서 프로모션으로 생긴 기물을 승리 후 인벤토리에 영구히 넣어 주는 클래스
    // -> 승패는 StageDirector가 선언하고, 이 클래스는 승리라는 결과만 구독함
    // -> 프로모션한 기물이 스테이지 끝까지 살아남았을 때만 넣고, 중간에 잡혔다면 아무것도 얻지 못함
    {
        TurnManager _turn;  // 승리 단계로 바뀌는 순간을 알려 주는 컴포넌트
        StageDirector _director;    // 지금 스테이지가 최종전인지 확인하는 용도
        BoardState _state;  // 보드 위에 살아남은 기물을 찾는 용도

        readonly List<Piece> _scratch = new();  // 아군 기물을 잠시 담아 두는 리스트 (매번 새로 만들지 않기 위해)

        void Awake()  // 같은 오브젝트(Board)의 컴포넌트를 찾아 둠
        {
            _turn = GetComponent<TurnManager>();
            _director = GetComponent<StageDirector>();
            _state = GetComponent<BoardState>();
        }

        void OnEnable() // 켜질 때 턴 단계 변경을 구독
        {
            if (_turn != null)
                _turn.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()    // 꺼질 때 구독 해제
        {
            if (_turn != null)
                _turn.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(TurnPhase phase)    // 턴 단계가 바뀔 때마다 호출됨 -> 일반 스테이지 승리일 때만 처리
        {
            if (phase != TurnPhase.Victory || _director == null || _director.IsChessDuel)
                return;

            _state.GetPiecesOf(Team.Player, _scratch);  // 살아남은 아군 기물 중
            foreach (var piece in _scratch)
            {
                if (!piece.Promoted)    // 프로모션으로 생긴 기물만
                    continue;

                piece.Promoted = false; // 같은 기물을 두 번 넣지 않도록 표시를 지우고
                Keep(piece);
            }
        }

        static void Keep(Piece piece)   // 기물 하나를 런 인벤토리에 넣는 함수
        {
            var inventory = FindAnyObjectByType<InventoryManager>();
            if (inventory != null)  // 인벤토리가 있다면 인벤토리에 넘김 -> 자리가 있으면 바로 저장, 가득 찼다면 확인창
            {
                inventory.ReceiveRewardPiece(piece.Def);
                return;
            }

            RunState.Instance.AddPiece(piece.Def);  // 인벤토리 UI가 없는 씬이라면 장부에 바로 추가
        }
    }
}
