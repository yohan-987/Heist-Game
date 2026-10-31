using System.Collections.Generic;
using UnityEngine;

namespace Game.LevelGen
{
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction d) => (Direction)(((int)d + 2) & 3);

        public static Vector2Int ToOffset(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return new Vector2Int(0, 1);
                case Direction.East: return new Vector2Int(1, 0);
                case Direction.South: return new Vector2Int(0, -1);
                default: return new Vector2Int(-1, 0);
            }
        }
    }

    /// <summary>A doorway: which cell of the room it's on and which way it faces.</summary>
    public readonly struct RoomSocket
    {
        public readonly Vector2Int Cell;
        public readonly Direction Direction;

        public RoomSocket(Vector2Int cell, Direction direction)
        {
            Cell = cell; Direction = direction;
        }
    }

    /// <summary>
    /// Engine-free description of a room: a rectangle of grid cells plus doorways.
    /// Authoring components convert themselves into one of these so the planner stays testable.
    /// </summary>
    public sealed class RoomTemplate
    {
        public readonly string Id;
        public readonly Vector2Int Size;
        public readonly IReadOnlyList<RoomSocket> Sockets;
        public readonly int Weight;

        public RoomTemplate(string id, Vector2Int size, IReadOnlyList<RoomSocket> sockets, int weight = 1)
        {
            Id = id;
            Size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
            Sockets = sockets ?? new List<RoomSocket>();
            Weight = Mathf.Max(0, weight);
        }

        public bool ContainsLocal(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Size.x && cell.y < Size.y;
    }

    public sealed class PlacedRoom
    {
        public int Index;
        public RoomTemplate Template;
        /// <summary>Grid coordinate of the template's local (0,0) cell.</summary>
        public Vector2Int Origin;
        public readonly List<int> Connections = new List<int>();
        public int DistanceFromStart;
    }

    /// <summary>A doorway left unconnected; the builder puts a wall or door block here.</summary>
    public readonly struct SealedDoor
    {
        public readonly int RoomIndex;
        public readonly Vector2Int Cell;       // world grid cell the doorway sits on
        public readonly Direction Direction;

        public SealedDoor(int roomIndex, Vector2Int cell, Direction direction)
        {
            RoomIndex = roomIndex; Cell = cell; Direction = direction;
        }
    }

    public sealed class LevelLayout
    {
        public int Seed;
        public readonly List<PlacedRoom> Rooms = new List<PlacedRoom>();
        public readonly List<SealedDoor> SealedDoors = new List<SealedDoor>();
        public int StartRoomIndex;
        public int ExtractionRoomIndex;

        /// <summary>True if every room can be reached from the start room.</summary>
        public bool IsConnected()
        {
            if (Rooms.Count == 0) return false;
            var seen = new bool[Rooms.Count];
            var queue = new Queue<int>();
            queue.Enqueue(StartRoomIndex);
            seen[StartRoomIndex] = true;
            int count = 1;

            while (queue.Count > 0)
            {
                var room = Rooms[queue.Dequeue()];
                foreach (int next in room.Connections)
                {
                    if (seen[next]) continue;
                    seen[next] = true;
                    count++;
                    queue.Enqueue(next);
                }
            }
            return count == Rooms.Count;
        }

        /// <summary>True if no two rooms share a grid cell.</summary>
        public bool HasOverlap()
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var room in Rooms)
            {
                for (int x = 0; x < room.Template.Size.x; x++)
                for (int y = 0; y < room.Template.Size.y; y++)
                {
                    if (!cells.Add(room.Origin + new Vector2Int(x, y))) return true;
                }
            }
            return false;
        }
    }
}
