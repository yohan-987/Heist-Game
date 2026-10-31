using UnityEngine;

namespace Game.Core
{
    /// <summary>Pure noise maths, kept separate from MonoBehaviours so it can be unit tested.</summary>
    public static class NoiseMath
    {
        /// <summary>Loudness for a physical impact. Quiet taps return 0.</summary>
        public static float ImpactLoudness(float impulse, float minImpulse, float loudnessPerImpulse, float maxLoudness)
        {
            if (impulse <= minImpulse) return 0f;
            return Mathf.Min((impulse - minImpulse) * loudnessPerImpulse, maxLoudness);
        }

        /// <summary>
        /// Distance-attenuated volume as a listener perceives it: full at the source, zero at
        /// <paramref name="loudness"/> metres (scaled by listener sensitivity).
        /// </summary>
        public static float Perceived(float loudness, float distance, float sensitivity = 1f)
        {
            float range = loudness * sensitivity;
            if (range <= 0f || distance >= range) return 0f;
            return 1f - distance / range;
        }

        public static bool CanHear(float loudness, float distance, float sensitivity = 1f)
        {
            return Perceived(loudness, distance, sensitivity) > 0f;
        }
    }
}
