using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
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
    /// 기물의 MovePattern을 도달 가능한 칸의 집합으로 변환한다.
    /// 상태가 없고 호출마다 할당도 없다 — 리스트는 호출자가 넘겨준다.
    /// </summary>
    public static class MoveResolver
    {
        /// <summary>
        /// 기물의 패턴이 닿는 모든 칸이다. 일반 스테이지에서는 이것이 곧 이동 가능 칸이고,
        /// 체스 대결에서는 ChessRules로 한 번 더 걸러 이동하는 쪽의 킹이
        /// 체크 상태로 남지 않게 한다.
        /// </summary>
        public static void GetLegalMoves(IBoardView state, Piece piece, List<MoveOption> results)
        {
            results.Clear();
            if (piece == null || piece.Def.patterns == null)
                return;

            foreach (var pattern in piece.Def.patterns)
            {
                if (pattern?.directions == null)
                    continue;

                if (pattern.firstMoveOnly && piece.HasMoved)
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
                            continue;   // 연속 이동 기물은 빈 칸을 지나 계속 나아간다
                        }

                        // 무언가에 막혔다. 잡을 수 있으면 잡고, 어느 쪽이든 여기서 멈춘다.
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

                // 두 패턴이 같은 칸에 닿으면 단순 이동보다 잡기를 우선한다.
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
