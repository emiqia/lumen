using System.Collections.Generic;
using Lumen.Core;
using Lumen.Level;
using NUnit.Framework;
using UnityEngine;

namespace Lumen.Tests
{
    /// <summary>Inventory, loot and stealth rules that used to live inside the GameManager.</summary>
    public class RunRewardsTests
    {
        readonly List<RunState> runs = new List<RunState>();
        GameTuning baseline;

        [SetUp]
        public void SetUp() => baseline = ScriptableObject.CreateInstance<GameTuning>();

        [TearDown]
        public void TearDown()
        {
            foreach (var r in runs) r.Dispose();
            runs.Clear();
            Object.DestroyImmediate(baseline);
        }

        RunState NewRun(string character = "lumen", bool daily = false)
        {
            var r = new RunState(Difficulty.Antorcha, baseline, Character.Get(character), daily);
            runs.Add(r);
            return r;
        }

        static bool NothingUnlocked(string _) => false;
        static bool AllUnlocked(string _) => true;

        [Test]
        public void Carry_RespectsEachSlotsLimit_AndNeverCarriesTheMap()
        {
            var run = NewRun();
            for (int i = 0; i < RunState.MaxOil; i++) Assert.IsTrue(run.TryCarry(ItemKind.Oil));
            Assert.IsFalse(run.TryCarry(ItemKind.Oil), "oil is capped");
            Assert.AreEqual(RunState.MaxOil, run.Oil);

            Assert.IsTrue(run.TryCarry(ItemKind.Lantern));
            Assert.IsFalse(run.TryCarry(ItemKind.Lantern), "only one lantern at a time");
            Assert.IsTrue(run.TryCarry(ItemKind.Key));
            Assert.IsTrue(run.TryCarry(ItemKind.Key), "a second key is harmless");
            Assert.IsFalse(run.TryCarry(ItemKind.Map), "the map is used on pickup, not carried");
        }

        [Test]
        public void NewFloor_DropsKeyAndCompass_AndRescoldoGetsALantern()
        {
            var run = NewRun();
            run.Keys = run.Compass = true;
            run.BeginFloor();
            Assert.IsFalse(run.Keys);
            Assert.IsFalse(run.Compass);
            Assert.IsFalse(run.Lantern);

            var rescoldo = NewRun("rescoldo");
            rescoldo.BeginFloor();
            Assert.IsTrue(rescoldo.Lantern);
        }

        [Test]
        public void Chest_SpendsTheKey_GivesLootAndCenizas()
        {
            var run = NewRun();
            run.Keys = true;
            // Always the first entry and the lowest roll: a lantern and the minimum Cenizas.
            var loot = RunRewards.OpenChest(run, (min, max) => min, NothingUnlocked);
            Assert.IsFalse(run.Keys);
            Assert.AreEqual(ItemKind.Lantern, loot.Item);
            Assert.IsTrue(run.Lantern);
            Assert.AreEqual(RunRewards.ChestCenizasMin, loot.Cenizas);
            Assert.AreEqual(RunRewards.ChestCenizasMin, run.CenizasBonus);
        }

        [Test]
        public void Chest_IsNeverEmpty_WhenItsItemCantBeCarried()
        {
            var run = NewRun();
            run.Lantern = true;
            run.Keys = true;
            RunRewards.OpenChest(run, (min, max) => min, NothingUnlocked);
            Assert.AreEqual(1, run.Oil, "a lantern you already hold turns into oil");
        }

        [Test]
        public void ChestAndFloorPools_GrowWithUnlocks_ButTheDailyRunUsesTheBaseSet()
        {
            Assert.AreEqual(2, RunRewards.ChestPool(NothingUnlocked).Count);
            Assert.AreEqual(5, RunRewards.ChestPool(AllUnlocked).Count);

            CollectionAssert.AreEquivalent(FloorSettings.BaseItems, RunRewards.ItemPool(NewRun(), NothingUnlocked));
            CollectionAssert.Contains(RunRewards.ItemPool(NewRun(), AllUnlocked), ItemKind.AshBoots);
            CollectionAssert.AreEquivalent(FloorSettings.BaseItems, RunRewards.ItemPool(NewRun(daily: true), AllUnlocked));
        }

        [Test]
        public void Guardian_GivesCenizasAndAFreeBlessing()
        {
            var run = NewRun(daily: true); // base blessing set, independent of the save file
            var reward = RunRewards.DefeatGuardian(run, 1234);
            Assert.AreEqual(run.Tuning.guardianCenizas, reward.Cenizas);
            Assert.AreEqual(run.Tuning.guardianCenizas, run.CenizasBonus);
            Assert.IsNotNull(reward.Gift);
            CollectionAssert.Contains(run.Blessings, reward.Gift.Id);
            Assert.AreEqual(Meta.CenizasFor(2000) + run.CenizasBonus, RunRewards.CenizasEarned(run, 2000));
        }

        [Test]
        public void Stealth_GrowsHalfAStepEveryTenSeconds_UpToX3_AndResetsWhenSeen()
        {
            var s = new StealthTracker();
            Assert.AreEqual(1f, s.Multiplier);
            s.Tick(StealthTracker.Step * 0.99f);
            Assert.AreEqual(1f, s.Multiplier);
            s.Tick(StealthTracker.Step * 0.02f);
            Assert.AreEqual(1.5f, s.Multiplier);
            s.Tick(StealthTracker.Step * 10f);
            Assert.AreEqual(StealthTracker.Max, s.Multiplier);

            s.Spotted();
            Assert.AreEqual(1f, s.Multiplier);
            Assert.IsTrue(s.SeenThisFloor);
            s.BeginFloor();
            Assert.IsFalse(s.SeenThisFloor);
        }
    }
}
