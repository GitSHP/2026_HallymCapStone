using System;
using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// 이동 규칙 하나를 나타낸다. 정통 체스 기물과 나중에 추가될 변형 기물 모두
    /// 이 규칙의 조합으로 표현되므로, 신규 기물은 새 코드가 아니라
    /// ScriptableObject 데이터로 만든다.
    /// </summary>
    [Serializable]
    public class MovePattern
    {
        [Tooltip("기물 기준의 오프셋이다. 연속 이동 기물이라면 방향으로 쓰이며, 막힐 때까지 반복된다.")]
        public Vector2Int[] directions = Array.Empty<Vector2Int>();

        [Tooltip("한 칸만 가는 대신 막힐 때까지 같은 방향으로 반복 이동한다(룩/비숍/퀸).")]
        public bool sliding;

        [Tooltip("연속 이동 패턴의 사거리 제한이다.")]
        public int maxDistance = 8;

        [Tooltip("빈 칸으로 이동할 수 있다(폰의 대각선 잡기는 false).")]
        public bool canMoveToEmpty = true;

        [Tooltip("적을 잡을 수 있다(폰의 전진은 false).")]
        public bool canCapture = true;

        [Tooltip("적 진영에서는 Y축을 뒤집어, 양쪽 모두에게 '앞'이 올바른 방향이 되게 한다.")]
        public bool relativeToFacing;

        [Tooltip("기물이 아직 움직이지 않았을 때만 쓸 수 있다(폰의 첫 두 칸 전진).")]
        public bool firstMoveOnly;
    }
}
