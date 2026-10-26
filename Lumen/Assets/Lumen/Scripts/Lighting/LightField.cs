using System.Collections.Generic;
using Lumen.Core;
using Lumen.World;
using UnityEngine;

namespace Lumen.Lighting
{
    /// <summary>
    /// Gameplay-side view of the lighting: answers "is this point lit, and by what?".
    /// The rendered Light2D shadows and these queries use the same wall colliders,
    /// so what the player sees is exactly what the AI reacts to.
    /// </summary>
    public static class LightField
    {
        static readonly List<Brazier> braziers = new List<Brazier>();
        public static IReadOnlyList<Brazier> Braziers => braziers;

        public static Player.PlayerFlame Player { get; set; }

        public static void Register(Brazier b) { if (!braziers.Contains(b)) braziers.Add(b); }
        public static void Unregister(Brazier b) => braziers.Remove(b);
        public static void Clear() { braziers.Clear(); Player = null; }

        public static bool LineOfSight(Vector2 a, Vector2 b) =>
            !Physics2D.Linecast(a, b, Layers.WallMask);

        /// <summary>True if a lit brazier's safe zone covers this point.</summary>
        public static bool InBrazierSafeZone(Vector2 p, out Brazier source)
        {
            float r = GameTuning.I.brazierSafeRadius;
            foreach (var b in braziers)
            {
                if (!b.IsLit) continue;
                Vector2 bp = b.transform.position;
                if ((bp - p).sqrMagnitude <= r * r && LineOfSight(bp, p))
                {
                    source = b;
                    return true;
                }
            }
            source = null;
            return false;
        }

        /// <summary>True if the player's bright core light is burning this point.</summary>
        public static bool PlayerBurns(Vector2 p)
        {
            if (Player == null || !Player.IsAlive || Player.IsDimmed) return false;
            float r = Player.LightRadius * GameTuning.I.burnRadiusFactor;
            Vector2 pp = Player.transform.position;
            return (pp - p).sqrMagnitude <= r * r && LineOfSight(pp, p);
        }

        /// <summary>How visible the player is from a point (0 = unseen). Depends on light size.</summary>
        public static bool CanSeePlayer(Vector2 from, float baseRange, float lightFactor)
        {
            if (Player == null || !Player.IsAlive) return false;
            Vector2 pp = Player.transform.position;
            float range = baseRange + Player.LightRadius * lightFactor;
            return (pp - from).sqrMagnitude <= range * range && LineOfSight(from, pp);
        }
    }
}
