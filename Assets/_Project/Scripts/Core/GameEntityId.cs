using System;

namespace Game.Core
{
    /// <summary>
    /// Stable identifier for anything that must be referenced across systems or, later, across the network.
    /// 0 is "none".
    /// </summary>
    public readonly struct GameEntityId : IEquatable<GameEntityId>
    {
        public readonly uint Value;

        public GameEntityId(uint value) { Value = value; }

        public bool IsValid => Value != 0;

        public bool Equals(GameEntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameEntityId other && Equals(other);
        public override int GetHashCode() => (int)Value;
        public override string ToString() => IsValid ? "#" + Value : "#none";

        public static bool operator ==(GameEntityId a, GameEntityId b) => a.Value == b.Value;
        public static bool operator !=(GameEntityId a, GameEntityId b) => a.Value != b.Value;

        public static readonly GameEntityId None = default;
    }

    /// <summary>
    /// Hands out ids in a deterministic order. Level generation resets it so every client that
    /// rebuilds the same seed gets the same ids.
    /// </summary>
    public sealed class EntityIdProvider
    {
        private uint _next = 1;

        public GameEntityId Next() => new GameEntityId(_next++);

        public void Reset(uint startAt = 1) => _next = startAt == 0 ? 1u : startAt;
    }
}
