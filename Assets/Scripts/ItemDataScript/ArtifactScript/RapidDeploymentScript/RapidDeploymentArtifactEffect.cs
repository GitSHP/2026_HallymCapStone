using Promotion.Core;
using Promotion.Gameplay;
using UnityEngine;

public class RapidDeploymentArtifactEffect : ArtifactEffect   // 소환 AP 반환 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly RapidDeploymentArtifactData _data;   // 이 효과의 설정값(턴당 반환 횟수)
    int _refundsUsedThisTurn;   // 이번 턴에 AP를 돌려준 횟수

    public RapidDeploymentArtifactEffect(RapidDeploymentArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 필요한 이벤트 구독
    {
        _refundsUsedThisTurn = 0;
        GameEventManager.PlayerTurnStarted += OnPlayerTurnStarted;  // 턴 시작 -> 반환 횟수 초기화
        GameEventManager.PieceSummoned += OnPieceSummoned;          // 기물 소환 -> AP 반환 여부 확인
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 구독했던 것을 모두 해제 (OnActivate와 짝을 맞춤)
    {
        GameEventManager.PlayerTurnStarted -= OnPlayerTurnStarted;
        GameEventManager.PieceSummoned -= OnPieceSummoned;
    }

    void OnPlayerTurnStarted(int turn)  // 플레이어 턴이 시작되면 호출됨
    {
        _refundsUsedThisTurn = 0;   // 새 턴이 되었으므로 다시 AP를 돌려받을 수 있게 함
        // 스테이지가 바뀌어도 새 스테이지의 1턴 시작 때 이 함수가 호출되므로 따로 StageStarted를 들을 필요가 없음
    }

    void OnPieceSummoned(Piece piece, int spentAp)  // 기물이 소환되면 호출됨 -> AP를 돌려줄지 판단
    {
        if (piece == null)
        {
            return;
        }
        if (spentAp <= 0)   // AP를 소모하지 않은 소환(배치 단계 등)이라면 돌려줄 AP가 없으므로 무관
        {
            return;
        }
        if (!_data.IsMajorPiece(piece.Def)) // 킹이나 폰이라면 무관 -> 폰을 소환해도 반환 기회는 그대로 남음
        {
            return;
        }
        if (_refundsUsedThisTurn >= _data.refundsPerTurn)   // 이번 턴의 반환 횟수를 다 썼다면 무관
        {
            return;
        }

        GiveReward(spentAp);
    }

    void GiveReward(int spentAp)    // 소환에 소모한 AP를 그대로 돌려주는 함수
    {
        TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
        // 아티팩트는 스테이지(씬)가 바뀌어도 살아 있으므로 TurnManager를 미리 저장하지 않고
        // 필요할 때마다 현재 씬에 있는 TurnManager를 찾음 (시실리안 디펜스와 같은 방식)
        if (turnManager == null)    // 전투 씬이 아니라면 돌려줄 곳이 없으므로 종료
        {
            return;
        }

        _refundsUsedThisTurn++; // 반환 횟수를 먼저 기록
        turnManager.GainAp(spentAp);
        // 소환할 때 TrySpend로 차감된 AP와 같은 양을 돌려줌 -> 결과적으로 이번 소환은 AP를 쓰지 않은 것과 같아짐

        Debug.Log("[RapidDeployment] 전개 아티팩트 효과 발동 -> " + piece_Name(spentAp));
    }

    string piece_Name(int spentAp)  // 로그 문구를 만드는 함수
    {
        return "소환 AP " + spentAp + " 반환 (이번 턴 " + _refundsUsedThisTurn + " / " + _data.refundsPerTurn + ")";
    }
}