using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Pure rules for being hit by a doctor (GAME_DESIGN 4.4). A hit lands in full, then the player is
    /// protected for a short time so one mistake is a scare and two are death. Health only regenerates
    /// inside a safe room. The Pressure burst never goes through this: it kills outright.
    /// </summary>
    public sealed class HitModel
    {
        /// <summary>Every attack must wind up for at least this long so the player can read it.</summary>
        public const float MinWindupSeconds = 0.75f;

        public float ProtectionSeconds { get; }
        public float SafeRoomRegenPerSecond { get; }
        public float ProtectionRemaining { get; private set; }

        public bool IsProtected => ProtectionRemaining > 0f;

        public HitModel(float protectionSeconds = 2f, float safeRoomRegenPerSecond = 5f)
        {
            ProtectionSeconds = Mathf.Max(0f, protectionSeconds);
            SafeRoomRegenPerSecond = Mathf.Max(0f, safeRoomRegenPerSecond);
        }

        /// <summary>
        /// Asks how much of an incoming hit lands. Returns 0 while protected. A landed hit starts the
        /// protection window.
        /// </summary>
        public float Absorb(float amount)
        {
            if (!(amount > 0f) || IsProtected) return 0f;
            ProtectionRemaining = ProtectionSeconds;
            return amount;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || ProtectionRemaining <= 0f) return;
            ProtectionRemaining = Mathf.Max(0f, ProtectionRemaining - dt);
        }

        /// <summary>Health to restore this frame. Zero outside safe rooms.</summary>
        public float Regen(float dt, bool inSafeRoom)
        {
            return inSafeRoom && dt > 0f ? SafeRoomRegenPerSecond * dt : 0f;
        }

        public void Reset() => ProtectionRemaining = 0f;

        /// <summary>The wind-up an enemy must actually use: never shorter than the minimum.</summary>
        public static float Windup(float configuredSeconds) => Mathf.Max(MinWindupSeconds, configuredSeconds);
    }
}
