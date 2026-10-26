using System.Collections.Generic;
using Lumen.Level;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>
    /// Gameplay queries about the current floor's biome hazards (water, wind, decoys),
    /// so the player and enemies can ask "what is here?" without knowing the level objects.
    /// </summary>
    public static class LevelHazards
    {
        static DungeonMap map;
        static readonly List<WindGust> gusts = new List<WindGust>();
        static readonly List<Lure> lures = new List<Lure>();
        public static IReadOnlyList<Lure> Lures => lures;

        public static void Set(DungeonMap current) { map = current; gusts.Clear(); lures.Clear(); }
        public static void Clear() { map = null; gusts.Clear(); lures.Clear(); }

        public static void Register(WindGust g) => gusts.Add(g);
        public static void Register(Lure l) => lures.Add(l);
        public static void Unregister(Lure l) => lures.Remove(l);

        public static bool InWater(Vector2 p) => map != null && map.IsWater(DungeonMap.WorldToCell(p));

        /// <summary>True if a gust is blowing at this point right now; <paramref name="push"/> is its force.</summary>
        public static bool GustAt(Vector2 p, out Vector2 push)
        {
            foreach (var g in gusts)
                if (g != null && g.Blowing && g.Contains(p)) { push = g.Push; return true; }
            push = Vector2.zero;
            return false;
        }
    }
}
