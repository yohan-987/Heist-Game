using Game.Core;
using Game.Items;
using Game.Player;
using NUnit.Framework;

namespace Game.Tests
{
    public class InsanityDefaultsTests
    {
        [Test]
        public void Defaults_MatchTheDesign()
        {
            var m = new InsanityModel();
            Assert.AreEqual(0.25f, m.RatePerSecond, 0.0001f);
            Assert.AreEqual(5f, m.SwellDuration, 0.0001f);
            Assert.AreEqual(40f, m.DoseRelief, 0.0001f);
        }

        [Test]
        public void Untreated_TakesFourHundredSecondsToFill_AndFirstUneasyAt200()
        {
            var m = new InsanityModel();
            for (int i = 0; i < 200; i++) m.Tick(1f);
            Assert.AreEqual(InsanityStage.Uneasy, m.Stage);
            Assert.AreEqual(50f, m.Value, 0.01f);

            for (int i = 0; i < 200; i++) m.Tick(1f);
            Assert.AreEqual(100f, m.Value, 0.01f);
            Assert.IsTrue(m.IsSwelling);
            Assert.IsFalse(m.Burst, "the swell warning has only just begun");
        }

        [Test]
        public void FiveSecondSwell_StillAllowsALastChanceDose()
        {
            var m = new InsanityModel();
            m.Add(100f);
            m.Tick(4.9f);
            Assert.IsFalse(m.Burst);

            m.TakeDose();
            m.Tick(1f);
            Assert.IsFalse(m.Burst);
            Assert.IsFalse(m.IsSwelling);
        }
    }

    public class DoseGateTests
    {
        private static DoseGate Gate() => new DoseGate(minPressure: 20f, cooldownSeconds: 10f);

        [Test]
        public void RefusesADose_WhenTooCalm()
        {
            Assert.AreEqual(DoseCheck.TooCalm, Gate().Check(19.9f, false, 999f));
            Assert.AreEqual(DoseCheck.Allowed, Gate().Check(20f, false, 999f));
        }

        [Test]
        public void RefusesADose_DuringTheCooldown()
        {
            Assert.AreEqual(DoseCheck.Cooldown, Gate().Check(60f, false, 9.9f));
            Assert.AreEqual(DoseCheck.Allowed, Gate().Check(60f, false, 10f));
        }

        [Test]
        public void TooCalm_IsReportedBeforeCooldown()
        {
            Assert.AreEqual(DoseCheck.TooCalm, Gate().Check(5f, false, 1f));
        }

        [Test]
        public void ASwellingPlayer_CanAlwaysTakeOne()
        {
            Assert.AreEqual(DoseCheck.Allowed, Gate().Check(100f, true, 0f));
        }
    }

    public class MusicReliefMixerTests
    {
        [Test]
        public void NoSources_NoRelief()
        {
            Assert.AreEqual(0f, new MusicReliefMixer().CurrentRate(0f));
        }

        [Test]
        public void OnlyTheStrongestSource_Counts_NeverTheSum()
        {
            var m = new MusicReliefMixer();
            m.Add(1, 0.3f, 10f);
            m.Add(2, 0.6f, 10f);
            m.Add(3, 0.5f, 10f);
            Assert.AreEqual(0.6f, m.CurrentRate(1f), 0.0001f);
        }

        [Test]
        public void ExpiredReports_AreIgnored_AndRemoved()
        {
            var m = new MusicReliefMixer();
            m.Add(1, 0.6f, 5f);
            Assert.AreEqual(0.6f, m.CurrentRate(4.9f), 0.0001f);
            Assert.AreEqual(0f, m.CurrentRate(5f));
            Assert.AreEqual(0, m.ActiveSources);
        }

        [Test]
        public void WhenTheStrongestFades_TheNextOneTakesOver()
        {
            var m = new MusicReliefMixer();
            m.Add(1, 0.6f, 5f);
            m.Add(2, 0.3f, 10f);
            Assert.AreEqual(0.3f, m.CurrentRate(6f), 0.0001f);
        }

        [Test]
        public void ANewReport_ReplacesTheOldOneFromTheSameSource()
        {
            var m = new MusicReliefMixer();
            m.Add(1, 0.6f, 5f);
            m.Add(1, 0.2f, 10f);
            Assert.AreEqual(0.2f, m.CurrentRate(6f), 0.0001f);
            Assert.AreEqual(1, m.ActiveSources);
        }
    }

    public class BatteryModelTests
    {
        [Test]
        public void Drains_AndReportsEmpty()
        {
            var b = new BatteryModel(40f);
            Assert.AreEqual(1f, b.Fraction);
            Assert.AreEqual(10f, b.Drain(10f));
            Assert.AreEqual(0.75f, b.Fraction, 0.0001f);
            Assert.IsFalse(b.IsEmpty);

            b.Drain(30f);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Drain_NearTheEnd_ReturnsOnlyWhatWasLeft()
        {
            var b = new BatteryModel(5f);
            Assert.AreEqual(5f, b.Drain(8f));
            Assert.AreEqual(0f, b.Drain(1f));
        }

        [Test]
        public void NegativeDrain_IsIgnored_AndRefillWorks()
        {
            var b = new BatteryModel(5f);
            Assert.AreEqual(0f, b.Drain(-3f));
            b.Drain(5f);
            b.Refill();
            Assert.AreEqual(5f, b.Remaining);
        }

        [Test]
        public void ARadio_RunsForFortySecondsAndGivesAboutSixTenthsOfADosePerPlayer()
        {
            const float reliefPerSecond = 0.6f;
            var b = new BatteryModel(40f);
            float gross = reliefPerSecond * b.Drain(100f);
            Assert.AreEqual(24f, gross, 0.001f);
            Assert.Less(gross, 40f, "one radio must be worth less than one dose per player");
        }
    }

    public class EconomyPlannerTests
    {
        [Test]
        public void Quota_GrowsWithPlayers()
        {
            Assert.AreEqual(6, EconomyPlanner.QuotaDoses(1));
            Assert.AreEqual(8, EconomyPlanner.QuotaDoses(2));
            Assert.AreEqual(10, EconomyPlanner.QuotaDoses(3));
            Assert.AreEqual(12, EconomyPlanner.QuotaDoses(4));
            Assert.AreEqual(6, EconomyPlanner.QuotaDoses(0), "zero players is treated as one");
        }

        [Test]
        public void SurvivalDoses_MatchTheDesignTable()
        {
            Assert.AreEqual(3, EconomyPlanner.SurvivalDoses(0.25f, 600f, 33f));
            Assert.AreEqual(4, EconomyPlanner.SurvivalDoses(0.3125f, 600f, 41.25f));
        }

        [Test]
        public void SurvivalDoses_ShortRunsNeedNone_AndBoundaryNeedsOneMore()
        {
            Assert.AreEqual(0, EconomyPlanner.SurvivalDoses(0.1f, 100f, 0f));
            // 0.25 * 560 = 140; 140 - 100 = 40 = exactly one dose, which would still leave 100: needs two.
            Assert.AreEqual(2, EconomyPlanner.SurvivalDoses(0.25f, 560f, 0f));
        }

        [Test]
        public void LongerRuns_NeedMoreDoses()
        {
            int ten = EconomyPlanner.SurvivalDoses(0.25f, 600f, 33f);
            int fourteen = EconomyPlanner.SurvivalDoses(0.25f, 840f, 33f);
            Assert.Greater(fourteen, ten);
            Assert.AreEqual(3, EconomyPlanner.SurvivalDoses(0.25f, 720f, 33f), "a 12 minute run still needs only 3");
        }

        private static EconomyPlan Plan(int players, DifficultyLevel level) =>
            EconomyPlanner.Plan(players, DifficultyProfile.For(level), 0.25f, 600f, 33f, 500f);

        [Test]
        public void NormalBudgets_MatchTheDesignTable()
        {
            Assert.AreEqual(6923f, Plan(1, DifficultyLevel.Normal).Budget, 5f);
            Assert.AreEqual(10769f, Plan(2, DifficultyLevel.Normal).Budget, 5f);
            Assert.AreEqual(14615f, Plan(3, DifficultyLevel.Normal).Budget, 5f);
            Assert.AreEqual(18462f, Plan(4, DifficultyLevel.Normal).Budget, 5f);
        }

        [Test]
        public void HighBudgets_MatchTheDesignTable()
        {
            Assert.AreEqual(10000f, Plan(1, DifficultyLevel.High).Budget, 5f);
            Assert.AreEqual(16000f, Plan(2, DifficultyLevel.High).Budget, 5f);
            Assert.AreEqual(22000f, Plan(3, DifficultyLevel.High).Budget, 5f);
            Assert.AreEqual(28000f, Plan(4, DifficultyLevel.High).Budget, 5f);
        }

        [Test]
        public void Plan_ReportsQuotaAndDosesPerPlayer()
        {
            var p = Plan(4, DifficultyLevel.Normal);
            Assert.AreEqual(12, p.QuotaDoses);
            Assert.AreEqual(3, p.DosesPerPlayer);
            Assert.AreEqual(4, p.Players);
            Assert.AreEqual(500f, p.ValuePerDose, 0.01f);
            Assert.AreEqual(600f, Plan(1, DifficultyLevel.High).ValuePerDose, 0.5f);
        }

        [Test]
        public void EveryPlayerCount_AndDifficulty_IsWinnableAtTheStatedEfficiency()
        {
            foreach (DifficultyLevel level in new[] { DifficultyLevel.Normal, DifficultyLevel.High })
            {
                float last = 0f;
                for (int players = 1; players <= 4; players++)
                {
                    var plan = Plan(players, level);
                    Assert.GreaterOrEqual(plan.Budget * plan.Efficiency + 0.5f, plan.MinimumValue,
                        $"{level} {players}p: delivering the expected share must cover quota and survival");
                    Assert.Greater(plan.Budget, plan.MinimumValue);
                    Assert.Greater(plan.Budget, last, "more players means more loot");
                    last = plan.Budget;
                }
            }
        }

        [Test]
        public void OldFixedBudget_WasNotEnoughForAFullTeam()
        {
            // The version 1 budget of 6000 could not pay for four players surviving a 10 minute run plus the quota.
            Assert.Less(6000f, Plan(4, DifficultyLevel.Normal).MinimumValue);
        }

        [Test]
        public void ZeroEfficiency_DoesNotDivideByZero()
        {
            float budget = EconomyPlanner.LootBudget(2, 8, 3, 500f, 0f);
            Assert.IsFalse(float.IsInfinity(budget) || float.IsNaN(budget));
        }

        [Test]
        public void HighDifficulty_IsHarderOnEveryEconomyAxis()
        {
            var n = DifficultyProfile.For(DifficultyLevel.Normal);
            var h = DifficultyProfile.For(DifficultyLevel.High);
            Assert.Greater(h.EventPressureMultiplier, n.EventPressureMultiplier);
            Assert.Less(h.CollectionEfficiency, n.CollectionEfficiency);
            Assert.Greater(h.ValuePerDoseMultiplier, n.ValuePerDoseMultiplier);
            Assert.Greater(h.InsanityRateMultiplier, n.InsanityRateMultiplier);
        }
    }
}
