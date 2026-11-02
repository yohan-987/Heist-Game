using UnityEngine;

namespace Game.Core
{
    /// <summary>Stable id carried by anything spawned through the <see cref="Spawner"/>.</summary>
    public sealed class EntityIdentity : MonoBehaviour
    {
        public GameEntityId Id { get; internal set; }
    }
}
