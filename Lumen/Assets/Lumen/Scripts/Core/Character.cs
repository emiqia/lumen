using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// A playable flame. Each one changes the run's numbers, has one unique ability and its own
    /// colors and personality. Unlocked with Cenizas (Lumen is free).
    /// </summary>
    public sealed class Character
    {
        public readonly string Id;
        public readonly int Price;
        public readonly Color32 Core, Mid, Outer, Edge; // flame palette (white-hot core -> edge)
        public readonly Color Light;
        readonly Action<GameTuning> apply;

        Character(string id, int price, uint core, uint mid, uint outer, uint edge, Color light, Action<GameTuning> apply)
        {
            Id = id; Price = price;
            Core = Hex(core); Mid = Hex(mid); Outer = Hex(outer); Edge = Hex(edge);
            Light = light;
            this.apply = apply;
        }

        static Color32 Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public string NameKey => "char." + Id;
        public string DescKey => "char." + Id + ".desc";
        public string AbilityKey => "char." + Id + ".ability";
        public string QuoteKey => "char." + Id + ".quote";
        public void ApplyTo(GameTuning t) => apply(t);

        public static readonly IReadOnlyList<Character> All = new[]
        {
            // Balanced. Embers drift to her from twice as far.
            new Character("lumen", 0, 0xfff6c8, 0xffd166, 0xff9f1c, 0xe85d04, new Color(1f, 0.72f, 0.42f),
                t => t.emberMagnetMultiplier = 2f),
            // Aggressive: cheaper, deadlier flares; burns hotter.
            new Character("brasa", 300, 0xffe0c0, 0xff7a4a, 0xe83a1c, 0x9d0208, new Color(1f, 0.45f, 0.3f),
                t => { t.flareCost *= 0.7f; t.flareKillRadius += 0.6f; t.drainBright *= 1.2f; }),
            // Stealth: dims cheaply and quickly; moths ignore her while dimmed; walks through webs. Weak flare.
            new Character("fatuo", 500, 0xe8fbff, 0x9fe8ff, 0x4fb4e8, 0x2a5fdb, new Color(0.5f, 0.8f, 1f),
                t => { t.drainDim *= 0.6f; t.dimMoveSpeed *= 1.15f; t.flareRadius *= 0.8f; t.flareKillRadius *= 0.8f;
                       t.mothsIgnoreDimmed = true; t.ignoreWebs = true; }),
            // Speed: running doesn't make her brighter and burns less; smaller tank.
            new Character("chispa", 400, 0xffffff, 0xfff4b0, 0xffe066, 0xf2b630, new Color(1f, 0.95f, 0.7f),
                t => { t.sprintLightBoost = 1f; t.sprintDrainMultiplier *= 0.6f; t.moveSpeed *= 1.1f; t.maxFuel -= 25f; }),
            // Tank: big reserve and a fresh lantern every floor; slow.
            new Character("rescoldo", 700, 0xf4e0ff, 0xc89aff, 0x8a5cd8, 0x4a2a8a, new Color(0.75f, 0.55f, 1f),
                t => { t.maxFuel += 40f; t.brazierRefuelPerSecond *= 1.25f; t.moveSpeed *= 0.9f; t.dimMoveSpeed *= 0.9f;
                       t.lanternEachFloor = true; }),
        };

        public static Character Get(string id)
        {
            foreach (var c in All) if (c.Id == id) return c;
            return All[0];
        }

        public static int IndexOf(Character c)
        {
            for (int i = 0; i < All.Count; i++) if (All[i] == c) return i;
            return 0;
        }
    }
}
