using UnityEngine;

namespace Game.Core
{
    /// <summary>The numbers a level needs so that it can be won. Produced by <see cref="EconomyPlanner.Plan"/>.</summary>
    public sealed class EconomyPlan
    {
        public readonly int Players;
        public readonly int QuotaDoses;
        public readonly int DosesPerPlayer;
        /// <summary>Value per dose after difficulty scaling.</summary>
        public readonly float ValuePerDose;
        public readonly float Efficiency;
        /// <summary>Loot value to spawn, including allowance for damaged and missed loot.</summary>
        public readonly float Budget;
        /// <summary>Least value that could ever win, assuming every item is delivered undamaged.</summary>
        public readonly float MinimumValue;

        public EconomyPlan(int players, int quotaDoses, int dosesPerPlayer, float valuePerDose,
            float efficiency, float budget, float minimumValue)
        {
            Players = players; QuotaDoses = quotaDoses; DosesPerPlayer = dosesPerPlayer;
            ValuePerDose = valuePerDose; Efficiency = efficiency; Budget = budget; MinimumValue = minimumValue;
        }
    }

    /// <summary>
    /// Pure maths behind the loot budget (GAME_DESIGN 4.3). A level must be winnable by a competent team:
    /// the loot has to pay for the quota and for the doses everyone needs to stay alive.
    /// </summary>
    public static class EconomyPlanner
    {
        public const float DoseRelief = 40f;
        public const float MaxPressure = 100f;

        /// <summary>Quota in doses: a base amount plus a share per player.</summary>
        public static int QuotaDoses(int players, int baseDoses = 4, int perPlayer = 2)
        {
            return baseDoses + perPlayer * Mathf.Max(1, players);
        }

        /// <summary>
        /// Fewest doses one player needs to still be below the burst point at the end of a run:
        /// the smallest n with rise + events - relief * n below the maximum.
        /// </summary>
        public static int SurvivalDoses(float risePerSecond, float runSeconds, float eventPressure,
            float doseRelief = DoseRelief, float maxPressure = MaxPressure)
        {
            if (doseRelief <= 0f) return 0;
            float excess = risePerSecond * runSeconds + eventPressure - maxPressure;
            if (excess < 0f) return 0;
            return Mathf.FloorToInt(excess / doseRelief) + 1;
        }

        /// <summary>Loot value needed so that quota plus everyone's survival doses can be delivered at the given efficiency.</summary>
        public static float LootBudget(int players, int quotaDoses, int dosesPerPlayer, float valuePerDose, float efficiency)
        {
            float eff = Mathf.Clamp(efficiency, 0.05f, 1f);
            int doses = quotaDoses + Mathf.Max(1, players) * dosesPerPlayer;
            return valuePerDose * doses / eff;
        }

        public static EconomyPlan Plan(int players, DifficultyProfile difficulty, float baseRisePerSecond,
            float runSeconds, float baseEventPressure, float baseValuePerDose, int baseQuota = 4, int quotaPerPlayer = 2)
        {
            int p = Mathf.Max(1, players);
            float rise = baseRisePerSecond * difficulty.InsanityRateMultiplier;
            float events = baseEventPressure * difficulty.EventPressureMultiplier;
            float vpd = baseValuePerDose * difficulty.ValuePerDoseMultiplier;

            int quota = QuotaDoses(p, baseQuota, quotaPerPlayer);
            int perPlayer = SurvivalDoses(rise, runSeconds, events);
            float budget = LootBudget(p, quota, perPlayer, vpd, difficulty.CollectionEfficiency);
            float minimum = vpd * (quota + p * perPlayer);

            return new EconomyPlan(p, quota, perPlayer, vpd, difficulty.CollectionEfficiency, budget, minimum);
        }
    }
}
