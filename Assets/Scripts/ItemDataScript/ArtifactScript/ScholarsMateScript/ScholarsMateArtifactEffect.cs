using Promotion.Core;
using UnityEngine;

public class ScholarsMateArtifactEffect : ArtifactEffect   // 빠른 클리어 보상 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly ScholarsMateArtifactData _data;   // 이 효과의 설정값(제한 턴 수, 보상 골드)
    int _currentTurn;   // 현재 스테이지에서 지금 몇 번째 플레이어 턴인지 기록 (배치 단계는 0)

    public ScholarsMateArtifactEffect(ScholarsMateArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트 구독
    {
        _currentTurn = 0;
        GameEventManager.StageStarted += OnStageStarted;            // 스테이지 시작 -> 턴 기록 초기화
        GameEventManager.PlayerTurnStarted += OnPlayerTurnStarted;  // 플레이어 턴 시작 -> 현재 턴 번호 기록 (턴 +1)
        GameEventManager.StageEnded += OnStageEnded;                // 스테이지 종료 -> 클리어 턴 수 확인 후 보상
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제 (OnActivate와 짝을 맞춤)
    {
        GameEventManager.StageStarted -= OnStageStarted;
        GameEventManager.PlayerTurnStarted -= OnPlayerTurnStarted;
        GameEventManager.StageEnded -= OnStageEnded;
    }

    void OnStageStarted()   // 새 스테이지가 시작되면 호출됨
    {
        _currentTurn = 0;
        // 이전 스테이지의 턴 수가 남아 있으면 안 되므로 0(배치 단계)으로 되돌림
        // TurnManager.ResetForStage()가 Turn을 0으로 되돌리는 것과 같은 기준을 맞춤
    }

    void OnPlayerTurnStarted(int turn)  // 플레이어 턴이 시작될 때마다 호출됨 -> 몇 번째 턴인지 turn 매개변수로 받아서 기록
    {
        _currentTurn = turn;
        // 승리 판정은 적 턴 처리 중(또는 플레이어 행동 중)에 일어나며 그 시점에는 턴 수가 증가하지 않으므로
        // 여기 기록된 값이 곧 "몇 턴 만에 클리어했는지"가 됨
    }

    void OnStageEnded(TurnPhase outcome)    // 스테이지가 끝나면 호출됨 -> 승리했는지 제한 턴 수 내에 승리했는지 확인
    {
        if (outcome != TurnPhase.Victory)   // 패배로 끝났다면 보상을 지급하지않음
        {
            return;
        }
        if (_currentTurn < 1)   // 턴이 한 번도 시작되지 않았다면(비정상 상황) 오류 방지 탈출
        {
            return;
        }
        if (_currentTurn > _data.maxClearTurn)  // 제한 턴 수를 넘겨서 클리어했다면 보상 지급하지않음
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // 보상 골드를 지급하는 함수
    {
        Debug.Log("[ScholarsMateEffect] 스콜라스 메이트 아티팩트 효과 발동 -> " + _currentTurn + "턴 클리어, 골드 +" + _data.bonusGold);

        if (RunState.Instance == null)  // 런 정보가 없는 씬(테스트 등)이라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        RunState.Instance.AddGold(_data.bonusGold);
        // RunState.AddGold가 GoldChanged 이벤트를 보내므로 골드 UI는 자동으로 갱신됨
        // AddGold 를 호출해 _data.bonusGold 의 값만큼 골드를 추가 보상
    }
}