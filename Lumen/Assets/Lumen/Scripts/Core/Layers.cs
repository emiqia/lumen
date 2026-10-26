using UnityEngine;

namespace Lumen.Core
{
    /// <summary>Physics layers. Created by the editor setup tool (Lumen > Setup Project).</summary>
    public static class Layers
    {
        public static readonly int Walls = Resolve("Walls", 8);
        public static readonly int Player = Resolve("Player", 9);
        public static readonly int Enemies = Resolve("Enemies", 10);
        public static readonly int Pickups = Resolve("Pickups", 11);

        public static int WallMask => 1 << Walls;

        static int Resolve(string name, int fallback)
        {
            int layer = LayerMask.NameToLayer(name);
            return layer >= 0 ? layer : fallback;
        }
    }
}
