using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Small seedable RNG (xorshift128 seeded via splitmix32). Same seed gives the same sequence on
    /// every platform and runtime, which System.Random and UnityEngine.Random do not guarantee.
    /// Required for level generation to match between host and clients.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _s0, _s1, _s2, _s3;

        public DeterministicRandom(int seed)
        {
            uint z = unchecked((uint)seed);
            _s0 = SplitMix(ref z);
            _s1 = SplitMix(ref z);
            _s2 = SplitMix(ref z);
            _s3 = SplitMix(ref z);
            if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 1; // all-zero state would lock the generator
        }

        private static uint SplitMix(ref uint state)
        {
            unchecked
            {
                state += 0x9E3779B9u;
                uint z = state;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }

        public uint NextUInt()
        {
            unchecked
            {
                // Marsaglia xorshift128.
                uint t = _s0 ^ (_s0 << 11);
                _s0 = _s1;
                _s1 = _s2;
                _s2 = _s3;
                _s3 = _s3 ^ (_s3 >> 19) ^ t ^ (t >> 8);
                return _s3;
            }
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public bool Chance(float probability) => NextFloat() < probability;

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("Cannot pick from an empty list.");
            return items[Range(0, items.Count)];
        }

        /// <summary>Fisher-Yates in place.</summary>
        public void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
