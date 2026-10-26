using System;
using System.Collections.Generic;

namespace Lumen.Core
{
    /// <summary>
    /// A permanent upgrade for the rest of the run, chosen (1 of 3) after clearing a floor.
    /// Each blessing edits the run-local tuning copy, so gameplay code just reads GameTuning.I.
    /// </summary>
    public sealed class Blessing
    {
        public readonly string Id;
        public readonly bool Stackable;
        /// <summary>Locked blessings only appear after being unlocked with Cenizas.</summary>
        public readonly bool Locked;
        readonly Action<GameTuning> apply;

        Blessing(string id, bool stackable, Action<GameTuning> apply, bool locked = false)
        {
            Id = id;
            Stackable = stackable;
            Locked = locked;
            this.apply = apply;
        }

        public string NameKey => "blessing." + Id;
        public string DescKey => "blessing." + Id + ".desc";
        public void ApplyTo(GameTuning t) => apply(t);

        public static readonly IReadOnlyList<Blessing> All = new[]
        {
            new Blessing("wick", true, t => t.maxFuel += 20f),                          // Mecha larga
            new Blessing("cheapflare", true, t => t.flareCost *= 0.75f),                // Destello barato
            new Blessing("quickflare", true, t => t.flareCooldown *= 0.7f),             // Chispa rápida
            new Blessing("embers", true, t => t.emberFuel *= 1.5f),                     // Brasas generosas
            new Blessing("swift", true, t => { t.moveSpeed *= 1.12f; t.dimMoveSpeed *= 1.12f; }), // Paso ligero
            new Blessing("hearth", true, t => t.brazierReserve += 30f),                 // Corazón de hogar
            new Blessing("shadow", true, t => t.drainDim *= 0.7f),                      // Sombra amiga
            new Blessing("fierce", true, t => t.stalkerBurnTime *= 0.6f),               // Llama feroz
            new Blessing("owl", true, t => t.stalkerSightLightFactor *= 0.85f),         // Paso de búho
            new Blessing("runner", true, t => t.sprintDrainMultiplier *= 0.65f),        // Pulmones de fragua
            new Blessing("phoenix", false, t => t.extraLives += 1),                     // Segunda chispa
            // Unlockable with Cenizas:
            new Blessing("echo", true, t => t.flareRadius *= 1.3f, locked: true),         // Eco de luz
            new Blessing("bond", true, t => t.brazierRefuelPerSecond *= 1.4f, locked: true), // Vínculo
            new Blessing("dragon", true, t => t.burnRadiusFactor *= 1.25f, locked: true),  // Sangre de dragón
        };

        /// <summary>
        /// Picks <paramref name="count"/> distinct blessings for an offer, skipping non-stackable
        /// ones already taken. Deterministic for a given seed.
        /// </summary>
        public static List<Blessing> Offer(int seed, int count, ICollection<string> taken, Func<string, bool> unlocked = null)
        {
            var pool = new List<Blessing>();
            foreach (var b in All)
                if ((b.Stackable || !taken.Contains(b.Id)) && (!b.Locked || (unlocked != null && unlocked(b.Id)))) pool.Add(b);
            var rng = new Random(seed);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
            return pool;
        }
    }
}
