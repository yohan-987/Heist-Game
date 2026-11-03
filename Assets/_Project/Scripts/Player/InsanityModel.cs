using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Pure insanity rules. Insanity rises over time, doses and music lower it, and sitting at the
    /// maximum starts a short swelling countdown that ends in a burst. Taking a dose during the
    /// swell saves the player.
    /// </summary>
    public sealed class InsanityModel
    {
        public const float MaxValue = 100f;

        public float Value { get; private set; }

        /// <summary>Base rise per second before difficulty scaling.</summary>
        public float RatePerSecond { get; set; }
        public float DoseRelief { get; set; }
        public float UneasyThreshold { get; set; }
        public float TerrifiedThreshold { get; set; }
        public float SwellDuration { get; set; }

        public float SwellTimer { get; private set; }
        public bool Burst { get; private set; }

        public InsanityModel(
            float ratePerSecond = 0.25f,
            float doseRelief = 40f,
            float uneasyThreshold = 50f,
            float terrifiedThreshold = 75f,
            float swellDuration = 5f)
        {
            RatePerSecond = ratePerSecond;
            DoseRelief = doseRelief;
            UneasyThreshold = uneasyThreshold;
            TerrifiedThreshold = terrifiedThreshold;
            SwellDuration = Mathf.Max(0.1f, swellDuration);
        }

        public bool IsSwelling => !Burst && Value >= MaxValue;

        /// <summary>0 to 1 progress toward bursting while swelling, otherwise 0.</summary>
        public float SwellProgress => IsSwelling ? Mathf.Clamp01(SwellTimer / SwellDuration) : (Burst ? 1f : 0f);

        public InsanityStage Stage
        {
            get
            {
                if (IsSwelling || Burst) return InsanityStage.Swelling;
                if (Value >= TerrifiedThreshold) return InsanityStage.Terrified;
                if (Value >= UneasyThreshold) return InsanityStage.Uneasy;
                return InsanityStage.Calm;
            }
        }

        /// <summary>Advance time. <paramref name="rateMultiplier"/> comes from the difficulty profile.</summary>
        public void Tick(float dt, float rateMultiplier = 1f)
        {
            if (Burst || dt <= 0f) return;

            Value = Mathf.Clamp(Value + RatePerSecond * Mathf.Max(0f, rateMultiplier) * dt, 0f, MaxValue);

            if (Value >= MaxValue)
            {
                SwellTimer += dt;
                if (SwellTimer >= SwellDuration) Burst = true;
            }
            else
            {
                SwellTimer = 0f;
            }
        }

        /// <summary>Take one dose. Returns false if the player has already burst.</summary>
        public bool TakeDose()
        {
            if (Burst) return false;
            Relieve(DoseRelief);
            return true;
        }

        public void Relieve(float amount)
        {
            if (Burst || amount <= 0f) return;
            Value = Mathf.Max(0f, Value - amount);
            if (Value < MaxValue) SwellTimer = 0f;
        }

        public void Add(float amount)
        {
            if (Burst || amount <= 0f) return;
            Value = Mathf.Min(MaxValue, Value + amount);
        }

        public void Reset()
        {
            Value = 0f;
            SwellTimer = 0f;
            Burst = false;
        }
    }
}
