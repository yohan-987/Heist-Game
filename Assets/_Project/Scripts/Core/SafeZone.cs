using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Marks a room as safe. Put it on the start wing and pharmacy wing prefabs together with a trigger
    /// collider that covers the room. Doctors give up and never attack inside it, and players regain health.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SafeZone : MonoBehaviour, ISafeZone
    {
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnEnable() => Services.SafeZones.Register(this);

        private void OnDisable() => Services.SafeZones.Unregister(this);

        public bool Contains(Vector3 point)
        {
            if (_collider == null) return false;
            return _collider.bounds.Contains(point);
        }
    }
}
