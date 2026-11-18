using UnityEngine;

[CreateAssetMenu(fileName = "KingOfGreedArtifactData", menuName = "Scriptable Objects/Artifacts/KingOfGreedArtifactData")]
public class KingOfGreedArtifactData : ArtifactData   // "스테이지 클리어 시 보유 골드에 비례해 이자 지급" 아티팩트의 설정값
// -> 실행 중에 바뀌는 상태가 없으므로 설정값만 보관함
{
    [Header("이자 조건")]
    [Min(1)]
    [Tooltip("이 골드만큼 보유할 때마다 이자를 1번 받음 (예: 10 = 10골드당 이자 1번)")]
    public int goldPerInterest = 10;    // 기준 골드 -> 0이 되면 나눗셈 오류가 나므로 Min(1)로 막아 둠

    [Header("보상")]
    [Min(1)]
    [Tooltip("기준 골드 1묶음당 받는 이자 골드 (예: 1 = 묶음당 1골드)")]
    public int interestAmount = 1;      // 이자량

    [Min(0)]
    [Tooltip("한 번에 받을 수 있는 최대 이자 골드. 0이면 한도 없음")]
    public int maxInterest = 5;         // 최대 이자 -> 골드를 많이 모을수록 이자가 끝없이 커지는 것을 막음

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new KingOfGreedArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }

    public int CalculateInterest(int currentGold)   // 현재 보유 골드를 받아서 지급할 이자를 계산하는 함수
    {
        if (currentGold <= 0)   // 골드가 없다면 이자도 없음
        {
            return 0;
        }

        int bundleCount = currentGold / goldPerInterest;
        // 정수끼리 나누면 나머지는 버려짐 -> 37 / 10 = 3 (기준 골드 묶음이 3개)

        int interest = bundleCount * interestAmount;
        // 묶음 수 × 묶음당 이자량 = 지급할 이자

        if (maxInterest > 0)    // 최대 이자가 설정되어 있다면 (0이면 한도 없음)
        {
            if (interest > maxInterest) // 계산한 이자가 최대치를 넘으면 최대치까지만 지급
            {
                interest = maxInterest;
            }
        }

        return interest;
    }
}
