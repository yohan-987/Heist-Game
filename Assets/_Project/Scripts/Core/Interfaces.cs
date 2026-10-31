using UnityEngine;

namespace Game.Core
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly object Instigator;

        public DamageInfo(float amount, Vector3 point, Vector3 direction, object instigator = null)
        {
            Amount = amount; Point = point; Direction = direction; Instigator = instigator;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(in DamageInfo info);
    }

    /// <summary>Implemented by anything that reacts to noise (enemies, later: traps, alarms).</summary>
    public interface INoiseListener
    {
        void OnNoiseHeard(in NoiseEmitted noise);
    }

    /// <summary>
    /// Implemented by whatever lets a player carry things. The motor reads it to slow the player
    /// down and drain stamina without referencing the Interaction assembly.
    /// </summary>
    public interface ICarrySource
    {
        /// <summary>Mass of the held object, 0 when nothing is held.</summary>
        float HeldMass { get; }

        /// <summary>1 = no slowdown; smaller values slow the player.</summary>
        float SpeedMultiplier { get; }
    }

    /// <summary>Anything a player can be: local or, later, remote. Lets systems avoid FindObjectOfType.</summary>
    public interface IPlayerAvatar
    {
        int PlayerId { get; }
        Transform Transform { get; }
        Transform Head { get; }
        bool IsAlive { get; }
        bool IsLocal { get; }
    }
}
