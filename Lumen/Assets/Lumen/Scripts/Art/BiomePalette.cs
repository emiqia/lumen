using Lumen.Level;
using UnityEngine;

namespace Lumen.Art
{
    /// <summary>Stone, wall and water colors plus ambient light for one biome.</summary>
    public sealed class BiomePalette
    {
        public Color32 FloorDark, Floor, FloorLight, FloorCrack, Moss, Void;
        public Color32 WallTop, WallRim, Brick, BrickLight, BrickDark, Mortar;
        public Color32 Water, WaterLight;
        public Color Ambient;

        static Color32 Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        public static readonly BiomePalette Crypts = new BiomePalette
        {
            FloorDark = Palette.FloorDark, Floor = Palette.Floor, FloorLight = Palette.FloorLight,
            FloorCrack = Palette.FloorCrack, Moss = Palette.Moss, Void = Palette.Void,
            WallTop = Palette.WallTop, WallRim = Palette.WallRim, Brick = Palette.Brick,
            BrickLight = Palette.BrickLight, BrickDark = Palette.BrickDark, Mortar = Palette.Mortar,
            Water = Hex(0x1d3a5a), WaterLight = Hex(0x3a6e96),
            Ambient = Palette.AmbientLight,
        };

        /// <summary>Drowned caverns: teal-grey stone, black water.</summary>
        public static readonly BiomePalette Flooded = new BiomePalette
        {
            FloorDark = Hex(0x23303a), Floor = Hex(0x2f3f4a), FloorLight = Hex(0x40525e),
            FloorCrack = Hex(0x182229), Moss = Hex(0x2e4a44), Void = Hex(0x080b0e),
            WallTop = Hex(0x24313a), WallRim = Hex(0x4a6270), Brick = Hex(0x4a5a66),
            BrickLight = Hex(0x5e7280), BrickDark = Hex(0x3a4852), Mortar = Hex(0x1c262d),
            Water = Hex(0x123a52), WaterLight = Hex(0x2f7aa2),
            Ambient = new Color(0.16f, 0.32f, 0.5f),
        };

        /// <summary>Root-choked depths: earth floor, bark-like walls, a sickly green ambient.</summary>
        public static readonly BiomePalette Roots = new BiomePalette
        {
            FloorDark = Hex(0x2e2a22), Floor = Hex(0x3d3729), FloorLight = Hex(0x524a36),
            FloorCrack = Hex(0x1e1b15), Moss = Hex(0x3e4a2a), Void = Hex(0x0c0b08),
            WallTop = Hex(0x2f2a20), WallRim = Hex(0x5e5238), Brick = Hex(0x5a4d36),
            BrickLight = Hex(0x76654a), BrickDark = Hex(0x46392a), Mortar = Hex(0x241f17),
            Water = Hex(0x1d3a3a), WaterLight = Hex(0x3a6e6e),
            Ambient = new Color(0.24f, 0.32f, 0.36f),
        };

        public static BiomePalette For(Biome biome) => biome switch
        {
            Biome.Flooded => Flooded,
            Biome.Roots => Roots,
            _ => Crypts,
        };
    }
}
