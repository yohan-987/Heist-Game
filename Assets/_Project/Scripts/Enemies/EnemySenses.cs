using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Pure perception and movement maths for enemies.</summary>
    public static class EnemySenses
    {
        /// <summary>True if <paramref name="target"/> is inside the view cone (ignores walls).</summary>
        public static bool InVisionCone(Vector3 origin, Vector3 forward, Vector3 target, float range, float halfAngleDegrees)
        {
            Vector3 to = target - origin;
            float sqr = to.sqrMagnitude;
            if (sqr > range * range) return false;
            if (sqr < 0.0001f) return true;

            float angle = Vector3.Angle(forward, to);
            return angle <= halfAngleDegrees;
        }

        /// <summary>
        /// Whether a noise is worth reacting to: audible after distance falloff and
        /// above the enemy's own threshold.
        /// </summary>
        public static bool ShouldReactToNoise(float perceived, float threshold)
        {
            return perceived > 0f && perceived >= threshold;
        }

        /// <summary>
        /// A point <paramref name="distance"/> metres from <paramref name="self"/>, directly away from
        /// <paramref name="threat"/> on the ground plane. Falls back to a fixed direction if they overlap.
        /// </summary>
        public static Vector3 AwayPoint(Vector3 self, Vector3 threat, float distance)
        {
            Vector3 away = self - threat;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = new Vector3(1f, 0f, 0f);
            return self + away.normalized * distance;
        }
    }
}
