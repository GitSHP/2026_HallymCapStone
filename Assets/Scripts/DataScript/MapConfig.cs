using System;
using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// 런의 맵을 생성하는 방식을 정한다. 맵 형태와 전투 풀이 데이터이므로,
    /// 더 길거나 촘촘한 맵은 새 코드가 아니라 새 에셋으로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("형태")]
        [Tooltip("보스 층 이전까지의 층 수다.")]
        [Min(2)] public int floors = 7;
        [Tooltip("경로가 오갈 수 있는 열의 수다.")]
        [Min(2)] public int columns = 5;
        [Tooltip("아래에서 위로 그어지는 경로의 수다. 경로가 많을수록 갈림길도 많아진다.")]
        [Min(1)] public int paths = 4;

        [Header("노드")]
        [Range(0f, 1f)] public float shopChance = 0.2f;
        [Tooltip("이 층보다 아래에는 상점이 없어, 런이 전투로 시작된다.")]
        [Min(1)] public int firstShopFloor = 2;

        [Tooltip("전투 노드는 여기서 스테이지를 무작위로 뽑는다.")]
        public StageDefinition[] battleStages = Array.Empty<StageDefinition>();

        [Tooltip("보스 노드에 연결된 스테이지다(최종 체스 대결). 비워 두면 보스는 임시 노드로 남는다.")]
        public StageDefinition bossStage;

        [Header("씬")]
        [Tooltip("전투 노드에 들어갈 때 불러오는 씬이다.")]
        public string battleScene = "Game";
    }
}
