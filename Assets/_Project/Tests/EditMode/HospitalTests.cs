using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Items;
using Game.Player;
using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class InsanityModelTests
    {
        [Test]
        public void Rises_AtConfiguredRate()
        {
            var m = new InsanityModel(ratePerSecond: 0.4f);
            m.Tick(10f);
            Assert.AreEqual(4f, m.Value, 0.001f);
        }

        [Test]
        public void DifficultyMultiplier_SpeedsItUp()
        {
            var m = new InsanityModel(ratePerSecond: 0.4f);
            m.Tick(10f, rateMultiplier: 1.5f);
            Assert.AreEqual(6f, m.Value, 0.001f);
        }

        [Test]
        public void Dose_LowersByRelief_AndNeverBelowZero()
        {
            var m = new InsanityModel(doseRelief: 40f);
            m.Add(30f);
            Assert.IsTrue(m.TakeDose());
            Assert.AreEqual(0f, m.Value, 0.001f);

            m.Add(70f);
            m.TakeDose();
            Assert.AreEqual(30f, m.Value, 0.001f);
        }

        [Test]
        public void Stage_FollowsThresholds()
        {
            var m = new InsanityModel();
            Assert.AreEqual(InsanityStage.Calm, m.Stage);
            m.Add(50f);
            Assert.AreEqual(InsanityStage.Uneasy, m.Stage);
            m.Add(25f);
            Assert.AreEqual(InsanityStage.Terrified, m.Stage);
            m.Add(25f);
            Assert.AreEqual(InsanityStage.Swelling, m.Stage);
        }

        [Test]
        public void Add_ClampsAtMax()
        {
            var m = new InsanityModel();
            m.Add(500f);
            Assert.AreEqual(100f, m.Value, 0.001f);
        }

        [Test]
        public void AtMax_SwellsThenBursts()
        {
            var m = new InsanityModel(swellDuration: 3f);
            m.Add(100f);
            Assert.IsFalse(m.Burst);

            m.Tick(2.9f);
            Assert.IsTrue(m.IsSwelling);
            Assert.IsFalse(m.Burst);
            Assert.Greater(m.SwellProgress, 0.9f);

            m.Tick(0.2f);
            Assert.IsTrue(m.Burst);
        }

        [Test]
        public void Dose_DuringSwell_SavesThePlayer()
        {
            var m = new InsanityModel(swellDuration: 3f, doseRelief: 40f);
            m.Add(100f);
            m.Tick(2f);
            m.TakeDose();

            Assert.IsFalse(m.IsSwelling);
            Assert.AreEqual(0f, m.SwellTimer, 0.001f);

            m.Tick(1f);
            Assert.IsFalse(m.Burst);
        }

        [Test]
        public void AfterBurst_NothingChanges()
        {
            var m = new InsanityModel(swellDuration: 1f);
            m.Add(100f);
            m.Tick(1.5f);
            Assert.IsTrue(m.Burst);

            Assert.IsFalse(m.TakeDose());
            m.Add(10f);
            m.Relieve(50f);
            Assert.AreEqual(100f, m.Value, 0.001f);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var m = new InsanityModel(swellDuration: 1f);
            m.Add(100f);
            m.Tick(2f);
            m.Reset();
            Assert.AreEqual(0f, m.Value);
            Assert.IsFalse(m.Burst);
            Assert.AreEqual(InsanityStage.Calm, m.Stage);
        }
    }

    public class CompounderTests
    {
        [Test]
        public void DosesFor_FloorsAndHandlesBadInput()
        {
            Assert.AreEqual(0, Compounder.DosesFor(499f, 500f));
            Assert.AreEqual(1, Compounder.DosesFor(500f, 500f));
            Assert.AreEqual(2, Compounder.DosesFor(1250f, 500f));
            Assert.AreEqual(0, Compounder.DosesFor(-10f, 500f));
            Assert.AreEqual(0, Compounder.DosesFor(1000f, 0f));
        }

        [Test]
        public void Quota_IsAFractionOfPossible_AndAtLeastOne()
        {
            // 6000 / 500 = 12 possible; 55 percent of that, floored, is 6.
            Assert.AreEqual(6, Compounder.QuotaDoses(6000f, 0.55f, 500f));
            Assert.AreEqual(1, Compounder.QuotaDoses(100f, 0.55f, 500f));
        }
    }

    public class MedicineLedgerTests
    {
        [Test]
        public void CommittedValue_BecomesDoses_AndTheRemainderCarriesOver()
        {
            var l = new MedicineLedger(quotaDoses: 3, valuePerDose: 500f);
            l.Commit(1250f);
            Assert.AreEqual(2, l.Produced);
            Assert.AreEqual(2, l.Banked);
            Assert.AreEqual(250f, l.Remainder, 0.001f);
            Assert.AreEqual(0.5f, l.RemainderFraction, 0.001f);

            l.Commit(250f);
            Assert.AreEqual(3, l.Produced);
            Assert.AreEqual(0f, l.Remainder, 0.001f);
        }

        [Test]
        public void SmallCommits_AddUpToADose()
        {
            var l = new MedicineLedger(1, 500f);
            l.Commit(300f);
            l.Commit(300f);
            Assert.AreEqual(1, l.Produced);
            Assert.AreEqual(100f, l.Remainder, 0.001f);
        }

        [Test]
        public void TakingADose_ReducesWhatTheTeamBanks()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(1000f);
            Assert.IsTrue(l.ExitUnlocked);

            Assert.IsTrue(l.TryConsume());
            Assert.AreEqual(1, l.Banked);
        }

        [Test]
        public void ExitStaysUnlocked_EvenAfterEveryDoseIsTaken()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(1000f);
            l.TryConsume();
            l.TryConsume();

            Assert.AreEqual(0, l.Banked);
            Assert.IsTrue(l.ExitUnlocked, "taking doses after the quota was reached must not lock the exit again");
            Assert.AreEqual(0, l.SurplusDoses);
        }

        [Test]
        public void ExitIsNotUnlocked_IfDosesWereTakenBeforeTheQuotaWasReached()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(500f);
            l.TryConsume();
            l.Commit(500f);
            Assert.AreEqual(1, l.Banked);
            Assert.IsFalse(l.ExitUnlocked);

            l.Commit(500f);
            Assert.AreEqual(2, l.Banked);
            Assert.IsTrue(l.ExitUnlocked);
        }

        [Test]
        public void SurplusDoses_AreWhatIsBankedBeyondTheQuota()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(1500f);
            Assert.AreEqual(1, l.SurplusDoses);

            var locked = new MedicineLedger(5, 500f);
            locked.Commit(1500f);
            Assert.AreEqual(0, locked.SurplusDoses, "no bonus before the exit unlocks");
        }

        [Test]
        public void Commit_IgnoresZeroNegativeAndInvalidAmounts()
        {
            var l = new MedicineLedger(2, 500f);
            Assert.IsFalse(l.Commit(0f));
            Assert.IsFalse(l.Commit(-5f));
            Assert.IsFalse(l.Commit(float.NaN));
            Assert.AreEqual(0f, l.CommittedValue);
        }

        [Test]
        public void CommittedValue_NeverDecreases()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(500f);
            l.Commit(-100f);
            Assert.AreEqual(500f, l.CommittedValue);
            Assert.AreEqual(1, l.Produced);
        }

        [Test]
        public void DosesAlreadyTaken_AreNeverUnmade()
        {
            var l = new MedicineLedger(3, 500f);
            l.Commit(1000f);
            l.TryConsume();
            l.Commit(10f);
            Assert.AreEqual(1, l.Banked);
            Assert.AreEqual(1, l.Consumed);
            Assert.AreEqual(2, l.Produced);
        }

        [Test]
        public void TryConsume_FailsWhenEmpty()
        {
            var l = new MedicineLedger(1, 500f);
            Assert.IsFalse(l.TryConsume());
            Assert.AreEqual(0, l.Consumed);
        }

        [Test]
        public void ZeroQuota_NeverUnlocksTheExit()
        {
            var l = new MedicineLedger(0, 500f);
            l.Commit(5000f);
            Assert.IsFalse(l.ExitUnlocked);
        }

        [Test]
        public void Reset_StartsFresh()
        {
            var l = new MedicineLedger(2, 500f);
            l.Commit(2000f);
            l.TryConsume();
            l.Reset(5, 250f);
            Assert.AreEqual(0, l.Consumed);
            Assert.AreEqual(0f, l.CommittedValue);
            Assert.IsFalse(l.ExitUnlocked);
            Assert.AreEqual(5, l.QuotaDoses);
            Assert.AreEqual(250f, l.ValuePerDose);
        }
    }

    public class SpeedModifiersTests
    {
        [Test]
        public void NoEntries_IsFullSpeed()
        {
            Assert.AreEqual(1f, new SpeedModifiers().Multiplier);
        }

        [Test]
        public void Entries_Multiply_AndCanBeRemoved()
        {
            var m = new SpeedModifiers();
            var a = new object();
            var b = new object();
            m.Set(a, 0.5f);
            m.Set(b, 0.8f);
            Assert.AreEqual(0.4f, m.Multiplier, 0.001f);

            m.Remove(a);
            Assert.AreEqual(0.8f, m.Multiplier, 0.001f);
        }

        [Test]
        public void SetAgain_ReplacesRatherThanStacks()
        {
            var m = new SpeedModifiers();
            var a = new object();
            m.Set(a, 0.5f);
            m.Set(a, 0.5f);
            Assert.AreEqual(0.5f, m.Multiplier, 0.001f);
            Assert.AreEqual(1, m.Count);
        }

        [Test]
        public void NeverFreezesThePlayer()
        {
            var m = new SpeedModifiers(floor: 0.1f);
            m.Set(new object(), 0f);
            Assert.AreEqual(0.1f, m.Multiplier, 0.001f);
        }

        [Test]
        public void NullOwner_IsIgnored()
        {
            var m = new SpeedModifiers();
            m.Set(null, 0.1f);
            Assert.AreEqual(0, m.Count);
            Assert.IsFalse(m.Remove(null));
        }
    }

    public class DifficultyProfileTests
    {
        [Test]
        public void High_IsHarderOnEveryAxis()
        {
            var normal = DifficultyProfile.For(DifficultyLevel.Normal);
            var high = DifficultyProfile.For(DifficultyLevel.High);

            Assert.IsFalse(normal.ApplesRot);
            Assert.IsTrue(high.ApplesRot);
            Assert.Greater(high.InsanityRateMultiplier, normal.InsanityRateMultiplier);
            Assert.Greater(high.ValuePerDoseMultiplier, normal.ValuePerDoseMultiplier);
            Assert.Greater(high.EnemyBudgetMultiplier, normal.EnemyBudgetMultiplier);
        }
    }

    public class RotModelTests
    {
        [Test]
        public void OnNormalDifficulty_NeverRots()
        {
            var r = new RotModel(rots: false, rotSeconds: 90f);
            r.Tick(10000f);
            Assert.AreEqual(1f, r.Freshness);
            Assert.AreEqual(1f, r.Effectiveness);
            Assert.IsFalse(r.IsRotten);
        }

        [Test]
        public void OnHighDifficulty_LosesFreshnessAndEffect()
        {
            var r = new RotModel(rots: true, rotSeconds: 90f, rottenThreshold: 0.15f);
            r.Tick(45f);
            Assert.AreEqual(0.5f, r.Freshness, 0.001f);
            Assert.AreEqual(0.5f, r.Effectiveness, 0.001f);
            Assert.IsFalse(r.IsRotten);

            r.Tick(45f);
            Assert.AreEqual(0f, r.Freshness, 0.001f);
            Assert.IsTrue(r.IsRotten);
            Assert.AreEqual(0f, r.Effectiveness);
        }

        [Test]
        public void RottenAtThreshold()
        {
            var r = new RotModel(rots: true, rotSeconds: 100f, rottenThreshold: 0.2f);
            r.Tick(79f);
            Assert.IsFalse(r.IsRotten);
            r.Tick(2f);
            Assert.IsTrue(r.IsRotten);
        }
    }

    public class EnemyHearingTests
    {
        [Test]
        public void Doctors_IgnoreMusic_ByDefault()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            Assert.AreEqual(0f, def.Hearing.Get(NoiseSource.Music));
            Assert.AreEqual(1f, def.Hearing.Get(NoiseSource.Footstep));
            Assert.AreEqual(1f, def.Hearing.Get(NoiseSource.Impact));
        }

        [Test]
        public void ZeroSensitivity_MeansNothingIsHeard()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            float sensitivity = def.HearingSensitivity * def.Hearing.Get(NoiseSource.Music);
            Assert.IsFalse(NoiseMath.CanHear(30f, 1f, sensitivity));
        }

        [Test]
        public void EveryNoiseSource_HasAMapping()
        {
            var h = new SourceHearing { Footstep = 2f, Sprint = 3f, Impact = 4f, Drop = 5f, Voice = 6f, Other = 7f, Music = 8f };
            Assert.AreEqual(2f, h.Get(NoiseSource.Footstep));
            Assert.AreEqual(3f, h.Get(NoiseSource.Sprint));
            Assert.AreEqual(4f, h.Get(NoiseSource.Impact));
            Assert.AreEqual(5f, h.Get(NoiseSource.Drop));
            Assert.AreEqual(6f, h.Get(NoiseSource.Voice));
            Assert.AreEqual(7f, h.Get(NoiseSource.Other));
            Assert.AreEqual(8f, h.Get(NoiseSource.Music));
        }
    }

    public class FleeMathTests
    {
        [Test]
        public void AwayPoint_IsOppositeTheThreat_AtTheRequestedDistance()
        {
            var p = EnemySenses.AwayPoint(new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f), 10f);
            Assert.AreEqual(-10f, p.x, 0.001f);
            Assert.AreEqual(0f, p.z, 0.001f);
        }

        [Test]
        public void AwayPoint_KeepsHeight_AndIgnoresVerticalOffset()
        {
            var p = EnemySenses.AwayPoint(new Vector3(0f, 2f, 0f), new Vector3(0f, 8f, 3f), 4f);
            Assert.AreEqual(2f, p.y, 0.001f);
            Assert.AreEqual(-4f, p.z, 0.001f);
        }

        [Test]
        public void AwayPoint_WhenOverlapping_StillPicksADirection()
        {
            var p = EnemySenses.AwayPoint(Vector3.zero, Vector3.zero, 5f);
            Assert.AreEqual(5f, (p - Vector3.zero).magnitude, 0.001f);
        }
    }

    public class DormantGrabberModelTests
    {
        private static DormantGrabberModel Make() => new DormantGrabberModel(wakeSeconds: 1.5f, struggleDecayPerSecond: 0.2f, cooldownSeconds: 10f);

        private static DormantGrabberModel Holding()
        {
            var m = Make();
            m.Wake();
            m.Tick(2f);
            m.Grab();
            return m;
        }

        [Test]
        public void StartsDormant_WakesOnlyFromDormant()
        {
            var m = Make();
            Assert.AreEqual(GrabberState.Dormant, m.State);
            Assert.IsTrue(m.Wake());
            Assert.AreEqual(GrabberState.Waking, m.State);
            Assert.IsFalse(m.Wake());
        }

        [Test]
        public void Waking_BecomesCrawling_AfterTheWakeTime()
        {
            var m = Make();
            m.Wake();
            m.Tick(1.4f);
            Assert.AreEqual(GrabberState.Waking, m.State);
            m.Tick(0.2f);
            Assert.AreEqual(GrabberState.Crawling, m.State);
        }

        [Test]
        public void CannotGrab_UnlessCrawling()
        {
            var m = Make();
            Assert.IsFalse(m.Grab());
            m.Wake();
            Assert.IsFalse(m.Grab());
            m.Tick(2f);
            Assert.IsTrue(m.Grab());
            Assert.IsTrue(m.IsHolding);
        }

        [Test]
        public void Struggling_AccumulatesAndEventuallyFreesThePlayer()
        {
            var m = Holding();
            m.Struggle(0.4f);
            Assert.AreEqual(0.4f, m.Progress, 0.001f);
            Assert.IsTrue(m.IsHolding);

            m.Struggle(0.4f);
            m.Struggle(0.4f);
            Assert.AreEqual(GrabberState.Cooldown, m.State);
            Assert.AreEqual(0f, m.Progress);
        }

        [Test]
        public void Progress_DecaysWhileHolding_SoYouMustKeepMashing()
        {
            var m = Holding();
            m.Struggle(0.5f);
            m.Tick(1f);
            Assert.AreEqual(0.3f, m.Progress, 0.001f);
            m.Tick(10f);
            Assert.AreEqual(0f, m.Progress, 0.001f);
        }

        [Test]
        public void Break_DrivesItOff_FromAnyActiveState()
        {
            var m = Holding();
            Assert.IsTrue(m.Break());
            Assert.AreEqual(GrabberState.Cooldown, m.State);

            var d = Make();
            Assert.IsFalse(d.Break(), "nothing to break while dormant");
            d.Wake();
            Assert.IsTrue(d.Break());
        }

        [Test]
        public void Cooldown_ReturnsToDormant_AndCannotBeWokenEarly()
        {
            var m = Holding();
            m.Break();
            Assert.IsFalse(m.Wake());

            m.Tick(9f);
            Assert.AreEqual(GrabberState.Cooldown, m.State);
            m.Tick(2f);
            Assert.AreEqual(GrabberState.Dormant, m.State);
            Assert.IsTrue(m.Wake());
        }
    }

    public class CrawlerSwarmModelTests
    {
        [Test]
        public void Add_NeverExceedsTheCoverageCap()
        {
            var swarm = new CrawlerSwarmModel(maxCoverage: 0.4f, coveragePerCrawler: 0.08f);
            int added = swarm.Add(20, new DeterministicRandom(1));

            Assert.AreEqual(5, added);
            Assert.LessOrEqual(swarm.Coverage, 0.4f + 0.001f);
            Assert.AreEqual(0, swarm.Add(1, new DeterministicRandom(1)));
        }

        [Test]
        public void Crawlers_ExpireAfterTheirLifetime()
        {
            var swarm = new CrawlerSwarmModel(maxLifetimeSeconds: 8f);
            swarm.Add(3, new DeterministicRandom(2));

            swarm.Tick(7.9f);
            Assert.AreEqual(3, swarm.Count);
            swarm.Tick(0.2f);
            Assert.AreEqual(0, swarm.Count);
        }

        [Test]
        public void Swat_RemovesOneAtATime_AndReportsEmpty()
        {
            var swarm = new CrawlerSwarmModel();
            swarm.Add(2, new DeterministicRandom(3));

            Assert.IsTrue(swarm.Swat());
            Assert.AreEqual(1, swarm.Count);
            Assert.IsTrue(swarm.Swat());
            Assert.IsFalse(swarm.Swat());
        }

        [Test]
        public void Positions_StayOnScreen_AndAreDeterministic()
        {
            var a = new CrawlerSwarmModel();
            var b = new CrawlerSwarmModel();
            a.Add(5, new DeterministicRandom(9));
            b.Add(5, new DeterministicRandom(9));

            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a.Crawlers[i].X, b.Crawlers[i].X);
                Assert.AreEqual(a.Crawlers[i].Y, b.Crawlers[i].Y);
                Assert.GreaterOrEqual(a.Crawlers[i].X, 0.1f);
                Assert.LessOrEqual(a.Crawlers[i].X, 0.9f);
                Assert.GreaterOrEqual(a.Crawlers[i].Y, 0.1f);
                Assert.LessOrEqual(a.Crawlers[i].Y, 0.9f);
            }
        }
    }

    public class JumpscareSchedulerTests
    {
        private static JumpscareScheduler Make(int seed = 1) =>
            new JumpscareScheduler(new DeterministicRandom(seed), hallucinationKinds: 4, minGapSeconds: 45f);

        [Test]
        public void ScriptedScare_FiresOncePerRun()
        {
            var s = Make();
            Assert.IsTrue(s.TryTriggerScripted(7));
            int id;
            s.TryHallucination(60f, 0f, out id);   // let the gap pass
            Assert.IsFalse(s.TryTriggerScripted(7));
            Assert.IsTrue(s.HasFired(7));
        }

        [Test]
        public void Gap_BlocksBackToBackScares()
        {
            var s = Make();
            Assert.IsTrue(s.TryTriggerScripted(1));
            Assert.IsFalse(s.TryTriggerScripted(2));

            int id;
            s.TryHallucination(46f, 0f, out id);
            Assert.IsTrue(s.TryTriggerScripted(2));
        }

        [Test]
        public void NoHallucinations_BelowTheStartInsanity()
        {
            var s = Make();
            for (int i = 0; i < 20000; i++)
            {
                int id;
                Assert.IsFalse(s.TryHallucination(0.1f, 40f, out id));
            }
        }

        [Test]
        public void HighInsanity_ProducesHallucinations_WithinTheGapLimit()
        {
            var s = Make(5);
            int fired = 0;
            for (int i = 0; i < 6000; i++)   // 600 seconds
            {
                int id;
                if (s.TryHallucination(0.1f, 100f, out id))
                {
                    fired++;
                    Assert.GreaterOrEqual(id, 0);
                    Assert.Less(id, 4);
                }
            }

            Assert.GreaterOrEqual(fired, 1);
            Assert.LessOrEqual(fired, 14, "45 second gap allows at most 600/45 + 1 scares");
        }

        [Test]
        public void SameSeed_SameHallucinations()
        {
            var a = Make(11);
            var b = Make(11);
            for (int i = 0; i < 3000; i++)
            {
                int ia, ib;
                bool fa = a.TryHallucination(0.1f, 90f, out ia);
                bool fb = b.TryHallucination(0.1f, 90f, out ib);
                Assert.AreEqual(fa, fb);
                Assert.AreEqual(ia, ib);
            }
        }

        [Test]
        public void Reset_ForgetsScriptedScares()
        {
            var s = Make();
            s.TryTriggerScripted(3);
            s.Reset();
            Assert.IsFalse(s.HasFired(3));
            Assert.IsTrue(s.TryTriggerScripted(3));
        }
    }
}
