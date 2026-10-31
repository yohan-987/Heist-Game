using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class EventBusTests
    {
        private struct Ping { public int Value; }

        [Test]
        public void Publish_ReachesSubscribers()
        {
            var bus = new EventBus();
            int total = 0;
            bus.Subscribe<Ping>(p => total += p.Value);
            bus.Subscribe<Ping>(p => total += p.Value * 10);

            bus.Publish(new Ping { Value = 2 });

            Assert.AreEqual(22, total);
        }

        [Test]
        public void Dispose_StopsDelivery()
        {
            var bus = new EventBus();
            int calls = 0;
            var sub = bus.Subscribe<Ping>(_ => calls++);

            bus.Publish(new Ping());
            sub.Dispose();
            bus.Publish(new Ping());

            Assert.AreEqual(1, calls);
            Assert.AreEqual(0, bus.SubscriberCount<Ping>());
        }

        [Test]
        public void Unsubscribe_DuringPublish_IsSafe()
        {
            var bus = new EventBus();
            int calls = 0;
            System.Action<Ping> handler = null;
            handler = _ => { calls++; bus.Unsubscribe(handler); };
            bus.Subscribe(handler);
            bus.Subscribe<Ping>(_ => calls++);

            bus.Publish(new Ping());
            bus.Publish(new Ping());

            // First publish: both run. Second publish: only the survivor.
            Assert.AreEqual(3, calls);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            var bus = new EventBus();
            Assert.DoesNotThrow(() => bus.Publish(new Ping()));
        }
    }

    public class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new DeterministicRandom(1234);
            var b = new DeterministicRandom(1234);
            for (int i = 0; i < 200; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeeds_Differ()
        {
            var a = new DeterministicRandom(1);
            var b = new DeterministicRandom(2);
            bool anyDifferent = false;
            for (int i = 0; i < 20; i++) anyDifferent |= a.NextUInt() != b.NextUInt();
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void ZeroSeed_StillProducesValues()
        {
            var r = new DeterministicRandom(0);
            uint first = r.NextUInt();
            uint second = r.NextUInt();
            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void NextFloat_StaysInUnitInterval()
        {
            var r = new DeterministicRandom(7);
            for (int i = 0; i < 5000; i++)
            {
                float f = r.NextFloat();
                Assert.GreaterOrEqual(f, 0f);
                Assert.Less(f, 1f);
            }
        }

        [Test]
        public void IntRange_RespectsBoundsAndCoversThem()
        {
            var r = new DeterministicRandom(99);
            var seen = new HashSet<int>();
            for (int i = 0; i < 2000; i++)
            {
                int v = r.Range(3, 8);
                Assert.GreaterOrEqual(v, 3);
                Assert.Less(v, 8);
                seen.Add(v);
            }
            Assert.AreEqual(5, seen.Count);
        }

        [Test]
        public void Range_EmptyOrInverted_ReturnsMin()
        {
            var r = new DeterministicRandom(5);
            Assert.AreEqual(4, r.Range(4, 4));
            Assert.AreEqual(4, r.Range(4, 1));
        }

        [Test]
        public void Shuffle_KeepsAllItems_AndIsDeterministic()
        {
            var a = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var b = new List<int>(a);
            new DeterministicRandom(42).Shuffle(a);
            new DeterministicRandom(42).Shuffle(b);

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, a);
        }
    }

    public class EntityIdTests
    {
        [Test]
        public void Provider_HandsOutIncreasingIds_AndResets()
        {
            var p = new EntityIdProvider();
            var a = p.Next();
            var b = p.Next();
            Assert.IsTrue(a.IsValid);
            Assert.AreNotEqual(a, b);

            p.Reset();
            Assert.AreEqual(a, p.Next());
        }

        [Test]
        public void None_IsInvalid()
        {
            Assert.IsFalse(EntityId.None.IsValid);
        }
    }

    public class GameStateMachineTests
    {
        [Test]
        public void StartsInBoot()
        {
            Assert.AreEqual(GameState.Boot, new GameStateMachine(new EventBus()).Current);
        }

        [Test]
        public void AllowedTransition_Succeeds_AndPublishes()
        {
            var bus = new EventBus();
            var machine = new GameStateMachine(bus);
            GameStateChanged? seen = null;
            bus.Subscribe<GameStateChanged>(e => seen = e);

            Assert.IsTrue(machine.TryTransitionTo(GameState.Menu));

            Assert.AreEqual(GameState.Menu, machine.Current);
            Assert.IsTrue(seen.HasValue);
            Assert.AreEqual(GameState.Boot, seen.Value.Previous);
            Assert.AreEqual(GameState.Menu, seen.Value.Current);
        }

        [Test]
        public void ForbiddenTransition_IsRejected()
        {
            var machine = new GameStateMachine(new EventBus());
            // Boot cannot jump straight into a level.
            Assert.IsFalse(machine.TryTransitionTo(GameState.InLevel));
            Assert.AreEqual(GameState.Boot, machine.Current);
        }

        [Test]
        public void FullRun_MenuToResultsAndBack()
        {
            var machine = new GameStateMachine(new EventBus());
            foreach (var next in new[]
                     {
                         GameState.Menu, GameState.Loading, GameState.InLevel,
                         GameState.Extraction, GameState.Results, GameState.Menu
                     })
            {
                Assert.IsTrue(machine.TryTransitionTo(next), "failed to enter " + next);
            }
        }
    }

    public class StateMachineTests
    {
        private enum S { A, B }

        [Test]
        public void Transition_RunsExitThenEnter_AndResetsTimer()
        {
            var log = new List<string>();
            var fsm = new StateMachine<S>()
                .Add(S.A, onEnter: _ => log.Add("enterA"), onExit: _ => log.Add("exitA"))
                .Add(S.B, onEnter: _ => log.Add("enterB"));

            fsm.Start(S.A);
            fsm.Update(2f);
            Assert.AreEqual(2f, fsm.TimeInState, 0.0001f);

            fsm.TransitionTo(S.B);

            CollectionAssert.AreEqual(new[] { "enterA", "exitA", "enterB" }, log);
            Assert.AreEqual(0f, fsm.TimeInState);
            Assert.AreEqual(S.A, fsm.Previous);
        }

        [Test]
        public void TransitionToSameState_IsNoOp()
        {
            var fsm = new StateMachine<S>().Add(S.A).Add(S.B);
            fsm.Start(S.A);
            Assert.IsFalse(fsm.TransitionTo(S.A));
        }
    }

    public class NoiseMathTests
    {
        [Test]
        public void QuietImpact_MakesNoNoise()
        {
            Assert.AreEqual(0f, NoiseMath.ImpactLoudness(2f, 3f, 0.8f, 25f));
        }

        [Test]
        public void Impact_ScalesWithImpulse_AndIsCapped()
        {
            Assert.AreEqual(8f, NoiseMath.ImpactLoudness(13f, 3f, 0.8f, 25f), 0.001f);
            Assert.AreEqual(25f, NoiseMath.ImpactLoudness(500f, 3f, 0.8f, 25f), 0.001f);
        }

        [Test]
        public void Perceived_FallsOffWithDistance()
        {
            Assert.AreEqual(1f, NoiseMath.Perceived(10f, 0f), 0.001f);
            Assert.AreEqual(0.5f, NoiseMath.Perceived(10f, 5f), 0.001f);
            Assert.AreEqual(0f, NoiseMath.Perceived(10f, 10f), 0.001f);
            Assert.AreEqual(0f, NoiseMath.Perceived(10f, 30f), 0.001f);
        }

        [Test]
        public void Sensitivity_ExtendsOrRemovesRange()
        {
            Assert.IsTrue(NoiseMath.CanHear(10f, 15f, sensitivity: 2f));
            Assert.IsFalse(NoiseMath.CanHear(10f, 1f, sensitivity: 0f));
        }
    }
}
