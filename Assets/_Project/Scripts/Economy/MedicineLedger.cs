using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Economy
{
    /// <summary>
    /// The compounder's books for one run. Value is committed when a valuable is accepted and can never
    /// be taken back, so the doses it produced stay produced. Doses taken by players are subtracted, and
    /// what remains counts toward the team quota. Every dose a player takes is a dose the team does not bank.
    /// </summary>
    public sealed class MedicineLedger
    {
        public float ValuePerDose { get; private set; }
        public int QuotaDoses { get; private set; }

        /// <summary>Total value accepted by the compounder so far. Only ever grows.</summary>
        public float CommittedValue { get; private set; }

        public int Consumed { get; private set; }

        /// <summary>Used by <see cref="TryConsume()"/> when the caller does not say who took the dose.</summary>
        public const int UnknownPlayer = -1;

        private readonly Dictionary<int, int> _takenBy = new Dictionary<int, int>();

        /// <summary>Doses this player has taken. Shown on the HUD and the results screen so a drained pool is visible.</summary>
        public int TakenBy(int playerId)
        {
            int n;
            return _takenBy.TryGetValue(playerId, out n) ? n : 0;
        }

        /// <summary>Ids of every player who has taken at least one dose, in ascending order.</summary>
        public List<int> Takers()
        {
            var ids = new List<int>(_takenBy.Keys);
            ids.Sort();
            return ids;
        }

        /// <summary>Whole doses produced from everything committed.</summary>
        public int Produced => Compounder.DosesFor(CommittedValue, ValuePerDose);

        /// <summary>Doses not yet taken. Both the shared pool players draw from and the team's progress to quota.</summary>
        public int Banked => Math.Max(0, Produced - Consumed);

        /// <summary>Committed value that has not yet made a whole dose.</summary>
        public float Remainder => CommittedValue - Produced * ValuePerDose;

        /// <summary>0 to 1: how full the next dose is.</summary>
        public float RemainderFraction => ValuePerDose <= 0f ? 0f : Remainder / ValuePerDose;

        /// <summary>
        /// True from the first moment banked doses reached the quota. It never goes back to false, so taking
        /// doses afterwards can only reduce the bonus, not lock the team out.
        /// </summary>
        public bool ExitUnlocked { get; private set; }

        /// <summary>Banked doses beyond the quota once the exit is unlocked. Pays a bonus at the exit.</summary>
        public int SurplusDoses => ExitUnlocked ? Math.Max(0, Banked - QuotaDoses) : 0;

        public MedicineLedger(int quotaDoses, float valuePerDose)
        {
            Reset(quotaDoses, valuePerDose);
        }

        public void Reset(int quotaDoses, float valuePerDose)
        {
            QuotaDoses = Math.Max(0, quotaDoses);
            ValuePerDose = Math.Max(1f, valuePerDose);
            CommittedValue = 0f;
            Consumed = 0;
            _takenBy.Clear();
            ExitUnlocked = false;
        }

        /// <summary>Accept value permanently. Returns false for zero or negative amounts, which are ignored.</summary>
        public bool Commit(float value)
        {
            if (!(value > 0f)) return false;

            CommittedValue += value;
            if (!ExitUnlocked && QuotaDoses > 0 && Banked >= QuotaDoses) ExitUnlocked = true;
            return true;
        }

        /// <summary>Take one dose out of the pool. Returns false if none are available.</summary>
        public bool TryConsume() => TryConsume(UnknownPlayer);

        /// <summary>Take one dose out of the pool for this player and remember who took it.</summary>
        public bool TryConsume(int playerId)
        {
            if (Banked <= 0) return false;
            Consumed++;
            int n;
            _takenBy.TryGetValue(playerId, out n);
            _takenBy[playerId] = n + 1;
            return true;
        }
    }
}
