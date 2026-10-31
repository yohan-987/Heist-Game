using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Items;
using UnityEngine;

namespace Game.LevelGen
{
    /// <summary>Anything that can bake a NavMesh over a built level (e.g. a NavMeshSurface wrapper).</summary>
    public interface INavMeshBaker
    {
        void Bake(Transform levelRoot);
    }

    public readonly struct LevelBuildResult
    {
        public readonly LevelLayout Layout;
        public readonly float SpawnedValue;
        public readonly Transform PlayerSpawn;
        public readonly int ValuableCount;
        public readonly int EnemyCount;
        public readonly int NuisanceCount;
        /// <summary>Quota in doses, already scaled for the current difficulty.</summary>
        public readonly int QuotaDoses;
        /// <summary>The level's base value per dose. Pass this to <c>RunTracker.Begin</c>, which applies difficulty itself.</summary>
        public readonly float ValuePerDose;

        public LevelBuildResult(LevelLayout layout, float spawnedValue, Transform playerSpawn,
            int valuables, int enemies, int nuisances, int quotaDoses, float valuePerDose)
        {
            Layout = layout; SpawnedValue = spawnedValue; PlayerSpawn = playerSpawn;
            ValuableCount = valuables; EnemyCount = enemies; NuisanceCount = nuisances;
            QuotaDoses = quotaDoses; ValuePerDose = valuePerDose;
        }
    }

    /// <summary>
    /// Turns a seed + <see cref="LevelDefinition"/> into a playable level: plans the layout, spawns rooms,
    /// places valuables and enemies within budget, seals unused doors and bakes the NavMesh.
    /// Everything is spawned through the Spawner in a fixed order so ids match across machines.
    /// </summary>
    public sealed class LevelBuilder : MonoBehaviour
    {
        [SerializeField] private LevelDefinition _definition;
        [SerializeField] private Transform _root;
        [Tooltip("Optional: a component implementing INavMeshBaker. Leave empty to skip baking.")]
        [SerializeField] private MonoBehaviour _navMeshBaker;

        public LevelBuildResult? Last { get; private set; }

        public LevelBuildResult Build(int seed)
        {
            if (_definition == null) throw new System.InvalidOperationException("LevelBuilder has no LevelDefinition.");
            if (_root == null) _root = transform;

            Clear();
            Services.Ids.Reset();

            // Authoring lookup: template id -> prefab.
            var prefabsById = new Dictionary<string, RoomAuthoring>();
            var startTemplates = ToTemplates(_definition.StartRooms, prefabsById);
            var poolTemplates = ToTemplates(_definition.RoomPool, prefabsById);

            var layout = LayoutPlanner.Plan(seed, startTemplates, poolTemplates, _definition.RoomCount);

            // Rooms
            var instances = new List<RoomAuthoring>(layout.Rooms.Count);
            foreach (var placed in layout.Rooms)
            {
                var prefab = prefabsById[placed.Template.Id];
                Vector3 pos = GridToWorld(placed.Origin);
                var go = Services.Spawner.Spawn(prefab.gameObject, pos, Quaternion.identity, _root);
                go.name = $"Room_{placed.Index}_{placed.Template.Id}";
                instances.Add(go.GetComponent<RoomAuthoring>());
            }

            var extractionRoom = instances[layout.ExtractionRoomIndex];
            if (extractionRoom.ExtractionZoneRoot != null) extractionRoom.ExtractionZoneRoot.SetActive(true);

            SealDoors(layout);

            // Separate generators so adding enemies later never shifts where valuables land.
            var valuableRng = new DeterministicRandom(seed ^ 0x5EED1);
            var enemyRng = new DeterministicRandom(seed ^ 0x5EED2);
            var nuisanceRng = new DeterministicRandom(seed ^ 0x5EED3);

            float spawnedValue = 0f;
            int valuableCount = PopulateValuables(layout, instances, valuableRng, ref spawnedValue);
            int enemyCount = PopulateEnemies(layout, instances, enemyRng);
            int nuisanceCount = PopulateNuisances(layout, instances, nuisanceRng);

            if (_navMeshBaker is INavMeshBaker baker) baker.Bake(_root);

            // Quota is a share of what the level could produce, so it is always winnable.
            float scaledValuePerDose = _definition.ValuePerDose * Services.Difficulty.ValuePerDoseMultiplier;
            int quotaDoses = Compounder.QuotaDoses(spawnedValue, _definition.QuotaFraction, scaledValuePerDose);

            var spawn = instances[layout.StartRoomIndex].PlayerSpawn;
            var result = new LevelBuildResult(layout, spawnedValue, spawn, valuableCount, enemyCount,
                nuisanceCount, quotaDoses, _definition.ValuePerDose);
            Last = result;
            return result;
        }

        public void Clear()
        {
            if (_root == null) _root = transform;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i).gameObject;
                // Destroy is deferred to end of frame; deactivate now so stale rooms can't leak
                // into the new level's NavMesh bake or physics.
                child.SetActive(false);
                Services.Spawner.Despawn(child);
            }
        }

        public Vector3 GridToWorld(Vector2Int cell) =>
            new Vector3(cell.x * _definition.CellSize, 0f, cell.y * _definition.CellSize);

        private static List<RoomTemplate> ToTemplates(RoomAuthoring[] prefabs, Dictionary<string, RoomAuthoring> byId)
        {
            var list = new List<RoomTemplate>();
            if (prefabs == null) return list;
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                var template = prefab.ToTemplate();
                if (byId.ContainsKey(template.Id) && byId[template.Id] != prefab)
                    Debug.LogError($"Two room prefabs share the id '{template.Id}'. Ids must be unique.", prefab);
                byId[template.Id] = prefab;
                list.Add(template);
            }
            return list;
        }

        private void SealDoors(LevelLayout layout)
        {
            if (_definition.DoorSealPrefab == null) return;

            float cell = _definition.CellSize;
            foreach (var door in layout.SealedDoors)
            {
                var dir = door.Direction.ToOffset();
                Vector3 center = GridToWorld(door.Cell) + new Vector3(cell * 0.5f, 0f, cell * 0.5f);
                Vector3 pos = center + new Vector3(dir.x, 0f, dir.y) * (cell * 0.5f);
                var rot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));
                Services.Spawner.Spawn(_definition.DoorSealPrefab, pos, rot, _root);
            }
        }

        private int PopulateValuables(LevelLayout layout, List<RoomAuthoring> rooms, DeterministicRandom rng, ref float totalValue)
        {
            var pool = _definition.ValuablePool;
            if (pool == null || pool.Length == 0) return 0;

            var points = CollectPoints(layout, rooms, r => r.ValuableSpawns);
            if (points.Count == 0) return 0;
            rng.Shuffle(points);

            var options = new List<SpawnOption>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].Prefab != null)
                    options.Add(new SpawnOption(i, pool[i].BaseValue, pool[i].SpawnWeight));

            var picks = SpawnPlanner.Choose(rng, options, _definition.ValueBudget, points.Count);

            for (int n = 0; n < picks.Count; n++)
            {
                var def = pool[picks[n]];
                var point = points[n];
                var go = Services.Spawner.Spawn(def.Prefab, point.position, point.rotation, _root);
                var valuable = go.GetComponent<Valuable>();
                if (valuable != null) valuable.Initialise(def);
                totalValue += def.BaseValue;
            }
            return picks.Count;
        }

        private int PopulateEnemies(LevelLayout layout, List<RoomAuthoring> rooms, DeterministicRandom rng)
        {
            var pool = _definition.EnemyPool;
            if (pool == null || pool.Length == 0) return 0;

            var points = CollectPoints(layout, rooms, r => r.EnemySpawns);
            if (points.Count == 0) return 0;
            rng.Shuffle(points);

            var options = new List<SpawnOption>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].Prefab != null)
                    options.Add(new SpawnOption(i, pool[i].SpawnCost, pool[i].SpawnWeight));

            float budget = _definition.EnemyBudget * Services.Difficulty.EnemyBudgetMultiplier;
            var picks = SpawnPlanner.Choose(rng, options, budget, points.Count);

            for (int n = 0; n < picks.Count; n++)
            {
                var def = pool[picks[n]];
                var point = points[n];
                Services.Spawner.Spawn(def.Prefab, point.position, point.rotation, _root);
            }
            return picks.Count;
        }

        /// <summary>Ground doctors, spider nests and similar non-lethal hazards, placed at nuisance spawn points.</summary>
        private int PopulateNuisances(LevelLayout layout, List<RoomAuthoring> rooms, DeterministicRandom rng)
        {
            var prefabs = _definition.NuisancePrefabs;
            if (prefabs == null || prefabs.Length == 0 || _definition.NuisanceCount <= 0) return 0;

            var points = CollectPoints(layout, rooms, r => r.NuisanceSpawns);
            if (points.Count == 0) return 0;
            rng.Shuffle(points);

            int wanted = System.Math.Min(_definition.NuisanceCount, points.Count);
            int spawned = 0;
            for (int n = 0; n < wanted; n++)
            {
                var prefab = prefabs[rng.Range(0, prefabs.Length)];
                if (prefab == null) continue;

                var point = points[n];
                Services.Spawner.Spawn(prefab, point.position, point.rotation, _root);
                spawned++;
            }
            return spawned;
        }

        /// <summary>Spawn points from every room except the start room, in a fixed order.</summary>
        private static List<Transform> CollectPoints(
            LevelLayout layout, List<RoomAuthoring> rooms, System.Func<RoomAuthoring, Transform[]> selector)
        {
            var points = new List<Transform>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == layout.StartRoomIndex) continue;
                foreach (var t in selector(rooms[i]))
                    if (t != null) points.Add(t);
            }
            return points;
        }
    }
}
