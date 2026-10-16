using System;
using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Map
{
    public enum MapNodeType // 지도 노드의 종류
    {
        Battle, // 전투 노드 -> 연결된 스테이지(stage)에서 전투를 치름
        Shop,   // 상점 노드
        Boss    // 보스 노드 -> 지도의 맨 위 끝, 최종전이 연결됨
    }

    [Serializable]
    public class MapNode // 런 지도의 지점(노드) 하나 -> 연결은 항상 위쪽으로, 한 층씩만 이어짐
    {
        public int floor;  // 이 노드가 있는 층 (0 = 맨 아래층)
        public int column; // 이 노드가 있는 가로 칸 번호
        public MapNodeType type; // 이 노드의 종류 (전투/상점/보스)

        [Tooltip("이 노드에서 치르는 전투다. 전투 노드에서만 쓴다.")]
        public StageDefinition stage; // 이 노드에 들어가면 치를 스테이지 데이터 (상점 노드는 비어 있음)

        [Tooltip("지도가 격자처럼 보이지 않도록 주는 [-1, 1] 범위의 작은 무작위 오프셋이다.")]
        public Vector2 jitter; // 화면에 그릴 때 위치를 살짝 흔드는 값 -> 지도가 딱딱한 격자처럼 보이지 않게 함

        public List<int> next = new(); // 이 노드에서 갈 수 있는 바로 위층 노드들의 번호 목록
    }

    [Serializable]
    public class MapData // 런 하나의 지도 전체
    // -> 스테이지 참조 외에는 Unity 오브젝트가 없는 순수 데이터라서 RunState 안에 두면 씬을 다시 불러와도 유지됨
    {
        public int seed;    // 이 지도를 만든 시드 값 (같은 시드 = 같은 지도)
        public int floors;  // 보스 층을 포함한 전체 층 수
        public int columns; // 한 층의 가로 칸 수
        public List<MapNode> nodes = new(); // 지도의 모든 노드 목록 -> 노드 번호는 이 리스트의 인덱스

        public int BossFloor => floors - 1; // 보스 노드가 있는 층 번호 (맨 위층)

        public void GetSelectable(int current, List<int> results)
        // 현재 노드(current)에서 플레이어가 다음으로 갈 수 있는 노드 번호들을 results에 담아 주는 함수
        // -> 아직 아무 곳에도 들어가지 않았다면 첫 층 노드들, 그 뒤로는 현재 노드와 연결된 노드들
        {
            results.Clear(); // 이전 결과를 비우고

            if (current < 0 || current >= nodes.Count) // 만약 현재 노드가 없다면 (런을 막 시작해서 아직 아무 노드도 안 들어감)
            {
                for (int i = 0; i < nodes.Count; i++) // 모든 노드 중에서
                    if (nodes[i].floor == 0) // 0층(맨 아래층) 노드라면
                        results.Add(i); // 선택 가능한 노드로 추가함
                return;
            }

            results.AddRange(nodes[current].next); // 현재 노드와 연결된 위층 노드들이 선택 가능
        }
    }
}
