using UnityEngine;

[CreateAssetMenu(fileName = "SicilianDefenseArtifactData", menuName = "Scriptable Objects/Artifacts/SicilianDefenseArtifactData")]
public class SicilianDefenseArtifactData : ArtifactData   // "킹이 적을 처치하면 AP +N" 아티팩트의 설정값
// -> 실행 중에 바뀌는 상태가 없으므로 설정값만 보관함
{
    [Header("보상")]
    [Min(1)]
    [Tooltip("킹이 적 기물을 처치할 때마다 얻는 AP 양")]
    public int apReward = 1;    // 처치 1회당 지급할 AP

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new SicilianDefenseArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}