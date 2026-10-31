using UnityEngine;

namespace Game.Enemies
{
    public enum GrabberState
    {
        /// <summary>Lying still on the floor.</summary>
        Dormant,
        /// <summary>Music woke it; it is getting up.</summary>
        Waking,
        /// <summary>Crawling toward the nearest player.</summary>
        Crawling,
        /// <summary>Holding a player by the waist.</summary>
        Holding,
        /// <summary>Released or driven off; resting before it can go dormant again.</summary>
        Cooldown
    }

    /// <summary>
    /// Pure state rules for the ground doctor: Dormant, Waking, Crawling, Holding, Cooldown.
    /// A held player escapes by struggling; an apple (or anything else) can also break the hold.
    /// </summary>
    public sealed class DormantGrabberModel
    {
        public float WakeSeconds { get; }
        public float StruggleDecayPerSecond { get; }
        public float CooldownSeconds { get; }

        public GrabberState State { get; private set; } = GrabberState.Dormant;

        /// <summary>0 to 1. Reaching 1 frees the player.</summary>
        public float Progress { get; private set; }

        private float _timer;

        public DormantGrabberModel(float wakeSeconds = 1.5f, float struggleDecayPerSecond = 0.15f, float cooldownSeconds = 15f)
        {
            WakeSeconds = Mathf.Max(0f, wakeSeconds);
            StruggleDecayPerSecond = Mathf.Max(0f, struggleDecayPerSecond);
            CooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        }

        public bool IsHolding => State == GrabberState.Holding;

        /// <summary>Music reached it. Only a dormant grabber wakes.</summary>
        public bool Wake()
        {
            if (State != GrabberState.Dormant) return false;
            State = GrabberState.Waking;
            _timer = 0f;
            return true;
        }

        /// <summary>It reached a player. Only a crawling grabber can grab.</summary>
        public bool Grab()
        {
            if (State != GrabberState.Crawling) return false;
            State = GrabberState.Holding;
            Progress = 0f;
            return true;
        }

        /// <summary>The held player struggles (a button press). Frees them when progress fills.</summary>
        public void Struggle(float amount)
        {
            if (State != GrabberState.Holding || amount <= 0f) return;
            Progress = Mathf.Min(1f, Progress + amount);
            if (Progress >= 1f) EnterCooldown();
        }

        /// <summary>Something drove it off (apple, teammate). Works from any active state.</summary>
        public bool Break()
        {
            if (State == GrabberState.Dormant || State == GrabberState.Cooldown) return false;
            EnterCooldown();
            return true;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            switch (State)
            {
                case GrabberState.Waking:
                    _timer += dt;
                    if (_timer >= WakeSeconds)
                    {
                        State = GrabberState.Crawling;
                        _timer = 0f;
                    }
                    break;

                case GrabberState.Holding:
                    Progress = Mathf.Max(0f, Progress - StruggleDecayPerSecond * dt);
                    break;

                case GrabberState.Cooldown:
                    _timer += dt;
                    if (_timer >= CooldownSeconds)
                    {
                        State = GrabberState.Dormant;
                        _timer = 0f;
                    }
                    break;
            }
        }

        private void EnterCooldown()
        {
            State = GrabberState.Cooldown;
            Progress = 0f;
            _timer = 0f;
        }
    }
}
