using Promotion.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "RapidDeploymentArtifactData", menuName = "Scriptable Objects/Artifacts/RapidDeploymentArtifactData")]
public class RapidDeploymentArtifactData : ArtifactData   // "매 턴 처음 소환하는 기물(킹·폰 제외)의 소환 AP를 돌려줌" 아티팩트의 설정값
// -> 실행 중 상태(이번 턴에 돌려준 횟수)는 여기 두지 않고 RapidDeploymentArtifactData에 둠
{
    [Header("AP 반환")]
    [Min(1)]
    [Tooltip("한 턴에 소환 AP를 돌려받을 수 있는 횟수 (킹·폰 제외, 1 = 매 턴 첫 소환만)")]
    public int refundsPerTurn = 1;  // 기본값 1 -> 요청대로 "해당 턴에 처음 소환하는 기물"만 반환

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new RapidDeploymentArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }

    public bool IsMajorPiece(PieceDefinition def)   // 킹과 폰을 제외한 기물인지 확인하는 함수 -> 룩, 비숍, 나이트, 퀸 등이면 true
    {
        if (def == null)
        {
            return false;
        }
        if (def.isObjective)    // 킹(지켜야 할 목표 기물)이라면 제외
        {
            return false;
        }
        if (def.canPromote) // 프로모션할 수 있는 기물(폰)이라면 제외
        {
            return false;
        }
        return true;
    }
}