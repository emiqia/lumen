using System;
using System.Collections.Generic;
using Lumen.Core;
using Lumen.Enemies;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.UI
{
    /// <summary>
    /// Teaches by reacting to what is happening instead of on a timer: each hint fires the
    /// first time its situation comes up in a run (first stalker that spots you, first enemy
    /// in flare range, first dry brazier...). Hints missed on floor 1 can still appear later.
    /// Urgent hints (dim, flare) jump the queue.
    /// </summary>
    public class TutorialDirector : IDisposable
    {
        const float HintDuration = 5f;
        const float MinGap = 1.6f;   // an urgent hint may replace the current one after this long
        const float NormalGap = 5.5f;

        static readonly HashSet<string> Urgent = new HashSet<string> { "tut.dim", "tut.flare", "tut.devourer", "tut.guardian", "tut.watcher" };

        readonly Hud hud;
        readonly HashSet<string> requested = new HashSet<string>();
        readonly List<string> queue = new List<string>();
        float sinceLastHint, floorTime, moved;
        Vector2 lastPos;
        bool hasLastPos, stalkerAlerted, brazierLit;

        public TutorialDirector(Hud hud)
        {
            this.hud = hud;
            GameEvents.StalkerAlerted += OnAlerted;
            GameEvents.BrazierLit += OnBrazierLit;
        }

        public void Dispose()
        {
            GameEvents.StalkerAlerted -= OnAlerted;
            GameEvents.BrazierLit -= OnBrazierLit;
        }

        void OnAlerted(Vector2 _) => stalkerAlerted = true;
        void OnBrazierLit(Vector2 _) => brazierLit = true;

        bool arrows = true;

        public void BeginRun(bool brazierArrows = true)
        {
            arrows = brazierArrows;
            requested.Clear();
            queue.Clear();
            sinceLastHint = NormalGap - 1f; // first hint shortly after the floor intro
            BeginFloor();
            Request("tut.move");
        }

        /// <summary>
        /// Called once the new floor is loaded. Clears event flags so nothing that happened
        /// before (e.g. the attract-mode demo during the fade) triggers a hint.
        /// </summary>
        public void BeginFloor()
        {
            floorTime = 0f;
            moved = 0f;
            hasLastPos = false;
            stalkerAlerted = brazierLit = false;
        }

        void Request(string key)
        {
            if (!requested.Add(key)) return;
            if (!Urgent.Contains(key)) { queue.Add(key); return; }
            // Urgent hints jump the queue, but never ahead of the very first "how to move" hint.
            int at = queue.Count > 0 && queue[0] == "tut.move" ? 1 : 0;
            queue.Insert(at, key);
        }

        public void Tick(float dt, LevelInstance level, PlayerFlame player)
        {
            if (level == null || player == null || !player.IsAlive) return;
            floorTime += dt;
            sinceLastHint += dt;

            Vector2 pos = player.transform.position;
            if (hasLastPos) moved += Vector2.Distance(pos, lastPos);
            lastPos = pos;
            hasLastPos = true;

            if (moved > 4f || floorTime > 6f) Request(arrows ? "tut.brazier" : "tut.brazier.noarrow");
            if (brazierLit) Request("tut.fuel");
            if (stalkerAlerted || NearestShade(pos, EnemyKind.Stalker, 7f)) Request("tut.dim");
            if (NearestShade(pos, null, 4.5f)) Request("tut.flare");
            foreach (var b in level.Braziers)
                if (b.IsLit && b.Depleted) { Request("tut.depleted"); break; }
            if (NearestShade(pos, EnemyKind.Devourer, 9f)) Request("tut.devourer");
            if (floorTime > 20f && player.TimeSprinting <= 0f) Request("tut.sprint");
            if (player.Run != null && player.Run.Lures > 0) Request("tut.item");
            if (NearestShade(pos, EnemyKind.Watcher, 9f)) Request("tut.watcher");
            if (player.InWeb) Request("tut.web");
            foreach (var s in Shade.All)
                if (s is Mimic m && !m.Disguised && Vector2.Distance(pos, m.transform.position) < 6f) { Request("tut.mimic"); break; }
            if (level.Guardian != null && level.Guardian.IsAwake && !level.Guardian.IsDead) Request("tut.guardian");

            if (queue.Count == 0) return;
            float gap = Urgent.Contains(queue[0]) ? MinGap : NormalGap;
            if (sinceLastHint < gap) return;
            hud.Hint(Locale.Get(queue[0]), HintDuration);
            queue.RemoveAt(0);
            sinceLastHint = 0f;
        }

        static bool NearestShade(Vector2 pos, EnemyKind? kind, float range)
        {
            foreach (var s in Shade.All)
            {
                if (s == null || s.IsDead || (kind.HasValue && s.Kind != kind.Value)) continue;
                Vector2 sp = s.transform.position;
                if ((sp - pos).sqrMagnitude < range * range && LightField.LineOfSight(pos, sp)) return true;
            }
            return false;
        }
    }
}
