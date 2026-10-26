namespace Lumen.Enemies
{
    /// <summary>
    /// Limits how often something can be wounded: after a wound, further hits are ignored for
    /// <c>Gap</c> seconds. Used by the Devourer so killing it takes separate engagements
    /// instead of three flares fired back-to-back.
    /// </summary>
    public class WoundGate
    {
        public float Gap { get; }
        public int Wounds { get; private set; }
        float lastWound = float.NegativeInfinity;

        public WoundGate(float gap) => Gap = gap;

        public bool CanWound(float now) => now - lastWound >= Gap;

        public bool TryWound(float now)
        {
            if (!CanWound(now)) return false;
            lastWound = now;
            Wounds++;
            return true;
        }
    }
}
