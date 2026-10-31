namespace Lumen.Core
{
    /// <summary>
    /// Stealth bonus clock: the score multiplier grows by +0.5 every <see cref="Step"/> seconds
    /// nothing sees you, up to x<see cref="Max"/>, and any detection resets it. Also remembers
    /// whether a stalker spotted you this floor (for the "ghost" challenge).
    /// </summary>
    public class StealthTracker
    {
        public const float Step = 10f, Max = 3f;

        float unseenTime;

        public float Multiplier => System.Math.Min(Max, 1f + 0.5f * (float)System.Math.Floor(unseenTime / Step));
        public bool SeenThisFloor { get; private set; }

        public void Tick(float dt) => unseenTime += dt;

        /// <summary>Something noticed you: the bonus drops back to x1.</summary>
        public void Break() => unseenTime = 0f;

        /// <summary>A stalker saw you: breaks the bonus and spoils this floor's "unseen" record.</summary>
        public void Spotted()
        {
            SeenThisFloor = true;
            Break();
        }

        public void BeginFloor()
        {
            SeenThisFloor = false;
            Break();
        }
    }
}
