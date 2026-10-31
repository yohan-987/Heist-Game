using System.Collections.Generic;
using Game.Economy;
using Game.Enemies;
using Game.Interaction;
using Game.Items;
using Game.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StaminaModelTests
    {
        private static StaminaModel Make() => new StaminaModel(100f, 20f, 10f, recoverDelay: 1f, exhaustRecoverFraction: 0.3f);

        [Test]
        public void Sprinting_DrainsStamina()
        {
            var s = Make();
            s.Tick(1f, sprinting: true);
            Assert.AreEqual(80f, s.Current, 0.001f);
        }

        [Test]
        public void Resting_RecoversOnlyAfterDelay()
        {
            var s = Make();
            s.Tick(1f, sprinting: true); // 80, delay timer reset
            s.Tick(0.5f, sprinting: false);
            Assert.AreEqual(80f, s.Current, 0.001f);

            s.Tick(1f, sprinting: false); // delay passed during this tick
            Assert.Greater(s.Current, 80f);
        }

        [Test]
        public void RunningDry_LocksSprint_UntilRecoveredPastThreshold()
        {
            var s = Make();
            s.Tick(10f, sprinting: true);
            Assert.AreEqual(0f, s.Current, 0.001f);
            Assert.IsTrue(s.Exhausted);
            Assert.IsFalse(s.CanSprint);

            s.Tick(1.1f, sprinting: false);   // past the delay, a little recovered
            Assert.IsTrue(s.Exhausted, "30% not reached yet");

            s.Tick(3f, sprinting: false);     // plenty
            Assert.IsFalse(s.Exhausted);
            Assert.IsTrue(s.CanSprint);
        }

        [Test]
        public void CarryDrain_AppliesWithoutSprinting()
        {
            var s = Make();
            s.Tick(1f, sprinting: false, extraDrain: 5f);
            Assert.AreEqual(95f, s.Current, 0.001f);
        }

        [Test]
        public void TrySpend_FailsWhenTooPoor_AndNeverGoesNegative()
        {
            var s = Make();
            Assert.IsTrue(s.TrySpend(60f));
            Assert.IsFalse(s.TrySpend(60f));
            Assert.AreEqual(40f, s.Current, 0.001f);
        }
    }

    public class HealthModelTests
    {
        [Test]
        public void Damage_KillsExactlyOnce()
        {
            var h = new HealthModel(50f);
            Assert.IsFalse(h.Damage(20f));
            Assert.IsTrue(h.Damage(40f));
            Assert.IsFalse(h.IsAlive);
            Assert.IsFalse(h.Damage(10f), "already dead");
            Assert.AreEqual(0f, h.Current);
        }

        [Test]
        public void Heal_CapsAtMax_AndDoesNotRevive()
        {
            var h = new HealthModel(50f);
            h.Damage(10f);
            h.Heal(100f);
            Assert.AreEqual(50f, h.Current);

            h.Damage(100f);
            h.Heal(10f);
            Assert.IsFalse(h.IsAlive);
        }
    }

    public class GrabMathTests
    {
        [Test]
        public void HoldVelocity_PointsAtTarget_AndIsClamped()
        {
            var v = GrabMath.HoldVelocity(Vector3.zero, new Vector3(1f, 0f, 0f), 10f, 14f);
            Assert.AreEqual(10f, v.magnitude, 0.001f);

            var far = GrabMath.HoldVelocity(Vector3.zero, new Vector3(100f, 0f, 0f), 10f, 14f);
            Assert.AreEqual(14f, far.magnitude, 0.001f);
        }

        [Test]
        public void MassSpeedMultiplier_LightIsFull_HeavyIsSlowest()
        {
            Assert.AreEqual(1f, GrabMath.MassSpeedMultiplier(1f, 2f, 25f, 0.4f), 0.001f);
            Assert.AreEqual(0.4f, GrabMath.MassSpeedMultiplier(25f, 2f, 25f, 0.4f), 0.001f);
            Assert.AreEqual(0.4f, GrabMath.MassSpeedMultiplier(500f, 2f, 25f, 0.4f), 0.001f);

            float mid = GrabMath.MassSpeedMultiplier(13.5f, 2f, 25f, 0.4f);
            Assert.Greater(mid, 0.4f);
            Assert.Less(mid, 1f);
        }

        [Test]
        public void HoldDistance_IsClamped()
        {
            Assert.AreEqual(3f, GrabMath.AdjustHoldDistance(2.9f, 5f, 0.25f, 0.9f, 3f), 0.001f);
            Assert.AreEqual(0.9f, GrabMath.AdjustHoldDistance(1f, -5f, 0.25f, 0.9f, 3f), 0.001f);
        }

        [Test]
        public void AutoRelease_OnlyBeyondLimit()
        {
            Assert.IsFalse(GrabMath.ShouldAutoRelease(2.0f, 2.2f));
            Assert.IsTrue(GrabMath.ShouldAutoRelease(2.5f, 2.2f));
        }

        [Test]
        public void ThrowSpeed_HeavierIsSlower_AndCapped()
        {
            float light = GrabMath.ThrowSpeed(1f, 11f, 16f);
            float heavy = GrabMath.ThrowSpeed(16f, 11f, 16f);
            Assert.Greater(light, heavy);
            Assert.LessOrEqual(light, 16f);
        }
    }

    public class ValueLossTests
    {
        [Test]
        public void BelowOrAtThreshold_CostsNothing()
        {
            Assert.AreEqual(0f, ValueLoss.Compute(5f, 6f, 0.01f, 1000f, 1000f));
            Assert.AreEqual(0f, ValueLoss.Compute(6f, 6f, 0.01f, 1000f, 1000f));
        }

        [Test]
        public void AboveThreshold_ScalesWithImpulse()
        {
            // 1000 * (16 - 6) * 0.01 = 100
            Assert.AreEqual(100f, ValueLoss.Compute(16f, 6f, 0.01f, 1000f, 1000f), 0.001f);
            Assert.AreEqual(200f, ValueLoss.Compute(26f, 6f, 0.01f, 1000f, 1000f), 0.001f);
        }

        [Test]
        public void Loss_NeverExceedsRemainingValue()
        {
            Assert.AreEqual(30f, ValueLoss.Compute(999f, 6f, 0.01f, 1000f, 30f), 0.001f);
            Assert.AreEqual(0f, ValueLoss.Compute(999f, 6f, 0.01f, 1000f, 0f));
        }
    }

    public class RunLedgerTests
    {
        [Test]
        public void QuotaMet_WhenDeliveredReachesQuota()
        {
            var l = new RunLedger(1000f);
            l.SetDelivered(999f);
            Assert.IsFalse(l.QuotaMet);
            l.SetDelivered(1000f);
            Assert.IsTrue(l.QuotaMet);
        }

        [Test]
        public void ZeroQuota_IsNeverMet()
        {
            var l = new RunLedger(0f);
            l.SetDelivered(500f);
            Assert.IsFalse(l.QuotaMet);
        }

        [Test]
        public void Delivered_ReplacesNotAccumulates_AndLostAccumulates()
        {
            var l = new RunLedger(1000f);
            l.SetDelivered(300f);
            l.SetDelivered(200f);
            l.AddLost(50f);
            l.AddLost(25f);
            l.AddLost(-5f);

            Assert.AreEqual(200f, l.Delivered);
            Assert.AreEqual(75f, l.Lost);
            Assert.AreEqual(0.2f, l.Progress, 0.001f);
        }

        [Test]
        public void Quota_GrowsPerLevel_AndRounds()
        {
            Assert.AreEqual(1000f, QuotaMath.ForLevel(1, 1000f, 1.25f));
            Assert.AreEqual(1250f, QuotaMath.ForLevel(2, 1000f, 1.25f));
            Assert.AreEqual(1550f, QuotaMath.ForLevel(3, 1000f, 1.25f)); // 1562.5 -> 1550
        }

        [Test]
        public void Quota_FromSpawnedValue_IsAFraction()
        {
            Assert.AreEqual(3300f, QuotaMath.FromSpawnedValue(6000f, 0.55f));
        }
    }

    public class UpgradeShopTests
    {
        private static UpgradeDefinition Def(string id, UpgradeStat stat, float cost = 500f, float growth = 2f, int max = 3, float perLevel = 10f)
        {
            var d = ScriptableObject.CreateInstance<UpgradeDefinition>();
            d.Id = id; d.Stat = stat; d.BaseCost = cost; d.CostGrowth = growth; d.MaxLevel = max; d.ValuePerLevel = perLevel;
            return d;
        }

        [Test]
        public void Cost_GrowsWithLevel()
        {
            Assert.AreEqual(500f, UpgradeShop.CostFor(500f, 2f, 0));
            Assert.AreEqual(1000f, UpgradeShop.CostFor(500f, 2f, 1));
            Assert.AreEqual(2000f, UpgradeShop.CostFor(500f, 2f, 2));
        }

        [Test]
        public void Buy_SpendsMoney_AndRaisesLevel()
        {
            var profile = new PlayerProfile { Money = 1600f };
            var hp = Def("hp", UpgradeStat.MaxHealth);

            Assert.IsTrue(UpgradeShop.TryBuy(profile, hp));   // -500
            Assert.IsTrue(UpgradeShop.TryBuy(profile, hp));   // -1000
            Assert.IsFalse(UpgradeShop.TryBuy(profile, hp));  // needs 2000, has 100

            Assert.AreEqual(2, profile.GetLevel("hp"));
            Assert.AreEqual(100f, profile.Money, 0.001f);
        }

        [Test]
        public void Buy_StopsAtMaxLevel()
        {
            var profile = new PlayerProfile { Money = 1e9f };
            var hp = Def("hp", UpgradeStat.MaxHealth, max: 2);

            Assert.IsTrue(UpgradeShop.TryBuy(profile, hp));
            Assert.IsTrue(UpgradeShop.TryBuy(profile, hp));
            Assert.IsFalse(UpgradeShop.TryBuy(profile, hp));
            Assert.AreEqual(2, profile.GetLevel("hp"));
        }

        [Test]
        public void TotalBonus_SumsOnlyMatchingStat()
        {
            var profile = new PlayerProfile();
            var hp = Def("hp", UpgradeStat.MaxHealth, perLevel: 10f);
            var sta = Def("sta", UpgradeStat.MaxStamina, perLevel: 7f);
            profile.SetLevel("hp", 3);
            profile.SetLevel("sta", 2);

            var all = new List<UpgradeDefinition> { hp, sta };
            Assert.AreEqual(30f, UpgradeShop.TotalBonus(profile, all, UpgradeStat.MaxHealth), 0.001f);
            Assert.AreEqual(14f, UpgradeShop.TotalBonus(profile, all, UpgradeStat.MaxStamina), 0.001f);
            Assert.AreEqual(0f, UpgradeShop.TotalBonus(profile, all, UpgradeStat.SprintSpeed), 0.001f);
        }

        [Test]
        public void Profile_SetLevel_UpdatesExistingEntry()
        {
            var profile = new PlayerProfile();
            profile.SetLevel("hp", 1);
            profile.SetLevel("hp", 4);
            Assert.AreEqual(1, profile.Upgrades.Count);
            Assert.AreEqual(4, profile.GetLevel("hp"));
            Assert.AreEqual(0, profile.GetLevel("missing"));
        }
    }

    public class EnemySensesTests
    {
        [Test]
        public void VisionCone_RespectsRangeAndAngle()
        {
            var origin = Vector3.zero;
            var forward = new Vector3(0f, 0f, 1f);

            Assert.IsTrue(EnemySenses.InVisionCone(origin, forward, new Vector3(0f, 0f, 5f), 10f, 55f));
            Assert.IsFalse(EnemySenses.InVisionCone(origin, forward, new Vector3(0f, 0f, 15f), 10f, 55f), "too far");
            Assert.IsFalse(EnemySenses.InVisionCone(origin, forward, new Vector3(0f, 0f, -5f), 10f, 55f), "behind");
            Assert.IsFalse(EnemySenses.InVisionCone(origin, forward, new Vector3(5f, 0f, 1f), 10f, 55f), "too far off-axis");
        }

        [Test]
        public void NoiseReaction_NeedsAudibleAndAboveThreshold()
        {
            Assert.IsFalse(EnemySenses.ShouldReactToNoise(0f, 0.1f));
            Assert.IsFalse(EnemySenses.ShouldReactToNoise(0.05f, 0.1f));
            Assert.IsTrue(EnemySenses.ShouldReactToNoise(0.4f, 0.1f));
        }
    }
}
