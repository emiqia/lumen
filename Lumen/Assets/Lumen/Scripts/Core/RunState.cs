using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Everything that persists across floors within one run: difficulty, blessings taken,
    /// carried items and the run's tuning copy. Plain C#, owned by the GameManager.
    /// </summary>
    public class RunState
    {
        public Difficulty Difficulty { get; }
        public GameTuning Tuning { get; }
        public readonly List<string> Blessings = new List<string>();

        // Inventory (carried between floors)
        public bool Lantern;   // absorbs the next hit
        public int Oil;        // each charge makes one flare free
        public int Lures;      // throwable light decoys
        public const int MaxOil = 2, MaxLures = 2;

        public RunState(Difficulty difficulty, GameTuning baseTuning)
        {
            Difficulty = difficulty;
            Tuning = Object.Instantiate(baseTuning);
            Tuning.name = "RunTuning";
            DifficultyProfile.For(difficulty).ApplyTo(Tuning);
        }

        public void Take(Blessing b)
        {
            b.ApplyTo(Tuning);
            Blessings.Add(b.Id);
        }

        public void Dispose()
        {
            if (Tuning == null) return;
            if (Application.isPlaying) Object.Destroy(Tuning);
            else Object.DestroyImmediate(Tuning);
        }
    }
}
