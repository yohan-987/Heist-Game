using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The only place gameplay reads time from. Wrapping UnityEngine.Time keeps pausing and (later)
    /// network time in one spot.
    /// </summary>
    public sealed class GameClock
    {
        public bool Paused { get; private set; }

        /// <summary>Extra multiplier on top of Unity's timeScale (dev console, slow-mo effects).</summary>
        public float Scale { get; set; } = 1f;

        public float DeltaTime => Paused ? 0f : Time.deltaTime * Scale;
        public float FixedDeltaTime => Paused ? 0f : Time.fixedDeltaTime * Scale;
        public float Now => Time.time;

        public void SetPaused(bool paused) => Paused = paused;
    }
}
