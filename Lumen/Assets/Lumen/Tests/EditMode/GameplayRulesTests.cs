using Lumen.Core;
using Lumen.Level;
using Lumen.Player;
using NUnit.Framework;
using UnityEngine;

namespace Lumen.Tests
{
    public class FuelModelTests
    {
        static FuelModel Full() => new FuelModel(100f, 1.5f, 0.4f);

        [Test]
        public void Dimming_BurnsSlowerThanFullLight()
        {
            var bright = Full();
            var dim = Full();
            bright.Burn(10f, dimmed: false);
            dim.Burn(10f, dimmed: true);
            Assert.AreEqual(85f, bright.Value, 1e-4f);
            Assert.AreEqual(96f, dim.Value, 1e-4f);
        }

        [Test]
        public void Fuel_IsClampedBetweenZeroAndMax()
        {
            var f = Full();
            Assert.AreEqual(0f, f.Add(50f), "already full: nothing is added");
            f.Add(-500f);
            Assert.AreEqual(0f, f.Value);
            Assert.IsTrue(f.Empty);
        }

        [Test]
        public void Flare_CannotBePaidWithYourLastFuel()
        {
            var f = Full();
            f.Set(23f);
            Assert.IsFalse(f.TrySpend(22f), "would leave less than 1 fuel");
            Assert.AreEqual(23f, f.Value);
            f.Set(23.5f);
            Assert.IsTrue(f.TrySpend(22f));
            Assert.AreEqual(1.5f, f.Value, 1e-4f);
        }

        [Test]
        public void Refuel_IsLimitedByRateReserveAndCapacity()
        {
            var f = Full();
            f.Set(50f);
            Assert.AreEqual(16f, f.RefuelFrom(60f, 16f, 1f), 1e-4f, "limited by rate");
            Assert.AreEqual(5f, f.RefuelFrom(5f, 16f, 1f), 1e-4f, "limited by the brazier's reserve");
            f.Set(95f);
            Assert.AreEqual(5f, f.RefuelFrom(60f, 16f, 1f), 1e-4f, "limited by capacity");
            Assert.AreEqual(0f, f.RefuelFrom(0f, 16f, 1f), "a dry brazier gives nothing");
        }

        [Test]
        public void Brazier_ReserveRunsOut_SoCampingIsNotInfinite()
        {
            // Standing at a brazier while flaring: refuel 16/s from a 60 reserve vs. flares of 22 every 2.2 s.
            var t = ScriptableObject.CreateInstance<GameTuning>();
            var f = new FuelModel(t.maxFuel, t.drainBright, t.drainDim);
            float reserve = t.brazierReserve, cooldown = 0f;
            for (int i = 0; i < 600; i++) // 60 s at 10 Hz
            {
                const float dt = 0.1f;
                float got = f.RefuelFrom(reserve, t.brazierRefuelPerSecond, dt);
                reserve -= got;
                if (got <= 0f) f.Burn(dt, false);
                cooldown -= dt;
                if (cooldown <= 0f && f.TrySpend(t.flareCost)) cooldown = t.flareCooldown;
            }
            Assert.AreEqual(0f, reserve, 1e-3f, "the reserve must be exhausted");
            Assert.Less(f.Value, 50f, "flare spam at a brazier must not be sustainable");
            Object.DestroyImmediate(t);
        }

        [Test]
        public void FloorCarry_AddsBonusWithAMinimum()
        {
            Assert.AreEqual(35f, FuelModel.CarryToNextFloor(2f, 15f, 35f, 100f));
            Assert.AreEqual(75f, FuelModel.CarryToNextFloor(60f, 15f, 35f, 100f));
            Assert.AreEqual(100f, FuelModel.CarryToNextFloor(95f, 15f, 35f, 100f));
        }
    }

    public class ScoreServiceTests
    {
        GameTuning tuning;
        ScoreService score;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            tuning = ScriptableObject.CreateInstance<GameTuning>();
            score = new ScoreService(tuning) { Counting = true };
        }

        [TearDown]
        public void TearDown()
        {
            score.Dispose();
            Object.DestroyImmediate(tuning);
            GameEvents.ClearAll();
        }

        [Test]
        public void Events_AreConvertedToScore()
        {
            GameEvents.RaiseEmberCollected(Vector2.zero);
            GameEvents.RaiseBrazierLit(Vector2.zero);
            GameEvents.RaiseEnemyKilled(Vector2.zero, EnemyKind.Devourer);
            Assert.AreEqual(tuning.scoreEmber + tuning.scoreBrazier + tuning.scoreDevourer, score.Score);
        }

        [Test]
        public void NothingCounts_OutsideARun()
        {
            score.Counting = false;
            GameEvents.RaiseEnemyKilled(Vector2.zero, EnemyKind.Stalker);
            Assert.AreEqual(0, score.Score);
        }

        [Test]
        public void ScoreGained_IsRaisedForPopups()
        {
            int popped = 0;
            GameEvents.ScoreGained += (amount, _) => popped += amount;
            GameEvents.RaiseEnemyKilled(Vector2.zero, EnemyKind.Moth);
            Assert.AreEqual(tuning.scoreMoth, popped);
        }

        [Test]
        public void FloorBonus_RewardsBeatingPar()
        {
            float par = tuning.FloorParTime(3);
            Assert.AreEqual(tuning.scoreFloorBase * 3, score.FloorBonus(3, par + 100f), "over par: base only");
            Assert.AreEqual(tuning.scoreFloorBase * 3 + 10 * tuning.scorePerSecondUnderPar, score.FloorBonus(3, par - 10f));
        }

        [Test]
        public void Disposed_ServiceStopsListening()
        {
            score.Dispose();
            GameEvents.RaiseEmberCollected(Vector2.zero);
            Assert.AreEqual(0, score.Score);
        }
    }

    public class DarknessSpawnerTests
    {
        [Test]
        public void Interval_ShrinksOnDeepFloors_ButNeverBelowMinimum()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            var d = new DarknessSpawner(t);
            Assert.AreEqual(t.darknessSpawnInterval, d.IntervalFor(1));
            Assert.AreEqual(t.darknessSpawnInterval, d.IntervalFor(t.darknessRampFromFloor));
            Assert.Less(d.IntervalFor(t.darknessRampFromFloor + 3), t.darknessSpawnInterval);
            Assert.AreEqual(t.darknessSpawnMinInterval, d.IntervalFor(100));
            Object.DestroyImmediate(t);
        }

        [Test]
        public void Tick_FiresOncePerInterval()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            var d = new DarknessSpawner(t);
            int fired = 0;
            for (int i = 0; i < 1000; i++) if (d.Tick(0.1f, 1)) fired++; // 100 s
            Assert.AreEqual(Mathf.FloorToInt(100f / t.darknessSpawnInterval), fired);
            Object.DestroyImmediate(t);
        }
    }
}

namespace Lumen.Tests
{
    public class WoundGateTests
    {
        [Test]
        public void Devourer_CannotBeFlareLockedToDeath()
        {
            // Fire a flare every cooldown for 20 s, as a player mashing Space would.
            var t = ScriptableObject.CreateInstance<GameTuning>();
            var gate = new Lumen.Enemies.WoundGate(t.devourerStunTime + t.devourerWoundImmunity);
            float killedAt = -1f;
            for (float time = 0f; time < 20f; time += t.flareCooldown)
                if (gate.TryWound(time) && gate.Wounds == t.devourerFlaresToKill) { killedAt = time; break; }

            Assert.Greater(gate.Gap, t.flareCooldown + 1f, "wounds must be separated by more than one cooldown");
            Assert.GreaterOrEqual(killedAt, 2f * gate.Gap - t.flareCooldown,
                "three wounds need at least two full immunity windows");
            Object.DestroyImmediate(t);
        }

        [Test]
        public void IgnoredFlares_DoNotCount()
        {
            var gate = new Lumen.Enemies.WoundGate(5f);
            Assert.IsTrue(gate.TryWound(0f));
            Assert.IsFalse(gate.TryWound(2.2f));
            Assert.IsFalse(gate.TryWound(4.4f));
            Assert.IsTrue(gate.TryWound(5f));
            Assert.AreEqual(2, gate.Wounds);
        }
    }
}
