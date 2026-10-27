using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Level
{
    /// <summary>
    /// Difficulty-dependent generation parameters. Built from the floor number so the
    /// whole difficulty curve lives in one place.
    /// </summary>
    [Serializable]
    public struct FloorSettings
    {
        public int Width, Height;
        public int RoomCount;
        public int MinRoomSize, MaxRoomSize;
        public int Braziers;
        public int Embers;
        public int Stalkers, Moths, Devourers;
        public int Mimics, Weavers, Watchers;
        public bool Guardian;             // boss floor: a Guardian waits in the exit room
        public float EnemySpeedMultiplier;
        public float ExtraLoopChance;
        public Biome Biome;
        public int Items;
        /// <summary>Kinds that may spawn (base items plus the ones unlocked with Cenizas).</summary>
        public ItemKind[] ItemPool;
        public bool Chest;                // a locked chest in one room, its key somewhere else
        public int Doors, WaterPools, WindZones; // the active biome's hazard

        /// <summary>
        /// The whole difficulty curve. Map size and objectives cap early (so floors stay
        /// navigable); enemy counts keep growing longer, and past floor 8 enemies speed up,
        /// so the late game keeps tightening instead of plateauing.
        /// </summary>
        public static FloorSettings ForFloor(int floor)
        {
            floor = Mathf.Max(1, floor);
            return new FloorSettings
            {
                Width = Mathf.Min(40 + floor * 4, 68),
                Height = Mathf.Min(30 + floor * 3, 50),
                RoomCount = Mathf.Min(5 + floor, 12),
                MinRoomSize = 5,
                MaxRoomSize = 10,
                Braziers = Mathf.Min(2 + (floor + 1) / 2, 7),
                Embers = Mathf.Max(13 - floor, 6),
                Stalkers = Mathf.Min(2 + floor * 2 / 3, 14),
                Moths = floor >= 2 ? Mathf.Min(floor / 2, 8) : 0,
                Devourers = floor >= 3 && floor % 5 != 0 ? 1 + (floor - 3) / 4 : 0, // the Guardian floor is its own fight
                Mimics = floor >= 2 ? Mathf.Min(1 + (floor - 2) / 2, 4) : 0,
                Weavers = floor >= 4 ? Mathf.Min(1 + (floor - 4) / 3 + (BiomeFor(floor) == Biome.Roots ? 1 : 0), 4) : 0,
                Watchers = floor >= 4 ? Mathf.Min(1 + (floor - 4) / 3, 3) : 0,
                Guardian = floor % 5 == 0,
                EnemySpeedMultiplier = Mathf.Min(1f + 0.03f * Mathf.Max(0, floor - 8), 1.3f),
                ExtraLoopChance = 0.35f,
                Biome = BiomeFor(floor),
                Items = Mathf.Min(1 + floor / 3, 3),
                ItemPool = BaseItems,
                Chest = floor >= 2,
                // Hazards ramp inside each biome's three floors; floor 1 stays clean.
                Doors = BiomeFor(floor) == Biome.Crypts && floor >= 2 ? Mathf.Min(1 + (floor - 1) % 3 + floor / 9, 5) : 0,
                WaterPools = BiomeFor(floor) == Biome.Flooded ? Mathf.Min(3 + (floor - 1) % 3 * 2 + floor / 9, 9) : 0,
                WindZones = BiomeFor(floor) == Biome.Roots ? Mathf.Min(2 + (floor - 1) % 3 + floor / 9, 6) : 0,
            };
        }

        public static readonly ItemKind[] BaseItems = { ItemKind.Lantern, ItemKind.Lure, ItemKind.Oil, ItemKind.Map };

        /// <summary>Crypts (1-3), Flooded caverns (4-6), Roots (7-9), then the cycle repeats harder.</summary>
        public static Biome BiomeFor(int floor) => (Biome)((Mathf.Max(1, floor) - 1) / 3 % 3);

        /// <summary>Each floor of a run gets its own deterministic seed, so whole runs are reproducible.</summary>
        public static int SeedFor(int runSeed, int floor) => unchecked(runSeed * 7919 + floor * 104729);
    }

    /// <summary>
    /// Rooms-and-corridors generator. Deterministic for a given seed.
    /// Steps: scatter rooms -> connect with a minimum spanning tree (+ a few loops so the
    /// player can circle around enemies) -> carve 2-wide corridors -> place objectives.
    /// </summary>
    public static class DungeonGenerator
    {
        const int Margin = 2;      // solid border around the map
        const int RoomPadding = 2; // keeps walls at least 2 tiles thick between rooms
        /// <summary>Enemies never spawn closer than this many walking steps to the player.</summary>
        public const int MinEnemySteps = 16;
        /// <summary>Embers stay away from braziers: next to one they'd be wasted (you refuel there anyway).</summary>
        public const float MinEmberBrazierDistance = 6f;

        public static DungeonMap Generate(int seed, FloorSettings s)
        {
            var rng = new System.Random(seed);
            var map = new DungeonMap(s.Width, s.Height);

            PlaceRooms(map, s, rng);
            if (map.Rooms.Count == 0) // pathological seed: guarantee a playable floor
                map.Rooms.Add(new RectInt(map.Width / 2 - 4, map.Height / 2 - 4, 8, 8));
            foreach (var r in map.Rooms) Carve(map, r);
            ConnectRooms(map, s, rng);

            PlaceObjectives(map, s, rng);
            map.Biome = s.Biome;
            PlaceHazards(map, s, rng);
            return map;
        }

        static void PlaceRooms(DungeonMap map, FloorSettings s, System.Random rng)
        {
            int attempts = s.RoomCount * 40;
            while (map.Rooms.Count < s.RoomCount && attempts-- > 0)
            {
                int w = rng.Next(s.MinRoomSize, s.MaxRoomSize + 1);
                int h = rng.Next(s.MinRoomSize, s.MaxRoomSize + 1);
                int x = rng.Next(Margin, map.Width - w - Margin);
                int y = rng.Next(Margin, map.Height - h - Margin);
                var room = new RectInt(x, y, w, h);
                var padded = new RectInt(x - RoomPadding, y - RoomPadding, w + RoomPadding * 2, h + RoomPadding * 2);

                bool overlaps = false;
                foreach (var other in map.Rooms)
                    if (padded.Overlaps(other)) { overlaps = true; break; }
                if (!overlaps) map.Rooms.Add(room);
            }
        }

        static void ConnectRooms(DungeonMap map, FloorSettings s, System.Random rng)
        {
            int n = map.Rooms.Count;
            if (n < 2) return;

            // Prim's MST over room centers (n is small, O(n^2) is fine).
            var inTree = new bool[n];
            inTree[0] = true;
            var edges = new List<(int a, int b)>();
            for (int added = 1; added < n; added++)
            {
                int bestA = -1, bestB = -1;
                float best = float.MaxValue;
                for (int a = 0; a < n; a++)
                {
                    if (!inTree[a]) continue;
                    for (int b = 0; b < n; b++)
                    {
                        if (inTree[b]) continue;
                        float d = Vector2.Distance(map.Rooms[a].center, map.Rooms[b].center);
                        if (d < best) { best = d; bestA = a; bestB = b; }
                    }
                }
                inTree[bestB] = true;
                edges.Add((bestA, bestB));
            }

            // Extra loops: connect each room to its second-nearest neighbour sometimes.
            for (int a = 0; a < n; a++)
            {
                if (rng.NextDouble() > s.ExtraLoopChance) continue;
                int nearest = -1, second = -1;
                float d1 = float.MaxValue, d2 = float.MaxValue;
                for (int b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    float d = Vector2.Distance(map.Rooms[a].center, map.Rooms[b].center);
                    if (d < d1) { d2 = d1; second = nearest; d1 = d; nearest = b; }
                    else if (d < d2) { d2 = d; second = b; }
                }
                if (second >= 0) edges.Add((a, second));
            }

            foreach (var (a, b) in edges)
                CarveCorridor(map, Center(map.Rooms[a]), Center(map.Rooms[b]), rng.Next(2) == 0);
        }

        static Vector2Int Center(RectInt r) => new Vector2Int(r.x + r.width / 2, r.y + r.height / 2);

        static void CarveCorridor(DungeonMap map, Vector2Int from, Vector2Int to, bool horizontalFirst)
        {
            var corner = horizontalFirst ? new Vector2Int(to.x, from.y) : new Vector2Int(from.x, to.y);
            CarveLine(map, from, corner);
            CarveLine(map, corner, to);
        }

        static void CarveLine(DungeonMap map, Vector2Int a, Vector2Int b)
        {
            var step = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
            var c = a;
            while (true)
            {
                // 2x2 brush -> corridors are two tiles wide, comfortable for 16px characters.
                for (int dx = 0; dx < 2; dx++)
                    for (int dy = 0; dy < 2; dy++)
                        SetFloor(map, c.x + dx, c.y + dy);
                if (c == b) break;
                c += step;
            }
        }

        static void Carve(DungeonMap map, RectInt r)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    SetFloor(map, x, y);
        }

        static void SetFloor(DungeonMap map, int x, int y)
        {
            if (x >= Margin - 1 && y >= Margin - 1 && x < map.Width - Margin + 1 && y < map.Height - Margin + 1)
                map.Tiles[x, y] = Tile.Floor;
        }

        static void PlaceObjectives(DungeonMap map, FloorSettings s, System.Random rng)
        {
            var start = map.Rooms[0];
            map.PlayerSpawn = Center(start);
            var dist = map.DistanceField(map.PlayerSpawn);

            // Exit: in the room whose center is farthest (by walking distance) from the start.
            // On boss floors only big rooms qualify, so there is space to fight the Guardian.
            int exitRoom = 0, far = -1;
            for (int pass = 0; pass < 3 && far < 0; pass++)
                for (int i = 1; i < map.Rooms.Count; i++)
                {
                    var r = map.Rooms[i];
                    var rc = Center(r);
                    if (s.Guardian && pass == 0 && (r.width * r.height < 56 || dist[rc.x, rc.y] < MinEnemySteps + 6)) continue;
                    if (s.Guardian && pass == 1 && r.width * r.height < 56) continue;
                    var c = Center(r);
                    if (dist[c.x, c.y] > far) { far = dist[c.x, c.y]; exitRoom = i; }
                }
            map.Exit = Center(map.Rooms[exitRoom]);

            var used = new HashSet<Vector2Int> { map.PlayerSpawn, map.Exit };

            // Braziers: one per room (never the start room), preferring rooms far away.
            var candidateRooms = new List<int>();
            for (int i = 1; i < map.Rooms.Count; i++) candidateRooms.Add(i);
            Shuffle(candidateRooms, rng);
            int braziers = Mathf.Min(s.Braziers, candidateRooms.Count);
            for (int i = 0; i < braziers; i++)
            {
                var r = map.Rooms[candidateRooms[i]];
                // Random cell inside the room (one tile from its walls), never on top of the exit.
                var cell = new Vector2Int(r.x + 1 + rng.Next(Mathf.Max(1, r.width - 2)), r.y + 1 + rng.Next(Mathf.Max(1, r.height - 2)));
                if (used.Contains(cell) || Vector2Int.Distance(cell, map.Exit) < 2f)
                    cell = new Vector2Int(r.x + 1, r.yMax - 2);
                if (used.Contains(cell) || Vector2Int.Distance(cell, map.Exit) < 2f) continue;
                map.Braziers.Add(cell);
                used.Add(cell);
            }

            // Embers: anywhere reachable, not right next to the start, and away from braziers, so
            // they bridge the dark stretches between fires instead of piling up where you refuel.
            var floor = new List<Vector2Int>();
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    if (dist[x, y] > 4) floor.Add(new Vector2Int(x, y));
            Shuffle(floor, rng);
            int idx = 0;
            for (int i = 0; i < s.Embers && idx < floor.Count; idx++)
            {
                if (used.Contains(floor[idx]) || NearAnyBrazier(map, floor[idx])) continue;
                map.Embers.Add(floor[idx]);
                used.Add(floor[idx]);
                i++;
            }

            // Enemies: only in cells far enough away that the opening is safe.
            var enemyCells = new List<Vector2Int>();
            foreach (var c in floor)
                if (dist[c.x, c.y] >= MinEnemySteps && !used.Contains(c)) enemyCells.Add(c);
            Shuffle(enemyCells, rng);
            int e = 0;
            void Spawn(EnemyKind kind, int count)
            {
                for (int i = 0; i < count && e < enemyCells.Count; i++, e++)
                    map.Enemies.Add(new EnemySpawn(kind, enemyCells[e]));
            }
            Spawn(EnemyKind.Stalker, s.Stalkers);
            Spawn(EnemyKind.Moth, s.Moths);
            Spawn(EnemyKind.Devourer, s.Devourers);
            Spawn(EnemyKind.Mimic, s.Mimics);
            Spawn(EnemyKind.Weaver, s.Weavers);

            // Watchers sit against a wall (the south face of a wall above them), far from the start.
            int watchers = 0;
            foreach (var c in enemyCells)
            {
                if (watchers >= s.Watchers) break;
                if (!map.IsWall(c.x, c.y + 1) || used.Contains(c)) continue;
                bool taken = false;
                foreach (var e2 in map.Enemies) if (e2.Cell == c) taken = true;
                if (taken) continue;
                map.Enemies.Add(new EnemySpawn(EnemyKind.Watcher, c));
                used.Add(c);
                watchers++;
            }

            // The Guardian guards the exit itself.
            // Never closer than any other enemy may be to the start; if the exit is too close for
            // that, the floor goes without its boss rather than ambushing the player at spawn.
            if (s.Guardian)
            {
                Vector2Int? spot = null;
                foreach (var offset in new[] { Vector2Int.up * 2, Vector2Int.down * 2, Vector2Int.left * 2, Vector2Int.right * 2, Vector2Int.zero })
                {
                    var g = map.Exit + offset;
                    if (map.IsFloor(g) && dist[g.x, g.y] >= MinEnemySteps) { spot = g; break; }
                }
                if (spot.HasValue) map.Enemies.Add(new EnemySpawn(EnemyKind.Guardian, spot.Value));
            }

            PlaceItems(map, s, rng, used);
            if (s.Chest) PlaceChest(map, rng, used);

            // Cosmetic props (bones, rubble) on floor cells next to walls.
            for (int i = idx; i < floor.Count && map.Decorations.Count < map.Rooms.Count * 3; i++)
            {
                var c = floor[i];
                if (used.Contains(c)) continue;
                if (map.IsWall(c.x, c.y + 1) || map.IsWall(c.x - 1, c.y) || map.IsWall(c.x + 1, c.y))
                    map.Decorations.Add(c);
            }
        }

        // ------------------------------------------------------------------ items & hazards

        static void PlaceItems(DungeonMap map, FloorSettings s, System.Random rng, HashSet<Vector2Int> used)
        {
            for (int i = 0, tries = 0; i < s.Items && tries < 200; tries++)
            {
                if (map.Rooms.Count < 2) return;
                var r = map.Rooms[1 + rng.Next(map.Rooms.Count - 1)];
                var c = new Vector2Int(r.x + 1 + rng.Next(Mathf.Max(1, r.width - 2)), r.y + 1 + rng.Next(Mathf.Max(1, r.height - 2)));
                if (used.Contains(c) || !map.IsFloor(c) || NearAnyBrazier(map, c, 3f)) continue;
                var pool = s.ItemPool != null && s.ItemPool.Length > 0 ? s.ItemPool : FloorSettings.BaseItems;
                ItemKind kind = pool[rng.Next(pool.Length)];
                map.Items.Add(new ItemSpawn(kind, c));
                used.Add(c);
                i++;
            }
        }

        /// <summary>A chest in one room and its key in another, far apart, so opening it is a detour.</summary>
        static void PlaceChest(DungeonMap map, System.Random rng, HashSet<Vector2Int> used)
        {
            if (map.Rooms.Count < 3 || rng.NextDouble() > 0.5) return;
            Vector2Int RoomCell(RectInt r) => new Vector2Int(r.x + 1 + rng.Next(Mathf.Max(1, r.width - 2)), r.y + 1 + rng.Next(Mathf.Max(1, r.height - 2)));
            for (int tries = 0; tries < 40; tries++)
            {
                var chestRoom = map.Rooms[1 + rng.Next(map.Rooms.Count - 1)];
                var keyRoom = map.Rooms[1 + rng.Next(map.Rooms.Count - 1)];
                if (chestRoom.Equals(keyRoom)) continue;
                var chest = RoomCell(chestRoom);
                var key = RoomCell(keyRoom);
                if (used.Contains(chest) || used.Contains(key) || Vector2Int.Distance(chest, key) < 12f) continue;
                if (NearAnyBrazier(map, chest, 2f) || NearAnyBrazier(map, key, 2f)) continue;
                map.Chest = chest;
                map.Items.Add(new ItemSpawn(ItemKind.Key, key));
                used.Add(chest);
                used.Add(key);
                return;
            }
        }

        static void PlaceHazards(DungeonMap map, FloorSettings s, System.Random rng)
        {
            var reserved = new HashSet<Vector2Int>(map.Braziers) { map.PlayerSpawn, map.Exit };
            foreach (var it in map.Items) reserved.Add(it.Cell);
            if (map.Chest.HasValue) reserved.Add(map.Chest.Value);

            // Water: blobs grown inside rooms (never the start room, never on objectives).
            for (int i = 0, tries = 0; i < s.WaterPools && tries < 60; tries++)
            {
                if (map.Rooms.Count < 2) break;
                var r = map.Rooms[1 + rng.Next(map.Rooms.Count - 1)];
                var seed = new Vector2Int(r.x + rng.Next(r.width), r.y + rng.Next(r.height));
                int radius = 2 + rng.Next(2);
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        var c = seed + new Vector2Int(dx, dy);
                        float d = new Vector2(dx, dy).magnitude + (float)rng.NextDouble() * 0.9f;
                        if (d <= radius && map.IsFloor(c) && !reserved.Contains(c)) map.Water[c.x, c.y] = true;
                    }
                i++;
            }

            // Doors: 2-wide doorways where a corridor meets a room side.
            var doorways = new List<RectInt>();
            foreach (var r in map.Rooms)
            {
                for (int y = r.yMin; y < r.yMax - 1; y++)
                {
                    TryDoorway(map, new RectInt(r.xMin - 1, y, 1, 2), vertical: true, doorways);
                    TryDoorway(map, new RectInt(r.xMax, y, 1, 2), vertical: true, doorways);
                }
                for (int x = r.xMin; x < r.xMax - 1; x++)
                {
                    TryDoorway(map, new RectInt(x, r.yMin - 1, 2, 1), vertical: false, doorways);
                    TryDoorway(map, new RectInt(x, r.yMax, 2, 1), vertical: false, doorways);
                }
            }
            Shuffle(doorways, rng);
            foreach (var d in doorways)
            {
                if (map.Doors.Count >= s.Doors) break;
                bool clash = false;
                foreach (var other in map.Doors) if (Vector2Int.Distance(other.Cells.position, d.position) < 4f) clash = true;
                foreach (var c in d.allPositionsWithin) if (reserved.Contains(c)) clash = true;
                if (!clash) map.Doors.Add(new DoorSpawn(d));
            }

            // Wind: straight corridor stretches (floor cells outside every room).
            for (int i = 0, tries = 0; i < s.WindZones && tries < 300; tries++)
            {
                var c = new Vector2Int(rng.Next(map.Width), rng.Next(map.Height));
                if (!map.IsFloor(c) || InAnyRoom(map, c)) continue;
                bool horizontal = map.IsFloor(c.x + 1, c.y) && map.IsFloor(c.x + 2, c.y) && !InAnyRoom(map, new Vector2Int(c.x + 3, c.y));
                bool vertical = map.IsFloor(c.x, c.y + 1) && map.IsFloor(c.x, c.y + 2) && !InAnyRoom(map, new Vector2Int(c.x, c.y + 3));
                if (!horizontal && !vertical) continue;
                var rect = horizontal ? new RectInt(c.x, c.y - 1, 4, 3) : new RectInt(c.x - 1, c.y, 3, 4);
                var dir = horizontal ? new Vector2Int(rng.Next(2) == 0 ? 1 : -1, 0) : new Vector2Int(0, rng.Next(2) == 0 ? 1 : -1);
                bool overlaps = false;
                foreach (var w in map.Wind) if (w.Cells.Overlaps(rect)) overlaps = true;
                if (overlaps) continue;
                map.Wind.Add(new WindSpawn(rect, dir));
                i++;
            }
        }

        static void TryDoorway(DungeonMap map, RectInt cells, bool vertical, List<RectInt> into)
        {
            foreach (var c in cells.allPositionsWithin) if (!map.IsFloor(c)) return;
            // Walls on both sides of the opening, so the gate really closes the passage.
            if (vertical && (!map.IsWall(cells.x, cells.yMin - 1) || !map.IsWall(cells.x, cells.yMax))) return;
            if (!vertical && (!map.IsWall(cells.xMin - 1, cells.y) || !map.IsWall(cells.xMax, cells.y))) return;
            into.Add(cells);
        }

        static bool InAnyRoom(DungeonMap map, Vector2Int c)
        {
            foreach (var r in map.Rooms) if (r.Contains(c)) return true;
            return false;
        }

        static bool NearAnyBrazier(DungeonMap map, Vector2Int c, float distance)
        {
            foreach (var b in map.Braziers)
                if (Vector2Int.Distance(b, c) < distance) return true;
            return false;
        }

        static bool NearAnyBrazier(DungeonMap map, Vector2Int c)
        {
            foreach (var b in map.Braziers)
                if (Vector2Int.Distance(b, c) < MinEmberBrazierDistance) return true;
            return false;
        }

        static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
