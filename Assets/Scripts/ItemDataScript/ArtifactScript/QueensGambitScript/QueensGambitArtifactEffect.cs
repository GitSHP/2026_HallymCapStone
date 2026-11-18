using Promotion.Core;
using UnityEngine;

public class QueensGambitArtifactEffect : ArtifactEffect   // 폰 희생 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly QueensGambitArtifactData _data;   // 이 효과의 설정값(필요 희생 수, 보상 기물 등)
    int _sacrificeCount;    // 지금까지 희생된 기물 수 -> 아직 보상으로 변환되지 않은 희생 기물 수
    public QueensGambitArtifactEffect(QueensGambitArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트만 구독
    {
        _sacrificeCount = 0;
        GameEventManager.PieceCaptured += OnPieceCaptured;
        GameEventManager.StageStarted += OnStageStarted;
        // GameEventManager 스크립트에서 기물이 잡히는 이벤트(RaisePieceCaptured)가 발생하면
        // GameEventManager. ~~ 델리게이트 변수 내에 저장된 모든 함수가 실행된다.
        // 따라서 위 델리게이트 변수에 기물이 잡히는 이벤트가 발생했을 때 실행할 함수를 델리게이트 변수에 저장한다.
        // 그러면 델리게이트 변수에 저장된 함수는 기물이 잡히거나 턴이 종료되거나 시작되는 등의 이벤트가 발생하면
        // 그 이벤트와 맞는 델리게이트 변수 안에 있는 모든 함수를 실행하고
        // 델리게이트 변수에서 호출되는 함수에 의해서 아티팩트 효과에 맞는 함수가 실행되기 때문에 아티팩트 효과가 발동된다.
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제
    {
        GameEventManager.PieceCaptured -= OnPieceCaptured;
        GameEventManager.StageStarted -= OnStageStarted;
        // GameEventManger 델리게이트 변수 안에 저장된 함수들을 삭제하는 함수
        // 델리게이트 변수 안에 함수를 삭제하면 더 이상 특정 이벤트가 발생해서
        // 델리게이트 변수 내 모든 함수가 실행되더라도 삭제된 변수는 실행되지않으므로
        // 아티팩트 아이템의 효과와 관련된 함수가 실행되지않기 때문에 아티팩트 아이템 효과가 발동되지 않는다.
    }

    void OnStageStarted()   // 새 스테이지 시작
    {
        if (_data.resetCountEachStage)  // 스테이지마다 초기화하는 설정이라면 희생 수를 0으로
        {
            _sacrificeCount = 0;
        }
    }

    void OnPieceCaptured(Piece attacker, Piece target)  // 어떤 기물이든 잡히면 호출됨 -> 여기서 "폰 희생인지" 직접 판단
    {
        if (attacker == null || target == null)
        {
            return;
        }
        if (attacker.Team != Team.Enemy)    // 적이 잡은 것이 아니라면 무관
        {
            return;
        }
        if (target.Team != Team.Player) // 잡힌 것이 아군이 아니라면 무관
        {
            return;
        }
        if (!_data.IsSacrificePiece(target.Def))    // 폰이 아니라면 무관
        {
            return;
        }

        _sacrificeCount++;
        Debug.Log("[PawnSacrificeEffect] 폰 희생 " + _sacrificeCount + " / " + _data.requiredSacrifices);

        if (_sacrificeCount < _data.requiredSacrifices) // 아직 모자라면 종료
        {
            return;
        }

        _sacrificeCount = _sacrificeCount - _data.requiredSacrifices;   // 보상 1번 분량을 소모하고
        GiveReward();
    }

    void GiveReward()   // 보상 기물(퀸)을 지급하는 함수
    {
        Debug.Log("퀸즈 겜빗 아티팩트 효과 발동");
        if (_data.rewardPiece == null)
        {
            return;
        }

        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        if (inventory != null)  // 인벤토리가 있다면 인벤토리에 넘김 -> 자리가 있으면 저장, 가득 찼으면 예/아니요 확인
        {
            inventory.ReceiveRewardPiece(_data.rewardPiece);
            return;
        }

        RunState.Instance.AddPiece(_data.rewardPiece);  // 인벤토리 UI가 없는 씬(테스트 등)이라면 장부에 바로 1개 추가
    }
}
