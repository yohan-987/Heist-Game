using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// All runtime spawning goes through here, never scattered Instantiate calls. That gives one
    /// place to assign ids now and to route through the network layer later.
    /// </summary>
    public sealed class Spawner
    {
        private readonly EntityIdProvider _ids;

        public Spawner(EntityIdProvider ids) => _ids = ids;

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var instance = Object.Instantiate(prefab, position, rotation, parent);
            EnsureIdentity(instance.gameObject);
            return instance;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var instance = Object.Instantiate(prefab, position, rotation, parent);
            EnsureIdentity(instance);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }

        /// <summary>
        /// Gives an object an id if it has none. Spawned objects get one automatically; objects placed
        /// by hand in a scene get one the first time something needs it.
        /// </summary>
        public GameEntityId EnsureIdentity(GameObject go)
        {
            var identity = go.GetComponent<EntityIdentity>();
            if (identity == null) identity = go.AddComponent<EntityIdentity>();
            if (!identity.Id.IsValid) identity.Id = _ids.Next();
            return identity.Id;
        }
    }
}
