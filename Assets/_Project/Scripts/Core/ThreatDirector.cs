using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>How a hazard affects a player. Cosmetic hazards are never counted (GAME_DESIGN 4.9).</summary>
    public enum HazardClass
    {
        Lethal,
        HardControl,
        Disruptive,
        Cosmetic
    }

    public enum HazardCheck
    {
        Allowed,
        /// <summary>The player already has the maximum number of active hazards.</summary>
        CapReached,
        /// <summary>A doctor is chasing the player or is close, so nothing else piles on.</summary>
        PileOn
    }

    /// <summary>
    /// Keeps fear from turning into frustration. All non-cosmetic hazards go through one place:
    /// at most two are active on a player, and hard-control or disruptive hazards wait while a doctor
    /// is chasing that player or is within <see cref="PileOnRadius"/>. A chasing doctor always registers,
    /// because it is the others that wait. Pure logic; gameplay code asks before starting a hazard.
    /// </summary>
    public sealed class ThreatDirector
    {
        public const int MaxActiveHazards = 2;
        /// <summary>Warning every lethal or hard-control effect gives before it can land.</summary>
        public const float TelegraphSeconds = 1.5f;
        public const float PileOnRadius = 15f;

        private sealed class Entry
        {
            public int PlayerId;
            public int HazardId;
            public HazardClass Class;
            /// <summary>Seconds left, or a negative number for "until ended".</summary>
            public float Remaining;
        }

        private readonly List<Entry> _active = new List<Entry>();
        private readonly Dictionary<int, Vector3> _doctors = new Dictionary<int, Vector3>();

        public int ActiveCount(int playerId)
        {
            int n = 0;
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].PlayerId == playerId) n++;
            return n;
        }

        public bool IsActive(int playerId, int hazardId)
        {
            return Find(playerId, hazardId) != null;
        }

        private bool HasLethal(int playerId)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].PlayerId == playerId && _active[i].Class == HazardClass.Lethal) return true;
            return false;
        }

        private bool DoctorNear(Vector3 playerPosition)
        {
            foreach (var kv in _doctors)
                if (Vector3.Distance(kv.Value, playerPosition) <= PileOnRadius) return true;
            return false;
        }

        /// <summary>Would this hazard be allowed on this player right now?</summary>
        public HazardCheck Check(int playerId, HazardClass hazardClass, Vector3 playerPosition)
        {
            if (hazardClass == HazardClass.Cosmetic || hazardClass == HazardClass.Lethal) return HazardCheck.Allowed;

            if (HasLethal(playerId) || DoctorNear(playerPosition)) return HazardCheck.PileOn;
            if (ActiveCount(playerId) >= MaxActiveHazards) return HazardCheck.CapReached;
            return HazardCheck.Allowed;
        }

        /// <summary>
        /// Registers a hazard if it is allowed. <paramref name="duration"/> is how long it stays active
        /// without being ended; use a negative number for "until <see cref="End"/>".
        /// Cosmetic hazards are allowed but never stored.
        /// </summary>
        public bool TryBegin(int playerId, int hazardId, HazardClass hazardClass, Vector3 playerPosition, float duration)
        {
            if (hazardClass == HazardClass.Cosmetic) return true;
            if (Find(playerId, hazardId) != null) return true; // already counted

            if (Check(playerId, hazardClass, playerPosition) != HazardCheck.Allowed) return false;

            _active.Add(new Entry { PlayerId = playerId, HazardId = hazardId, Class = hazardClass, Remaining = duration });
            return true;
        }

        public void End(int playerId, int hazardId)
        {
            var e = Find(playerId, hazardId);
            if (e != null) _active.Remove(e);
        }

        public void EndAll(int playerId)
        {
            _active.RemoveAll(e => e.PlayerId == playerId);
        }

        /// <summary>Expires timed hazards.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var e = _active[i];
                if (e.Remaining < 0f) continue;
                e.Remaining -= dt;
                if (e.Remaining <= 0f) _active.RemoveAt(i);
            }
        }

        /// <summary>A doctor (patrolling or chasing) is here. Used for the "within 15 metres" rule.</summary>
        public void SetDoctor(int enemyId, Vector3 position) => _doctors[enemyId] = position;

        public void RemoveDoctor(int enemyId) => _doctors.Remove(enemyId);

        public void Reset()
        {
            _active.Clear();
            _doctors.Clear();
        }

        private Entry Find(int playerId, int hazardId)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].PlayerId == playerId && _active[i].HazardId == hazardId) return _active[i];
            return null;
        }
    }
}
