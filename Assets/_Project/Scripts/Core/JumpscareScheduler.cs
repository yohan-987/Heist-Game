using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Decides when scares are allowed. It enforces a minimum gap between any two scares, lets each
    /// scripted scare fire once per run, and rolls random hallucinations whose chance rises with insanity.
    /// Pure logic: a director component feeds it time and insanity and reacts to what it allows.
    /// </summary>
    public sealed class JumpscareScheduler
    {
        private readonly DeterministicRandom _rng;
        private readonly HashSet<int> _used = new HashSet<int>();

        public float MinGapSeconds { get; }
        public float HallucinationStartInsanity { get; }
        public int HallucinationKinds { get; }
        /// <summary>Hallucination chance per second at the lowest and highest eligible insanity.</summary>
        public float ChancePerSecondLow { get; }
        public float ChancePerSecondHigh { get; }

        /// <summary>Seconds since the last scare. Starts "ready" so the first scare is not delayed.</summary>
        public float TimeSinceLast { get; private set; }

        public JumpscareScheduler(
            DeterministicRandom rng,
            int hallucinationKinds = 4,
            float minGapSeconds = 45f,
            float hallucinationStartInsanity = 50f,
            float chancePerSecondLow = 0.02f,
            float chancePerSecondHigh = 0.15f)
        {
            _rng = rng ?? new DeterministicRandom(0);
            HallucinationKinds = Mathf.Max(1, hallucinationKinds);
            MinGapSeconds = Mathf.Max(0f, minGapSeconds);
            HallucinationStartInsanity = hallucinationStartInsanity;
            ChancePerSecondLow = chancePerSecondLow;
            ChancePerSecondHigh = chancePerSecondHigh;
            TimeSinceLast = MinGapSeconds;
        }

        public bool GapElapsed => TimeSinceLast >= MinGapSeconds;

        public bool HasFired(int scareId) => _used.Contains(scareId);

        /// <summary>How many scripted scares have fired this run.</summary>
        public int ScriptedFired => _used.Count;

        /// <summary>A scripted scare was triggered (player walked into it). Allowed once per run, and only after the gap.</summary>
        public bool TryTriggerScripted(int scareId)
        {
            bool costsPressure;
            return TryTriggerScripted(scareId, out costsPressure);
        }

        /// <summary>
        /// Same, and says whether this scare costs Pressure. The first scripted scare of a run is the
        /// introduction and is purely cosmetic (GAME_DESIGN 4.8); every later one costs Pressure.
        /// </summary>
        public bool TryTriggerScripted(int scareId, out bool costsPressure)
        {
            costsPressure = false;
            if (_used.Contains(scareId) || !GapElapsed) return false;
            costsPressure = _used.Count > 0;
            _used.Add(scareId);
            TimeSinceLast = 0f;
            return true;
        }

        /// <summary>
        /// Advances time and maybe rolls a hallucination. Call once per frame; do not call both this and
        /// anything else that advances time.
        /// </summary>
        public bool TryHallucination(float dt, float insanity, out int hallucinationId)
        {
            hallucinationId = -1;
            if (dt > 0f) TimeSinceLast += dt;

            if (dt <= 0f || insanity < HallucinationStartInsanity || !GapElapsed) return false;

            float t = Mathf.InverseLerp(HallucinationStartInsanity, 100f, insanity);
            float chancePerSecond = Mathf.Lerp(ChancePerSecondLow, ChancePerSecondHigh, t);
            if (_rng.NextFloat() >= chancePerSecond * dt) return false;

            hallucinationId = _rng.Range(0, HallucinationKinds);
            TimeSinceLast = 0f;
            return true;
        }

        /// <summary>New run: forget which scripted scares fired.</summary>
        public void Reset()
        {
            _used.Clear();
            TimeSinceLast = MinGapSeconds;
        }
    }
}
