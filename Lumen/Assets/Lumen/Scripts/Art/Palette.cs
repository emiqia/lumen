using UnityEngine;

namespace Lumen.Art
{
    /// <summary>Single source of truth for every color in the game.</summary>
    public static class Palette
    {
        static Color32 Hex(uint rgb, byte a = 255) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);

        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        // Floor stone
        public static readonly Color32 FloorDark = Hex(0x2c2834);
        public static readonly Color32 Floor = Hex(0x3d3846);
        public static readonly Color32 FloorLight = Hex(0x554c5e);
        public static readonly Color32 FloorCrack = Hex(0x1d1a23);
        public static readonly Color32 Moss = Hex(0x2f3b33);

        // Walls
        public static readonly Color32 Void = Hex(0x0b0a0e);
        public static readonly Color32 WallTop = Hex(0x2b2733);
        public static readonly Color32 WallRim = Hex(0x5a5266);
        public static readonly Color32 Brick = Hex(0x5a5163);
        public static readonly Color32 BrickLight = Hex(0x716680);
        public static readonly Color32 BrickDark = Hex(0x443d4d);
        public static readonly Color32 Mortar = Hex(0x29242f);

        // Fire (also used for the player, braziers and embers)
        public static readonly Color32 FireWhite = Hex(0xfff6c8);
        public static readonly Color32 FireYellow = Hex(0xffd166);
        public static readonly Color32 FireOrange = Hex(0xff9f1c);
        public static readonly Color32 FireRed = Hex(0xe85d04);
        public static readonly Color32 FireDeep = Hex(0x9d0208);
        public static readonly Color32 Eye = Hex(0x2b1a12);

        // Shades
        public static readonly Color32 ShadeCore = Hex(0x0f0c14);
        public static readonly Color32 ShadeBody = Hex(0x231b2e);
        public static readonly Color32 ShadeEdge = Hex(0x3b2d4d);
        public static readonly Color32 ShadeEye = Hex(0xff3355);
        public static readonly Color32 MothWing = Hex(0xd8d0c0);
        public static readonly Color32 MothWingDark = Hex(0x9d9384);
        public static readonly Color32 MothBody = Hex(0x5b5146);
        public static readonly Color32 DevourerBody = Hex(0x120e18);
        public static readonly Color32 DevourerEdge = Hex(0x2e2240);
        public static readonly Color32 DevourerEye = Hex(0xb8ff3c);
        public static readonly Color32 DevourerMaw = Hex(0x4a0f22);

        // Props
        public static readonly Color32 Stone = Hex(0x6b6270);
        public static readonly Color32 StoneDark = Hex(0x48404f);
        public static readonly Color32 StoneLight = Hex(0x8a8091);
        public static readonly Color32 Metal = Hex(0x3d3540);
        public static readonly Color32 Bone = Hex(0xcfc6b0);
        public static readonly Color32 BoneDark = Hex(0x958b78);
        public static readonly Color32 Portal = Hex(0x5ec8ff);
        public static readonly Color32 PortalDeep = Hex(0x2a5fdb);
        public static readonly Color32 PortalCore = Hex(0xd8f6ff);

        // UI
        public static readonly Color32 UiText = Hex(0xf1e9da);
        public static readonly Color32 UiDim = Hex(0x8c8296);
        public static readonly Color32 UiPanel = Hex(0x110e16, 220);
        public static readonly Color32 UiDanger = Hex(0xff4f5e);

        // Light colors (linear-ish, used by Light2D)
        public static readonly Color PlayerLight = new Color(1f, 0.72f, 0.42f);
        public static readonly Color BrazierLight = new Color(1f, 0.6f, 0.3f);
        public static readonly Color PortalLight = new Color(0.4f, 0.75f, 1f);
        public static readonly Color AmbientLight = new Color(0.22f, 0.28f, 0.55f); // cold moonlit blue vs warm fire

        public static Color32 Lerp(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);
    }
}
