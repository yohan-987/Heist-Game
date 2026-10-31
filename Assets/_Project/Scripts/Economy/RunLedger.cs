using UnityEngine;

namespace Game.Economy
{
    /// <summary>Pure bookkeeping for one run: quota, what's safely in the extraction zone, and what was destroyed.</summary>
    public sealed class RunLedger
    {
        public float Quota { get; private set; }
        public float Delivered { get; private set; }
        public float Lost { get; private set; }

        public bool QuotaMet => Quota > 0f && Delivered >= Quota;
        public float Progress => Quota <= 0f ? 0f : Mathf.Clamp01(Delivered / Quota);

        public RunLedger(float quota) => Quota = Mathf.Max(0f, quota);

        public void SetQuota(float quota) => Quota = Mathf.Max(0f, quota);

        /// <summary>Total value currently inside extraction zones. Replaces, not accumulates.</summary>
        public void SetDelivered(float total) => Delivered = Mathf.Max(0f, total);

        public void AddLost(float amount)
        {
            if (amount > 0f) Lost += amount;
        }

        public void Reset(float quota)
        {
            Quota = Mathf.Max(0f, quota);
            Delivered = 0f;
            Lost = 0f;
        }
    }

    public static class QuotaMath
    {
        /// <summary>
        /// Quota for a level: base * growth^(level-1), rounded to the nearest <paramref name="roundTo"/>.
        /// Level 1 returns the (rounded) base.
        /// </summary>
        public static float ForLevel(int level, float baseQuota, float growth, float roundTo = 50f)
        {
            int steps = Mathf.Max(0, level - 1);
            float raw = baseQuota * Mathf.Pow(Mathf.Max(1f, growth), steps);
            if (roundTo <= 0f) return raw;
            return Mathf.Round(raw / roundTo) * roundTo;
        }

        /// <summary>Quota as a fraction of the total value spawned, so a level is always winnable.</summary>
        public static float FromSpawnedValue(float totalSpawnedValue, float fraction, float roundTo = 50f)
        {
            float raw = totalSpawnedValue * Mathf.Clamp01(fraction);
            if (roundTo <= 0f) return raw;
            return Mathf.Round(raw / roundTo) * roundTo;
        }
    }
}
