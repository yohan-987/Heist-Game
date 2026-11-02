using UnityEngine;

namespace Game.Core
{
    // Event payloads. Plain structs, no references to other systems.

    // ---------- Items and loot ----------

    public readonly struct ValuableDamaged
    {
        public readonly GameEntityId Id;
        public readonly float Lost;
        public readonly float Remaining;
        public readonly Vector3 Position;

        public ValuableDamaged(GameEntityId id, float lost, float remaining, Vector3 position)
        {
            Id = id; Lost = lost; Remaining = remaining; Position = position;
        }
    }

    public readonly struct ValuableDelivered
    {
        public readonly GameEntityId Id;
        public readonly float Value;

        public ValuableDelivered(GameEntityId id, float value)
        {
            Id = id; Value = value;
        }
    }

    public readonly struct ObjectGrabbed
    {
        public readonly GameEntityId Id;
        public readonly int PlayerId;

        public ObjectGrabbed(GameEntityId id, int playerId)
        {
            Id = id; PlayerId = playerId;
        }
    }

    public readonly struct ObjectReleased
    {
        public readonly GameEntityId Id;
        public readonly int PlayerId;

        public ObjectReleased(GameEntityId id, int playerId)
        {
            Id = id; PlayerId = playerId;
        }
    }

    /// <summary>Published when a held object is thrown (as opposed to dropped).</summary>
    public readonly struct ObjectThrown
    {
        public readonly GameEntityId Id;
        public readonly int PlayerId;

        public ObjectThrown(GameEntityId id, int playerId)
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

    // ---------- Noise and scares ----------

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

    /// <summary>A radio (or similar) is playing. Players inside the radius are calmed.</summary>
    public readonly struct MusicPlaying
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        /// <summary>Insanity relief for this tick at point-blank range; falls off with distance.</summary>
        public readonly float Relief;

        public MusicPlaying(Vector3 position, float radius, float relief)
        {
            Position = position; Radius = radius; Relief = relief;
        }
    }

    /// <summary>Something scared doctors off (a thrown apple). Doctors inside the radius flee.</summary>
    public readonly struct RepelBurst
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly float Duration;

        public RepelBurst(Vector3 position, float radius, float duration)
        {
            Position = position; Radius = radius; Duration = duration;
        }
    }

    public readonly struct PlayerGrabbedByEnemy
    {
        public readonly int PlayerId;
        public readonly GameEntityId EnemyId;

        public PlayerGrabbedByEnemy(int playerId, GameEntityId enemyId)
        {
            PlayerId = playerId; EnemyId = enemyId;
        }
    }

    public readonly struct PlayerReleasedByEnemy
    {
        public readonly int PlayerId;
        public readonly GameEntityId EnemyId;

        public PlayerReleasedByEnemy(int playerId, GameEntityId enemyId)
        {
            PlayerId = playerId; EnemyId = enemyId;
        }
    }

    /// <summary>Spiders crawl onto this player's screen.</summary>
    public readonly struct SpiderAttack
    {
        public readonly int PlayerId;
        public readonly int Count;

        public SpiderAttack(int playerId, int count)
        {
            PlayerId = playerId; Count = count;
        }
    }

    /// <summary>Audio and visual systems react to this; the director only decides when.</summary>
    public readonly struct Jumpscare
    {
        public readonly int ScareId;
        public readonly ScareKind Kind;

        public Jumpscare(int scareId, ScareKind kind)
        {
            ScareId = scareId; Kind = kind;
        }
    }

    // ---------- Player ----------

    public readonly struct PlayerDied
    {
        public readonly int PlayerId;
        public PlayerDied(int playerId) { PlayerId = playerId; }
    }

    /// <summary>The player ran out of sanity and burst. Play the balloon effect.</summary>
    public readonly struct PlayerBurst
    {
        public readonly int PlayerId;
        public PlayerBurst(int playerId) { PlayerId = playerId; }
    }

    public readonly struct InsanityChanged
    {
        public readonly int PlayerId;
        public readonly float Value;
        public readonly InsanityStage Stage;
        /// <summary>0 to 1 while the player is swelling toward bursting, otherwise 0.</summary>
        public readonly float SwellProgress;

        public InsanityChanged(int playerId, float value, InsanityStage stage, float swellProgress)
        {
            PlayerId = playerId; Value = value; Stage = stage; SwellProgress = swellProgress;
        }
    }

    /// <summary>Something pushed a player's insanity up (e.g. throwing an apple).</summary>
    public readonly struct InsanityAdded
    {
        public readonly int PlayerId;
        public readonly float Amount;

        public InsanityAdded(int playerId, float amount)
        {
            PlayerId = playerId; Amount = amount;
        }
    }

    // ---------- Medicine ----------

    /// <summary>A player wants to take a dose. The run tracker decides.</summary>
    public readonly struct DoseRequested
    {
        public readonly int PlayerId;
        public DoseRequested(int playerId) { PlayerId = playerId; }
    }

    public readonly struct DoseGranted
    {
        public readonly int PlayerId;
        public DoseGranted(int playerId) { PlayerId = playerId; }
    }

    public readonly struct DoseDenied
    {
        public readonly int PlayerId;
        public DoseDenied(int playerId) { PlayerId = playerId; }
    }

    // ---------- Run flow ----------

    public readonly struct RunProgressChanged
    {
        public readonly float DeliveredValue;
        public readonly float LostValue;
        public readonly int BankedDoses;
        public readonly int ConsumedDoses;
        public readonly int QuotaDoses;

        public RunProgressChanged(float deliveredValue, float lostValue, int bankedDoses, int consumedDoses, int quotaDoses)
        {
            DeliveredValue = deliveredValue; LostValue = lostValue;
            BankedDoses = bankedDoses; ConsumedDoses = consumedDoses; QuotaDoses = quotaDoses;
        }
    }

    public readonly struct LevelCompleted
    {
        public readonly bool Success;
        public readonly int BankedDoses;
        public readonly int QuotaDoses;

        public LevelCompleted(bool success, int bankedDoses, int quotaDoses)
        {
            Success = success; BankedDoses = bankedDoses; QuotaDoses = quotaDoses;
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

    // ---------- Enums ----------

    public enum NoiseSource
    {
        Footstep,
        Sprint,
        Impact,
        Drop,
        Voice,
        Other,
        /// <summary>Doctors ignore this; nuisance enemies react to it.</summary>
        Music
    }

    public enum InsanityStage
    {
        Calm,
        Uneasy,
        Terrified,
        Swelling
    }

    public enum ScareKind
    {
        Scripted,
        Hallucination
    }
}
