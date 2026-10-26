using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>A permanent unlock bought with Cenizas: a character, an item that joins the pool, or a blessing.</summary>
    public sealed class Unlock
    {
        public enum Kind { Character, Item, Blessing }
        public readonly string Id;
        public readonly Kind Type;
        public readonly int Price;
        public Unlock(string id, Kind type, int price) { Id = id; Type = type; Price = price; }

        public string NameKey => Type switch
        {
            Kind.Character => "char." + Id,
            Kind.Item => "item." + Id,
            _ => "blessing." + Id,
        };
        public string DescKey => NameKey + ".desc";

        public static readonly IReadOnlyList<Unlock> All = BuildAll();

        static List<Unlock> BuildAll()
        {
            var list = new List<Unlock>();
            foreach (var c in Character.All) if (c.Price > 0) list.Add(new Unlock(c.Id, Kind.Character, c.Price));
            list.Add(new Unlock("Compass", Kind.Item, 150));
            list.Add(new Unlock("ShadowDust", Kind.Item, 250));
            list.Add(new Unlock("LightBomb", Kind.Item, 300));
            list.Add(new Unlock("AshBoots", Kind.Item, 200));
            list.Add(new Unlock("echo", Kind.Blessing, 180));
            list.Add(new Unlock("bond", Kind.Blessing, 220));
            list.Add(new Unlock("dragon", Kind.Blessing, 260));
            return list;
        }
    }

    /// <summary>Something bought before a run, consumed by that run.</summary>
    public sealed class Provision
    {
        public readonly string Id;
        public readonly int Price;
        public readonly Action<RunState> Apply;
        Provision(string id, int price, Action<RunState> apply) { Id = id; Price = price; Apply = apply; }

        public string NameKey => "prov." + Id;
        public string DescKey => "prov." + Id + ".desc";

        public static readonly IReadOnlyList<Provision> All = new[]
        {
            new Provision("lantern", 60, r => r.Lantern = true),
            new Provision("oil", 40, r => r.Oil = Mathf.Min(r.Oil + 1, RunState.MaxOil)),
            new Provision("lure", 40, r => r.Lures = Mathf.Min(r.Lures + 1, RunState.MaxLures)),
            new Provision("wick", 80, r => r.Tuning.maxFuel += 20f),
            new Provision("blessing", 120, r => r.StartWithBlessing = true),
        };
    }

    /// <summary>
    /// A goal that pays Cenizas once. Progress is checked from run telemetry at the end of a run,
    /// or live for cumulative ones.
    /// </summary>
    public sealed class Challenge
    {
        public readonly string Id;
        public readonly int Reward;
        readonly Func<Bot.RunTelemetry.RunRecord, bool> check;
        Challenge(string id, int reward, Func<Bot.RunTelemetry.RunRecord, bool> check) { Id = id; Reward = reward; this.check = check; }

        public string NameKey => "chal." + Id;
        public string DescKey => "chal." + Id + ".desc";
        public bool IsMetBy(Bot.RunTelemetry.RunRecord r) => r != null && check(r);

        public static readonly IReadOnlyList<Challenge> All = new[]
        {
            new Challenge("firststeps", 50, r => r.floorReached >= 2),
            new Challenge("hunter", 150, r => r.killsDevourer >= 1),
            new Challenge("ghost", 120, r => r.unseenFloors >= 1),
            new Challenge("swift", 150, r => r.floorReached >= 4 && r.secondsAtFloor4 > 0 && r.secondsAtFloor4 <= 240f),
            new Challenge("deep", 250, r => r.floorReached >= 7),
            new Challenge("pyro", 120, r => r.killsByLight >= 10),
            new Challenge("guardian", 300, r => r.guardiansKilled >= 1),
            new Challenge("ash", 400, r => r.difficulty == nameof(Difficulty.Ceniza) && r.floorReached >= 4),
        };
    }

    /// <summary>Persistent meta progression: Cenizas, unlocks, completed challenges, chosen character, daily records.</summary>
    public static class Meta
    {
        const string KeyCenizas = "lumen.meta.cenizas";
        const string KeyUnlocks = "lumen.meta.unlocks";
        const string KeyChallenges = "lumen.meta.challenges";
        const string KeyCharacter = "lumen.meta.character";
        const string KeyDaily = "lumen.meta.daily.";

        public static int Cenizas
        {
            get => PlayerPrefs.GetInt(KeyCenizas, 0);
            private set { PlayerPrefs.SetInt(KeyCenizas, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Cenizas earned by a run: a share of the score, so playing well pays more.</summary>
        public static int CenizasFor(int score) => score / 20;

        public static void Earn(int amount) { if (amount > 0) Cenizas += amount; }

        public static bool TrySpend(int amount)
        {
            if (amount > Cenizas) return false;
            Cenizas -= amount;
            return true;
        }

        static HashSet<string> GetSet(string key) =>
            new HashSet<string>(PlayerPrefs.GetString(key, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

        static void SaveSet(string key, HashSet<string> set) { PlayerPrefs.SetString(key, string.Join(",", set)); PlayerPrefs.Save(); }

        public static bool IsUnlocked(string id) => id == "lumen" || GetSet(KeyUnlocks).Contains(id);

        public static bool TryUnlock(Unlock u)
        {
            if (IsUnlocked(u.Id) || !TrySpend(u.Price)) return false;
            var set = GetSet(KeyUnlocks);
            set.Add(u.Id);
            SaveSet(KeyUnlocks, set);
            return true;
        }

        public static bool IsCompleted(Challenge c) => GetSet(KeyChallenges).Contains(c.Id);

        /// <summary>Marks every challenge this run met for the first time and pays its reward. Returns them.</summary>
        public static List<Challenge> CompleteChallenges(Bot.RunTelemetry.RunRecord record)
        {
            var done = GetSet(KeyChallenges);
            var newly = new List<Challenge>();
            foreach (var c in Challenge.All)
                if (!done.Contains(c.Id) && c.IsMetBy(record)) { done.Add(c.Id); newly.Add(c); Earn(c.Reward); }
            if (newly.Count > 0) SaveSet(KeyChallenges, done);
            return newly;
        }

        public static string SelectedCharacter
        {
            get => PlayerPrefs.GetString(KeyCharacter, "lumen");
            set { PlayerPrefs.SetString(KeyCharacter, value); PlayerPrefs.Save(); }
        }

        // ------------------------------------------------------------------ daily seed

        /// <summary>Same seed for everyone on a given (UTC) day.</summary>
        public static int DailySeed(DateTime utcDay) => utcDay.Year * 10000 + utcDay.Month * 100 + utcDay.Day;
        public static int TodaySeed => DailySeed(DateTime.UtcNow.Date);
        public static int DailyBest(int seed) => PlayerPrefs.GetInt(KeyDaily + seed, 0);

        public static bool SubmitDaily(int seed, int score)
        {
            if (score <= DailyBest(seed)) return false;
            PlayerPrefs.SetInt(KeyDaily + seed, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
