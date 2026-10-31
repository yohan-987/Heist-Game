using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Pure stamina rules: sprinting and carrying drain it, resting refills it, and running dry
    /// locks sprinting until it recovers past a threshold (so you can't tap-sprint on empty).
    /// </summary>
    public sealed class StaminaModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool Exhausted { get; private set; }

        public float DrainPerSecond { get; set; }
        public float RecoverPerSecond { get; set; }
        public float RecoverDelay { get; set; }
        /// <summary>Fraction of Max that must be regained before sprinting unlocks again.</summary>
        public float ExhaustRecoverFraction { get; set; }

        private float _timeSinceDrain;

        public StaminaModel(float max, float drainPerSecond, float recoverPerSecond,
            float recoverDelay = 1f, float exhaustRecoverFraction = 0.3f)
        {
            Max = Mathf.Max(0.01f, max);
            Current = Max;
            DrainPerSecond = drainPerSecond;
            RecoverPerSecond = recoverPerSecond;
            RecoverDelay = recoverDelay;
            ExhaustRecoverFraction = Mathf.Clamp01(exhaustRecoverFraction);
            _timeSinceDrain = recoverDelay;
        }

        public float Fraction => Current / Max;
        public bool CanSprint => !Exhausted && Current > 0f;

        public void SetMax(float max, bool refill = false)
        {
            Max = Mathf.Max(0.01f, max);
            Current = refill ? Max : Mathf.Min(Current, Max);
        }

        /// <summary>Advance one step. <paramref name="extraDrain"/> is a per-second drain from carrying.</summary>
        public void Tick(float dt, bool sprinting, float extraDrain = 0f)
        {
            if (dt <= 0f) return;

            float drain = (sprinting && CanSprint ? DrainPerSecond : 0f) + Mathf.Max(0f, extraDrain);
            if (drain > 0f)
            {
                Current = Mathf.Max(0f, Current - drain * dt);
                _timeSinceDrain = 0f;
                if (Current <= 0f) Exhausted = true;
            }
            else
            {
                _timeSinceDrain += dt;
                if (_timeSinceDrain >= RecoverDelay)
                    Current = Mathf.Min(Max, Current + RecoverPerSecond * dt);
            }

            if (Exhausted && Current >= Max * ExhaustRecoverFraction) Exhausted = false;
        }

        /// <summary>Instant cost (e.g. jumping). Returns false if there wasn't enough.</summary>
        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (Current < amount) return false;
            Current -= amount;
            _timeSinceDrain = 0f;
            if (Current <= 0f) Exhausted = true;
            return true;
        }

        public void Refill()
        {
            Current = Max;
            Exhausted = false;
        }
    }

    /// <summary>Pure health rules.</summary>
    public sealed class HealthModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        public HealthModel(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public float Fraction => Current / Max;

        /// <summary>Returns true if this damage killed the owner.</summary>
        public bool Damage(float amount)
        {
            if (!IsAlive || amount <= 0f) return false;
            Current = Mathf.Max(0f, Current - amount);
            return Current <= 0f;
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public void SetMax(float max, bool refill = false)
        {
            Max = Mathf.Max(1f, max);
            Current = refill ? Max : Mathf.Min(Current, Max);
        }
    }
}
