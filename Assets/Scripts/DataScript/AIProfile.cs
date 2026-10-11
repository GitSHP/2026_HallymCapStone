using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// 체스 AI의 플레이 방식을 정한다. 난이도는 코드가 아니라 여기서 조정한다. 초심자용
    /// 상대는 한 수 앞만 보고, 좋은 수들 가운데서 느슨하게 고르며, 가끔 그럴듯한 실수를
    /// 하지만 체크메이트는 절대 놓치지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/AI Profile", fileName = "AIProfile")]
    public class AIProfile : ScriptableObject
    {
        [Header("탐색")]
        [Tooltip("앞을 내다보는 수의 깊이다. 1이면 각 수로 당장 얻거나 잃는 것만 본다.")]
        [Range(1, 4)] public int maxDepth = 1;

        [Header("느슨함")]
        [Tooltip("최선이 아닌 수를 얼마나 자유롭게 고르는지로, 폰 단위다. 0이면 항상 최선의 수를 둔다.")]
        [Min(0f)] public float temperature = 1f;
        [Tooltip("의도적인 실수를 할 확률이다. 실수는 그럴듯한 수 중에서 무작위로 고른다.")]
        [Range(0f, 1f)] public float blunderRate = 0.25f;
        [Tooltip("실수 한 번으로 내줄 수 있는 최대치로, 폰 단위다(3 = 비숍. 퀸은 절대 내주지 않는다).")]
        [Min(0f)] public float maxBlunderLoss = 3f;

        [Header("봐주기")]
        [Tooltip("AI가 폰 기준으로 이만큼 앞서면 실수를 더 자주 하기 시작한다.")]
        [Min(0f)] public float mercyLead = 5f;
        [Tooltip("봐주기 기준 격차에 도달한 뒤의 실수 확률이다.")]
        [Range(0f, 1f)] public float mercyBlunderRate = 0.4f;

        [Header("반드시 할 일")]
        [Tooltip("체크메이트가 가능할 때 실제로 두는 확률이다. 1이면 항상 게임을 끝낸다.")]
        [Range(0f, 1f)] public float mateFindRate = 1f;
        [Tooltip("느슨함이 적용되기 전까지 정석대로 두는 초반 수의 개수다(중앙 폰 전진, 기물 전개).")]
        [Min(0)] public int sensibleOpeningMoves = 4;

        [Header("연출")]
        [Tooltip("AI가 수를 두기 전에 생각하는 것처럼 보이는 최소 시간(초)이다.")]
        [Min(0f)] public float minThinkSeconds = 0.6f;
    }
}
