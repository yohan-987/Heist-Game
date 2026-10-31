using UnityEngine;

namespace Game.Interaction
{
    /// <summary>Pure grab-and-carry maths, separated so it can be unit tested without physics.</summary>
    public static class GrabMath
    {
        /// <summary>
        /// Velocity to apply so a held body homes in on the hold point: proportional to the gap,
        /// clamped so a body stuck behind a wall can't be flung through it.
        /// </summary>
        public static Vector3 HoldVelocity(Vector3 bodyPosition, Vector3 targetPosition, float gain, float maxSpeed)
        {
            Vector3 v = (targetPosition - bodyPosition) * gain;
            return Vector3.ClampMagnitude(v, maxSpeed);
        }

        /// <summary>1 for light objects, falling linearly to <paramref name="minMultiplier"/> at heavyMass and above.</summary>
        public static float MassSpeedMultiplier(float mass, float lightMass, float heavyMass, float minMultiplier)
        {
            if (heavyMass <= lightMass) return 1f;
            float t = Mathf.InverseLerp(lightMass, heavyMass, mass);
            return Mathf.Lerp(1f, Mathf.Clamp01(minMultiplier), t);
        }

        public static float AdjustHoldDistance(float current, float scroll, float step, float min, float max)
        {
            return Mathf.Clamp(current + scroll * step, min, max);
        }

        public static bool ShouldAutoRelease(float distanceToHoldPoint, float maxDistance)
        {
            return distanceToHoldPoint > maxDistance;
        }

        /// <summary>Heavier objects leave the hand slower; always capped.</summary>
        public static float ThrowSpeed(float mass, float baseSpeed, float maxSpeed)
        {
            float speed = baseSpeed / Mathf.Sqrt(Mathf.Max(1f, mass));
            return Mathf.Min(speed, maxSpeed);
        }
    }
}
