using UnityEngine;

[CreateAssetMenu(fileName = "PillageArtifactData", menuName = "Scriptable Objects/Artifacts/PillageArtifactData")]
public class PillageArtifactData : ArtifactData   // "적 기물을 잡을 때마다 골드 +N (스테이지당 최대 M)" 아티팩트의 설정값
// -> 실행 중 상태(이번 스테이지에서 받은 골드)는 여기 두지 않고 PillageArtifactEffect에 둠
{
    [Header("보상")]
    [Min(1)]
    [Tooltip("아군이 적 기물을 1번 잡을 때마다 지급할 골드")]
    public int goldPerCapture = 1;  // 임시값 1골드 -> 밸런스 조정 시 인스펙터에서 변경

    [Min(0)]
    [Tooltip("한 스테이지에서 이 아티팩트로 받을 수 있는 최대 골드. 0이면 한도 없음")]
    public int maxGoldPerStage = 5; // 임시값 5골드 -> 적이 많은 스테이지에서 골드가 끝없이 늘어나는 것을 막음

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new PillageArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}