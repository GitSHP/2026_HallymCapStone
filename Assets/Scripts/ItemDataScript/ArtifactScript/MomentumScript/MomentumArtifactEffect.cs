using Promotion.Gameplay;
using UnityEngine;

public class MomentumArtifactEffect : ArtifactEffect   // AP 이월 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly MomentumArtifactData _data;   // 이 효과의 설정값(최대 이월 AP)
    int _carryAp;   // 다음 턴 시작 때 더해 줄 AP (지난 턴에 남긴 AP)

    public MomentumArtifactEffect(MomentumArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트 구독
    {
        _carryAp = 0;
        GameEventManager.StageStarted += OnStageStarted;            // 스테이지 시작 -> 이월 AP 초기화
        GameEventManager.PlayerTurnEnded += OnPlayerTurnEnded;      // 턴 종료 -> 남은 AP 저장
        GameEventManager.PlayerTurnStarted += OnPlayerTurnStarted;  // 턴 시작 -> 저장한 AP 지급
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제 (OnActivate와 짝을 맞춤)
    {
        GameEventManager.StageStarted -= OnStageStarted;
        GameEventManager.PlayerTurnEnded -= OnPlayerTurnEnded;
        GameEventManager.PlayerTurnStarted -= OnPlayerTurnStarted;
    }

    void OnStageStarted()   // 새 스테이지가 시작되면 호출됨
    {
        _carryAp = 0;
        // 이전 스테이지의 마지막 턴에 남은 AP가 새 스테이지 1턴으로 넘어가면 안 되므로 비움
    }

    void OnPlayerTurnEnded(int turn)    // 플레이어가 턴 종료 버튼을 누르면 호출됨 -> 남은 AP를 저장
    {
        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        // 아티팩트는 스테이지(씬)가 바뀌어도 살아 있으므로 TurnManager를 미리 저장하지 않고
        // 필요할 때마다 현재 씬에 있는 TurnManager를 찾음 (시실리안 디펜스와 같은 방식)
        if (turnManager == null)    // 전투 씬이 아니라면 읽을 AP가 없으므로 종료
        {
            return;
        }

        int leftAp = turnManager.Ap;
        // 이 이벤트는 적 턴으로 넘어가기 직전에 발생하므로 Ap에는 이번 턴에 쓰지 않고 남긴 AP가 들어 있음

        if (leftAp > _data.maxCarryAp)  // 남은 AP가 최대 이월량보다 많다면 최대 이월량까지만 저장
        {
            leftAp = _data.maxCarryAp;
        }
        if (leftAp < 0) // 혹시 모를 음수 값 방지
        {
            leftAp = 0;
        }

        _carryAp = leftAp;
        Debug.Log("[MomentumEffect] " + turn + "턴 종료 -> 다음 턴으로 AP " + _carryAp + " 이월");
    }

    void OnPlayerTurnStarted(int turn)  // 플레이어 턴이 시작되면 호출됨 -> 저장해 둔 AP를 더해 줌
    {
        if (_carryAp <= 0)  // 이월할 AP가 없다면(1턴이거나 지난 턴에 AP를 모두 썼다면) 종료
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // 저장해 둔 AP를 지급하는 함수
    {
        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        if (turnManager == null)    // 전투 씬이 아니라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        Debug.Log("[MomentumEffect] 모멘텀 아티팩트 효과 발동 -> 이월 AP +" + _carryAp);

        turnManager.GainAp(_carryAp);
        // BeginPlayerTurn()이 AP를 최대치로 채운 뒤에 이 이벤트를 보내므로 최대치 위에 이월 AP가 더해짐

        _carryAp = 0;   // 지급했으니 비움 -> 같은 AP가 두 번 지급되지 않게 함
    }
}