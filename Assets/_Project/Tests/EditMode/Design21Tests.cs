using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.LevelGen;
using Game.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class HitModelTests
    {
        [Test]
        public void FirstHitLands_SecondDuringProtectionIsAbsorbed()
        {
            var hit = new HitModel(2f, 5f);
            Assert.AreEqual(60f, hit.Absorb(60f), 0.001f);
            Assert.IsTrue(hit.IsProtected);
            Assert.AreEqual(0f, hit.Absorb(60f), 0.001f);
        }

        [Test]
        public void ProtectionExpiresAfterTwoSeconds()
        {
            var hit = new HitModel(2f, 5f);
            hit.Absorb(60f);
            hit.Tick(1.9f);
            Assert.IsTrue(hit.IsProtected);
            hit.Tick(0.2f);
            Assert.IsFalse(hit.IsProtected);
            Assert.AreEqual(60f, hit.Absorb(60f), 0.001f);
        }

        [Test]
        public void OneMistakeIsAScare_TwoAreDeath()
        {
            var hit = new HitModel(2f, 5f);
            var health = new HealthModel(100f);

            Assert.IsFalse(health.Damage(hit.Absorb(60f)));
            Assert.AreEqual(40f, health.Current, 0.001f);

            hit.Tick(2.1f);
            Assert.IsTrue(health.Damage(hit.Absorb(60f)));
        }

        [Test]
        public void Regeneration_OnlyInSafeRooms_FullInTwentySeconds()
        {
            var hit = new HitModel(2f, 5f);
            Assert.AreEqual(0f, hit.Regen(1f, false), 0.0001f);

            var health = new HealthModel(100f);
            health.Damage(100f - 1f);
            for (int i = 0; i < 20; i++) health.Heal(hit.Regen(1f, true));
            Assert.AreEqual(100f, health.Current, 0.001f);
        }

        [Test]
        public void Windup_IsNeverShorterThanThreeQuartersOfASecond()
        {
            Assert.AreEqual(0.75f, HitModel.Windup(0.1f), 0.0001f);
            Assert.AreEqual(1.2f, HitModel.Windup(1.2f), 0.0001f);
        }

        [Test]
        public void ZeroOrNegativeDamage_DoesNotStartProtection()
        {
            var hit = new HitModel();
            Assert.AreEqual(0f, hit.Absorb(0f), 0.0001f);
            Assert.IsFalse(hit.IsProtected);
        }
    }

    public class SafeZoneRegistryTests
    {
        private sealed class Sphere : ISafeZone
        {
            public Vector3 Centre; public float Radius;
            public bool Contains(Vector3 p) => Vector3.Distance(p, Centre) <= Radius;
        }

        [Test]
        public void IsSafe_TrueOnlyInsideARegisteredZone()
        {
            var reg = new SafeZoneRegistry();
            var zone = new Sphere { Centre = Vector3.zero, Radius = 5f };
            Assert.IsFalse(reg.IsSafe(Vector3.zero));

            reg.Register(zone);
            Assert.IsTrue(reg.IsSafe(new Vector3(3, 0, 0)));
            Assert.IsFalse(reg.IsSafe(new Vector3(9, 0, 0)));

            reg.Unregister(zone);
            Assert.IsFalse(reg.IsSafe(Vector3.zero));
        }

        [Test]
        public void Register_SameZoneTwice_IsCountedOnce()
        {
            var reg = new SafeZoneRegistry();
            var zone = new Sphere { Radius = 1f };
            reg.Register(zone); reg.Register(zone);
            Assert.AreEqual(1, reg.Count);
        }
    }

    public class ThreatDirectorTests
    {
        private static readonly Vector3 Here = Vector3.zero;

        [Test]
        public void AtMostTwoHazards_ThirdWaits()
        {
            var d = new ThreatDirector();
            Assert.IsTrue(d.TryBegin(1, 10, HazardClass.Disruptive, Here, -1f));
            Assert.IsTrue(d.TryBegin(1, 11, HazardClass.HardControl, Here, -1f));
            Assert.IsFalse(d.TryBegin(1, 12, HazardClass.Disruptive, Here, -1f));
            Assert.AreEqual(HazardCheck.CapReached, d.Check(1, HazardClass.Disruptive, Here));

            d.End(1, 10);
            Assert.IsTrue(d.TryBegin(1, 12, HazardClass.Disruptive, Here, -1f));
        }

        [Test]
        public void CapIsPerPlayer()
        {
            var d = new ThreatDirector();
            d.TryBegin(1, 10, HazardClass.Disruptive, Here, -1f);
            d.TryBegin(1, 11, HazardClass.Disruptive, Here, -1f);
            Assert.IsTrue(d.TryBegin(2, 12, HazardClass.Disruptive, Here, -1f));
        }

        [Test]
        public void CosmeticHazards_AreNeverCounted()
        {
            var d = new ThreatDirector();
            for (int i = 0; i < 10; i++) Assert.IsTrue(d.TryBegin(1, i, HazardClass.Cosmetic, Here, -1f));
            Assert.AreEqual(0, d.ActiveCount(1));
            Assert.IsTrue(d.TryBegin(1, 100, HazardClass.Disruptive, Here, -1f));
        }

        [Test]
        public void ChasingDoctor_BlocksPileOns_ButAlwaysRegisters()
        {
            var d = new ThreatDirector();
            d.TryBegin(1, 10, HazardClass.Disruptive, Here, -1f);
            d.TryBegin(1, 11, HazardClass.Disruptive, Here, -1f);

            Assert.IsTrue(d.TryBegin(1, 99, HazardClass.Lethal, Here, -1f), "a chasing doctor always registers");
            Assert.AreEqual(HazardCheck.PileOn, d.Check(1, HazardClass.HardControl, Here));

            d.End(1, 99);
            d.End(1, 10);
            Assert.AreEqual(HazardCheck.Allowed, d.Check(1, HazardClass.HardControl, Here));
        }

        [Test]
        public void NearbyDoctor_BlocksPileOns_FarDoctorDoesNot()
        {
            var d = new ThreatDirector();
            d.SetDoctor(7, new Vector3(14f, 0, 0));
            Assert.AreEqual(HazardCheck.PileOn, d.Check(1, HazardClass.HardControl, Here));

            d.SetDoctor(7, new Vector3(16f, 0, 0));
            Assert.AreEqual(HazardCheck.Allowed, d.Check(1, HazardClass.HardControl, Here));

            d.SetDoctor(7, Here);
            d.RemoveDoctor(7);
            Assert.AreEqual(HazardCheck.Allowed, d.Check(1, HazardClass.HardControl, Here));
        }

        [Test]
        public void TimedHazards_ExpireByThemselves()
        {
            var d = new ThreatDirector();
            d.TryBegin(1, 10, HazardClass.Disruptive, Here, 4f);
            d.Tick(3.9f);
            Assert.IsTrue(d.IsActive(1, 10));
            d.Tick(0.2f);
            Assert.IsFalse(d.IsActive(1, 10));
        }

        [Test]
        public void OpenEndedHazards_StayUntilEnded()
        {
            var d = new ThreatDirector();
            d.TryBegin(1, 10, HazardClass.HardControl, Here, -1f);
            d.Tick(600f);
            Assert.IsTrue(d.IsActive(1, 10));
        }

        [Test]
        public void SameHazardTwice_IsCountedOnce()
        {
            var d = new ThreatDirector();
            d.TryBegin(1, 10, HazardClass.Disruptive, Here, -1f);
            d.TryBegin(1, 10, HazardClass.Disruptive, Here, -1f);
            Assert.AreEqual(1, d.ActiveCount(1));
        }
    }

    public class LootPlacementTests
    {
        [Test]
        public void MaxVaultShare_FollowsTheGuaranteeRule()
        {
            Assert.AreEqual(0.22f, LootPlacement.MaxVaultShare(0.65f), 0.0001f);
            Assert.AreEqual(0.28f, LootPlacement.MaxVaultShare(0.60f), 0.0001f);
        }

        [Test]
        public void ConfiguredVaultShares_NeverBreakTheRule()
        {
            var normal = DifficultyProfile.For(DifficultyLevel.Normal);
            var high = DifficultyProfile.For(DifficultyLevel.High);
            Assert.LessOrEqual(normal.VaultShare, LootPlacement.MaxVaultShare(normal.CollectionEfficiency));
            Assert.LessOrEqual(high.VaultShare, LootPlacement.MaxVaultShare(high.CollectionEfficiency));
            Assert.AreEqual(0.20f, normal.VaultShare, 0.0001f);
            Assert.AreEqual(0.25f, high.VaultShare, 0.0001f);
        }

        [Test]
        public void ClassifyVault_FarthestThirdOfNonStartRooms()
        {
            // Room 0 is the start. Distances from it: 1,1,2,2,3,3,4.
            var distances = new List<int> { 0, 1, 1, 2, 2, 3, 3, 4 };
            var vault = LootPlacement.ClassifyVault(distances, 0);

            int count = 0;
            for (int i = 0; i < vault.Length; i++) if (vault[i]) count++;
            Assert.AreEqual(2, count, "7 rooms: 5 near, 2 vault");
            Assert.IsFalse(vault[0], "the start room is never the vault");
            Assert.IsTrue(vault[7], "the farthest room is in the vault");
            Assert.IsTrue(vault[6]);
        }

        [Test]
        public void ClassifyVault_TiesBrokenByIndex_Deterministic()
        {
            var distances = new List<int> { 0, 2, 2, 2 };
            var a = LootPlacement.ClassifyVault(distances, 0);
            var b = LootPlacement.ClassifyVault(distances, 0);
            CollectionAssert.AreEqual(a, b);
            Assert.IsTrue(a[3]);
            Assert.IsFalse(a[1]);
        }

        [Test]
        public void ClassifyVault_TinyLevels_HaveNoVault()
        {
            var vault = LootPlacement.ClassifyVault(new List<int> { 0, 1, 2 }, 0);
            Assert.IsFalse(vault[1]);
            Assert.IsFalse(vault[2]);
        }

        [Test]
        public void Assign_VaultNeverExceedsItsShare_AndNearMeetsTheGuarantee()
        {
            var values = new List<float>();
            for (int i = 0; i < 40; i++) values.Add(300f + (i % 5) * 150f);
            float total = 0f; foreach (float v in values) total += v;

            float minimum = total * 0.65f;
            var r = LootPlacement.Assign(values, 40, 10, 0.20f, minimum);

            Assert.LessOrEqual(r.VaultValue, total * 0.20f + 0.01f);
            Assert.IsTrue(r.GuaranteeMet);
            Assert.AreEqual(total, r.NearValue + r.VaultValue, 0.01f);
        }

        [Test]
        public void Assign_TheVaultGetsTheHeaviestItems()
        {
            var values = new List<float> { 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 900f };
            var r = LootPlacement.Assign(values, 10, 2, 0.5f, 0f);
            Assert.GreaterOrEqual(r.SlotForItem[9], 10, "the 900 item goes to a vault slot");
        }

        [Test]
        public void Assign_CapIsTightenedToProtectTheGuarantee()
        {
            // Total 1000, minimum 800: near must hold 960, so at most 40 may go to the vault even with a 50 percent share.
            var values = new List<float>();
            for (int i = 0; i < 10; i++) values.Add(100f);
            var r = LootPlacement.Assign(values, 10, 10, 0.5f, 800f);
            Assert.AreEqual(0f, r.VaultValue, 0.001f, "no 100 item fits under a cap of 40");
            Assert.IsTrue(r.GuaranteeMet);
        }

        [Test]
        public void Assign_NotEnoughLoot_ReportsGuaranteeBroken()
        {
            var values = new List<float> { 200f, 200f };
            var r = LootPlacement.Assign(values, 5, 5, 0.2f, 1000f);
            Assert.IsFalse(r.GuaranteeMet);
            Assert.AreEqual(0f, r.VaultValue, 0.001f, "nothing may leave the near rooms when the guarantee is already short");
        }

        [Test]
        public void Assign_ZeroVaultSlots_PutsEverythingNear()
        {
            var values = new List<float> { 300f, 300f, 300f };
            var r = LootPlacement.Assign(values, 3, 0, 0.2f, 100f);
            Assert.AreEqual(900f, r.NearValue, 0.001f);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, r.SlotForItem);
        }

        [Test]
        public void Assign_TooFewNearSlots_OverflowsIntoTheVaultAndSaysSo()
        {
            var values = new List<float> { 100f, 100f, 100f, 100f };
            var r = LootPlacement.Assign(values, 2, 2, 0f, 0f);
            Assert.AreEqual(2, r.Overflow);
            Assert.AreEqual(0, r.Unplaced);
            foreach (int slot in r.SlotForItem) Assert.GreaterOrEqual(slot, 0);
        }

        [Test]
        public void Assign_TooFewNearSlots_BreaksTheGuaranteeAndSaysSo()
        {
            // 10 items of 100, minimum 700 so near must hold 840. Only 8 near slots: two items must go to the vault.
            var values = new List<float>();
            for (int i = 0; i < 10; i++) values.Add(100f);
            var r = LootPlacement.Assign(values, 8, 4, 0.2f, 700f);
            Assert.AreEqual(800f, r.NearValue, 0.001f);
            Assert.IsFalse(r.GuaranteeMet, "the layout, not the placement, is at fault: the builder warns about it");
        }

        [Test]
        public void Assign_NoRoomAnywhere_MarksItemsUnplaced()
        {
            var values = new List<float> { 100f, 100f, 100f };
            var r = LootPlacement.Assign(values, 1, 1, 0f, 0f);
            Assert.AreEqual(1, r.Unplaced);
        }

        [Test]
        public void Assign_SlotsAreUnique()
        {
            var values = new List<float>();
            for (int i = 0; i < 30; i++) values.Add(250f + i * 10f);
            var r = LootPlacement.Assign(values, 30, 10, 0.25f, 3000f);
            var seen = new HashSet<int>();
            foreach (int s in r.SlotForItem) if (s >= 0) Assert.IsTrue(seen.Add(s), "slot used twice: " + s);
        }

        // End to end: the real budget formula, the real spawn planner and the real placement, many seeds.
        private static void CheckWinnable(DifficultyLevel level, int players)
        {
            var difficulty = DifficultyProfile.For(level);
            var plan = EconomyPlanner.Plan(players, difficulty, 0.25f, 600f, 33f, 500f);

            var options = new List<SpawnOption>
            {
                new SpawnOption(0, 150f, 3f), new SpawnOption(1, 300f, 3f), new SpawnOption(2, 450f, 2f),
                new SpawnOption(3, 700f, 1f), new SpawnOption(4, 1000f, 0.5f),
            };
            float[] costs = { 150f, 300f, 450f, 700f, 1000f };

            for (int seed = 1; seed <= 100; seed++)
            {
                var rng = new DeterministicRandom(seed);
                var picks = SpawnPlanner.Choose(rng, options, plan.Budget, 256);
                var values = new List<float>();
                foreach (int p in picks) values.Add(costs[p]);

                // Enough near slots for every item, and a separate set of vault slots.
                int nearSlots = values.Count + 5;
                int vaultSlots = values.Count / 3 + 3;
                var r = LootPlacement.Assign(values, nearSlots, vaultSlots, difficulty.VaultShare, plan.MinimumValue);

                Assert.IsTrue(r.GuaranteeMet,
                    level + " " + players + "p seed " + seed + ": near " + r.NearValue + " < " + (1.2f * plan.MinimumValue));
                Assert.AreEqual(0, r.Unplaced);
            }
        }

        [Test] public void EndToEnd_Normal_OneToFourPlayers()
        {
            for (int p = 1; p <= 4; p++) CheckWinnable(DifficultyLevel.Normal, p);
        }

        [Test] public void EndToEnd_High_OneToFourPlayers()
        {
            for (int p = 1; p <= 4; p++) CheckWinnable(DifficultyLevel.High, p);
        }
    }

    public class PerPlayerDoseTests
    {
        [Test]
        public void TryConsume_RemembersWhoTookEachDose()
        {
            var ledger = new MedicineLedger(2, 100f);
            ledger.Commit(500f);

            Assert.IsTrue(ledger.TryConsume(3));
            Assert.IsTrue(ledger.TryConsume(3));
            Assert.IsTrue(ledger.TryConsume(7));

            Assert.AreEqual(2, ledger.TakenBy(3));
            Assert.AreEqual(1, ledger.TakenBy(7));
            Assert.AreEqual(0, ledger.TakenBy(9));
            Assert.AreEqual(3, ledger.Consumed);
            CollectionAssert.AreEqual(new[] { 3, 7 }, ledger.Takers());
        }

        [Test]
        public void RefusedConsume_IsNotAttributed()
        {
            var ledger = new MedicineLedger(2, 100f);
            Assert.IsFalse(ledger.TryConsume(1));
            Assert.AreEqual(0, ledger.TakenBy(1));
        }

        [Test]
        public void Reset_ForgetsEveryone()
        {
            var ledger = new MedicineLedger(2, 100f);
            ledger.Commit(300f);
            ledger.TryConsume(1);
            ledger.Reset(2, 100f);
            Assert.AreEqual(0, ledger.TakenBy(1));
            Assert.AreEqual(0, ledger.Takers().Count);
        }

        [Test]
        public void UnknownPlayerOverload_StillWorks()
        {
            var ledger = new MedicineLedger(1, 100f);
            ledger.Commit(100f);
            Assert.IsTrue(ledger.TryConsume());
            Assert.AreEqual(1, ledger.TakenBy(MedicineLedger.UnknownPlayer));
        }
    }

    public class IntroScareTests
    {
        [Test]
        public void FirstScriptedScare_IsCosmetic_LaterOnesCostPressure()
        {
            var s = new JumpscareScheduler(new DeterministicRandom(1), 4, 0f);
            bool costs;

            Assert.IsTrue(s.TryTriggerScripted(1, out costs));
            Assert.IsFalse(costs);

            Assert.IsTrue(s.TryTriggerScripted(2, out costs));
            Assert.IsTrue(costs);
            Assert.AreEqual(2, s.ScriptedFired);
        }

        [Test]
        public void ARefusedScare_DoesNotUseUpTheIntroduction()
        {
            var s = new JumpscareScheduler(new DeterministicRandom(1), 4, 45f);
            bool costs;
            Assert.IsTrue(s.TryTriggerScripted(1, out costs));
            Assert.IsFalse(s.TryTriggerScripted(2, out costs), "inside the 45 s gap");
            Assert.IsFalse(costs);

            s.Reset();
            Assert.IsTrue(s.TryTriggerScripted(2, out costs));
            Assert.IsFalse(costs, "after a reset the next scare is the introduction again");
        }

        [Test]
        public void Timeline_WithCosmeticIntro_PressureFiftyComesAtTwoHundredSeconds()
        {
            var m = new InsanityModel();
            for (int i = 0; i < 180; i++) m.Tick(1f);
            Assert.Less(m.Value, 50f, "at 3:00 the introduction scare adds nothing, so Pressure is only 45");
            for (int i = 0; i < 20; i++) m.Tick(1f);
            Assert.AreEqual(50f, m.Value, 0.01f);
        }
    }
}

namespace Game.Tests
{
    public class InstanceKeyTests
    {
        [Test]
        public void SameObject_SameKey_DifferentObjects_DifferentKeys()
        {
            var a = new object();
            var b = new object();
            Assert.AreEqual(InstanceKey.Of(a), InstanceKey.Of(a));
            Assert.AreNotEqual(InstanceKey.Of(a), InstanceKey.Of(b));
            Assert.AreNotEqual(0, InstanceKey.Of(a));
        }

        [Test]
        public void Null_IsZero()
        {
            Assert.AreEqual(0, InstanceKey.Of(null));
        }
    }
}
