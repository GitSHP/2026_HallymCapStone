using Promotion.Data;
using UnityEngine;

public class ItalianGameArtifactEffect : ArtifactEffect   // 스테이지 첫 턴 기물 지급 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly ItalianGameArtifactData _data;   // 이 효과의 설정값(지급할 기물들, 지급할 턴)

    public ItalianGameArtifactEffect(ItalianGameArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 플레이어 턴 시작 이벤트를 듣기 시작
    {
        GameEventManager.PlayerTurnStarted += OnPlayerTurnStarted;
        // 이제 플레이어 턴이 시작될 때마다 OnPlayerTurnStarted가 호출되고, 몇 번째 턴인지(turn)를 받음
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 플레이어 턴 시작 이벤트를 듣지 않음
    {
        GameEventManager.PlayerTurnStarted -= OnPlayerTurnStarted;
    }

    void OnPlayerTurnStarted(int turn)  // 플레이어 턴이 시작되면 호출됨 -> 스테이지의 최초 턴인지 확인
    {
        if (turn != _data.rewardTurn)   // 최초 턴(1턴)이 아니라면 함수 탈출 -> 2턴, 3턴...에는 지급하지 않음
        {
            return;
        }
        // TurnManager.ResetForStage()가 스테이지를 시작할 때마다 Turn을 0으로 되돌리고
        // 배치를 끝내고 첫 턴을 시작하면 Turn이 1이 되므로
        // "turn == 1"은 항상 "현재 스테이지의 최초 턴"을 뜻함
        // -> 스테이지를 클리어하고 다음 스테이지로 넘어가면 다시 1턴부터 시작하므로 자동으로 다시 지급됨

        GiveReward();
    }

    void GiveReward()   // 설정된 보상 기물들(나이트, 비숍)을 1개씩 지급하는 함수
    {
        Debug.Log("[ItalianGameEffect] 이탈리안 게임 아티팩트 효과 발동");

        InventoryManager inventory = Object.FindAnyObjectByType<InventoryManager>();
        // 아티팩트는 스테이지(씬)가 바뀌어도 살아 있으므로 InventoryManager를 미리 저장하지 않고
        // 지급할 때마다 현재 씬에 있는 InventoryManager를 찾음

        for (int i = 0; i < _data.rewardPieces.Length; i++) 
        // for문으로 반복하면서 _data.rewardPieces 배열 안에 있는 모든 기물 데이터를 확인
        {
            PieceDefinition piece = _data.rewardPieces[i];
            // _data.rewardPieces 배열 안에는 유니티 인스펙터 창에서 지정한 보상 기물에 대한 데이터가 들어있음
            if (piece == null)  // _data.rewardPieces 배열 안에 저장된 기물 데이터가 없다면
            {
                continue;   // 다음 반복 사이클로 넘어감
                // 만약 배열 내에 첫 번째 칸과 세 번째 칸에만 기물 데이터가 저장되어있다면
                // 두 번째 배열은 빈 칸이므로 두 번째 배열에 대한 보상 지급은 해서는 안됨
                // 따라서 continue 를 사용해서 정상적인 배열 정보에 대해서는 정상적으로 진행하고
                // null 값인 비정상적인 배열 데이터에 대해서는 안전하게 배열의 다음 칸으로 넘어가도록 함
            }

            if (inventory != null)
            // 인벤토리가 있다면 인벤토리에 넘김 -> 자리가 있으면 바로 저장, 가득 찼으면 예/아니요 확인창
            {
                inventory.ReceiveRewardPiece(piece);
            }
        }
    }
}
