using System.Collections.Generic;
using Lumen.Core;
using Lumen.Level;
using NUnit.Framework;
using UnityEngine;

namespace Lumen.Tests
{
    public class DifficultyTests
    {
        [Test]
        public void Antorcha_IsTheUntouchedReference()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var run = new RunState(Difficulty.Antorcha, baseline);
            Assert.AreEqual(baseline.drainBright, run.Tuning.drainBright);
            Assert.AreEqual(baseline.stalkerChaseSpeed, run.Tuning.stalkerChaseSpeed);
            Assert.AreEqual(baseline.devourerDamage, run.Tuning.devourerDamage);
            run.Dispose();
            Object.DestroyImmediate(baseline);
        }

        [Test]
        public void Difficulties_AreOrderedFromEasyToNightmare()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            float prevDrain = 0f, prevSpeed = 0f, prevReserve = float.MaxValue;
            foreach (var d in DifficultyProfile.All)
            {
                var run = new RunState(d, baseline);
                Assert.Greater(run.Tuning.drainBright, prevDrain, d + " should drain more than the previous level");
                Assert.Greater(run.Tuning.stalkerChaseSpeed, prevSpeed, d + " should be faster");
                Assert.Less(run.Tuning.brazierReserve, prevReserve, d + " should have stingier braziers");
                prevDrain = run.Tuning.drainBright; prevSpeed = run.Tuning.stalkerChaseSpeed; prevReserve = run.Tuning.brazierReserve;
                run.Dispose();
            }
            Object.DestroyImmediate(baseline);
        }

        [Test]
        public void Ceniza_DevourerKillsInOneHit()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var run = new RunState(Difficulty.Ceniza, baseline);
            Assert.GreaterOrEqual(run.Tuning.devourerDamage, baseline.maxFuel);
            run.Dispose();
            Object.DestroyImmediate(baseline);
        }

        [Test]
        public void RunTuning_NeverModifiesTheSharedAsset()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            float before = baseline.drainBright;
            var run = new RunState(Difficulty.Ceniza, baseline);
            run.Take(Blessing.All[0]);
            Assert.AreEqual(before, baseline.drainBright);
            Assert.AreEqual(100f, baseline.maxFuel);
            run.Dispose();
            Object.DestroyImmediate(baseline);
        }
    }

    public class BlessingTests
    {
        [Test]
        public void Offer_IsDeterministicAndDistinct()
        {
            var a = Blessing.Offer(77, 3, new List<string>());
            var b = Blessing.Offer(77, 3, new List<string>());
            Assert.AreEqual(3, a.Count);
            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AllItemsAreUnique(a);
        }

        [Test]
        public void NonStackableBlessings_AreNeverOfferedTwice()
        {
            var taken = new List<string> { "phoenix" };
            for (int seed = 0; seed < 200; seed++)
                foreach (var b in Blessing.Offer(seed, 3, taken))
                    Assert.AreNotEqual("phoenix", b.Id);
        }

        [Test]
        public void TakingABlessing_ChangesTheRunNumbers()
        {
            var baseline = ScriptableObject.CreateInstance<GameTuning>();
            var run = new RunState(Difficulty.Antorcha, baseline);
            float cost = run.Tuning.flareCost;
            foreach (var b in Blessing.All) if (b.Id == "cheapflare") run.Take(b);
            Assert.Less(run.Tuning.flareCost, cost);
            CollectionAssert.Contains(run.Blessings, "cheapflare");
            run.Dispose();
            Object.DestroyImmediate(baseline);
        }

        [Test]
        public void EveryBlessing_HasTextInBothLanguages()
        {
            foreach (var b in Blessing.All)
            {
                Assert.AreNotEqual(b.NameKey.ToUpperInvariant(), Locale.Get(b.NameKey), "missing name for " + b.Id);
                Assert.AreNotEqual(b.DescKey.ToUpperInvariant(), Locale.Get(b.DescKey), "missing description for " + b.Id);
            }
        }
    }

    public class BiomeTests
    {
        [Test]
        public void Biomes_CycleEveryThreeFloors()
        {
            Assert.AreEqual(Biome.Crypts, FloorSettings.BiomeFor(1));
            Assert.AreEqual(Biome.Crypts, FloorSettings.BiomeFor(3));
            Assert.AreEqual(Biome.Flooded, FloorSettings.BiomeFor(4));
            Assert.AreEqual(Biome.Roots, FloorSettings.BiomeFor(7));
            Assert.AreEqual(Biome.Crypts, FloorSettings.BiomeFor(10));
        }

        [Test]
        public void FloorOne_HasNoHazards()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(1));
                Assert.AreEqual(0, map.Doors.Count);
                Assert.AreEqual(0, map.Wind.Count);
                foreach (var w in map.Water) Assert.IsFalse(w);
            }
        }

        [Test]
        public void EachBiome_GetsOnlyItsOwnHazard()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var crypt = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(3));
                var flooded = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(5));
                var roots = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(8));
                Assert.AreEqual(0, crypt.Wind.Count); Assert.IsFalse(AnyWater(crypt));
                Assert.AreEqual(0, flooded.Doors.Count); Assert.AreEqual(0, flooded.Wind.Count); Assert.IsTrue(AnyWater(flooded), "flooded floor without water (seed " + seed + ")");
                Assert.AreEqual(0, roots.Doors.Count); Assert.IsFalse(AnyWater(roots));
            }
        }

        [Test]
        public void Water_NeverCoversObjectives([ValueSource(nameof(FloodedFloors))] int floor)
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(floor));
                Assert.IsFalse(map.IsWater(map.PlayerSpawn));
                Assert.IsFalse(map.IsWater(map.Exit));
                foreach (var b in map.Braziers) Assert.IsFalse(map.IsWater(b));
            }
        }

        static IEnumerable<int> FloodedFloors() => new[] { 4, 5, 6 };
        static bool AnyWater(DungeonMap map) { foreach (var w in map.Water) if (w) return true; return false; }

        [Test]
        public void Items_AreReachable()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(4));
                var dist = map.DistanceField(map.PlayerSpawn);
                foreach (var it in map.Items) Assert.GreaterOrEqual(dist[it.Cell.x, it.Cell.y], 0, "unreachable item (seed " + seed + ")");
            }
        }

        [Test]
        public void FloorNames_AreStablePerSeed()
        {
            Assert.AreEqual(FloorNames.Title(5, 1234), FloorNames.Title(5, 1234));
            StringAssert.DoesNotContain("FLOOR.", FloorNames.Title(8, 99));
            StringAssert.DoesNotContain("FLOOR.", FloorNames.Subtitle(11));
        }
    }
}
