using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.LevelGen
{
    [Serializable]
    public sealed class SocketAuthoring
    {
        [Tooltip("Cell inside the room (0,0 is the room's bottom-left cell).")]
        public Vector2Int Cell;
        [Tooltip("Which edge of that cell the doorway is on.")]
        public Direction Direction;
    }

    /// <summary>
    /// Put on the root of a room prefab. Describes its grid footprint, doorways and spawn points.
    /// The planner only sees the engine-free <see cref="RoomTemplate"/> this produces.
    /// </summary>
    public sealed class RoomAuthoring : MonoBehaviour
    {
        [SerializeField] private string _id = "room";
        [SerializeField] private Vector2Int _size = new Vector2Int(1, 1);
        [SerializeField, Min(0)] private int _weight = 1;
        [SerializeField] private List<SocketAuthoring> _sockets = new List<SocketAuthoring>();

        [Header("Spawn points (children of this prefab)")]
        [SerializeField] private Transform[] _valuableSpawns;
        [SerializeField] private Transform[] _enemySpawns;
        [Tooltip("Only used on start rooms.")]
        [SerializeField] private Transform _playerSpawn;
        [Tooltip("Inactive child holding the ExtractionZone. Enabled only in the extraction room.")]
        [SerializeField] private GameObject _extractionZoneRoot;

        [Header("Gizmos")]
        [SerializeField, Min(1f)] private float _gizmoCellSize = 8f;

        public string Id => _id;
        public Transform[] ValuableSpawns => _valuableSpawns ?? Array.Empty<Transform>();
        public Transform[] EnemySpawns => _enemySpawns ?? Array.Empty<Transform>();
        public Transform PlayerSpawn => _playerSpawn;
        public GameObject ExtractionZoneRoot => _extractionZoneRoot;

        public RoomTemplate ToTemplate()
        {
            var sockets = new List<RoomSocket>(_sockets.Count);
            foreach (var s in _sockets) sockets.Add(new RoomSocket(s.Cell, s.Direction));
            return new RoomTemplate(_id, _size, sockets, _weight);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            var size = new Vector3(_size.x * _gizmoCellSize, 0.2f, _size.y * _gizmoCellSize);
            Gizmos.DrawCube(size * 0.5f, size);

            Gizmos.color = Color.yellow;
            foreach (var s in _sockets)
            {
                Vector3 center = new Vector3((s.Cell.x + 0.5f) * _gizmoCellSize, 0.5f, (s.Cell.y + 0.5f) * _gizmoCellSize);
                var dir = s.Direction.ToOffset();
                Vector3 edge = center + new Vector3(dir.x, 0f, dir.y) * (_gizmoCellSize * 0.5f);
                Gizmos.DrawSphere(edge, 0.4f);
                Gizmos.DrawLine(center, edge);
            }
        }
    }
}
