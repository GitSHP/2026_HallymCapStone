using UnityEngine;

[CreateAssetMenu(fileName = "ScholarsMateArtifactData", menuName = "Scriptable Objects/Artifacts/ScholarsMateArtifactData")]
public class ScholarsMateArtifactData : ArtifactData   // "N턴 안에 스테이지를 클리어하면 보상 골드 추가 지급" 아티팩트의 설정값
// -> 실행 중 상태(현재 몇 턴째인지)는 여기 두지 않고 ScholarsMateArtifactEffect에 둠
{
    [Header("조건")]
    [Min(1)]
    [Tooltip("이 턴 수 이내에 스테이지를 클리어하면 보상 지급 (예: 3 = 3턴 안에 클리어)")]
    public int maxClearTurn = 3;    // 몇 턴 내로 클리어 시 보상 골드를 지급할 지 결정하는 턴 변수
    
    [Header("보상")]
    [Min(0)]
    [Tooltip("조건을 만족했을 때 추가로 지급할 골드")]
    public int bonusGold = 5;       // 보상으로 지급할 골드 양
    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new ScholarsMateArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}