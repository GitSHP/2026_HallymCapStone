using UnityEngine;

[CreateAssetMenu(fileName = "MomentumArtifactData", menuName = "Scriptable Objects/Artifacts/MomentumArtifactData")]
public class MomentumArtifactData : ArtifactData   // "턴을 마칠 때 남은 AP를 다음 턴으로 이월" 아티팩트의 설정값
// -> 실행 중 상태(이월할 AP 양)는 여기 두지 않고 MomentumArtifactEffect에 둠
{
    [Header("이월")]
    [Min(1)]
    [Tooltip("다음 턴으로 넘길 수 있는 최대 AP (예: 2 = 아무리 많이 남겨도 2까지만 이월)")]
    public int maxCarryAp = 2;  // 임시값 2 -> 밸런스 조정 시 인스펙터에서 변경
    // 한도를 둔 이유 -> 한도가 없으면 턴을 계속 넘기며 AP를 무한히 모은 뒤 한 턴에 몰아 쓰는 플레이가 가능해짐

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new MomentumArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}
