using System.Collections.Generic;
using Lumen.Core;
using Lumen.Enemies;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using Lumen.World;
using UnityEngine;

namespace Lumen.Bot
{
    /// <summary>
    /// Utility-style AI that plays Lumen through the same input interface as a human.
    /// Powers the title-screen attract mode and automated balance playtests.
    /// </summary>
    public class AutoPilot : IPlayerInput
    {
        public Vector2 Move { get; private set; }
        public bool Dim { get; private set; }
        public bool FlarePressed { get; private set; }

        enum Goal { None, LightBrazier, Refuel, Ember, Exit }

        readonly List<Vector2Int> path = new List<Vector2Int>();
        Goal goal;
        Vector2Int goalCell;
        int pathIndex;
        float repathTimer, stuckTimer;
        Vector2 lastPos;
        Vector2 jitter;
        bool refueling;
        float thinkTimer;
        float logTimer;
        static readonly bool Verbose = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-botlog") >= 0;

        public void Tick()
        {
            FlarePressed = false;
            var gm = GameManager.Instance;
            var player = LightField.Player;
            if (gm == null || player == null || gm.Level == null || !player.IsAlive) { Move = Vector2.zero; return; }

            var map = gm.Level.Map;
            Vector2 pos = player.transform.position;
            float tuningCost = GameTuning.I.flareCost;

            // ---- threats
            Shade nearest = null;
            float nearestD = float.MaxValue;
            int stalkersNear = 0;
            Vector2 repel = Vector2.zero;
            foreach (var s in Shade.All)
            {
                if (s == null || s.IsDead) continue;
                Vector2 sp = s.transform.position;
                float d = Vector2.Distance(pos, sp);
                if (d < nearestD && LightField.LineOfSight(pos, sp)) { nearestD = d; nearest = s; }
                if (s.Kind == EnemyKind.Stalker && d < 7f) stalkersNear++;
                if (d < 3f && s.Kind != EnemyKind.Moth) repel += (pos - sp).normalized * (3f - d);
                // An enraged Devourer can't be flared: get away from it, as a person would.
                if (s is Devourer rage && rage.Enraged && d < 7f) repel += (pos - sp).normalized * (7f - d) * 0.6f;
                // Keep clear of any Devourer it can't flare right now (cooldown, low fuel or immune).
                else if (s is Devourer dev && d < 4.5f && (player.FlareCooldownLeft > 0f || !player.CanAffordFlare || !dev.Vulnerable))
                    repel += (pos - sp).normalized * (4.5f - d) * 0.5f;
            }

            bool flareReady = player.FlareCooldownLeft <= 0f && player.Fuel > tuningCost + 8f;
            if (nearest != null && flareReady && !(nearest is Devourer devourer && !devourer.Vulnerable))
            {
                // Inside the kill radius (with a margin) so the flare is worth its cost.
                float kill = GameTuning.I.flareKillRadius;
                float trigger = nearest.Kind == EnemyKind.Devourer ? kill * 0.8f : nearest.Kind == EnemyKind.Moth ? 1.6f : kill * 0.7f;
                if (nearestD < trigger) FlarePressed = true;
            }

            // ---- choose goal (a few times per second is plenty)
            thinkTimer -= Time.deltaTime;
            if (thinkTimer <= 0f)
            {
                thinkTimer = 0.25f;
                ChooseGoal(gm, player, map, pos);
            }

            // ---- follow path
            Vector2 dir = Vector2.zero;
            if (goal != Goal.None)
            {
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f || pathIndex >= path.Count)
                {
                    repathTimer = 0.5f;
                    gm.Level.Pathfinder.FindPath(DungeonMap.WorldToCell(pos), goalCell, path);
                    pathIndex = 0;
                }
                // Skip ahead to the farthest waypoint we can walk to directly.
                while (pathIndex + 1 < path.Count)
                {
                    Vector2 next = DungeonMap.CellToWorld(path[pathIndex + 1]);
                    if (Physics2D.CircleCast(pos, 0.3f, next - pos, Vector2.Distance(pos, next), Layers.WallMask)) break;
                    pathIndex++;
                }
                if (pathIndex < path.Count)
                {
                    Vector2 wp = DungeonMap.CellToWorld(path[pathIndex]);
                    if (Vector2.Distance(pos, wp) < 0.3f) pathIndex++;
                    dir = (wp - pos).normalized;
                }
                else
                {
                    Vector2 target = DungeonMap.CellToWorld(goalCell);
                    dir = Vector2.Distance(pos, target) > 0.2f ? (target - pos).normalized : Vector2.zero;
                }
            }

            dir += repel * 0.6f;

            // ---- unstick
            stuckTimer = Vector2.Distance(pos, lastPos) < 0.01f && dir.sqrMagnitude > 0.1f ? stuckTimer + Time.deltaTime : 0f;
            lastPos = pos;
            if (stuckTimer > 0.6f) { jitter = Random.insideUnitCircle.normalized; stuckTimer = 0f; repathTimer = 0f; }
            jitter = Vector2.Lerp(jitter, Vector2.zero, Time.deltaTime * 2f);

            Move = Vector2.ClampMagnitude(dir + jitter, 1f);

            if (Verbose && (logTimer -= Time.deltaTime) <= 0f)
            {
                logTimer = 2f;
                Debug.Log($"[Bot] t={gm.FloorTime:0} fuel={player.Fuel:0} pos={pos} cell={DungeonMap.WorldToCell(pos)} goal={goal}@{goalCell} " +
                          $"goalFloor={map.IsFloor(goalCell)} path={pathIndex}/{path.Count} move={Move} lit={gm.BraziersLit}");
            }

            // Sneak when stalkers are around but not on top of us; don't bother while refueling.
            Dim = stalkersNear > 0 && nearestD > 2.5f && !player.IsRefueling && player.Fuel > 12f;
        }

        void ChooseGoal(GameManager gm, PlayerFlame player, DungeonMap map, Vector2 pos)
        {
            var dist = map.DistanceField(DungeonMap.WorldToCell(pos));
            int Steps(Vector2 p)
            {
                var c = DungeonMap.WorldToCell(p);
                return map.InBounds(c) ? dist[c.x, c.y] : -1;
            }

            // Refuel hysteresis: commit until mostly full, but only at braziers that still have fuel.
            if (refueling && (player.Fuel > 88f || !AnyUsable(gm))) refueling = false;
            if (!refueling && player.Fuel < 38f)
            {
                var lit = Nearest(gm.Level.Braziers, Usable, Steps, out int s);
                if (lit != null && s >= 0 && s < player.Fuel * 0.9f) refueling = true;
            }

            if (refueling)
            {
                var lit = Nearest(gm.Level.Braziers, Usable, Steps, out _);
                if (lit != null) { SetGoal(Goal.Refuel, DungeonMap.WorldToCell((Vector2)lit.transform.position + Vector2.down * 0.9f)); return; }
            }

            // Grab close embers when not full.
            if (player.Fuel < 85f)
            {
                Ember best = null;
                int bestS = 8;
                foreach (var e in Ember.All)
                {
                    int s = Steps(e.transform.position);
                    if (s >= 0 && s < bestS) { bestS = s; best = e; }
                }
                if (best != null) { SetGoal(Goal.Ember, DungeonMap.WorldToCell(best.transform.position)); return; }
            }

            var unlit = Nearest(gm.Level.Braziers, b => !b.IsLit, Steps, out _);
            if (unlit != null) { SetGoal(Goal.LightBrazier, DungeonMap.WorldToCell((Vector2)unlit.transform.position + Vector2.down * 0.7f)); return; }

            if (gm.Level.Exit.IsOpen) { SetGoal(Goal.Exit, DungeonMap.WorldToCell(gm.Level.Exit.transform.position)); return; }
            SetGoal(Goal.None, goalCell);
        }

        static bool Usable(Brazier b) => b.IsLit && !b.Depleted;

        static bool AnyUsable(GameManager gm)
        {
            foreach (var b in gm.Level.Braziers) if (Usable(b)) return true;
            return false;
        }

        static Brazier Nearest(List<Brazier> list, System.Predicate<Brazier> filter, System.Func<Vector2, int> steps, out int bestSteps)
        {
            Brazier best = null;
            bestSteps = int.MaxValue;
            foreach (var b in list)
            {
                if (!filter(b)) continue;
                // Braziers are solid: measure from the cell below them.
                int s = steps((Vector2)b.transform.position + Vector2.down * 0.9f);
                if (s < 0) s = steps(b.transform.position);
                if (s >= 0 && s < bestSteps) { bestSteps = s; best = b; }
            }
            return best;
        }

        void SetGoal(Goal g, Vector2Int cell)
        {
            if (g != goal || cell != goalCell) repathTimer = 0f;
            goal = g;
            goalCell = cell;
        }
    }
}
