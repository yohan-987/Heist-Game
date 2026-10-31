using System;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// How strongly an enemy reacts to each kind of noise, as a multiplier on its hearing range.
    /// 0 means it ignores that kind entirely. Doctors ignore music by default.
    /// </summary>
    [Serializable]
    public sealed class SourceHearing
    {
        [Min(0f)] public float Footstep = 1f;
        [Min(0f)] public float Sprint = 1f;
        [Min(0f)] public float Impact = 1f;
        [Min(0f)] public float Drop = 1f;
        [Min(0f)] public float Voice = 1f;
        [Min(0f)] public float Other = 1f;
        [Min(0f)] public float Music = 0f;

        public float Get(NoiseSource source)
        {
            switch (source)
            {
                case NoiseSource.Footstep: return Footstep;
                case NoiseSource.Sprint: return Sprint;
                case NoiseSource.Impact: return Impact;
                case NoiseSource.Drop: return Drop;
                case NoiseSource.Voice: return Voice;
                case NoiseSource.Music: return Music;
                default: return Other;
            }
        }
    }
}
