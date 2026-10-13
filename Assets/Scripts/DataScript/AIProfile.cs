using UnityEngine;

namespace Promotion.Data
{
    [CreateAssetMenu(menuName = "Promotion/AI Profile", fileName = "AIProfile")]
    public class AIProfile : ScriptableObject   // 최종전 체스 AI의 난이도(성격)를 담는 데이터 - 난이도는 코드가 아니라 이 에셋에서 조정함
    // -> 초심자용 상대는 한 수 앞만 보고, 좋은 수들 중에서 느슨하게 고르며, 가끔 그럴듯한 실수를 하지만 체크메이트는 놓치지 않음
    {
        [Header("탐색")]
        [Tooltip("앞을 내다보는 수의 깊이다. 1이면 각 수로 당장 얻거나 잃는 것만 본다.")]
        [Range(1, 4)] public int maxDepth = 1;  // AI가 몇 수 앞까지 계산하는지 -> 클수록 강해지지만 생각하는 데 시간이 더 걸림

        [Header("느슨함")]
        [Tooltip("최선이 아닌 수를 얼마나 자유롭게 고르는지로, 폰 단위다. 0이면 항상 최선의 수를 둔다.")]
        [Min(0f)] public float temperature = 1f;    // 최선의 수와 점수 차이가 이 정도(폰 단위)인 수까지 후보로 두고 무작위로 고름 -> 값이 클수록 AI가 엉성해짐
        [Tooltip("의도적인 실수를 할 확률이다. 실수는 그럴듯한 수 중에서 무작위로 고른다.")]
        [Range(0f, 1f)] public float blunderRate = 0.25f;   // 한 수마다 일부러 실수할 확률 (0.25 = 4번에 1번꼴)
        [Tooltip("실수 한 번으로 내줄 수 있는 최대치로, 폰 단위다(3 = 비숍. 퀸은 절대 내주지 않는다).")]
        [Min(0f)] public float maxBlunderLoss = 3f; // 실수할 때 잃어도 되는 기물 가치의 상한 -> 너무 터무니없는 실수는 하지 않게 막음

        [Header("봐주기")]
        [Tooltip("AI가 폰 기준으로 이만큼 앞서면 실수를 더 자주 하기 시작한다.")]
        [Min(0f)] public float mercyLead = 5f;  // AI가 기물 가치로 이만큼 앞서면 '봐주기' 모드에 들어감 -> 플레이어가 일방적으로 지지 않게 함
        [Tooltip("봐주기 기준 격차에 도달한 뒤의 실수 확률이다.")]
        [Range(0f, 1f)] public float mercyBlunderRate = 0.4f;   // 봐주기 모드일 때 blunderRate 대신 쓰는 실수 확률

        [Header("반드시 할 일")]
        [Tooltip("체크메이트가 가능할 때 실제로 두는 확률이다. 1이면 항상 게임을 끝낸다.")]
        [Range(0f, 1f)] public float mateFindRate = 1f; // 외통수가 보일 때 실제로 두는 확률 -> 1이면 절대 놓치지 않음
        [Tooltip("느슨함이 적용되기 전까지 정석대로 두는 초반 수의 개수다(중앙 폰 전진, 기물 전개).")]
        [Min(0)] public int sensibleOpeningMoves = 4;   // 게임 초반 이 수만큼은 실수 없이 정석적으로 둠 -> 시작부터 이상한 수를 두지 않게 함

        [Header("연출")]
        [Tooltip("AI가 수를 두기 전에 생각하는 것처럼 보이는 최소 시간(초)이다.")]
        [Min(0f)] public float minThinkSeconds = 0.6f;  // AI가 수를 두기 전 최소 대기 시간(초) -> 계산이 빨라도 바로 두지 않고 고민하는 것처럼 보이게 함
    }
}
