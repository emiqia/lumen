using UnityEngine;

namespace Lumen.Player
{
    /// <summary>
    /// The fuel economy as plain C#: fuel is health, light and clock at once.
    /// Kept free of Unity objects so the rules can be unit tested.
    /// </summary>
    public class FuelModel
    {
        public float Max { get; }
        public float Value { get; private set; }
        public float Normalized => Value / Max;
        public bool Empty => Value <= 0f;

        readonly float drainBright, drainDim;

        public FuelModel(float max, float drainBright, float drainDim)
        {
            Max = max;
            Value = max;
            this.drainBright = drainBright;
            this.drainDim = drainDim;
        }

        public void Set(float value) => Value = Mathf.Clamp(value, 0f, Max);

        /// <summary>Adds (or removes, if negative) fuel. Returns the change actually applied.</summary>
        public float Add(float amount)
        {
            float before = Value;
            Set(Value + amount);
            return Value - before;
        }

        /// <summary>Passive burn. Dimming the flame stretches the fuel much further.</summary>
        public void Burn(float dt, bool dimmed) => Add(-(dimmed ? drainDim : drainBright) * dt);

        /// <summary>
        /// Draws fuel from a finite source (a brazier's reserve) at the given rate.
        /// Never takes more than the source holds or than the flame can store.
        /// Returns the amount transferred, which the caller removes from the source.
        /// </summary>
        public float RefuelFrom(float sourceReserve, float ratePerSecond, float dt)
        {
            float wanted = Mathf.Min(ratePerSecond * dt, Mathf.Max(0f, sourceReserve));
            return Add(wanted);
        }

        /// <summary>A flare can't be paid with your last breath: it must leave at least 1 fuel.</summary>
        public bool CanAfford(float cost) => Value > cost + 1f;

        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost)) return false;
            Add(-cost);
            return true;
        }

        /// <summary>Fuel to start the next floor with: carried fuel plus a bonus, with a floor.</summary>
        public static float CarryToNextFloor(float carried, float bonus, float minimum, float max) =>
            Mathf.Clamp(Mathf.Max(carried + bonus, minimum), 0f, max);
    }
}
