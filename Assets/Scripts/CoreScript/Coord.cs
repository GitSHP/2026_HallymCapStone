using System;
using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// 보드 격자 좌표다. 파일(x)은 오른쪽으로, 랭크(y)는 위쪽으로 증가한다.
    /// 모든 보드 로직은 Coord로 소통하며, 월드 공간을 아는 것은 BoardGrid뿐이다.
    /// </summary>
    [Serializable]
    public struct Coord : IEquatable<Coord>
    {
        public int x;
        public int y;

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static readonly Coord Invalid = new Coord(-1, -1);

        public bool IsValid => x >= 0 && y >= 0;

        public static Coord operator +(Coord a, Vector2Int b) => new Coord(a.x + b.x, a.y + b.y);
        public static Coord operator -(Coord a, Coord b) => new Coord(a.x - b.x, a.y - b.y);
        public static bool operator ==(Coord a, Coord b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Coord a, Coord b) => !(a == b);

        /// <summary>체비쇼프 거리 — 두 칸 사이를 킹이 이동하는 횟수이다.</summary>
        public static int KingDistance(Coord a, Coord b)
            => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>맨해튼 거리. 적의 처리 순서를 정할 때 동률을 가르는 데 쓴다.</summary>
        public static int ManhattanDistance(Coord a, Coord b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        public bool Equals(Coord other) => this == other;
        public override bool Equals(object obj) => obj is Coord other && Equals(other);
        public override int GetHashCode() => (x << 8) ^ y;
        public override string ToString() => $"({x},{y})";
    }
}
