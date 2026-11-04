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
        /// <summary>Loot value the economy plan asked for.</summary>
        public readonly float Budget;
        /// <summary>Least value that could ever win this level. Spawned value below this means the level cannot be won.</summary>
        public readonly float MinimumWinnableValue;
        /// <summary>Value placed in the nearest two thirds of the rooms. At least 1.2 times the minimum when <see cref="GuaranteeMet"/>.</summary>
        public readonly float NearValue;
        /// <summary>Value placed in the vault wing (farthest third of the rooms).</summary>
        public readonly float VaultValue;
        public readonly bool GuaranteeMet;

        public LevelBuildResult(LevelLayout layout, float spawnedValue, Transform playerSpawn,
            int valuables, int enemies, int nuisances, int quotaDoses, float valuePerDose,
            float budget, float minimumWinnableValue, float nearValue = 0f, float vaultValue = 0f, bool guaranteeMet = true)
        {
            NearValue = nearValue; VaultValue = vaultValue; GuaranteeMet = guaranteeMet;
            Layout = layout; SpawnedValue = spawnedValue; PlayerSpawn = playerSpawn;
            ValuableCount = valuables; EnemyCount = enemies; NuisanceCount = nuisances;
            QuotaDoses = quotaDoses; ValuePerDose = valuePerDose;
            Budget = budget; MinimumWinnableValue = minimumWinnableValue;
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

        /// <summary>
        /// Builds a level. <paramref name="playerCount"/> sizes the quota and the loot budget so the level
        /// stays winnable for that many players (GAME_DESIGN 4.3).
        /// </summary>
        public LevelBuildResult Build(int seed, int playerCount = 1)
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

            var plan = EconomyPlanner.Plan(playerCount, Services.Difficulty,
                _definition.BasePressurePerSecond, _definition.RunMinutes * 60f, _definition.EventPressure,
                _definition.ValuePerDose, _definition.BaseQuotaDoses, _definition.QuotaDosesPerPlayer);
            float budget = _definition.UseManualBudget ? _definition.ManualValueBudget : plan.Budget;

            int counterIndex = _definition.CounterInStartRoom ? layout.StartRoomIndex : layout.ExtractionRoomIndex;
            var counterRoom = instances[counterIndex];
            if (counterRoom.ExtractionZoneRoot != null) counterRoom.ExtractionZoneRoot.SetActive(true);
            else Debug.LogWarning($"Room '{counterRoom.name}' holds the compounder but has no Extraction Zone Root assigned.", counterRoom);

            SealDoors(layout);

            // Separate generators so adding enemies later never shifts where valuables land.
            var valuableRng = new DeterministicRandom(seed ^ 0x5EED1);
            var enemyRng = new DeterministicRandom(seed ^ 0x5EED2);
            var nuisanceRng = new DeterministicRandom(seed ^ 0x5EED3);

            // Manual budgets have no formula, so treat the budget as what a team at the expected efficiency can win.
            float minimum = _definition.UseManualBudget ? budget * Services.Difficulty.CollectionEfficiency : plan.MinimumValue;

            float spawnedValue = 0f, nearValue = 0f, vaultValue = 0f;
            bool guaranteeMet = true;
            int valuableCount = PopulateValuables(layout, instances, valuableRng, budget, minimum,
                ref spawnedValue, ref nearValue, ref vaultValue, ref guaranteeMet);
            int enemyCount = PopulateEnemies(layout, instances, enemyRng);
            int nuisanceCount = PopulateNuisances(layout, instances, nuisanceRng);

            if (_navMeshBaker is INavMeshBaker baker) baker.Bake(_root);

            // Quota comes from the plan (quota first); the loot budget was derived from it. If the rooms
            // could not hold enough loot, say so loudly: the level cannot be won.
            if (spawnedValue < plan.MinimumValue)
                Debug.LogWarning($"Level cannot be won: spawned value {spawnedValue:0} is below the {plan.MinimumValue:0} needed " +
                                 $"for {plan.Players} player(s) (quota {plan.QuotaDoses} doses). Add valuable spawn points or raise item values.");

            var spawn = instances[layout.StartRoomIndex].PlayerSpawn;
            var result = new LevelBuildResult(layout, spawnedValue, spawn, valuableCount, enemyCount,
                nuisanceCount, plan.QuotaDoses, _definition.ValuePerDose, budget, plan.MinimumValue,
                nearValue, vaultValue, guaranteeMet);
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

        private int PopulateValuables(LevelLayout layout, List<RoomAuthoring> rooms, DeterministicRandom rng,
            float budget, float minimumValue, ref float totalValue, ref float nearValue, ref float vaultValue,
            ref bool guaranteeMet)
        {
            var pool = _definition.ValuablePool;
            if (pool == null || pool.Length == 0) return 0;

            // Near rooms (nearest two thirds) and the vault wing (farthest third) get their own point lists.
            var distances = new List<int>(layout.Rooms.Count);
            for (int i = 0; i < layout.Rooms.Count; i++) distances.Add(layout.Rooms[i].DistanceFromStart);
            bool[] isVault = LootPlacement.ClassifyVault(distances, layout.StartRoomIndex);

            var nearPoints = new List<Transform>();
            var vaultPoints = new List<Transform>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (i == layout.StartRoomIndex) continue;
                var target = isVault[i] ? vaultPoints : nearPoints;
                foreach (var t in rooms[i].ValuableSpawns)
                    if (t != null) target.Add(t);
            }
            if (nearPoints.Count + vaultPoints.Count == 0) return 0;
            rng.Shuffle(nearPoints);
            rng.Shuffle(vaultPoints);

            var options = new List<SpawnOption>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].Prefab != null)
                    options.Add(new SpawnOption(i, pool[i].BaseValue, pool[i].SpawnWeight));

            var picks = SpawnPlanner.Choose(rng, options, budget, nearPoints.Count + vaultPoints.Count);

            var values = new List<float>(picks.Count);
            foreach (int pick in picks) values.Add(pool[pick].BaseValue);

            var placement = LootPlacement.Assign(values, nearPoints.Count, vaultPoints.Count,
                Services.Difficulty.VaultShare, minimumValue);

            int placed = 0;
            for (int n = 0; n < picks.Count; n++)
            {
                int slot = placement.SlotForItem[n];
                if (slot < 0) continue;

                var def = pool[picks[n]];
                var point = slot < nearPoints.Count ? nearPoints[slot] : vaultPoints[slot - nearPoints.Count];
                var go = Services.Spawner.Spawn(def.Prefab, point.position, point.rotation, _root);
                var valuable = go.GetComponent<Valuable>();
                if (valuable != null) valuable.Initialise(def);
                totalValue += def.BaseValue;
                placed++;
            }

            nearValue = placement.NearValue;
            vaultValue = placement.VaultValue;
            guaranteeMet = placement.GuaranteeMet;
            if (!placement.GuaranteeMet)
                Debug.LogWarning($"Guaranteed-value rule broken: the nearest rooms hold {placement.NearValue:0}, " +
                                 $"but 1.2 x the minimum winnable value is {LootPlacement.GuaranteeFactor * minimumValue:0}. " +
                                 "Add valuable spawn points in the near rooms.");
            if (placement.Overflow > 0 || placement.Unplaced > 0)
                Debug.LogWarning($"Not enough valuable spawn points in the near rooms: {placement.Overflow} item(s) moved to the vault, " +
                                 $"{placement.Unplaced} could not be placed.");
            return placed;
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
