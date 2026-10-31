using System.Collections.Generic;
using Game.Core;
using Game.DevTools;
using Game.LevelGen;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class LayoutPlannerTests
    {
        private static RoomTemplate Hub(string id = "hub") => new RoomTemplate(id, new Vector2Int(1, 1), new[]
        {
            new RoomSocket(new Vector2Int(0, 0), Direction.North),
            new RoomSocket(new Vector2Int(0, 0), Direction.East),
            new RoomSocket(new Vector2Int(0, 0), Direction.South),
            new RoomSocket(new Vector2Int(0, 0), Direction.West),
        });

        // 2x1 corridor: doors at both short ends.
        private static RoomTemplate Hall(string id = "hall") => new RoomTemplate(id, new Vector2Int(2, 1), new[]
        {
            new RoomSocket(new Vector2Int(0, 0), Direction.West),
            new RoomSocket(new Vector2Int(1, 0), Direction.East),
            new RoomSocket(new Vector2Int(0, 0), Direction.North),
        }, weight: 2);

        // 2x2 room, doors on all sides.
        private static RoomTemplate Big(string id = "big") => new RoomTemplate(id, new Vector2Int(2, 2), new[]
        {
            new RoomSocket(new Vector2Int(0, 1), Direction.North),
            new RoomSocket(new Vector2Int(1, 0), Direction.East),
            new RoomSocket(new Vector2Int(0, 0), Direction.South),
            new RoomSocket(new Vector2Int(0, 0), Direction.West),
        });

        private static LevelLayout Plan(int seed, int rooms = 10) =>
            LayoutPlanner.Plan(seed, new[] { Hub("start") }, new[] { Hub(), Hall(), Big() }, rooms);

        [Test]
        public void SameSeed_GivesIdenticalLayout()
        {
            var a = Plan(77);
            var b = Plan(77);

            Assert.AreEqual(a.Rooms.Count, b.Rooms.Count);
            for (int i = 0; i < a.Rooms.Count; i++)
            {
                Assert.AreEqual(a.Rooms[i].Template.Id, b.Rooms[i].Template.Id);
                Assert.AreEqual(a.Rooms[i].Origin, b.Rooms[i].Origin);
            }
            Assert.AreEqual(a.ExtractionRoomIndex, b.ExtractionRoomIndex);
            Assert.AreEqual(a.SealedDoors.Count, b.SealedDoors.Count);
        }

        [Test]
        public void DifferentSeeds_UsuallyGiveDifferentLayouts()
        {
            int different = 0;
            var reference = Plan(1);
            for (int seed = 2; seed < 22; seed++)
            {
                var other = Plan(seed);
                bool same = other.Rooms.Count == reference.Rooms.Count;
                for (int i = 0; same && i < other.Rooms.Count; i++)
                    same = other.Rooms[i].Origin == reference.Rooms[i].Origin &&
                           other.Rooms[i].Template.Id == reference.Rooms[i].Template.Id;
                if (!same) different++;
            }
            Assert.Greater(different, 10);
        }

        [Test]
        public void Soak_ManySeeds_AreConnected_NonOverlapping_AndHaveAnExit()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var layout = Plan(seed, rooms: 12);

                Assert.IsTrue(layout.IsConnected(), "disconnected, seed " + seed);
                Assert.IsFalse(layout.HasOverlap(), "overlap, seed " + seed);
                Assert.GreaterOrEqual(layout.Rooms.Count, 2, "too small, seed " + seed);
                Assert.LessOrEqual(layout.Rooms.Count, 12, "too big, seed " + seed);
                Assert.AreNotEqual(layout.StartRoomIndex, layout.ExtractionRoomIndex, "exit == start, seed " + seed);
            }
        }

        [Test]
        public void Connections_AreSymmetric()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var layout = Plan(seed);
                foreach (var room in layout.Rooms)
                    foreach (int other in room.Connections)
                        CollectionAssert.Contains(layout.Rooms[other].Connections, room.Index,
                            $"asymmetric link {room.Index}->{other}, seed {seed}");
            }
        }

        [Test]
        public void ExtractionRoom_IsAsFarAsAnyOther()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var layout = Plan(seed);
                int exitDistance = layout.Rooms[layout.ExtractionRoomIndex].DistanceFromStart;
                foreach (var room in layout.Rooms)
                    Assert.LessOrEqual(room.DistanceFromStart, exitDistance, "seed " + seed);
            }
        }

        [Test]
        public void EveryDoorway_IsEitherConnectedOrSealed()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var layout = Plan(seed);
                int totalSockets = 0;
                foreach (var room in layout.Rooms) totalSockets += room.Template.Sockets.Count;

                // Each connection uses one socket on each end.
                int links = 0;
                foreach (var room in layout.Rooms) links += room.Connections.Count;

                Assert.AreEqual(totalSockets, links + layout.SealedDoors.Count, "seed " + seed);
            }
        }

        [Test]
        public void RoomCountIsRespected_WhenRoomIsPlentiful()
        {
            var layout = Plan(5, rooms: 6);
            Assert.AreEqual(6, layout.Rooms.Count);
        }

        [Test]
        public void SingleDeadEndRoom_StopsGrowingGracefully()
        {
            // A start room with no doorways can't grow; the planner must return, not hang.
            var closed = new RoomTemplate("closed", new Vector2Int(1, 1), new RoomSocket[0]);
            var layout = LayoutPlanner.Plan(1, new[] { closed }, new[] { Hub() }, 10);
            Assert.AreEqual(1, layout.Rooms.Count);
        }

        [Test]
        public void Plan_RejectsEmptyInputs()
        {
            Assert.Throws<System.ArgumentException>(() =>
                LayoutPlanner.Plan(1, new RoomTemplate[0], new[] { Hub() }, 5));
            Assert.Throws<System.ArgumentException>(() =>
                LayoutPlanner.Plan(1, new[] { Hub() }, new RoomTemplate[0], 5));
        }

        [Test]
        public void Direction_OppositeAndOffsetsAgree()
        {
            foreach (Direction d in System.Enum.GetValues(typeof(Direction)))
            {
                Assert.AreEqual(d, d.Opposite().Opposite());
                Assert.AreEqual(Vector2Int.zero, d.ToOffset() + d.Opposite().ToOffset());
            }
        }
    }

    public class SpawnPlannerTests
    {
        private static List<SpawnOption> Options() => new List<SpawnOption>
        {
            new SpawnOption(0, 100f, 3f),
            new SpawnOption(1, 400f, 1f),
            new SpawnOption(2, 1500f, 0.5f),
        };

        [Test]
        public void NeverExceedsBudget()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var picks = SpawnPlanner.Choose(new DeterministicRandom(seed), Options(), 2000f);
                float spent = 0f;
                var opts = Options();
                foreach (int id in picks) spent += opts[id].Cost;
                Assert.LessOrEqual(spent, 2000f, "seed " + seed);
            }
        }

        [Test]
        public void SameSeed_SamePicks()
        {
            var a = SpawnPlanner.Choose(new DeterministicRandom(9), Options(), 3000f);
            var b = SpawnPlanner.Choose(new DeterministicRandom(9), Options(), 3000f);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void MaxCount_IsHonoured()
        {
            var picks = SpawnPlanner.Choose(new DeterministicRandom(3), Options(), 100000f, maxCount: 7);
            Assert.AreEqual(7, picks.Count);
        }

        [Test]
        public void UnaffordableOptions_AreSkipped()
        {
            var picks = SpawnPlanner.Choose(new DeterministicRandom(1), Options(), 150f);
            foreach (int id in picks) Assert.AreEqual(0, id);
        }

        [Test]
        public void ZeroCostOrZeroWeight_NeverPickedAndNeverLoopsForever()
        {
            var bad = new List<SpawnOption> { new SpawnOption(0, 0f, 1f), new SpawnOption(1, 10f, 0f) };
            var picks = SpawnPlanner.Choose(new DeterministicRandom(1), bad, 1000f);
            Assert.AreEqual(0, picks.Count);
        }

        [Test]
        public void EmptyOrNoBudget_ReturnsNothing()
        {
            Assert.AreEqual(0, SpawnPlanner.Choose(new DeterministicRandom(1), new List<SpawnOption>(), 100f).Count);
            Assert.AreEqual(0, SpawnPlanner.Choose(new DeterministicRandom(1), Options(), 0f).Count);
        }
    }

    public class CommandRegistryTests
    {
        [Test]
        public void Tokenise_HandlesQuotes()
        {
            var t = CommandRegistry.Tokenise("spawn \"blue vase\" 3");
            CollectionAssert.AreEqual(new[] { "spawn", "blue vase", "3" }, t);
        }

        [Test]
        public void Execute_RunsHandlerWithArgs_CaseInsensitive()
        {
            var reg = new CommandRegistry();
            reg.Register("echo", args => string.Join("|", args));
            Assert.AreEqual("a|b", reg.Execute("ECHO a b"));
        }

        [Test]
        public void Execute_UnknownCommand_ReportsIt()
        {
            var reg = new CommandRegistry();
            StringAssert.Contains("Unknown command", reg.Execute("nope"));
        }

        [Test]
        public void Execute_HandlerThrows_IsContained()
        {
            var reg = new CommandRegistry();
            reg.Register("boom", _ => throw new System.InvalidOperationException("bad"));
            StringAssert.Contains("bad", reg.Execute("boom"));
        }

        [Test]
        public void Execute_BlankLine_ReturnsEmpty()
        {
            Assert.AreEqual("", new CommandRegistry().Execute("   "));
        }
    }
}
