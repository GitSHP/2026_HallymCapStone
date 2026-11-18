using Promotion.Core;
using Promotion.Data;
using UnityEngine;

public class GuardianAngelArtifactEffect : ArtifactEffect   // 수호천사 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly GuardianAngelArtifactData _data;   // 이 효과의 설정값(부활 확률)

    public GuardianAngelArtifactEffect(GuardianAngelArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 인벤토리 기물 손실 이벤트를 듣기 시작
    {
        GameEventManager.OwnedPieceLost += OnOwnedPieceLost;
        // PieceCaptured 대신 OwnedPieceLost를 듣는 이유
        // -> 이 이벤트는 "인벤토리에서 꺼낸 기물을 영구히 잃었을 때"만 오므로
        //    킹·상시 폰 같은 임시 기물을 따로 걸러낼 필요가 없음
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 이벤트를 듣지 않음
    {
        GameEventManager.OwnedPieceLost -= OnOwnedPieceLost;
    }

    void OnOwnedPieceLost(Piece piece)  // 인벤토리 기물이 적에게 잡혀 사라지면 호출됨 -> 확률 판정
    {
        if (piece == null)
        {
            return;
        }
        if (piece.Def == null)  // 기물 원본 데이터가 없다면 무엇을 되돌려 줄지 알 수 없으므로 종료
        {
            return;
        }
        if (!_data.RollRevive())    // 확률 판정 실패 -> 부활하지 않음
        {
            Debug.Log("[GuardianAngelEffect] " + piece.Def.name + " 부활 실패");
            return;
        }

        GiveReward(piece.Def);
    }

    void GiveReward(PieceDefinition def)    // 잡힌 기물과 같은 기물을 인벤토리에 1개 되돌려 주는 함수
    {
        Debug.Log("[GuardianAngelEffect] 수호천사 아티팩트 효과 발동 -> " + def.name + " 부활");

        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        // 아티팩트는 스테이지(씬)가 바뀌어도 살아 있으므로 InventoryManager를 미리 저장하지 않고
        // 지급할 때마다 현재 씬에 있는 InventoryManager를 찾음

        if (inventory != null)  // 인벤토리가 있다면 인벤토리에 넘김 -> 자리가 있으면 저장, 가득 찼으면 예/아니요 확인
        {
            inventory.ReceiveRewardPiece(def);
            return;
        }

        RunState.Instance.AddPiece(def);    // 인벤토리 UI가 없는 씬(테스트 등)이라면 장부에 바로 1개 추가
    }
}