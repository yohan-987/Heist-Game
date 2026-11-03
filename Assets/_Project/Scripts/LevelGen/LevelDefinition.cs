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

        [Header("Economy (see GAME_DESIGN 4.3)")]
        [Tooltip("Target length of a run. The loot budget is sized so that everyone can stay alive for this long and still meet the quota.")]
        [Min(1f)] public float RunMinutes = 10f;
        [Tooltip("Delivered value needed to produce one dose (before difficulty scaling).")]
        [Min(1f)] public float ValuePerDose = 500f;
        [Tooltip("Quota in doses is base + per-player * players.")]
        [Min(0)] public int BaseQuotaDoses = 4;
        [Min(0)] public int QuotaDosesPerPlayer = 2;
        [Tooltip("Pressure per player per run from apples, grabs and scares. Used to size the budget.")]
        [Min(0f)] public float EventPressure = 33f;
        [Tooltip("Passive pressure rise per second. Must match PlayerInsanity.")]
        [Min(0f)] public float BasePressurePerSecond = 0.25f;
        [Tooltip("Tick to ignore the formula and spawn exactly the manual budget (for quick test levels).")]
        public bool UseManualBudget;
        [Min(0f)] public float ManualValueBudget = 6000f;
        [Tooltip("The compounder and exit live in the start room (safe, next to home). Untick to use the farthest room.")]
        public bool CounterInStartRoom = true;

        [Header("Doctors (lethal)")]
        public EnemyDefinition[] EnemyPool;
        [Min(0f)] public float EnemyBudget = 3f;

        [Header("Nuisances (non-lethal)")]
        [Tooltip("Prefabs such as a ground doctor or a spider nest. One is picked per spawn point.")]
        public GameObject[] NuisancePrefabs;
        [Min(0)] public int NuisanceCount = 4;
    }
}
