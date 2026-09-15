using System;
using UnityEngine;

namespace Gambonanza.Core
{
    /// <summary>
    /// Board grid coordinate. File(x) increases to the right, rank(y) increases upward.
    /// All board logic speaks in Coord; only BoardGrid knows about world space.
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

        /// <summary>Chebyshev distance — the number of king moves between two squares.</summary>
        public static int KingDistance(Coord a, Coord b)
            => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>Manhattan distance, used as a tie-breaker when ordering enemies.</summary>
        public static int ManhattanDistance(Coord a, Coord b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        public bool Equals(Coord other) => this == other;
        public override bool Equals(object obj) => obj is Coord other && Equals(other);
        public override int GetHashCode() => (x << 8) ^ y;
        public override string ToString() => $"({x},{y})";
    }
}
