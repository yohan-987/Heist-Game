namespace Game.Player
{
    public enum DoseCheck
    {
        Allowed,
        TooCalm,
        Cooldown
    }

    /// <summary>
    /// Rules for when a player may draw a dose from the shared pool. Stops wasted doses and one player
    /// draining the pool. A swelling player can always take one: it is the last chance.
    /// </summary>
    public sealed class DoseGate
    {
        public float MinPressure { get; set; }
        public float CooldownSeconds { get; set; }

        public DoseGate(float minPressure = 20f, float cooldownSeconds = 10f)
        {
            MinPressure = minPressure;
            CooldownSeconds = cooldownSeconds;
        }

        public DoseCheck Check(float pressure, bool swelling, float secondsSinceLastDose)
        {
            if (swelling) return DoseCheck.Allowed;
            if (pressure < MinPressure) return DoseCheck.TooCalm;
            if (secondsSinceLastDose < CooldownSeconds) return DoseCheck.Cooldown;
            return DoseCheck.Allowed;
        }
    }
}
