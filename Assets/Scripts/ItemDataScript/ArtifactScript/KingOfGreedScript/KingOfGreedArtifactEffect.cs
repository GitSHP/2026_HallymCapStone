using Promotion.Core;
using UnityEngine;

public class KingOfGreedArtifactEffect : ArtifactEffect   // 이자 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly KingOfGreedArtifactData _data;   // 이 효과의 설정값(기준 골드, 이자량, 최대 이자)

    public KingOfGreedArtifactEffect(KingOfGreedArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 스테이지 종료 이벤트를 듣기 시작
    {
        GameEventManager.StageEnded += OnStageEnded;
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 스테이지 종료 이벤트를 듣지 않음
    {
        GameEventManager.StageEnded -= OnStageEnded;
    }

    void OnStageEnded(TurnPhase outcome)    // 스테이지가 끝나면 호출됨 -> 승리했을 때만 이자 지급
    {
        if (outcome != TurnPhase.Victory)   // 패배로 끝났다면 이자 없음
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // 보유 골드를 기준으로 이자를 계산해서 지급하는 함수
    {
        if (RunState.Instance == null)  // 런 정보가 없는 씬(테스트 등)이라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        int currentGold = RunState.Instance.Gold;   // 이자 계산 기준이 되는 현재 보유 골드
        int interest = _data.CalculateInterest(currentGold);

        if (interest <= 0)  // 받을 이자가 없다면(기준 골드보다 적게 보유) 지급하지 않음
        {
            return;
        }

        Debug.Log("[KingOfGreed] 탐욕의 왕 아티팩트 효과 발동 -> 보유 골드 " + currentGold + ", 이자 +" + interest);

        RunState.Instance.AddGold(interest);
        // RunState.AddGold가 GoldChanged 이벤트를 보내므로 골드 UI는 자동으로 갱신됨
    }
}
