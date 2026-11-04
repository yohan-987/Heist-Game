namespace Game.Core
{
    public enum DifficultyLevel
    {
        Normal,
        High
    }

    /// <summary>Numbers that change with difficulty. Gameplay reads these instead of hard-coding them.</summary>
    public sealed class DifficultyProfile
    {
        public DifficultyLevel Level { get; }

        /// <summary>Multiplies how fast pressure (insanity) rises.</summary>
        public float InsanityRateMultiplier { get; }

        /// <summary>Multiplies the value needed to produce one dose (higher = harder).</summary>
        public float ValuePerDoseMultiplier { get; }

        /// <summary>Apples visibly rot and lose their effect over time.</summary>
        public bool ApplesRot { get; }

        /// <summary>Multiplies how many doctors the level may spawn.</summary>
        public float EnemyBudgetMultiplier { get; }

        /// <summary>Multiplies the pressure added by events (apples, grabs, scares).</summary>
        public float EventPressureMultiplier { get; }

        /// <summary>Share of spawned value a team is expected to deliver undamaged. Used to size the loot budget.</summary>
        public float CollectionEfficiency { get; }

        /// <summary>Share of spawned value placed in the vault wing. Must stay below 1 - 1.2 x efficiency (see LootPlacement).</summary>
        public float VaultShare { get; }

        private DifficultyProfile(DifficultyLevel level, float insanityRate, float valuePerDose, bool applesRot,
            float enemyBudget, float eventPressure, float collectionEfficiency, float vaultShare)
        {
            Level = level;
            InsanityRateMultiplier = insanityRate;
            ValuePerDoseMultiplier = valuePerDose;
            ApplesRot = applesRot;
            EnemyBudgetMultiplier = enemyBudget;
            EventPressureMultiplier = eventPressure;
            CollectionEfficiency = collectionEfficiency;
            VaultShare = vaultShare;
        }

        public static DifficultyProfile For(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.High:
                    return new DifficultyProfile(level, insanityRate: 1.25f, valuePerDose: 1.2f, applesRot: true,
                        enemyBudget: 1.4f, eventPressure: 1.25f, collectionEfficiency: 0.6f, vaultShare: 0.25f);
                default:
                    return new DifficultyProfile(DifficultyLevel.Normal, insanityRate: 1f, valuePerDose: 1f, applesRot: false,
                        enemyBudget: 1f, eventPressure: 1f, collectionEfficiency: 0.65f, vaultShare: 0.20f);
            }
        }
    }
}
