using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// How an apple decays. On normal difficulty it never rots; on high difficulty it loses freshness
    /// over time and, once rotten, no longer repels doctors.
    /// </summary>
    public sealed class RotModel
    {
        public bool Rots { get; }
        public float RotSeconds { get; }
        public float RottenThreshold { get; }

        /// <summary>1 = fresh, 0 = fully rotten.</summary>
        public float Freshness { get; private set; } = 1f;

        public RotModel(bool rots, float rotSeconds = 90f, float rottenThreshold = 0.15f)
        {
            Rots = rots;
            RotSeconds = Mathf.Max(1f, rotSeconds);
            RottenThreshold = Mathf.Clamp01(rottenThreshold);
        }

        public bool IsRotten => Rots && Freshness <= RottenThreshold;

        /// <summary>Scales the repel effect: 0 when rotten, otherwise tracks freshness (always 1 if it never rots).</summary>
        public float Effectiveness => IsRotten ? 0f : (Rots ? Freshness : 1f);

        public void Tick(float dt)
        {
            if (!Rots || dt <= 0f) return;
            Freshness = Mathf.Max(0f, Freshness - dt / RotSeconds);
        }
    }
}
