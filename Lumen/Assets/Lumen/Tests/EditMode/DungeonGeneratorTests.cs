using System.Collections.Generic;
using Lumen.Level;
using NUnit.Framework;
using UnityEngine;

namespace Lumen.Tests
{
    public class DungeonGeneratorTests
    {
        static IEnumerable<int> Floors() => new[] { 1, 2, 3, 5, 8, 12 };

        [Test]
        public void SameSeed_ProducesIdenticalMap()
        {
            var s = FloorSettings.ForFloor(3);
            var a = DungeonGenerator.Generate(42, s);
            var b = DungeonGenerator.Generate(42, s);
            for (int x = 0; x < a.Width; x++)
                for (int y = 0; y < a.Height; y++)
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y]);
            Assert.AreEqual(a.Exit, b.Exit);
        }

        [Test]
        public void EveryObjective_IsReachableFromSpawn([ValueSource(nameof(Floors))] int floor)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(floor));
                var dist = map.DistanceField(map.PlayerSpawn);
                Assert.Greater(dist[map.Exit.x, map.Exit.y], 0, $"exit unreachable (seed {seed})");
                foreach (var b in map.Braziers)
                    Assert.IsTrue(map.IsFloor(b) && dist[b.x, b.y] >= 0, $"brazier unreachable (seed {seed})");
                foreach (var e in map.Embers)
                    Assert.GreaterOrEqual(dist[e.x, e.y], 0, $"ember unreachable (seed {seed})");
            }
        }

        [Test]
        public void Enemies_NeverSpawnNearThePlayer([ValueSource(nameof(Floors))] int floor)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(floor));
                var dist = map.DistanceField(map.PlayerSpawn);
                foreach (var e in map.Enemies)
                    Assert.GreaterOrEqual(dist[e.Cell.x, e.Cell.y], DungeonGenerator.MinEnemySteps, $"enemy too close (seed {seed})");
            }
        }

        [Test]
        public void Embers_AreNeverWastedNextToABrazier([ValueSource(nameof(Floors))] int floor)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(floor));
                foreach (var e in map.Embers)
                    foreach (var b in map.Braziers)
                        Assert.GreaterOrEqual(Vector2Int.Distance(e, b), DungeonGenerator.MinEmberBrazierDistance,
                            $"ember next to a brazier (seed {seed})");
                Assert.Greater(map.Embers.Count, 0, $"no embers at all (seed {seed})");
            }
        }

        [Test]
        public void MapBorder_IsAlwaysSolid()
        {
            var map = DungeonGenerator.Generate(7, FloorSettings.ForFloor(6));
            for (int x = 0; x < map.Width; x++)
            {
                Assert.IsTrue(map.IsWall(x, 0));
                Assert.IsTrue(map.IsWall(x, map.Height - 1));
            }
            for (int y = 0; y < map.Height; y++)
            {
                Assert.IsTrue(map.IsWall(0, y));
                Assert.IsTrue(map.IsWall(map.Width - 1, y));
            }
        }

        [Test]
        public void Difficulty_ScalesWithFloor()
        {
            var early = FloorSettings.ForFloor(1);
            var late = FloorSettings.ForFloor(6);
            Assert.Greater(late.Stalkers, early.Stalkers);
            Assert.Greater(late.Braziers, early.Braziers);
            Assert.Less(late.Embers, early.Embers);
            Assert.AreEqual(0, early.Moths);
            Assert.AreEqual(0, early.Devourers);
        }

        [Test]
        public void Difficulty_KeepsRisingPastTheMapSizeCap()
        {
            // Map size caps around floor 7; pressure must keep growing after that.
            var f9 = FloorSettings.ForFloor(9);
            var f13 = FloorSettings.ForFloor(13);
            Assert.AreEqual(f9.Width, f13.Width, "map size should be capped");
            Assert.Greater(f13.Stalkers, f9.Stalkers);
            Assert.Greater(f13.Devourers, f9.Devourers);
            Assert.Greater(f13.EnemySpeedMultiplier, f9.EnemySpeedMultiplier);
            Assert.LessOrEqual(FloorSettings.ForFloor(99).EnemySpeedMultiplier, 1.3f);
        }

        [Test]
        public void FloorSeeds_AreDeterministicAndDistinct()
        {
            Assert.AreEqual(FloorSettings.SeedFor(1234, 3), FloorSettings.SeedFor(1234, 3));
            Assert.AreNotEqual(FloorSettings.SeedFor(1234, 3), FloorSettings.SeedFor(1234, 4));
            Assert.AreNotEqual(FloorSettings.SeedFor(1234, 3), FloorSettings.SeedFor(1235, 3));
        }
    }

    public class PathfinderTests
    {
        [Test]
        public void FindsPath_BetweenSpawnAndExit()
        {
            var map = DungeonGenerator.Generate(123, FloorSettings.ForFloor(4));
            var pf = new GridPathfinder(map);
            var path = new List<Vector2Int>();
            Assert.IsTrue(pf.FindPath(map.PlayerSpawn, map.Exit, path, 20000));
            Assert.AreEqual(map.Exit, path[path.Count - 1]);
            foreach (var c in path) Assert.IsTrue(map.IsFloor(c));
        }

        [Test]
        public void ReturnsFalse_ForWallGoal()
        {
            var map = DungeonGenerator.Generate(5, FloorSettings.ForFloor(1));
            var pf = new GridPathfinder(map);
            Assert.IsFalse(pf.FindPath(map.PlayerSpawn, Vector2Int.zero, new List<Vector2Int>()));
        }

        [Test]
        public void GreedyRects_CoverExactlyTheSolidCells()
        {
            var grid = new bool[6, 4];
            grid[0, 0] = grid[1, 0] = grid[0, 1] = grid[1, 1] = true;
            grid[4, 2] = grid[4, 3] = grid[5, 3] = true;
            var rects = LevelBuilder.GreedyRects(grid);
            var covered = new bool[6, 4];
            foreach (var r in rects)
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++)
                    {
                        Assert.IsFalse(covered[x, y], "rects overlap");
                        covered[x, y] = true;
                    }
            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 4; y++)
                    Assert.AreEqual(grid[x, y], covered[x, y]);
        }
    }
}
