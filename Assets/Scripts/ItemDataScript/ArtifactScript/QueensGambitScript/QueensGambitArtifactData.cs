using Promotion.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "QueensGambitArtifactData", menuName = "Scriptable Objects/Artifacts/QueensGambitArtifactData")]
public class QueensGambitArtifactData : ArtifactData   // "아군 폰 N개가 적에게 잡힐 때마다 퀸 1개" 아티팩트의 설정값
// -> 실행 중 상태(지금까지 몇 개 잡혔는지)는 여기 두지 않고 QueensGambitArtifactEffect에 둠
{
    [Header("희생 조건")]
    [Tooltip("잡혔을 때 희생으로 세는 기물들 (예: Pawn, Pawn_Duel)")]
    public PieceDefinition[] sacrificePieces = new PieceDefinition[0];

    [Min(1)]
    public int requiredSacrifices = 3;  // 보상 1번에 필요한 희생 수

    [Tooltip("켜면 스테이지가 시작될 때마다 희생 수를 0으로 되돌림. 끄면 런 전체에서 이어서 셈.")]
    public bool resetCountEachStage = true;

    [Header("보상")]
    public PieceDefinition rewardPiece; // 지급할 기물 (퀸)
    // 유니티 인스펙터 상에서 이미 만들어져있는 기물을 드래그 앤 드롭으로 지정해서 사용

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new QueensGambitArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }

    public bool IsSacrificePiece(PieceDefinition def)   // 희생으로 세는 기물인지 확인하는 함수
    {
        if (def == null)
        {
            return false;
        }
        for (int i = 0; i < sacrificePieces.Length; i++)
        {
            if (sacrificePieces[i] == def)
            {
                return true;
            }
        }
        return false;
    }
}
