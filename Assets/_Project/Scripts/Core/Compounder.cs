using UnityEngine;

namespace Game.Core
{
    /// <summary>The compounder's recipe: how much delivered value becomes how much medicine.</summary>
    public static class Compounder
    {
        /// <summary>Whole doses produced from a total value. Partial doses are not produced.</summary>
        public static int DosesFor(float totalValue, float valuePerDose)
        {
            if (valuePerDose <= 0f || totalValue <= 0f) return 0;
            return Mathf.FloorToInt(totalValue / valuePerDose);
        }

        /// <summary>
        /// Quota in doses for a level: a fraction of what the level could produce, so the level is always
        /// winnable, and never less than one dose.
        /// </summary>
        public static int QuotaDoses(float totalSpawnedValue, float fraction, float valuePerDose)
        {
            int possible = DosesFor(totalSpawnedValue, valuePerDose);
            int quota = Mathf.FloorToInt(possible * Mathf.Clamp01(fraction));
            return Mathf.Max(1, quota);
        }
    }
}
