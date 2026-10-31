using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Everything that needs "the player" asks here. Offline there is exactly one entry;
    /// online there is one per connected player.
    /// </summary>
    public sealed class PlayerRegistry
    {
        private readonly List<IPlayerAvatar> _players = new List<IPlayerAvatar>();

        public IReadOnlyList<IPlayerAvatar> All => _players;
        public int Count => _players.Count;

        public IPlayerAvatar Local
        {
            get
            {
                for (int i = 0; i < _players.Count; i++)
                    if (_players[i].IsLocal) return _players[i];
                return null;
            }
        }

        public void Register(IPlayerAvatar player)
        {
            if (player != null && !_players.Contains(player)) _players.Add(player);
        }

        public void Unregister(IPlayerAvatar player) => _players.Remove(player);

        public bool AnyAlive()
        {
            for (int i = 0; i < _players.Count; i++)
                if (_players[i].IsAlive) return true;
            return false;
        }

        /// <summary>Closest living player to a point, or null.</summary>
        public IPlayerAvatar Nearest(Vector3 position, out float distance)
        {
            IPlayerAvatar best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _players.Count; i++)
            {
                var p = _players[i];
                if (!p.IsAlive) continue;
                float sqr = (p.Transform.position - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = p; }
            }

            distance = best == null ? float.PositiveInfinity : Mathf.Sqrt(bestSqr);
            return best;
        }

        public void Clear() => _players.Clear();
    }
}
