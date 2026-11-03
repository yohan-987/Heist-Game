using System.Collections.Generic;

namespace Game.Player
{
    /// <summary>
    /// Combines the music a player can currently hear into one relief rate. Each source reports a rate and
    /// how long the report stays valid; only the strongest valid source counts, so radios never stack.
    /// </summary>
    public sealed class MusicReliefMixer
    {
        private struct Report
        {
            public float Rate;
            public float Expires;
        }

        private readonly Dictionary<int, Report> _sources = new Dictionary<int, Report>();
        private readonly List<int> _expired = new List<int>();

        public void Add(int sourceId, float ratePerSecond, float expiresAt)
        {
            _sources[sourceId] = new Report { Rate = ratePerSecond, Expires = expiresAt };
        }

        /// <summary>Strongest relief per second among sources that are still valid at <paramref name="now"/>.</summary>
        public float CurrentRate(float now)
        {
            float best = 0f;
            _expired.Clear();

            foreach (var pair in _sources)
            {
                if (pair.Value.Expires <= now) { _expired.Add(pair.Key); continue; }
                if (pair.Value.Rate > best) best = pair.Value.Rate;
            }

            for (int i = 0; i < _expired.Count; i++) _sources.Remove(_expired[i]);
            return best;
        }

        public int ActiveSources => _sources.Count;

        public void Clear() => _sources.Clear();
    }
}
