using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Spiders crawling over the player's screen. Pure rules: a hard cap on how much of the screen they
    /// may cover, a maximum lifetime, and swatting. Positions are normalised screen coordinates (0 to 1).
    /// </summary>
    public sealed class CrawlerSwarmModel
    {
        public sealed class Crawler
        {
            public float X;
            public float Y;
            /// <summary>Fraction of the screen area this crawler covers.</summary>
            public float Coverage;
            public float Age;
        }

        private readonly List<Crawler> _crawlers = new List<Crawler>();

        public float MaxCoverage { get; }
        public float MaxLifetimeSeconds { get; }
        public float CoveragePerCrawler { get; }

        public CrawlerSwarmModel(float maxCoverage = 0.4f, float maxLifetimeSeconds = 8f, float coveragePerCrawler = 0.08f)
        {
            MaxCoverage = Mathf.Clamp01(maxCoverage);
            MaxLifetimeSeconds = Mathf.Max(0.1f, maxLifetimeSeconds);
            CoveragePerCrawler = Mathf.Max(0.001f, coveragePerCrawler);
        }

        public IReadOnlyList<Crawler> Crawlers => _crawlers;
        public int Count => _crawlers.Count;

        public float Coverage
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _crawlers.Count; i++) total += _crawlers[i].Coverage;
                return total;
            }
        }

        /// <summary>Adds up to <paramref name="count"/> crawlers without exceeding the coverage cap. Returns how many were added.</summary>
        public int Add(int count, DeterministicRandom rng)
        {
            int added = 0;
            for (int i = 0; i < count; i++)
            {
                if (Coverage + CoveragePerCrawler > MaxCoverage + 0.0001f) break;

                _crawlers.Add(new Crawler
                {
                    X = rng.Range(0.1f, 0.9f),
                    Y = rng.Range(0.1f, 0.9f),
                    Coverage = CoveragePerCrawler,
                    Age = 0f
                });
                added++;
            }
            return added;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            for (int i = _crawlers.Count - 1; i >= 0; i--)
            {
                _crawlers[i].Age += dt;
                if (_crawlers[i].Age >= MaxLifetimeSeconds) _crawlers.RemoveAt(i);
            }
        }

        /// <summary>Swat the oldest crawler. Returns false if there were none.</summary>
        public bool Swat()
        {
            if (_crawlers.Count == 0) return false;
            _crawlers.RemoveAt(0);
            return true;
        }

        public void Clear() => _crawlers.Clear();
    }
}
