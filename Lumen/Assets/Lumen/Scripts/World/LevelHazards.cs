using System.Collections.Generic;
using Lumen.Level;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>
    /// Gameplay queries about the current floor's hazards (water, wind, webs, decoys),
    /// so the player and enemies can ask "what is here?" without knowing the level objects.
    /// </summary>
    public static class LevelHazards
    {
        static DungeonMap map;
        static readonly List<WindGust> gusts = new List<WindGust>();
        static readonly List<Lure> lures = new List<Lure>();
        static readonly List<Web> webs = new List<Web>();
        public static IReadOnlyList<Lure> Lures => lures;

        public static void Set(DungeonMap current) { map = current; gusts.Clear(); lures.Clear(); webs.Clear(); }
        public static void Clear() { map = null; gusts.Clear(); lures.Clear(); webs.Clear(); }

        public static void Register(Web w) => webs.Add(w);
        public static void Unregister(Web w) => webs.Remove(w);

        public static bool InWeb(Vector2 p)
        {
            foreach (var w in webs) if (w != null && w.Contains(p)) return true;
            return false;
        }

        public static bool WebNear(Vector2 p, float distance)
        {
            foreach (var w in webs) if (w != null && ((Vector2)w.transform.position - p).sqrMagnitude < distance * distance) return true;
            return false;
        }

        public static void Register(WindGust g) => gusts.Add(g);
        public static void Register(Lure l) => lures.Add(l);
        public static void Unregister(Lure l) => lures.Remove(l);

        static readonly HashSet<Vector2Int> extraWater = new HashSet<Vector2Int>(); // the rising tide event

        public static void AddExtraWater(Vector2Int c) => extraWater.Add(c);
        public static void ClearExtraWater() => extraWater.Clear();

        public static bool InWater(Vector2 p)
        {
            var c = DungeonMap.WorldToCell(p);
            return map != null && (map.IsWater(c) || extraWater.Contains(c));
        }

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
