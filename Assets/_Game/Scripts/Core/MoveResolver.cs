using System.Collections.Generic;
using Gambonanza.Data;
using UnityEngine;

namespace Gambonanza.Core
{
    public struct MoveOption
    {
        public Coord target;
        public bool isCapture;

        public MoveOption(Coord target, bool isCapture)
        {
            this.target = target;
            this.isCapture = isCapture;
        }
    }

    /// <summary>
    /// Turns a piece's MovePatterns into the set of squares it may reach.
    /// Stateless and allocation-free per call — the caller supplies the list.
    /// </summary>
    public static class MoveResolver
    {
        public static void GetLegalMoves(BoardState state, Piece piece, List<MoveOption> results)
        {
            results.Clear();
            if (piece == null || piece.Def.patterns == null)
                return;

            foreach (var pattern in piece.Def.patterns)
            {
                if (pattern?.directions == null)
                    continue;

                foreach (var rawDir in pattern.directions)
                {
                    var dir = rawDir;
                    if (pattern.relativeToFacing)
                        dir.y *= piece.Team.ForwardY();

                    if (dir == Vector2Int.zero)
                        continue;

                    int range = pattern.sliding ? Mathf.Max(1, pattern.maxDistance) : 1;
                    var cursor = piece.Coord;

                    for (int step = 0; step < range; step++)
                    {
                        cursor += dir;
                        if (!state.IsInside(cursor))
                            break;

                        var occupant = state.GetPiece(cursor);

                        if (occupant == null)
                        {
                            if (pattern.canMoveToEmpty)
                                AddUnique(results, new MoveOption(cursor, false));
                            continue;   // sliding pieces keep going through empty squares
                        }

                        // Blocked by something: capture it if allowed, then stop either way.
                        if (occupant.Team != piece.Team && pattern.canCapture && !occupant.IsObjective)
                            AddUnique(results, new MoveOption(cursor, true));

                        break;
                    }
                }
            }
        }

        static void AddUnique(List<MoveOption> results, MoveOption option)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].target != option.target)
                    continue;

                // A capture beats a plain move when two patterns reach the same square.
                if (option.isCapture && !results[i].isCapture)
                    results[i] = option;
                return;
            }

            results.Add(option);
        }

        public static bool IsLegalTarget(List<MoveOption> options, Coord target, out MoveOption match)
        {
            foreach (var option in options)
            {
                if (option.target == target)
                {
                    match = option;
                    return true;
                }
            }

            match = default;
            return false;
        }
    }
}
