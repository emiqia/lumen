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
            public int hitsStalker, hitsMoth, hitsDevourer;
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
            GameEvents.Flare += (_, __) =>
            {
                if (Current == null) return;
                Current.flaresUsed++;
                if (floor != null) floor.flaresUsed++;
            };
            GameEvents.BrazierExtinguished += _ => { if (Current != null) Current.braziersExtinguished++; };
            GameEvents.EnemyKilled += (_, k) =>
            {
                if (Current == null) return;
                if (k == EnemyKind.Stalker) Current.killsStalker++;
                else if (k == EnemyKind.Moth) Current.killsMoth++;
                else Current.killsDevourer++;
            };
            GameEvents.PlayerHit += (_, amount, k) =>
            {
                if (Current == null) return;
                if (k == EnemyKind.Stalker) Current.hitsStalker++;
                else if (k == EnemyKind.Moth) Current.hitsMoth++;
                else Current.hitsDevourer++;
                if (floor != null) { floor.hitsTaken++; floor.fuelLostToHits += amount; }
            };
        }

        public void BeginRun(int seed = 0)
        {
            Current = new RunRecord { seed = seed };
        }

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

        public void Sample(float fuel, bool dimmed, float dt)
        {
            if (Current == null) return;
            if (fuel < Current.minFuel) Current.minFuel = fuel;
            if (dimmed) Current.secondsDimmed += dt;
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
