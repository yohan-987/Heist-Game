using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.LevelGen
{
    /// <summary>
    /// Grows a level layout from a seed: start room, then repeatedly attach a compatible room to a
    /// random open doorway. Connectivity holds by construction (every room attaches to an existing one),
    /// and rooms never overlap. Deterministic: same seed + same inputs = same layout, which is what lets
    /// multiplayer clients rebuild the host's level.
    /// </summary>
    public static class LayoutPlanner
    {
        private struct OpenSocket
        {
            public int RoomIndex;
            public Vector2Int Cell;      // world grid cell the doorway sits on
            public Direction Direction;
            public int Failures;
        }

        public static LevelLayout Plan(
            int seed,
            IReadOnlyList<RoomTemplate> startRooms,
            IReadOnlyList<RoomTemplate> roomPool,
            int targetRoomCount,
            int maxAttemptsPerDoor = 12)
        {
            if (startRooms == null || startRooms.Count == 0) throw new ArgumentException("Need at least one start room.");
            if (roomPool == null || roomPool.Count == 0) throw new ArgumentException("Need at least one room in the pool.");

            var rng = new DeterministicRandom(seed);
            var layout = new LevelLayout { Seed = seed };
            var occupied = new HashSet<Vector2Int>();
            var open = new List<OpenSocket>();

            // 1. Start room at the origin.
            var start = PickWeighted(rng, startRooms);
            AddRoom(layout, occupied, open, start, Vector2Int.zero, parentIndex: -1);
            layout.StartRoomIndex = 0;

            // 2. Grow until we have enough rooms or run out of doorways to try.
            while (layout.Rooms.Count < targetRoomCount && open.Count > 0)
            {
                int slot = rng.Range(0, open.Count);
                var door = open[slot];

                if (TryAttach(rng, layout, occupied, open, door, roomPool))
                {
                    // AddRoom only appends to the list, so this slot index is still valid.
                    open.RemoveAt(slot);
                }
                else
                {
                    door.Failures++;
                    if (door.Failures >= maxAttemptsPerDoor)
                    {
                        layout.SealedDoors.Add(new SealedDoor(door.RoomIndex, door.Cell, door.Direction));
                        open.RemoveAt(slot);
                    }
                    else
                    {
                        open[slot] = door;
                    }
                }
            }

            // 3. Whatever is still open gets sealed.
            foreach (var door in open)
                layout.SealedDoors.Add(new SealedDoor(door.RoomIndex, door.Cell, door.Direction));

            ComputeDistances(layout);
            layout.ExtractionRoomIndex = FarthestRoom(layout);
            return layout;
        }

        private static bool TryAttach(
            DeterministicRandom rng, LevelLayout layout, HashSet<Vector2Int> occupied,
            List<OpenSocket> open, OpenSocket door, IReadOnlyList<RoomTemplate> pool)
        {
            var template = PickWeighted(rng, pool);
            Vector2Int target = door.Cell + door.Direction.ToOffset();
            if (occupied.Contains(target)) return false;

            // Doorways on the new room that face back toward us.
            var facing = new List<RoomSocket>();
            foreach (var s in template.Sockets)
                if (s.Direction == door.Direction.Opposite()) facing.Add(s);
            if (facing.Count == 0) return false;

            var chosen = facing[rng.Range(0, facing.Count)];
            Vector2Int origin = target - chosen.Cell;

            // Reject if any cell is taken.
            for (int x = 0; x < template.Size.x; x++)
            for (int y = 0; y < template.Size.y; y++)
                if (occupied.Contains(origin + new Vector2Int(x, y))) return false;

            int newIndex = AddRoom(layout, occupied, open, template, origin, door.RoomIndex, usedSocket: chosen);
            layout.Rooms[door.RoomIndex].Connections.Add(newIndex);
            return true;
        }

        private static int AddRoom(
            LevelLayout layout, HashSet<Vector2Int> occupied, List<OpenSocket> open,
            RoomTemplate template, Vector2Int origin, int parentIndex, RoomSocket? usedSocket = null)
        {
            var room = new PlacedRoom { Index = layout.Rooms.Count, Template = template, Origin = origin };
            layout.Rooms.Add(room);

            for (int x = 0; x < template.Size.x; x++)
            for (int y = 0; y < template.Size.y; y++)
                occupied.Add(origin + new Vector2Int(x, y));

            if (parentIndex >= 0) room.Connections.Add(parentIndex);

            foreach (var s in template.Sockets)
            {
                if (usedSocket.HasValue && s.Cell == usedSocket.Value.Cell && s.Direction == usedSocket.Value.Direction)
                    continue; // this one is the connection we just made

                open.Add(new OpenSocket
                {
                    RoomIndex = room.Index,
                    Cell = origin + s.Cell,
                    Direction = s.Direction
                });
            }
            return room.Index;
        }

        private static RoomTemplate PickWeighted(DeterministicRandom rng, IReadOnlyList<RoomTemplate> list)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++) total += list[i].Weight;
            if (total <= 0) return list[rng.Range(0, list.Count)];

            int roll = rng.Range(0, total);
            for (int i = 0; i < list.Count; i++)
            {
                roll -= list[i].Weight;
                if (roll < 0) return list[i];
            }
            return list[list.Count - 1];
        }

        private static void ComputeDistances(LevelLayout layout)
        {
            foreach (var r in layout.Rooms) r.DistanceFromStart = -1;

            var queue = new Queue<int>();
            layout.Rooms[layout.StartRoomIndex].DistanceFromStart = 0;
            queue.Enqueue(layout.StartRoomIndex);

            while (queue.Count > 0)
            {
                var room = layout.Rooms[queue.Dequeue()];
                foreach (int next in room.Connections)
                {
                    var n = layout.Rooms[next];
                    if (n.DistanceFromStart >= 0) continue;
                    n.DistanceFromStart = room.DistanceFromStart + 1;
                    queue.Enqueue(next);
                }
            }
        }

        /// <summary>Room with the greatest hop distance from the start (lowest index wins ties).</summary>
        private static int FarthestRoom(LevelLayout layout)
        {
            int best = layout.StartRoomIndex;
            int bestDistance = -1;
            foreach (var r in layout.Rooms)
            {
                if (r.Index == layout.StartRoomIndex) continue;
                if (r.DistanceFromStart > bestDistance)
                {
                    bestDistance = r.DistanceFromStart;
                    best = r.Index;
                }
            }
            return best;
        }
    }
}
