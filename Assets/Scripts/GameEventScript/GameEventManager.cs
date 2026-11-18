using System;
using Promotion.Core;
using UnityEngine;

public static class GameEventManager  
// 게임에서 일어난 모든 이벤트들을 듣고 이를 아티팩트들에게 알려주는 스크립트
// 특정 이벤트가 일어났을 때 아티팩트 아이템의 효과가 발동되도록 하는 중계소
// "무슨 일이 일어났다"는 사실만 전달하고, 그 일로 무엇을 할지는 받는 쪽(아티팩트)이 정함
// 아티팩트는 이 스크립트에서 발생했다고 알려주는 이벤트를 듣고 있다가 특정 조건이 만족하면 효과가 발동되게 된다
{
    // ---------- 이벤트 목록 ----------
    public static event Action<Piece, Piece> PieceCaptured; 
    // 기물이 잡혔을 때 발생하는 이벤트 <Piece, Piece> -> (공격한 기물, 잡힌 기물)
    // PieceCaptured 는 델리게이트 변수로 이 곳에 함수들을 저장할 수 있다.
    // .Invoke()를 이용해 델리게이트 변수 안에 저장된 모든 함수를 실행할 수 있다.
    public static event Action<int> PlayerTurnStarted;      
    // 플레이어 턴 시작 -> (턴 번호)
    public static event Action<int> PlayerTurnEnded;        
    // 플레이어가 턴을 마침 -> (턴 번호)
    public static event Action StageStarted;                
    // 스테이지 시작 (배치 단계 진입)
    public static event Action<TurnPhase> StageEnded;       
    // 스테이지 종료 -> (Victory 또는 Defeat)
    public static event Action<ItemData> ItemPurchased;     
    // 상점에서 아이템 구매 -> (구매한 아이템)
    public static event Action<Piece> OwnedPieceLost;
    // 인벤토리에서 꺼낸 아군 기물이 적에게 잡혀 인벤토리에서 영구히 사라졌을 때 발생 -> (잃은 기물)
    // PieceCaptured와 달리 킹·상시 폰 같은 임시 기물이 잡혔을 때는 발생하지 않음
    public static event Action<Piece, int> PieceSummoned;
    // 아군 기물을 보드에 소환했을 때 발생 -> (소환된 기물, 소환에 실제로 소모한 AP)
    // 배치 단계에서 놓은 기물은 소모한 AP가 0으로 전달됨

    // ---------- 이벤트 보내기 ----------
    public static void RaisePieceCaptured(Piece attacker, Piece target) // 공격 기물이 타겟 기물을 공격했을 때 발생하는 이벤트 함수
    {
        if (PieceCaptured != null)  // 현재 듣고 있는 스크립트가 하나라도 있을 때만 보냄 (없는데 Invoke하면 오류)
        {
            PieceCaptured.Invoke(attacker, target); 
            // RaisePieceCaptured 함수가 실행됐는지 듣고 있는 모든 스크립트들에게 이벤트가 발생했음을 알려줌  
            Debug.Log("GameEventManager 기물 공격 확인" + attacker + "가" + target + "을 공격함");
        }
    }

    public static void RaisePlayerTurnStarted(int turn)
    {
        if (PlayerTurnStarted != null)
        {
            PlayerTurnStarted.Invoke(turn);
        }
        Debug.Log("GameEventManager 플레이어 턴 시작 확인" + turn.ToString());
    }

    public static void RaisePlayerTurnEnded(int turn)
    {
        if (PlayerTurnEnded != null)
        {
            PlayerTurnEnded.Invoke(turn);
        }
        Debug.Log("GameEventManager 플레이어 턴 종료 확인" + turn.ToString());
    }

    public static void RaiseStageStarted()
    {
        if (StageStarted != null)
        {
            StageStarted.Invoke();
        }
        Debug.Log("GameEventManager 스테이지 시작 확인");
    }

    public static void RaiseStageEnded(TurnPhase outcome)
    {
        if (StageEnded != null)
        {
            StageEnded.Invoke(outcome);
        }
        Debug.Log("GameEventManager 스테이지 종료 확인" + outcome.ToString());
    }

    public static void RaiseItemPurchased(ItemData item)
    {
        if (ItemPurchased != null)
        {
            ItemPurchased.Invoke(item);
        }
        Debug.Log("GameEventManager 아이템 구매 확인" + item.ToString());
    }

    public static void RaiseOwnedPieceLost(Piece piece) 
    // 킹, 폰은 상시로 주워지는 기물이므로 이 함수가 호출되지 않음
    {
        if (OwnedPieceLost != null)
        {
            OwnedPieceLost.Invoke(piece);
        }
        Debug.Log("GameEventManager 아군 기물 잃음 감지" + piece.Def.ToString());
    }

    public static void RaisePieceSummoned(Piece piece, int spentAp)
    {
        if (PieceSummoned != null)
        {
            PieceSummoned.Invoke(piece, spentAp);
        }
        Debug.Log("GameEventManager 기물 소환 감지" + piece.Def.itemName.ToString() + "소모된 AP : " + spentAp);
    }

    // ---------- 초기화 ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    // 플레이를 시작할 때 Unity가 자동으로 한 번 호출 
    // -> 이전 플레이의 기록을 모두 지움
    static void ResetEvents()   
    {
        PieceCaptured = null;
        PlayerTurnStarted = null;
        PlayerTurnEnded = null;
        StageStarted = null;
        StageEnded = null;
        ItemPurchased = null;
        OwnedPieceLost = null;
        PieceSummoned = null;
    }
}
