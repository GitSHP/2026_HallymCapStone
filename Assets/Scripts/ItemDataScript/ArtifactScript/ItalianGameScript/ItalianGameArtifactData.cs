using Promotion.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "ItalianGameArtifactData", menuName = "Scriptable Objects/Artifacts/ItalianGameArtifactData")]
public class ItalianGameArtifactData : ArtifactData   // "매 스테이지 첫 턴 시작 시 나이트, 비숍 1개씩 지급" 아티팩트의 설정값
// -> 실행 중에 바뀌는 상태가 없으므로 설정값만 보관함
{
    [Header("보상")]
    [Tooltip("스테이지 첫 턴이 시작될 때 1개씩 지급할 기물들 (예: Knight, Bishop)")]
    public PieceDefinition[] rewardPieces = new PieceDefinition[0];
    // 유니티 인스펙터에서 이미 만들어져 있는 나이트, 비숍 기물을 드래그 앤 드롭으로 지정해서 사용
    // 배열로 둔 이유 -> 나중에 지급 기물을 바꾸거나 늘리고 싶을 때 코드 수정 없이 인스펙터에서만 바꿀 수 있게

    [Min(1)]
    [Tooltip("몇 번째 턴에 지급할지 (1 = 스테이지의 최초 턴)")]
    public int rewardTurn = 1;  // 기본값 1 -> 스테이지 최초 턴에만 지급

    public override ArtifactEffect CreateEffect()   // 이 아티팩트를 얻을 때 ArtifactManager가 호출
    {
        return new ItalianGameArtifactEffect(this);   // 자기 설정값을 넘겨서 효과 객체를 만듦
    }
}
