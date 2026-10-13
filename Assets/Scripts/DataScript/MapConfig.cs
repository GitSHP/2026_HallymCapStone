using System;
using UnityEngine;

namespace Promotion.Data
{
    [CreateAssetMenu(menuName = "Promotion/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject   // 한 판(런)의 맵을 어떻게 생성할지 정하는 설정 데이터
    // -> 맵 형태와 전투 스테이지 목록이 데이터이므로 더 길거나 촘촘한 맵은 코드 수정 없이 새 에셋으로 만들 수 있음
    {
        [Header("형태")]
        [Tooltip("보스 층 이전까지의 층 수다.")]
        [Min(2)] public int floors = 7; // 맵의 세로 길이(층 수) -> 보스 층은 이 층들 위에 따로 하나 붙음
        [Tooltip("경로가 오갈 수 있는 열의 수다.")]
        [Min(2)] public int columns = 5;    // 맵의 가로 폭(열 수) -> 노드가 놓일 수 있는 칸의 개수
        [Tooltip("아래에서 위로 그어지는 경로의 수다. 경로가 많을수록 갈림길도 많아진다.")]
        [Min(1)] public int paths = 4;  // 맨 아래층에서 위로 그어지는 길의 개수 -> 많을수록 선택지(갈림길)가 늘어남

        [Header("노드")]
        [Range(0f, 1f)] public float shopChance = 0.2f; // 한 노드가 전투 대신 상점이 될 확률 (0.2 = 20%)
        [Tooltip("이 층보다 아래에는 상점이 없어, 런이 전투로 시작된다.")]
        [Min(1)] public int firstShopFloor = 2; // 상점이 처음 나올 수 있는 층 -> 그 아래층은 모두 전투 노드

        [Tooltip("전투 노드는 여기서 스테이지를 무작위로 뽑는다.")]
        public StageDefinition[] battleStages = Array.Empty<StageDefinition>(); // 전투 노드에 배정될 스테이지 후보 목록

        [Tooltip("보스 노드에 연결된 스테이지다(최종 체스 대결). 비워 두면 보스는 임시 노드로 남는다.")]
        public StageDefinition bossStage;   // 맨 위 보스 노드에서 진행할 스테이지 (최종전 체스 대결)

        [Header("씬")]
        [Tooltip("전투 노드에 들어갈 때 불러오는 씬이다.")]
        public string battleScene = "Game"; // 전투 노드를 눌렀을 때 불러올 씬의 이름
    }
}
