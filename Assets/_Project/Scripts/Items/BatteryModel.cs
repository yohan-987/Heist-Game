using System;

namespace Game.Items
{
    /// <summary>A simple battery measured in seconds of use. Not rechargeable in version 1.</summary>
    public sealed class BatteryModel
    {
        public float Capacity { get; private set; }
        public float Remaining { get; private set; }

        public bool IsEmpty => Remaining <= 0f;
        public float Fraction => Capacity <= 0f ? 0f : Remaining / Capacity;

        public BatteryModel(float capacitySeconds)
        {
            Capacity = Math.Max(0f, capacitySeconds);
            Remaining = Capacity;
        }

        /// <summary>Use up time. Returns how much was actually drained, which is less near the end.</summary>
        public float Drain(float seconds)
        {
            if (seconds <= 0f || IsEmpty) return 0f;
            float used = Math.Min(seconds, Remaining);
            Remaining -= used;
            return used;
        }

        public void Refill() => Remaining = Capacity;
    }
}
