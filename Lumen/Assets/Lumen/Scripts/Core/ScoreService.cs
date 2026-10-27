using System;
using Lumen.Level;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Turns gameplay events into score. Gameplay code never touches the score directly:
    /// it raises events (ember collected, brazier lit, enemy killed) and this service decides
    /// what they are worth. Only counts while a run is in progress.
    /// </summary>
    public class ScoreService : IDisposable
    {
        public int Score { get; private set; }
        public bool Counting { get; set; }
        /// <summary>Stealth bonus: grows while nothing has seen you, resets when something does.</summary>
        public float Multiplier { get; set; } = 1f;

        readonly GameTuning tuning;

        public ScoreService(GameTuning tuning)
        {
            this.tuning = tuning;
            GameEvents.EmberCollected += OnEmber;
            GameEvents.BrazierLit += OnBrazier;
            GameEvents.EnemyKilled += OnKill;
        }

        public void Dispose()
        {
            GameEvents.EmberCollected -= OnEmber;
            GameEvents.BrazierLit -= OnBrazier;
            GameEvents.EnemyKilled -= OnKill;
        }

        public void Reset() => Score = 0;

        void OnEmber(Vector2 p) => Add(tuning.scoreEmber, p);
        void OnBrazier(Vector2 p) => Add(tuning.scoreBrazier, p);
        // The boss is a fight, not a sneak: its bounty ignores the stealth bonus.
        void OnKill(Vector2 p, EnemyKind kind, KillCause _) => Add(ValueOf(kind), p, kind != EnemyKind.Guardian);

        public int ValueOf(EnemyKind kind) => kind switch
        {
            EnemyKind.Moth => tuning.scoreMoth,
            EnemyKind.Devourer => tuning.scoreDevourer,
            EnemyKind.Mimic => 30,
            EnemyKind.Weaver => 20,
            EnemyKind.Watcher => 35,
            EnemyKind.Guardian => 500,
            _ => tuning.scoreStalker,
        };

        /// <summary>Clearing a floor pays a base per depth plus a bonus for beating par time.</summary>
        public int FloorBonus(int floor, float seconds)
        {
            float underPar = tuning.FloorParTime(floor) - seconds;
            return tuning.scoreFloorBase * floor + Mathf.Max(0, Mathf.RoundToInt(underPar * tuning.scorePerSecondUnderPar));
        }

        public void Add(int amount, Vector2 worldPos, bool stealthBonus = true)
        {
            if (!Counting || amount <= 0) return;
            if (stealthBonus) amount = Mathf.RoundToInt(amount * Multiplier);
            Score += amount;
            GameEvents.RaiseScoreGained(amount, worldPos);
        }
    }
}
