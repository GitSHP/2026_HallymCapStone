using Promotion.Core;
using UnityEngine;

public class PillageArtifactEffect : ArtifactEffect   // 처치 수당 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly PillageArtifactData _data;   // 이 효과의 설정값(1회 지급 골드, 스테이지당 최대 골드)
    int _earnedThisStage;   // 이번 스테이지에서 이 아티팩트로 이미 받은 골드

    public PillageArtifactEffect(PillageArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트 구독
    {
        _earnedThisStage = 0;
        GameEventManager.StageStarted += OnStageStarted;    // 스테이지 시작 -> 받은 골드 기록 초기화
        GameEventManager.PieceCaptured += OnPieceCaptured;  // 기물이 잡힘 -> 아군이 적을 잡았는지 확인
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제 (OnActivate와 짝을 맞춤)
    {
        GameEventManager.StageStarted -= OnStageStarted;
        GameEventManager.PieceCaptured -= OnPieceCaptured;
    }

    void OnStageStarted()   // 새 스테이지가 시작되면 호출됨
    {
        _earnedThisStage = 0;
        // 최대 골드는 "스테이지당" 한도이므로 스테이지가 바뀔 때마다 받은 양을 0으로 되돌림
    }

    void OnPieceCaptured(Piece attacker, Piece target)  // 어떤 기물이든 잡히면 호출됨 -> 여기서 "아군이 적을 잡았는지" 직접 판단
    {
        if (attacker == null || target == null)
        {
            return;
        }
        if (attacker.Team != Team.Player)   // 잡은 것이 아군이 아니라면 무관
        {
            return;
        }
        if (target.Team != Team.Enemy)  // 잡힌 것이 적이 아니라면 무관
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // 한도를 확인하고 골드를 지급하는 함수
    {
        if (RunState.Instance == null)  // 런 정보가 없는 씬(테스트 등)이라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        int amount = _data.goldPerCapture;  // 이번에 지급할 골드 (기본은 1회 지급량)

        if (_data.maxGoldPerStage > 0)  // 최대 골드가 설정되어 있다면 (0이면 한도 없음)
        {
            int remaining = _data.maxGoldPerStage - _earnedThisStage;   // 이번 스테이지에서 더 받을 수 있는 골드

            if (remaining <= 0) // 이미 한도까지 받았다면 지급하지 않음
            {
                return;
            }
            if (amount > remaining) // 1회 지급량이 남은 한도보다 많다면 남은 만큼만 지급 -> 한도를 넘지 않게
            {
                amount = remaining;
            }
        }

        _earnedThisStage = _earnedThisStage + amount;   // 받은 양을 기록
        Debug.Log("[약탈] 약탈 아티팩트 효과 발동 -> 골드 +" + amount + " (이번 스테이지 " + _earnedThisStage + " / " + _data.maxGoldPerStage + ")");

        RunState.Instance.AddGold(amount);
        // RunState.AddGold가 GoldChanged 이벤트를 보내므로 골드 UI는 자동으로 갱신됨
    }
}