using UnityEngine;

namespace Game.Items
{
    /// <summary>The one formula that turns an impact into lost money. Pure, so it's easy to test and tune.</summary>
    public static class ValueLoss
    {
        /// <summary>
        /// Loss = BaseValue * (impulse - threshold) * lossPerImpulse, never more than what's left.
        /// Impulses at or below the threshold cost nothing.
        /// </summary>
        public static float Compute(float impulse, float threshold, float lossPerImpulse, float baseValue, float currentValue)
        {
            if (impulse <= threshold || currentValue <= 0f) return 0f;
            float loss = baseValue * (impulse - threshold) * lossPerImpulse;
            return Mathf.Clamp(loss, 0f, currentValue);
        }
    }
}
