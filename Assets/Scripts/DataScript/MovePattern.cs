using System;
using UnityEngine;

namespace Promotion.Data
{
    [Serializable]
    public class MovePattern    // 기물의 이동 규칙 하나를 나타내는 클래스
    // -> 정통 체스 기물과 나중에 추가될 변형 기물 모두 이 규칙의 조합으로 표현함 -> 신규 기물은 코드가 아니라 에셋 데이터로 만듦
    {
        [Tooltip("기물 기준의 오프셋이다. 연속 이동 기물이라면 방향으로 쓰이며, 막힐 때까지 반복된다.")]
        public Vector2Int[] directions = Array.Empty<Vector2Int>(); // 기물 위치 기준으로 갈 수 있는 칸의 상대 좌표 목록 (예: 나이트는 (1,2), (2,1) ...)

        [Tooltip("한 칸만 가는 대신 막힐 때까지 같은 방향으로 반복 이동한다(룩/비숍/퀸).")]
        public bool sliding;    // true면 directions를 '방향'으로 보고 막힐 때까지 계속 나아감, false면 그 칸으로 한 번만 이동

        [Tooltip("연속 이동 패턴의 사거리 제한이다.")]
        public int maxDistance = 8; // sliding일 때 최대 몇 칸까지 갈 수 있는지 (8이면 판 끝까지)

        [Tooltip("빈 칸으로 이동할 수 있다(폰의 대각선 잡기는 false).")]
        public bool canMoveToEmpty = true;  // 빈 칸으로 이동하는 데 쓸 수 있는 규칙인지 -> 폰의 대각선은 잡을 때만 갈 수 있으므로 false

        [Tooltip("적을 잡을 수 있다(폰의 전진은 false).")]
        public bool canCapture = true;  // 적이 있는 칸으로 들어가 잡을 수 있는 규칙인지 -> 폰의 앞 전진은 잡기가 안 되므로 false

        [Tooltip("적 진영에서는 Y축을 뒤집어, 양쪽 모두에게 '앞'이 올바른 방향이 되게 한다.")]
        public bool relativeToFacing;   // true면 적 기물일 때 위아래를 뒤집음 -> 폰처럼 '앞'으로만 가는 기물이 양 진영 모두 상대 쪽으로 전진함

        [Tooltip("기물이 아직 움직이지 않았을 때만 쓸 수 있다(폰의 첫 두 칸 전진).")]
        public bool firstMoveOnly;  // true면 한 번도 움직이지 않은 기물만 쓸 수 있는 규칙 (최종전 폰의 첫 두 칸 전진)
    }
}
