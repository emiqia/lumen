using System;
using Lumen.Level;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Lightweight global event hub. Gameplay code raises events; audio, FX, HUD and
    /// telemetry subscribe, so systems never reference each other directly.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<Vector2, float> Flare;              // position, radius
        public static event Action<Vector2, EnemyKind, KillCause> EnemyKilled;
        public static event Action<Vector2, float, EnemyKind> PlayerHit; // position, fuel lost, source
        public static event Action<Vector2> EmberCollected;
        public static event Action<Vector2, ItemKind> ItemCollected;
        public static event Action<Vector2, ItemKind> ItemUsed;
        public static event Action<Vector2> BrazierLit;
        public static event Action<Vector2> BrazierExtinguished;
        public static event Action ExitOpened;
        public static event Action PlayerReachedExit;
        public static event Action<Vector2> StalkerAlerted;             // a stalker spotted the player
        public static event Action<int> FloorStarted;
        public static event Action<int> FloorCleared;
        public static event Action<string> PlayerDied;                  // cause key
        public static event Action<Vector2> PlayerRevived;
        public static event Action<int, Vector2> ScoreGained;           // amount, world position

        public static void RaiseFlare(Vector2 p, float r) => Flare?.Invoke(p, r);
        public static void RaiseEnemyKilled(Vector2 p, EnemyKind k, KillCause c) => EnemyKilled?.Invoke(p, k, c);
        public static void RaisePlayerHit(Vector2 p, float amount, EnemyKind k) => PlayerHit?.Invoke(p, amount, k);
        public static void RaiseEmberCollected(Vector2 p) => EmberCollected?.Invoke(p);
        public static void RaiseItemCollected(Vector2 p, ItemKind k) => ItemCollected?.Invoke(p, k);
        public static void RaiseItemUsed(Vector2 p, ItemKind k) => ItemUsed?.Invoke(p, k);
        public static void RaiseBrazierLit(Vector2 p) => BrazierLit?.Invoke(p);
        public static void RaiseBrazierExtinguished(Vector2 p) => BrazierExtinguished?.Invoke(p);
        public static void RaiseExitOpened() => ExitOpened?.Invoke();
        public static void RaisePlayerReachedExit() => PlayerReachedExit?.Invoke();
        public static void RaiseStalkerAlerted(Vector2 p) => StalkerAlerted?.Invoke(p);
        public static void RaiseFloorStarted(int f) => FloorStarted?.Invoke(f);
        public static void RaiseFloorCleared(int f) => FloorCleared?.Invoke(f);
        public static void RaisePlayerDied(string cause) => PlayerDied?.Invoke(cause);
        public static void RaisePlayerRevived(Vector2 p) => PlayerRevived?.Invoke(p);
        public static void RaiseScoreGained(int amount, Vector2 p) => ScoreGained?.Invoke(amount, p);

        /// <summary>Static events survive domain reloads being disabled; clear them on boot.</summary>
        public static void ClearAll()
        {
            Flare = null; EnemyKilled = null; PlayerHit = null; EmberCollected = null;
            BrazierLit = null; BrazierExtinguished = null; ExitOpened = null;
            PlayerReachedExit = null; StalkerAlerted = null; ItemCollected = null; ItemUsed = null; PlayerRevived = null;
            FloorStarted = null; FloorCleared = null; PlayerDied = null; ScoreGained = null;
        }
    }
}
