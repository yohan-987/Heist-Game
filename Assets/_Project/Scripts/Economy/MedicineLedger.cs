using System;
using Game.Core;

namespace Game.Economy
{
    /// <summary>
    /// The compounder's books for one run. Delivered value becomes doses; doses taken by players are
    /// subtracted, and what remains counts toward the team quota. Every dose a player takes is a dose
    /// the team does not bank.
    /// </summary>
    public sealed class MedicineLedger
    {
        public float ValuePerDose { get; private set; }
        public int QuotaDoses { get; private set; }

        /// <summary>Total value currently inside the medicine counter.</summary>
        public float DeliveredValue { get; private set; }

        public int Consumed { get; private set; }

        /// <summary>Doses the delivered value produces right now (falls if delivered value is damaged).</summary>
        public int Produced => Compounder.DosesFor(DeliveredValue, ValuePerDose);

        /// <summary>Doses not yet taken. Both the shared pool players draw from and the team's progress to quota.</summary>
        public int Banked => Math.Max(0, Produced - Consumed);

        public bool QuotaMet => QuotaDoses > 0 && Banked >= QuotaDoses;

        public MedicineLedger(int quotaDoses, float valuePerDose)
        {
            Reset(quotaDoses, valuePerDose);
        }

        public void Reset(int quotaDoses, float valuePerDose)
        {
            QuotaDoses = Math.Max(0, quotaDoses);
            ValuePerDose = Math.Max(1f, valuePerDose);
            DeliveredValue = 0f;
            Consumed = 0;
        }

        /// <summary>Replaces (does not add to) the value currently delivered.</summary>
        public void SetDeliveredValue(float value) => DeliveredValue = Math.Max(0f, value);

        /// <summary>Take one dose out of the pool. Returns false if none are available.</summary>
        public bool TryConsume()
        {
            if (Banked <= 0) return false;
            Consumed++;
            return true;
        }
    }
}
