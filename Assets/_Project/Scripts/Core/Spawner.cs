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
            Tag(instance.gameObject);
            return instance;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var instance = Object.Instantiate(prefab, position, rotation, parent);
            Tag(instance);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance != null) Object.Destroy(instance);
        }

        private void Tag(GameObject go)
        {
            var identity = go.GetComponent<EntityIdentity>();
            if (identity == null) identity = go.AddComponent<EntityIdentity>();
            identity.Id = _ids.Next();
        }
    }
}
