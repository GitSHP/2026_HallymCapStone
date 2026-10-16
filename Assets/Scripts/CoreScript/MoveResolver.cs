using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Core
{
    public struct MoveOption   // 기물이 갈 수 있는 칸 하나에 대한 정보
    {
        public Coord target;   // 도착할 칸
        public bool isCapture;   // 그 칸으로 가면 상대 기물을 잡는지 여부 (하이라이트 색 구분에 사용)

        public MoveOption(Coord target, bool isCapture)   // 도착 칸과 잡기 여부로 이동 후보를 만드는 생성자
        {
            this.target = target;
            this.isCapture = isCapture;
        }
    }

    public static class MoveResolver   // 기물의 행마법(MovePattern) 데이터를 "실제로 갈 수 있는 칸 목록"으로 바꿔주는 클래스
    // -> 상태를 저장하지 않고, 결과 리스트도 호출한 쪽이 넘겨주므로 호출할 때마다 메모리를 새로 쓰지 않음
    {
        public static void GetLegalMoves(IBoardView state, Piece piece, List<MoveOption> results)
        // 기물의 행마법이 닿는 모든 칸을 results에 담는 함수
        // -> 일반 스테이지에서는 이 결과가 곧 이동 가능 칸이고,
        //    최종전 체스 대결에서는 ChessRules가 한 번 더 걸러 자기 킹이 공격받는 수를 제외함
        {
            results.Clear();   // 이전 결과를 비움
            if (piece == null || piece.Def.patterns == null)   // 만약 기물이 없거나 행마법이 하나도 없다면 갈 곳이 없음
                return;

            foreach (var pattern in piece.Def.patterns)   // 기물이 가진 행마법 하나하나를 검사 (예: 나이트는 L자 패턴 하나, 폰은 전진 + 대각선 잡기 두 개)
            {
                if (pattern?.directions == null)   // 만약 비어있는 패턴이라면 건너뜀
                    continue;

                if (pattern.firstMoveOnly && piece.HasMoved)   // 만약 첫 수에만 쓸 수 있는 패턴인데 이미 움직인 기물이라면 건너뜀 (예: 폰의 2칸 전진)
                    continue;

                foreach (var rawDir in pattern.directions)   // 패턴에 들어있는 방향 하나하나에 대해
                {
                    var dir = rawDir;   // 원본 방향을 복사해서 사용 (원본 데이터를 바꾸지 않기 위함)
                    if (pattern.relativeToFacing)   // 만약 진영에 따라 앞뒤가 바뀌는 패턴이라면 (예: 폰)
                        dir.y *= piece.Team.ForwardY();   // 적 기물은 y 방향을 뒤집음 -> 적에게 "앞"은 아래쪽이기 때문

                    if (dir == Vector2Int.zero)   // 만약 방향이 (0,0)이면 제자리라 의미가 없으므로 건너뜀 (무한 반복 방지)
                        continue;

                    int range = pattern.sliding ? Mathf.Max(1, pattern.maxDistance) : 1;   // 몇 칸까지 나아갈지 결정
                    // -> 연속 이동(룩·비숍·퀸처럼 쭉 미끄러지는) 패턴이면 최대 사거리만큼, 아니면 딱 한 칸
                    var cursor = piece.Coord;   // 기물의 현재 위치에서 출발

                    for (int step = 0; step < range; step++)   // 한 칸씩 앞으로 나아가며 검사
                    {
                        cursor += dir;   // 그 방향으로 한 칸 이동
                        if (!state.IsInside(cursor))   // 만약 보드 밖으로 나갔다면 이 방향은 여기서 끝
                            break;

                        var occupant = state.GetPiece(cursor);   // 그 칸에 서 있는 기물

                        if (occupant == null)   // 만약 빈 칸이라면
                        {
                            if (pattern.canMoveToEmpty)   // 빈 칸으로 이동할 수 있는 패턴일 때만 후보로 추가 (폰의 대각선은 잡기 전용이라 추가 안 됨)
                                AddUnique(results, new MoveOption(cursor, false));
                            continue;   // 빈 칸은 지나갈 수 있으므로 연속 이동 기물은 계속 나아감
                        }

                        // 여기까지 왔다면 무언가에 막힌 것 -> 잡을 수 있으면 잡고, 어느 쪽이든 여기서 멈춤
                        if (occupant.Team != piece.Team && pattern.canCapture && !occupant.IsObjective)
                        // 만약 상대 기물이고 + 잡기 가능한 패턴이고 + 그 기물이 킹이 아니라면 -> 잡기 후보로 추가 (킹은 잡히지 않는 규칙)
                            AddUnique(results, new MoveOption(cursor, true));

                        break;   // 기물을 뛰어넘을 수는 없으므로 이 방향 탐색 종료
                    }
                }
            }
        }

        static void AddUnique(List<MoveOption> results, MoveOption option)
        // 같은 칸이 중복으로 들어가지 않게 후보를 추가하는 함수
        {
            for (int i = 0; i < results.Count; i++)   // 이미 담긴 후보들을 검사해서
            {
                if (results[i].target != option.target)   // 다른 칸이면 다음 후보로
                    continue;

                // 같은 칸이 이미 있다면 -> 두 패턴이 같은 칸에 닿은 경우
                if (option.isCapture && !results[i].isCapture)   // 새 후보가 잡기이고 기존 후보가 단순 이동이면
                    results[i] = option;   // 잡기로 덮어씀 -> 단순 이동보다 잡기를 우선
                return;   // 어느 쪽이든 새로 추가하지 않음
            }

            results.Add(option);   // 처음 나온 칸이면 그대로 추가
        }

        public static bool IsLegalTarget(List<MoveOption> options, Coord target, out MoveOption match)
        // 플레이어가 기물을 놓은 칸이 이동 후보 목록에 있는지 확인하는 함수 -> 있다면 그 후보 정보를 match로 돌려줌
        {
            foreach (var option in options)   // 모든 후보를 검사해서
            {
                if (option.target == target)   // 만약 놓은 칸과 같은 후보가 있다면
                {
                    match = option;   // 그 후보를 돌려주고
                    return true;   // 이동 가능하다고 알림
                }
            }

            match = default;   // 찾지 못했다면 빈 값을 넣고
            return false;   // 이동 불가로 처리
        }
    }
}
