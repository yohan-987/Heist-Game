using System.Collections.Generic;
using Game.Core;

namespace Game.LevelGen
{
    public readonly struct SpawnOption
    {
        public readonly int Id;
        public readonly float Cost;
        public readonly float Weight;

        public SpawnOption(int id, float cost, float weight)
        {
            Id = id; Cost = cost; Weight = weight;
        }
    }

    /// <summary>
    /// Spends a budget on weighted random picks (valuables by value, enemies by threat cost).
    /// Deterministic for a given rng state. Options with non-positive cost or weight are never picked,
    /// which also guarantees the loop terminates.
    /// </summary>
    public static class SpawnPlanner
    {
        public static List<int> Choose(
            DeterministicRandom rng,
            IReadOnlyList<SpawnOption> options,
            float budget,
            int maxCount = 256)
        {
            var picked = new List<int>();
            if (options == null || options.Count == 0 || budget <= 0f) return picked;

            float remaining = budget;
            var affordable = new List<SpawnOption>(options.Count);

            while (picked.Count < maxCount)
            {
                affordable.Clear();
                float totalWeight = 0f;
                for (int i = 0; i < options.Count; i++)
                {
                    var o = options[i];
                    if (o.Cost <= 0f || o.Weight <= 0f || o.Cost > remaining) continue;
                    affordable.Add(o);
                    totalWeight += o.Weight;
                }
                if (affordable.Count == 0) break;

                float roll = rng.NextFloat() * totalWeight;
                SpawnOption chosen = affordable[affordable.Count - 1];
                for (int i = 0; i < affordable.Count; i++)
                {
                    roll -= affordable[i].Weight;
                    if (roll < 0f) { chosen = affordable[i]; break; }
                }

                picked.Add(chosen.Id);
                remaining -= chosen.Cost;
            }

            return picked;
        }
    }
}
