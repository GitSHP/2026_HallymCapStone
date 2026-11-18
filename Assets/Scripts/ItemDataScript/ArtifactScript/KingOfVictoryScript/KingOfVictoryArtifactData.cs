using UnityEngine;

[CreateAssetMenu(fileName = "KingOfVictoryArtifactData", menuName = "Scriptable Objects/Artifacts/KingOfVictoryArtifactData")]
public class KingOfVictoryArtifactData : ArtifactData   // "아군 기물을 하나도 잃지 않고 스테이지 클리어 시 골드 추가 지급" 아티팩트의 설정값
// -> 실행 중 상태(이번 스테이지에서 아군을 잃었는지)는 여기 두지 않고 KingOfVictoryArtifactEffect에 둠
{
    [Header("보상")]
    [Min(0)]
    [Tooltip("아군 기물을 하나도 잃지 않고 스테이지를 클리어했을 때 추가로 지급할 골드")]
    public int bonusGold = 5;   // 임시값 5골드 -> 밸런스 조정 시 인스펙터에서 변경

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new KingOfVictoryArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}