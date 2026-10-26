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

        static readonly HashSet<string> Urgent = new HashSet<string> { "tut.dim", "tut.flare" };

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

        public void BeginRun()
        {
            requested.Clear();
            queue.Clear();
            sinceLastHint = NormalGap - 1f; // first hint shortly after the floor intro
            stalkerAlerted = brazierLit = false;
            BeginFloor();
        }

        public void BeginFloor()
        {
            floorTime = 0f;
            moved = 0f;
            hasLastPos = false;
        }

        void Request(string key)
        {
            if (!requested.Add(key)) return;
            if (Urgent.Contains(key)) queue.Insert(0, key);
            else queue.Add(key);
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

            Request("tut.move");
            if (moved > 4f || floorTime > 6f) Request("tut.brazier");
            if (brazierLit) Request("tut.fuel");
            if (stalkerAlerted || NearestShade(pos, EnemyKind.Stalker, 7f)) Request("tut.dim");
            if (NearestShade(pos, null, 4.5f)) Request("tut.flare");
            foreach (var b in level.Braziers)
                if (b.IsLit && b.Depleted) { Request("tut.depleted"); break; }

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
