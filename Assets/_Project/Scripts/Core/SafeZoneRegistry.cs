using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>An area doctors never enter and where players recover health.</summary>
    public interface ISafeZone
    {
        bool Contains(Vector3 point);
    }

    /// <summary>All safe zones in the current level (the start wing and the pharmacy wing, GAME_DESIGN 4.4).</summary>
    public sealed class SafeZoneRegistry
    {
        private readonly List<ISafeZone> _zones = new List<ISafeZone>();

        public int Count => _zones.Count;

        public void Register(ISafeZone zone)
        {
            if (zone != null && !_zones.Contains(zone)) _zones.Add(zone);
        }

        public void Unregister(ISafeZone zone) => _zones.Remove(zone);

        public bool IsSafe(Vector3 point)
        {
            for (int i = 0; i < _zones.Count; i++)
                if (_zones[i].Contains(point)) return true;
            return false;
        }

        public void Clear() => _zones.Clear();
    }
}
