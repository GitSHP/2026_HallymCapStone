using Promotion.Core;
using UnityEngine;

public class KingOfVictoryArtifactEffect : ArtifactEffect   // 무손실 클리어 보상 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly KingOfVictoryArtifactData _data;   // 이 효과의 설정값(보상 골드)
    bool _lostAlly; // 이번 스테이지에서 아군 기물이 하나라도 잡혔는지 여부 (true = 잃음)

    public KingOfVictoryArtifactEffect(KingOfVictoryArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트 구독
    {
        _lostAlly = false;
        GameEventManager.StageStarted += OnStageStarted;    // 스테이지 시작 -> 기록 초기화
        GameEventManager.PieceCaptured += OnPieceCaptured;  // 기물이 잡힘 -> 아군이 잡혔는지 확인
        GameEventManager.StageEnded += OnStageEnded;        // 스테이지 종료 -> 무손실 승리인지 확인 후 보상
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제 (OnActivate와 짝을 맞춤)
    {
        GameEventManager.StageStarted -= OnStageStarted;
        GameEventManager.PieceCaptured -= OnPieceCaptured;
        GameEventManager.StageEnded -= OnStageEnded;
    }

    void OnStageStarted()   // 새 스테이지가 시작되면 호출됨
    {
        _lostAlly = false;
        // 이전 스테이지에서 아군을 잃었던 기록이 남아 있으면 안 되므로 매 스테이지마다 다시 "잃지 않음"으로 시작
    }

    void OnPieceCaptured(Piece attacker, Piece target)  // 어떤 기물이든 잡히면 호출됨 -> 여기서 "아군이 잡혔는지" 직접 판단
    {
        if (target == null)
        {
            return;
        }
        if (target.Team != Team.Player) // 잡힌 것이 아군이 아니라면(적이 잡힌 것이라면) 무관
        {
            return;
        }

        _lostAlly = true;   // 아군을 잃었다고 기록 -> 이번 스테이지는 보상 조건 실패
        Debug.Log("[KingOfVictoryEffect] 아군 기물 손실 -> 이번 스테이지 무손실 보상 조건 실패");
    }

    void OnStageEnded(TurnPhase outcome)    // 스테이지가 끝나면 호출됨 -> 승리 + 무손실인지 확인
    {
        if (outcome != TurnPhase.Victory)   // 패배로 끝났다면 무관
        {
            return;
        }
        if (_lostAlly)  // 아군을 하나라도 잃었다면 무관
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // 보상 골드를 지급하는 함수
    {
        if (RunState.Instance == null)  // 런 정보가 없는 씬(테스트 등)이라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        Debug.Log("[KingOfVictoryEffect] 승리의 왕 아티팩트 효과 발동 -> 무손실 클리어, 골드 +" + _data.bonusGold);

        RunState.Instance.AddGold(_data.bonusGold);
        // RunState.AddGold가 GoldChanged 이벤트를 보내므로 골드 UI는 자동으로 갱신됨
    }
}
