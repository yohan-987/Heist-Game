using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A set of named slowdowns (or boosts) multiplied together. Each source sets or removes its own
    /// entry, so several things can slow the player at once without knowing about each other.
    /// </summary>
    public sealed class SpeedModifiers
    {
        private readonly Dictionary<object, float> _entries = new Dictionary<object, float>();

        /// <summary>The combined multiplier never drops below this, so a player can never be frozen.</summary>
        public float Floor { get; }

        public SpeedModifiers(float floor = 0.1f) => Floor = Mathf.Max(0.01f, floor);

        public int Count => _entries.Count;

        public float Multiplier
        {
            get
            {
                float result = 1f;
                foreach (var kv in _entries) result *= kv.Value;
                return Mathf.Max(Floor, result);
            }
        }

        /// <summary>Adds or replaces the entry owned by <paramref name="owner"/>.</summary>
        public void Set(object owner, float multiplier)
        {
            if (owner == null) return;
            _entries[owner] = Mathf.Max(0f, multiplier);
        }

        public bool Remove(object owner) => owner != null && _entries.Remove(owner);

        public void Clear() => _entries.Clear();
    }
}
