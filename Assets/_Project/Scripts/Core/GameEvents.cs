using UnityEngine;

namespace Game.Core
{
    // Event payloads. Plain structs, no references to other systems.

    public readonly struct ValuableDamaged
    {
        public readonly EntityId Id;
        public readonly float Lost;
        public readonly float Remaining;
        public readonly Vector3 Position;

        public ValuableDamaged(EntityId id, float lost, float remaining, Vector3 position)
        {
            Id = id; Lost = lost; Remaining = remaining; Position = position;
        }
    }

    public readonly struct ValuableDelivered
    {
        public readonly EntityId Id;
        public readonly float Value;

        public ValuableDelivered(EntityId id, float value)
        {
            Id = id; Value = value;
        }
    }

    public readonly struct ObjectGrabbed
    {
        public readonly EntityId Id;
        public readonly int PlayerId;

        public ObjectGrabbed(EntityId id, int playerId)
        {
            Id = id; PlayerId = playerId;
        }
    }

    public readonly struct ObjectReleased
    {
        public readonly EntityId Id;
        public readonly int PlayerId;

        public ObjectReleased(EntityId id, int playerId)
        {
            Id = id; PlayerId = playerId;
        }
    }

    public readonly struct ExtractionValueChanged
    {
        /// <summary>Total current value of every valuable fully inside an extraction zone.</summary>
        public readonly float Total;
        public readonly int Count;

        public ExtractionValueChanged(float total, int count)
        {
            Total = total; Count = count;
        }
    }

    public readonly struct NoiseEmitted
    {
        public readonly Vector3 Position;
        /// <summary>Roughly "how many metres away could a normal listener hear this".</summary>
        public readonly float Loudness;
        public readonly NoiseSource Source;

        public NoiseEmitted(Vector3 position, float loudness, NoiseSource source)
        {
            Position = position; Loudness = loudness; Source = source;
        }
    }

    public readonly struct PlayerDied
    {
        public readonly int PlayerId;
        public PlayerDied(int playerId) { PlayerId = playerId; }
    }

    public readonly struct RunProgressChanged
    {
        public readonly float Delivered;
        public readonly float Quota;
        public readonly float Lost;

        public RunProgressChanged(float delivered, float quota, float lost)
        {
            Delivered = delivered; Quota = quota; Lost = lost;
        }
    }

    public readonly struct LevelCompleted
    {
        public readonly bool Success;
        public readonly float Delivered;
        public readonly float Quota;

        public LevelCompleted(bool success, float delivered, float quota)
        {
            Success = success; Delivered = delivered; Quota = quota;
        }
    }

    public readonly struct GameStateChanged
    {
        public readonly GameState Previous;
        public readonly GameState Current;

        public GameStateChanged(GameState previous, GameState current)
        {
            Previous = previous; Current = current;
        }
    }

    public enum NoiseSource
    {
        Footstep,
        Sprint,
        Impact,
        Drop,
        Voice,
        Other
    }
}
