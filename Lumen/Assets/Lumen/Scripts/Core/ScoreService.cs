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
        void OnKill(Vector2 p, EnemyKind kind) => Add(ValueOf(kind), p);

        public int ValueOf(EnemyKind kind) => kind switch
        {
            EnemyKind.Moth => tuning.scoreMoth,
            EnemyKind.Devourer => tuning.scoreDevourer,
            _ => tuning.scoreStalker,
        };

        /// <summary>Clearing a floor pays a base per depth plus a bonus for beating par time.</summary>
        public int FloorBonus(int floor, float seconds)
        {
            float underPar = tuning.FloorParTime(floor) - seconds;
            return tuning.scoreFloorBase * floor + Mathf.Max(0, Mathf.RoundToInt(underPar * tuning.scorePerSecondUnderPar));
        }

        public void Add(int amount, Vector2 worldPos)
        {
            if (!Counting || amount <= 0) return;
            Score += amount;
            GameEvents.RaiseScoreGained(amount, worldPos);
        }
    }
}
