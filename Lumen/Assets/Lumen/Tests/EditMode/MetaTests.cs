using System;
using System.Collections.Generic;
using Lumen.Bot;
using Lumen.Core;
using NUnit.Framework;
using UnityEngine;

namespace Lumen.Tests
{
    /// <summary>Meta progression rules. PlayerPrefs keys are backed up and restored around each test.</summary>
    public class MetaTests
    {
        static readonly string[] Keys = { "lumen.meta.cenizas", "lumen.meta.unlocks", "lumen.meta.challenges", "lumen.meta.character" };
        readonly Dictionary<string, string> backup = new Dictionary<string, string>();

        [SetUp]
        public void SetUp()
        {
            foreach (var k in Keys)
            {
                backup[k] = PlayerPrefs.HasKey(k) ? (k.EndsWith("cenizas") ? PlayerPrefs.GetInt(k).ToString() : PlayerPrefs.GetString(k)) : null;
                PlayerPrefs.DeleteKey(k);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var kv in backup)
            {
                if (kv.Value == null) PlayerPrefs.DeleteKey(kv.Key);
                else if (kv.Key.EndsWith("cenizas")) PlayerPrefs.SetInt(kv.Key, int.Parse(kv.Value));
                else PlayerPrefs.SetString(kv.Key, kv.Value);
            }
            PlayerPrefs.Save();
        }

        [Test]
        public void Cenizas_CannotGoNegative_AndUnlocksCostTheirPrice()
        {
            Meta.Earn(250);
            var brasa = Unlock.All[0];
            Assert.AreEqual("brasa", brasa.Id);
            Assert.IsFalse(Meta.TryUnlock(brasa), "300 is more than 250");
            Assert.AreEqual(250, Meta.Cenizas);
            Meta.Earn(100);
            Assert.IsTrue(Meta.TryUnlock(brasa));
            Assert.AreEqual(50, Meta.Cenizas);
            Assert.IsTrue(Meta.IsUnlocked("brasa"));
            Assert.IsFalse(Meta.TryUnlock(brasa), "already owned");
        }

        [Test]
        public void Lumen_IsAlwaysAvailable() => Assert.IsTrue(Meta.IsUnlocked("lumen"));

        [Test]
        public void Characters_ChangeTheRunNumbersOnly()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var rescoldo = new RunState(Difficulty.Antorcha, baseline, Character.Get("rescoldo"));
            Assert.AreEqual(baseline.maxFuel + 40f, rescoldo.Tuning.maxFuel);
            Assert.IsTrue(rescoldo.Tuning.lanternEachFloor);
            var fatuo = new RunState(Difficulty.Antorcha, baseline, Character.Get("fatuo"));
            Assert.IsTrue(fatuo.Tuning.mothsIgnoreDimmed && fatuo.Tuning.ignoreWebs);
            Assert.Less(fatuo.Tuning.drainDim, baseline.drainDim);
            Assert.AreEqual(100f, baseline.maxFuel, "the shared asset must never change");
            rescoldo.Dispose(); fatuo.Dispose();
            UnityEngine.Object.DestroyImmediate(baseline);
        }

        [Test]
        public void EveryCharacter_HasAllItsTexts()
        {
            foreach (var c in Character.All)
                foreach (var key in new[] { c.NameKey, c.QuoteKey, c.AbilityKey, c.DescKey })
                    Assert.IsTrue(Locale.Has(key), "missing text " + key);
            foreach (var u in Unlock.All) Assert.IsTrue(Locale.Has(u.NameKey) && Locale.Has(u.DescKey), "missing unlock text " + u.Id);
            foreach (var p in Provision.All) Assert.IsTrue(Locale.Has(p.NameKey) && Locale.Has(p.DescKey), "missing provision text " + p.Id);
            foreach (var ch in Challenge.All) Assert.IsTrue(Locale.Has(ch.NameKey) && Locale.Has(ch.DescKey), "missing challenge text " + ch.Id);
        }

        [Test]
        public void Challenges_PayOnce()
        {
            var run = new RunTelemetry.RunRecord { floorReached = 7, killsDevourer = 1, difficulty = "Antorcha" };
            var first = Meta.CompleteChallenges(run);
            CollectionAssert.IsNotEmpty(first);
            int paid = Meta.Cenizas;
            Assert.Greater(paid, 0);
            Assert.IsEmpty(Meta.CompleteChallenges(run), "a challenge pays only the first time");
            Assert.AreEqual(paid, Meta.Cenizas);
        }

        [Test]
        public void LockedBlessings_OnlyAppearOnceUnlocked()
        {
            for (int seed = 0; seed < 100; seed++)
                foreach (var b in Blessing.Offer(seed, 3, new List<string>()))
                    Assert.IsFalse(b.Locked, "locked blessing offered without unlock");
            bool seen = false;
            for (int seed = 0; seed < 300 && !seen; seed++)
                foreach (var b in Blessing.Offer(seed, 3, new List<string>(), id => true))
                    if (b.Locked) seen = true;
            Assert.IsTrue(seen, "unlocked blessings should eventually be offered");
        }

        [Test]
        public void DailySeed_IsTheSameAllDay()
        {
            var morning = new DateTime(2026, 10, 26, 1, 0, 0, DateTimeKind.Utc);
            var night = new DateTime(2026, 10, 26, 23, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(Meta.DailySeed(morning.Date), Meta.DailySeed(night.Date));
            Assert.AreNotEqual(Meta.DailySeed(morning.Date), Meta.DailySeed(morning.Date.AddDays(1)));
        }

        [Test]
        public void Provisions_ApplyToTheRun()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var run = new RunState(Difficulty.Antorcha, baseline);
            foreach (var p in Provision.All) p.Apply(run);
            Assert.IsTrue(run.Lantern);
            Assert.AreEqual(1, run.Oil);
            Assert.AreEqual(1, run.Lures);
            Assert.AreEqual(baseline.maxFuel + 20f, run.Tuning.maxFuel);
            Assert.IsTrue(run.StartWithBlessing);
            run.Dispose();
            UnityEngine.Object.DestroyImmediate(baseline);
        }

        [Test]
        public void ActiveItems_CycleOnlyThroughHeldOnes()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var run = new RunState(Difficulty.Antorcha, baseline) { LightBombs = 1 };
            Assert.IsTrue(run.TryConsumeActive(out var used));
            Assert.AreEqual(RunState.Active.LightBomb, used, "falls back to the item actually held");
            Assert.IsFalse(run.TryConsumeActive(out _));
            run.Dispose();
            UnityEngine.Object.DestroyImmediate(baseline);
        }
    }
}
