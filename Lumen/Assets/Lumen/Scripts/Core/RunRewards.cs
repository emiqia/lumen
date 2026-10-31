using System.Collections.Generic;
using Lumen.Bot;
using Lumen.Level;

namespace Lumen.Core
{
    /// <summary>
    /// What a run hands out: the item pool per floor, chest loot, the Guardian's reward and the
    /// end-of-run settlement (records, Cenizas, challenges). Randomness and unlock checks are
    /// passed in, so the rules can be unit-tested without a scene or a save file.
    /// </summary>
    public static class RunRewards
    {
        public const int ChestCenizasMin = 50, ChestCenizasMax = 80;

        static readonly ItemKind[] FloorUnlockables = { ItemKind.Compass, ItemKind.ShadowDust, ItemKind.LightBomb, ItemKind.AshBoots };
        static readonly ItemKind[] ChestUnlockables = { ItemKind.LightBomb, ItemKind.ShadowDust, ItemKind.Compass };

        public struct ChestLoot
        {
            public ItemKind Item;
            public int Cenizas;
        }

        public struct GuardianReward
        {
            public int Cenizas;
            public Blessing Gift; // null when every blessing is already taken
        }

        /// <summary>Base items plus the ones unlocked with Cenizas (the daily run uses only the base set).</summary>
        public static ItemKind[] ItemPool(RunState run, System.Func<string, bool> isUnlocked)
        {
            var pool = new List<ItemKind>(FloorSettings.BaseItems);
            if (run != null && !run.Daily)
                foreach (var k in FloorUnlockables)
                    if (isUnlocked(k.ToString())) pool.Add(k);
            return pool.ToArray();
        }

        /// <summary>Lantern and oil always; bombs, dust and compass once unlocked.</summary>
        public static List<ItemKind> ChestPool(System.Func<string, bool> isUnlocked)
        {
            var pool = new List<ItemKind> { ItemKind.Lantern, ItemKind.Oil };
            foreach (var k in ChestUnlockables)
                if (isUnlocked(k.ToString())) pool.Add(k);
            return pool;
        }

        /// <summary>
        /// Spends the key and grants a random item plus some Cenizas. If the item can't be carried
        /// the chest gives an oil charge instead, so it is never empty.
        /// </summary>
        /// <param name="range">Exclusive-max random int, e.g. UnityEngine.Random.Range.</param>
        public static ChestLoot OpenChest(RunState run, System.Func<int, int, int> range, System.Func<string, bool> isUnlocked)
        {
            run.Keys = false;
            var pool = ChestPool(isUnlocked);
            var item = pool[range(0, pool.Count)];
            if (!run.TryCarry(item)) run.Oil = System.Math.Min(run.Oil + 1, RunState.MaxOil);
            int cenizas = range(ChestCenizasMin, ChestCenizasMax + 1);
            run.CenizasBonus += cenizas;
            return new ChestLoot { Item = item, Cenizas = cenizas };
        }

        /// <summary>Boss reward: Cenizas and a free blessing, applied to the run.</summary>
        public static GuardianReward DefeatGuardian(RunState run, int seed)
        {
            var reward = new GuardianReward { Cenizas = run.Tuning.guardianCenizas };
            run.CenizasBonus += reward.Cenizas;
            var gift = run.OfferBlessings(seed, 1);
            if (gift.Count > 0)
            {
                reward.Gift = gift[0];
                run.Take(reward.Gift);
            }
            return reward;
        }

        /// <summary>Total Cenizas a run earns: its score converted plus what it found.</summary>
        public static int CenizasEarned(RunState run, int score) => Meta.CenizasFor(score) + run.CenizasBonus;

        /// <summary>
        /// Writes the run to the save: record, Cenizas and completed challenges.
        /// Returns whether it was a record and the reward lines for the game-over screen.
        /// </summary>
        public static (bool record, string text) Settle(RunState run, int runSeed, int score, int floor, RunTelemetry.RunRecord last)
        {
            int cenizas = CenizasEarned(run, score);
            bool record = run.Daily ? Meta.SubmitDaily(runSeed, score) : SaveData.SubmitRun(run.Difficulty, score, floor);
            Meta.Earn(cenizas);
            var lines = new System.Text.StringBuilder("+" + cenizas + " " + Locale.Get("meta.cenizas"));
            foreach (var c in Meta.CompleteChallenges(last))
                lines.Append("\n" + Locale.Get("chal.done") + ": " + Locale.Get(c.NameKey) + "  +" + c.Reward);
            return (record, lines.ToString());
        }
    }
}
