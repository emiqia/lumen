using System.Collections.Generic;
using Lumen.Level;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Everything that persists across floors within one run: difficulty, blessings taken,
    /// carried items and the run's tuning copy. Plain C#, owned by the GameManager.
    /// </summary>
    public class RunState
    {
        public Difficulty Difficulty { get; }
        public Character Character { get; }
        public GameTuning Tuning { get; }
        public bool Daily { get; }
        public readonly List<string> Blessings = new List<string>();

        // Inventory (carried between floors)
        public bool Lantern;   // absorbs the next hit
        public int Oil;        // each charge makes one flare free
        public int Lures;      // throwable light decoys
        public int ShadowDust; // E: a few seconds of invisibility
        public int LightBombs; // E: a free, wide, lethal flare
        public bool Keys;      // opens this floor's chest (reset every floor)
        public bool Compass;   // this floor: the exit is always marked
        public bool AshBoots;  // whole run: water and wind no longer affect you
        public bool StartWithBlessing;
        public int CenizasBonus; // found this run (chests, Guardian), added to the score's Cenizas
        public const int MaxOil = 2, MaxLures = 2, MaxActive = 2;

        /// <summary>Adds a picked-up item. False when that slot is already full.</summary>
        public bool TryCarry(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Lantern:
                    if (Lantern) return false;
                    Lantern = true;
                    return true;
                case ItemKind.Oil:
                    if (Oil >= MaxOil) return false;
                    Oil++;
                    return true;
                case ItemKind.Lure:
                    if (Lures >= MaxLures) return false;
                    Lures++;
                    return true;
                case ItemKind.ShadowDust:
                    if (ShadowDust >= MaxActive) return false;
                    ShadowDust++;
                    return true;
                case ItemKind.LightBomb:
                    if (LightBombs >= MaxActive) return false;
                    LightBombs++;
                    return true;
                case ItemKind.Key:
                    Keys = true;
                    return true;
                case ItemKind.Compass:
                    Compass = true;
                    return true;
                case ItemKind.AshBoots:
                    if (AshBoots) return false;
                    AshBoots = true;
                    return true;
                default:
                    return false; // the map is used on the spot, never carried
            }
        }

        /// <summary>Keys and the compass only last one floor; Rescoldo gets a fresh lantern on each.</summary>
        public void BeginFloor()
        {
            Keys = false;
            Compass = false;
            if (Tuning.lanternEachFloor) Lantern = true;
        }

        /// <summary>Active items in the order E cycles them (Q selects).</summary>
        public enum Active { Lure, ShadowDust, LightBomb }
        public Active Selected = Active.Lure;

        public int CountOf(Active a) => a switch { Active.Lure => Lures, Active.ShadowDust => ShadowDust, _ => LightBombs };

        /// <summary>
        /// The blessings to offer. The daily run uses the base set so everyone plays the same seed;
        /// other runs include the blessings bought with Cenizas.
        /// </summary>
        public List<Blessing> OfferBlessings(int seed, int count) =>
            Blessing.Offer(seed, count, Blessings, Daily ? null : (System.Func<string, bool>)Meta.IsUnlocked);

        public RunState(Difficulty difficulty, GameTuning baseTuning, Character character = null, bool daily = false)
        {
            Difficulty = difficulty;
            Character = character ?? Character.All[0];
            Daily = daily;
            Tuning = Object.Instantiate(baseTuning);
            Tuning.name = "RunTuning";
            DifficultyProfile.For(difficulty).ApplyTo(Tuning);
            Character.ApplyTo(Tuning);
        }

        /// <summary>Selects the next active item the run actually holds (Q).</summary>
        public void CycleActive()
        {
            for (int i = 1; i <= 3; i++)
            {
                var next = (Active)(((int)Selected + i) % 3);
                if (CountOf(next) > 0) { Selected = next; return; }
            }
        }

        /// <summary>Consumes one of the selected active item, falling back to any held one.</summary>
        public bool TryConsumeActive(out Active used)
        {
            used = Selected;
            if (CountOf(used) <= 0) { CycleActive(); used = Selected; }
            if (CountOf(used) <= 0) return false;
            if (used == Active.Lure) Lures--;
            else if (used == Active.ShadowDust) ShadowDust--;
            else LightBombs--;
            return true;
        }

        public void Take(Blessing b)
        {
            b.ApplyTo(Tuning);
            Blessings.Add(b.Id);
        }

        public void Dispose()
        {
            if (Tuning == null) return;
            if (Application.isPlaying) Object.Destroy(Tuning);
            else Object.DestroyImmediate(Tuning);
        }
    }
}
