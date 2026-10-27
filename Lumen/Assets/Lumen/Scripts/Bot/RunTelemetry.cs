using System;
using System.Collections.Generic;
using Lumen.Core;
using Lumen.Level;
using UnityEngine;

namespace Lumen.Bot
{
    /// <summary>
    /// Records what happens during runs (deaths, damage sources, floor times...).
    /// Used by automated playtests to produce balance reports.
    /// </summary>
    public class RunTelemetry
    {
        [Serializable]
        public class FloorRecord
        {
            public int floor;
            public float seconds;
            public float fuelAtExit;
            public int hitsTaken;
            public float fuelLostToHits;
            public int flaresUsed;
        }

        [Serializable]
        public class RunRecord
        {
            public int seed;
            public string difficulty;
            public List<string> blessings = new List<string>();
            public float secondsSprinting;
            public int itemsCollected, itemsUsed, revives;
            public string character;
            public int unseenFloors;          // floors cleared without any stalker spotting you
            public float secondsAtFloor4;     // run time when floor 4 was reached (speed challenge)
            public int guardiansKilled;
            public int chestsOpened;
            public int cenizasEarned;
            public int floorReached;
            public int score;
            public float seconds;
            public string deathCause;
            public int flaresUsed;
            public float secondsDimmed;
            public int embersCollected;
            public int braziersLit;
            public int braziersExtinguished;
            public int darknessSpawns;
            public int killsStalker, killsMoth, killsDevourer;
            public int killsByFlare, killsByLight, killsByBrazier;
            public int hitsStalker, hitsMoth, hitsDevourer;
            public int killsOther, hitsOther; // mimics, weavers, watchers, guardians
            public float minFuel = 999f;
            public List<FloorRecord> floors = new List<FloorRecord>();
        }

        public readonly List<RunRecord> Runs = new List<RunRecord>();
        public RunRecord Current { get; private set; }
        public int DarknessSpawns { get => Current?.darknessSpawns ?? 0; set { if (Current != null) Current.darknessSpawns = value; } }
        FloorRecord floor;

        public RunTelemetry()
        {
            GameEvents.EmberCollected += _ => { if (Current != null) Current.embersCollected++; };
            GameEvents.ItemCollected += (_, __) => { if (Current != null) Current.itemsCollected++; };
            GameEvents.ItemUsed += (_, __) => { if (Current != null) Current.itemsUsed++; };
            GameEvents.PlayerRevived += _ => { if (Current != null) Current.revives++; };
            GameEvents.Flare += (_, __) =>
            {
                if (Current == null) return;
                Current.flaresUsed++;
                if (floor != null) floor.flaresUsed++;
            };
            GameEvents.BrazierExtinguished += _ => { if (Current != null) Current.braziersExtinguished++; };
            GameEvents.EnemyKilled += (_, k, cause) =>
            {
                if (Current == null) return;
                if (cause == KillCause.Flare) Current.killsByFlare++;
                else if (cause == KillCause.PlayerLight) Current.killsByLight++;
                else Current.killsByBrazier++;
                switch (k)
                {
                    case EnemyKind.Stalker: Current.killsStalker++; break;
                    case EnemyKind.Moth: Current.killsMoth++; break;
                    case EnemyKind.Devourer: Current.killsDevourer++; break;
                    default: Current.killsOther++; break;
                }
            };
            GameEvents.PlayerHit += (_, amount, k) =>
            {
                if (Current == null) return;
                switch (k)
                {
                    case EnemyKind.Stalker: Current.hitsStalker++; break;
                    case EnemyKind.Moth: Current.hitsMoth++; break;
                    case EnemyKind.Devourer: Current.hitsDevourer++; break;
                    default: Current.hitsOther++; break;
                }
                if (floor != null) { floor.hitsTaken++; floor.fuelLostToHits += amount; }
            };
        }

        public void BeginRun(int seed = 0, Difficulty difficulty = Difficulty.Antorcha)
        {
            Current = new RunRecord { seed = seed, difficulty = difficulty.ToString() };
        }

        public void OnBlessing(string id) => Current?.blessings.Add(id);

        public void BeginFloor(int f)
        {
            if (Current == null) return;
            floor = new FloorRecord { floor = f };
            Current.floors.Add(floor);
        }

        public void OnBraziersChanged(int now, int before)
        {
            if (Current != null && now > before) Current.braziersLit += now - before;
        }

        public void Sample(float fuel, bool dimmed, bool sprinting, float dt)
        {
            if (Current == null) return;
            if (fuel < Current.minFuel) Current.minFuel = fuel;
            if (dimmed) Current.secondsDimmed += dt;
            if (sprinting) Current.secondsSprinting += dt;
        }

        public void EndFloor(float seconds, float fuel)
        {
            if (floor == null) return;
            floor.seconds = seconds;
            floor.fuelAtExit = fuel;
        }

        /// <summary>Closes the run. <paramref name="fuelLeft"/> is recorded for runs that end without dying (timeouts).</summary>
        public void EndRun(string cause, int floorReached, int score, float seconds, float floorSeconds, float fuelLeft = 0f)
        {
            if (Current == null) return;
            Current.deathCause = cause;
            Current.floorReached = floorReached;
            Current.score = score;
            Current.seconds = seconds;
            if (floor != null && floor.seconds == 0)
            {
                floor.seconds = floorSeconds;
                floor.fuelAtExit = fuelLeft;
            }
            Runs.Add(Current);
            Current = null;
            floor = null;
        }
    }
}
