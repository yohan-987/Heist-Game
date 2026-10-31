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

        /// <summary>Multiplies how fast insanity rises.</summary>
        public float InsanityRateMultiplier { get; }

        /// <summary>Multiplies the value needed to produce one dose (higher = harder).</summary>
        public float ValuePerDoseMultiplier { get; }

        /// <summary>Apples visibly rot and lose their effect over time.</summary>
        public bool ApplesRot { get; }

        /// <summary>Multiplies how many doctors the level may spawn.</summary>
        public float EnemyBudgetMultiplier { get; }

        private DifficultyProfile(DifficultyLevel level, float insanityRate, float valuePerDose, bool applesRot, float enemyBudget)
        {
            Level = level;
            InsanityRateMultiplier = insanityRate;
            ValuePerDoseMultiplier = valuePerDose;
            ApplesRot = applesRot;
            EnemyBudgetMultiplier = enemyBudget;
        }

        public static DifficultyProfile For(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.High:
                    return new DifficultyProfile(level, insanityRate: 1.5f, valuePerDose: 1.25f, applesRot: true, enemyBudget: 1.5f);
                default:
                    return new DifficultyProfile(DifficultyLevel.Normal, insanityRate: 1f, valuePerDose: 1f, applesRot: false, enemyBudget: 1f);
            }
        }
    }
}
