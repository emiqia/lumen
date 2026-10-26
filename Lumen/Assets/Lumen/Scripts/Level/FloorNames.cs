using Lumen.Core;

namespace Lumen.Level
{
    /// <summary>
    /// A name and a line of flavor for each floor. The first three are hand-written; deeper
    /// floors combine biome-specific words deterministically, so a floor keeps its name.
    /// </summary>
    public static class FloorNames
    {
        public static string Title(int floor, int seed)
        {
            if (floor <= 3) return Locale.Get("floor.name." + floor);
            var biome = FloorSettings.BiomeFor(floor);
            var nouns = Locale.Get("floor.noun." + biome).Split('|');
            var adjs = Locale.Get("floor.adj." + biome).Split('|');
            var rng = new System.Random(seed);
            return nouns[rng.Next(nouns.Length)] + " " + adjs[rng.Next(adjs.Length)];
        }

        public static string Subtitle(int floor)
        {
            if (floor <= 3) return Locale.Get("floor.sub." + floor);
            return Locale.Get("floor.sub." + FloorSettings.BiomeFor(floor));
        }
    }
}
