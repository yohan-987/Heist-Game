using Game.Enemies;
using Game.Items;
using UnityEngine;

namespace Game.LevelGen
{
    /// <summary>Settings for one kind of level. Create via Assets > Create > Game > Level.</summary>
    [CreateAssetMenu(menuName = "Game/Level", fileName = "Level_New")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Layout")]
        [Min(2)] public int RoomCount = 8;
        [Tooltip("World size of one grid cell, in metres. Must match how rooms were built.")]
        [Min(1f)] public float CellSize = 8f;
        public RoomAuthoring[] StartRooms;
        public RoomAuthoring[] RoomPool;
        [Tooltip("Spawned at every doorway that ended up with no neighbour.")]
        public GameObject DoorSealPrefab;

        [Header("Valuables")]
        public ValuableDefinition[] ValuablePool;
        [Min(0f)] public float ValueBudget = 6000f;

        [Header("Medicine")]
        [Tooltip("Delivered value needed to produce one dose (before difficulty scaling).")]
        [Min(1f)] public float ValuePerDose = 500f;
        [Tooltip("Quota is this fraction of the doses the spawned value could produce, so a level is always winnable.")]
        [Range(0.1f, 1f)] public float QuotaFraction = 0.55f;

        [Header("Doctors (lethal)")]
        public EnemyDefinition[] EnemyPool;
        [Min(0f)] public float EnemyBudget = 3f;

        [Header("Nuisances (non-lethal)")]
        [Tooltip("Prefabs such as a ground doctor or a spider nest. One is picked per spawn point.")]
        public GameObject[] NuisancePrefabs;
        [Min(0)] public int NuisanceCount = 4;
    }
}
