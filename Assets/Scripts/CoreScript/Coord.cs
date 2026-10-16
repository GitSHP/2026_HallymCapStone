using System;
using UnityEngine;

namespace Promotion.Core
{
    [Serializable]
    public struct Coord : IEquatable<Coord>   // 체스판 위의 칸 하나를 (열, 행) 정수 좌표로 나타내는 구조체
    // -> 월드 좌표(소수점)가 아니라 칸 번호로 다루기 때문에 규칙 계산이 단순해짐
    {
        public int x;   // 열 번호 (왼쪽이 0)
        public int y;   // 행 번호 (아래쪽이 0, 플레이어 진영 쪽)

        public Coord(int x, int y)   // 열·행 값을 받아 좌표를 만드는 생성자
        {
            this.x = x;
            this.y = y;
        }

        public static readonly Coord Invalid = new Coord(-1, -1);   // "칸이 없음"을 뜻하는 특수 좌표 -> 보드 밖을 가리키거나 선택된 칸이 없을 때 사용

        public bool IsValid => x >= 0 && y >= 0;   // Invalid 같은 음수 좌표가 아닌지 확인 (보드 크기 검사는 BoardGrid.IsInside가 담당)

        public static Coord operator +(Coord a, Vector2Int b) => new Coord(a.x + b.x, a.y + b.y);   // 좌표에 방향(한 칸 이동량)을 더해 다음 칸을 구하는 연산자
        public static Coord operator -(Coord a, Coord b) => new Coord(a.x - b.x, a.y - b.y);   // 두 좌표의 차이(얼마나 떨어져 있는지)를 구하는 연산자
        public static bool operator ==(Coord a, Coord b) => a.x == b.x && a.y == b.y;   // 열과 행이 모두 같으면 같은 칸
        public static bool operator !=(Coord a, Coord b) => !(a == b);   // 같은 칸이 아닌지 검사

        public static int KingDistance(Coord a, Coord b)   // 킹이 한 칸씩(대각선 포함) 움직일 때 몇 번 만에 도착하는지 구하는 함수
            => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));   // 가로 차이와 세로 차이 중 큰 값 -> 대각선 이동이 가로·세로를 동시에 줄여주기 때문

        public static int ManhattanDistance(Coord a, Coord b)   // 가로·세로로만 움직일 때의 거리를 구하는 함수
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);   // 가로 차이 + 세로 차이

        public bool Equals(Coord other) => this == other;   // IEquatable 구현 -> Dictionary/HashSet에서 박싱 없이 비교하기 위함
        public override bool Equals(object obj) => obj is Coord other && Equals(other);   // object로 넘어온 값도 Coord라면 같은 방식으로 비교
        public override int GetHashCode() => (x << 8) ^ y;   // 해시값 계산 -> x를 8비트 밀어서 y와 섞어 칸마다 다른 값이 나오게 함
        public override string ToString() => $"({x},{y})";   // 로그 출력용 문자열 "(x,y)"
    }
}
