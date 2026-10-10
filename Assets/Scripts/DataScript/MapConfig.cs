using System;
using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// How a run's map is generated. The shape and the battle pool are data, so
    /// a longer or denser map is a new asset, not new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Map Config", fileName = "MapConfig")]
    public class MapConfig : ScriptableObject
    {
        [Header("Shape")]
        [Tooltip("Floors before the boss floor.")]
        [Min(2)] public int floors = 7;
        [Tooltip("Columns a path can wander across.")]
        [Min(2)] public int columns = 5;
        [Tooltip("Paths walked from bottom to top. More paths, more branches.")]
        [Min(1)] public int paths = 4;

        [Header("Nodes")]
        [Range(0f, 1f)] public float shopChance = 0.2f;
        [Tooltip("No shop below this floor, so the run opens with fights.")]
        [Min(1)] public int firstShopFloor = 2;

        [Tooltip("Battle nodes draw their stage from here at random.")]
        public StageDefinition[] battleStages = Array.Empty<StageDefinition>();

        [Header("Scenes")]
        [Tooltip("Scene loaded when a battle node is entered.")]
        public string battleScene = "Game";
    }
}
