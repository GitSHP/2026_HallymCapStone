using UnityEngine;

[CreateAssetMenu(fileName = "GuardianAngelArtifactData", menuName = "Scriptable Objects/Artifacts/GuardianAngelArtifactData")]
public class GuardianAngelArtifactData : ArtifactData   // "인벤토리 기물이 적에게 잡히면 확률적으로 같은 기물을 인벤토리에 되돌려 줌" 아티팩트의 설정값
// -> 실행 중에 바뀌는 상태가 없으므로 설정값만 보관함
{
    [Header("부활 확률")]
    [Range(0, 100)]
    [Tooltip("아군 기물이 잡혔을 때 같은 기물을 인벤토리에 되돌려 줄 확률(%) (예: 30 = 30%)")]
    public int reviveChance = 30;   // 임시값 30% -> 밸런스 조정 시 인스펙터에서 변경 (Range로 0~100 슬라이더가 생김)

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new GuardianAngelArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }

    public bool RollRevive()    // 부활 확률 판정 함수 -> 성공하면 true
    {
        int roll = Random.Range(0, 100);
        // 정수 Random.Range(최소, 최대)는 최대값을 포함하지 않으므로 0~99 중 하나가 나옴 -> 숫자 100개

        if (roll < reviveChance)    // 예: 확률 30이면 0~29(30개)가 나왔을 때 성공 -> 정확히 30%
        {
            return true;
        }
        return false;
        // reviveChance가 0이면 항상 실패, 100이면 항상 성공
    }
}