using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.Enemies;
using Lumen.Lighting;
using Lumen.World;
using UnityEngine;

namespace Lumen.Level
{
    /// <summary>A built, playable floor. Owns the runtime textures it baked, so they can be freed.</summary>
    public class LevelInstance : System.IDisposable
    {
        public DungeonMap Map;
        public FloorSettings Settings;
        public GridPathfinder Pathfinder;
        public GameObject Root;
        public readonly List<Brazier> Braziers = new List<Brazier>();
        public ExitPortal Exit;
        public readonly List<Door> Doors = new List<Door>();
        internal readonly List<Object> OwnedAssets = new List<Object>();

        public int LitBraziers
        {
            get
            {
                int n = 0;
                foreach (var b in Braziers) if (b.IsLit) n++;
                return n;
            }
        }

        /// <summary>Destroying the root does not free runtime-created textures and sprites; this does.</summary>
        public void Dispose()
        {
            if (Root != null) Object.Destroy(Root);
            foreach (var asset in OwnedAssets) if (asset != null) Object.Destroy(asset);
            OwnedAssets.Clear();
        }
    }

    /// <summary>Turns a DungeonMap into scene objects: baked art, colliders, shadow casters, actors.</summary>
    public static class LevelBuilder
    {
        public static LevelInstance Build(DungeonMap map, FloorSettings settings, int seed, Transform parent)
        {
            var level = new LevelInstance
            {
                Map = map,
                Settings = settings,
                Pathfinder = new GridPathfinder(map),
                Root = new GameObject("Level"),
            };
            var root = level.Root.transform;
            root.SetParent(parent, false);

            BuildArt(level, seed, root);
            BuildWallColliders(map, root);
            BuildShadowCasters(map, root);
            BuildDecorations(map, seed, root);

            foreach (var c in map.Braziers) level.Braziers.Add(Brazier.Create(DungeonMap.CellToWorld(c), root));
            foreach (var c in map.Embers) Ember.Create(DungeonMap.CellToWorld(c), root);
            level.Exit = ExitPortal.Create(DungeonMap.CellToWorld(map.Exit), root);
            foreach (var e in map.Enemies) SpawnEnemy(level, e.Kind, DungeonMap.CellToWorld(e.Cell));

            // Items and the biome's hazards. Phases are staggered so gates and gusts don't sync up.
            LevelHazards.Set(map);
            foreach (var it in map.Items) ItemPickup.Create(it, root);
            if (map.Chest.HasValue) Chest.Create(map.Chest.Value, root);
            var rng = new System.Random(seed + 911);
            foreach (var d in map.Doors) level.Doors.Add(Door.Create(d, root, (float)rng.NextDouble()));
            foreach (var w in map.Wind) WindGust.Create(w, root, (float)rng.NextDouble());

            return level;
        }

        public static Shade SpawnEnemy(LevelInstance level, EnemyKind kind, Vector2 pos)
        {
            Shade s = kind switch
            {
                EnemyKind.Moth => Moth.Create(pos, level.Root.transform),
                EnemyKind.Devourer => Devourer.Create(pos, level.Root.transform),
                _ => Stalker.Create(pos, level.Root.transform),
            };
            s.Init(level.Map, level.Pathfinder, level.Settings.EnemySpeedMultiplier);
            return s;
        }

        static void BuildArt(LevelInstance level, int seed, Transform root)
        {
            var (albedo, normal) = LevelPainter.Paint(level.Map, seed);
            var secondary = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
            var sprite = Sprite.Create(albedo, new Rect(0, 0, albedo.width, albedo.height), Vector2.zero,
                SpriteBank.PPU, 0, SpriteMeshType.FullRect, Vector4.zero, false, secondary);
            level.OwnedAssets.Add(albedo);
            level.OwnedAssets.Add(normal);
            level.OwnedAssets.Add(sprite);

            var go = new GameObject("LevelArt");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = LumenAssets.I.Lit;
            sr.sortingOrder = -30000;
        }

        static void BuildWallColliders(DungeonMap map, Transform root)
        {
            var go = new GameObject("WallColliders");
            go.layer = Layers.Walls;
            go.transform.SetParent(root, false);
            var solid = new bool[map.Width, map.Height];
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    solid[x, y] = map.IsWall(x, y) && HasFloorNeighbour(map, x, y);

            foreach (var r in GreedyRects(solid))
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.offset = r.center;
                box.size = r.size;
            }
        }

        /// <summary>
        /// Shadow casters at half-tile resolution. The lower half of a brick "face" tile is left
        /// out so light spills onto the face itself — walls read as having height.
        /// </summary>
        static void BuildShadowCasters(DungeonMap map, Transform root)
        {
            var parent = new GameObject("ShadowCasters");
            parent.transform.SetParent(root, false);
            int w = map.Width * 2, h = map.Height * 2;
            var solid = new bool[w, h];
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    if (!map.IsWall(x, y) || !NearFloor(map, x, y, 2)) continue;
                    bool face = map.IsFloor(x, y - 1);
                    solid[x * 2, y * 2 + 1] = solid[x * 2 + 1, y * 2 + 1] = true;
                    if (!face) solid[x * 2, y * 2] = solid[x * 2 + 1, y * 2] = true;
                }

            foreach (var r in GreedyRects(solid))
            {
                var rect = new Rect(r.x * 0.5f, r.y * 0.5f, r.width * 0.5f, r.height * 0.5f);
                ShadowCasterCompat.CreateBox(parent.transform, rect);
            }
        }

        const int CandleKind = 4;

        static void BuildDecorations(DungeonMap map, int seed, Transform root)
        {
            var rng = new System.Random(seed + 77);
            foreach (var c in map.Decorations)
            {
                int kind = rng.Next(SpriteBank.Decorations.Length);
                // Cobwebs belong in corners near the top wall.
                if (kind == 3 && !map.IsWall(c.x, c.y + 1)) kind = 2;
                var go = new GameObject("Deco");
                go.transform.SetParent(root, false);
                go.transform.position = DungeonMap.CellToWorld(c);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Decorations[kind];
                sr.sharedMaterial = LumenAssets.I.Lit;
                sr.sortingOrder = -20000;
                sr.flipX = rng.Next(2) == 0 && kind != 3;
                // One value step darker than gameplay objects so props never read as threats or pickups.
                sr.color = new Color(0.7f, 0.68f, 0.75f);

                if (kind == CandleKind)
                {
                    var flame = new GameObject("Flame").AddComponent<SpriteRenderer>();
                    flame.transform.SetParent(go.transform, false);
                    flame.sprite = SpriteBank.CandleFlame;
                    flame.sharedMaterial = LumenAssets.I.Unlit;
                    flame.sortingOrder = -19999;
                    var glow = LightFactory.Point(go.transform, Palette.FireOrange, 0.9f, 0.35f, false);
                    glow.transform.localPosition = new Vector3(0, 0.05f, 0);
                }
            }
        }

        static bool HasFloorNeighbour(DungeonMap map, int x, int y) => NearFloor(map, x, y, 1);

        static bool NearFloor(DungeonMap map, int x, int y, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    if (map.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        /// <summary>Merges a boolean grid into a small set of covering rectangles (greedy meshing).</summary>
        public static List<RectInt> GreedyRects(bool[,] grid)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            var used = new bool[w, h];
            var rects = new List<RectInt>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!grid[x, y] || used[x, y]) continue;
                    int rw = 1;
                    while (x + rw < w && grid[x + rw, y] && !used[x + rw, y]) rw++;
                    int rh = 1;
                    bool grow = true;
                    while (grow && y + rh < h)
                    {
                        for (int i = 0; i < rw; i++)
                            if (!grid[x + i, y + rh] || used[x + i, y + rh]) { grow = false; break; }
                        if (grow) rh++;
                    }
                    for (int ix = 0; ix < rw; ix++)
                        for (int iy = 0; iy < rh; iy++)
                            used[x + ix, y + iy] = true;
                    rects.Add(new RectInt(x, y, rw, rh));
                }
            return rects;
        }
    }
}
