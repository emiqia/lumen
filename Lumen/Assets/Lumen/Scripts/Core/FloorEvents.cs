using System.Collections.Generic;
using Lumen.Art;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.World;
using UnityEngine;

namespace Lumen.Core
{
    public enum FloorEventKind { None, Blackout, EmberRain, Tide }

    /// <summary>
    /// Occasional floor-wide events, announced on arrival. Chosen deterministically from the
    /// floor seed, never on floor 1.
    /// </summary>
    public static class FloorEvents
    {
        public static FloorEventKind Pick(int floor, int seed)
        {
            if (floor < 2) return FloorEventKind.None;
            var rng = new System.Random(seed ^ 0x5eed);
            if (rng.NextDouble() > 0.3) return FloorEventKind.None;
            return (FloorEventKind)(1 + rng.Next(3));
        }

        public static string NameKey(FloorEventKind k) => "event." + k;
        public static string DescKey(FloorEventKind k) => "event." + k + ".desc";
    }

    /// <summary>Runs the current floor's event. Created per floor by the GameManager.</summary>
    public class FloorEventDirector : MonoBehaviour
    {
        const float BlackoutEvery = 40f, BlackoutWarn = 2.5f, BlackoutLength = 12f;
        const float RainEvery = 9f; // slower than the light drains: a help, not a farm
        const float TideEvery = 35f, TideLength = 15f, TideStep = 1.2f;

        public FloorEventKind Kind { get; private set; }
        /// <summary>0..1 how dark the blackout currently is (PostFx and the player's light read this).</summary>
        public static float Darkness { get; private set; }

        LevelInstance level;
        float timer;
        readonly List<GameObject> tideTiles = new List<GameObject>();
        readonly HashSet<Vector2Int> tide = new HashSet<Vector2Int>();
        int tideRing;
        bool tideRising;

        public static FloorEventDirector Create(Transform parent, LevelInstance level, FloorEventKind kind)
        {
            var go = new GameObject("FloorEvent");
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<FloorEventDirector>();
            d.level = level;
            d.Kind = kind;
            Darkness = 0f;
            return d;
        }

        void OnDestroy()
        {
            Darkness = 0f;
            LevelHazards.ClearExtraWater();
        }

        void Update()
        {
            var player = LightField.Player;
            if (player == null || !player.IsAlive) return;
            timer += Time.deltaTime;
            switch (Kind)
            {
                case FloorEventKind.Blackout: UpdateBlackout(); break;
                case FloorEventKind.EmberRain: UpdateRain(player.transform.position); break;
                case FloorEventKind.Tide: UpdateTide(); break;
            }
        }

        void UpdateBlackout()
        {
            float cycle = Mathf.Repeat(timer, BlackoutEvery);
            float start = BlackoutEvery - BlackoutLength;
            if (cycle >= start - BlackoutWarn && cycle < start - BlackoutWarn + Time.deltaTime) GameEvents.RaiseBlackoutWarning();
            float target = cycle >= start ? 1f : 0f;
            Darkness = Mathf.MoveTowards(Darkness, target, Time.deltaTime / 1.2f);
        }

        void UpdateRain(Vector2 near)
        {
            if (timer < RainEvery) return;
            timer = 0f;
            var map = level.Map;
            for (int i = 0; i < 20; i++)
            {
                var c = DungeonMap.WorldToCell(near) + new Vector2Int(Random.Range(-8, 9), Random.Range(-6, 7));
                if (!map.IsFloor(c)) continue;
                Vector2 p = DungeonMap.CellToWorld(c);
                Ember.Create(p, level.Root.transform);
                for (int k = 0; k < 6; k++)
                    PixelParticles.Emit(p + new Vector2(0, 2f + k * 0.4f), Vector2.down * 4f, Palette.FireOrange, 0.5f);
                return;
            }
        }

        /// <summary>Water spreads ring by ring from existing pools (or a random room), then recedes.</summary>
        void UpdateTide()
        {
            float cycle = Mathf.Repeat(timer, TideEvery);
            bool shouldRise = cycle >= TideEvery - TideLength;
            if (shouldRise != tideRising) { tideRising = shouldRise; tideRing = 0; if (!shouldRise) ClearTide(); }
            if (!tideRising || tideRing >= 3) return;
            if (cycle < TideEvery - TideLength + tideRing * TideStep) return;
            tideRing++;
            Spread();
        }

        void Spread()
        {
            var map = level.Map;
            var frontier = new List<Vector2Int>();
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    if (map.Water[x, y] || tide.Contains(new Vector2Int(x, y))) frontier.Add(new Vector2Int(x, y));
            if (frontier.Count == 0 && map.Rooms.Count > 1)
            {
                var r = map.Rooms[1 + Random.Range(0, map.Rooms.Count - 1)];
                frontier.Add(new Vector2Int(r.x + r.width / 2, r.y + r.height / 2));
            }
            var reserved = new HashSet<Vector2Int>(map.Braziers) { map.Exit, map.PlayerSpawn };
            foreach (var c in frontier)
                foreach (var d in DungeonMap.Dirs4)
                {
                    var n = c + d;
                    if (!map.IsFloor(n) || map.Water[n.x, n.y] || tide.Contains(n) || reserved.Contains(n)) continue;
                    tide.Add(n);
                    LevelHazards.AddExtraWater(n);
                    var tile = new GameObject("Tide").AddComponent<SpriteRenderer>();
                    tile.transform.SetParent(level.Root.transform, false);
                    tile.transform.position = DungeonMap.CellToWorld(n);
                    tile.sprite = SpriteBank.Pixel;
                    tile.transform.localScale = new Vector3(16f, 16f, 1f);
                    tile.sharedMaterial = LumenAssets.I.Lit;
                    var water = BiomePalette.For(map.Biome).Water;
                    tile.color = new Color(water.r / 255f, water.g / 255f, water.b / 255f, 0.75f);
                    tile.sortingOrder = -25000;
                    tideTiles.Add(tile.gameObject);
                }
        }

        void ClearTide()
        {
            foreach (var t in tideTiles) if (t != null) Destroy(t);
            tideTiles.Clear();
            tide.Clear();
            LevelHazards.ClearExtraWater();
        }
    }
}
