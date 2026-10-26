using Lumen.Level;
using UnityEngine;

namespace Lumen.Art
{
    /// <summary>
    /// Bakes the whole dungeon into one albedo texture plus a matching normal map.
    /// One sprite for the entire level keeps draw calls minimal, and the normal map lets
    /// Light2D shade brick faces and stone bevels, which sells the 3/4 "depth" look.
    /// </summary>
    public static class LevelPainter
    {
        const int T = SpriteBank.PPU; // pixels per tile

        enum WallKind { None, Face, Rim, Void }

        public static (Texture2D albedo, Texture2D normal) Paint(DungeonMap map, int seed)
        {
            int w = map.Width * T, h = map.Height * T;
            var albedo = new PixelCanvas(w, h);
            var normal = new PixelCanvas(w, h);

            for (int tx = 0; tx < map.Width; tx++)
                for (int ty = 0; ty < map.Height; ty++)
                {
                    var kind = Classify(map, tx, ty);
                    if (kind == WallKind.None) PaintFloor(map, albedo, normal, tx, ty, seed);
                    else if (kind == WallKind.Face) PaintFace(map, albedo, normal, tx, ty, seed);
                    else PaintTop(map, albedo, normal, tx, ty, kind == WallKind.Void);
                }

            return (albedo.ToTexture("level_albedo"), normal.ToLinearTexture("level_normal"));
        }

        static WallKind Classify(DungeonMap map, int x, int y)
        {
            if (map.IsFloor(x, y)) return WallKind.None;
            if (map.IsFloor(x, y - 1)) return WallKind.Face;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -2; dy <= 1; dy++)
                    if (map.IsFloor(x + dx, y + dy)) return WallKind.Rim;
            return WallKind.Void;
        }

        static Color32 N(float x, float y, float z)
        {
            var v = new Vector3(x, y, z).normalized;
            return new Color32((byte)((v.x * 0.5f + 0.5f) * 255), (byte)((v.y * 0.5f + 0.5f) * 255), (byte)((v.z * 0.5f + 0.5f) * 255), 255);
        }

        static readonly Color32 Flat = N(0, 0, 1);

        // ------------------------------------------------------------------ floor
        static void PaintFloor(DungeonMap map, PixelCanvas a, PixelCanvas n, int tx, int ty, int seed)
        {
            float variant = Noise.Hash(tx, ty, seed);
            bool quartered = variant > 0.62f;
            bool wallN = map.IsWall(tx, ty + 1), wallW = map.IsWall(tx - 1, ty), wallE = map.IsWall(tx + 1, ty);
            float slabTint = (Noise.Hash(tx * 3, ty * 7, seed + 1) - 0.5f) * 0.18f;

            for (int px = 0; px < T; px++)
                for (int py = 0; py < T; py++)
                {
                    int lx = quartered ? px % 8 : px, ly = quartered ? py % 8 : py;
                    int size = quartered ? 8 : T;
                    int wx = tx * T + px, wy = ty * T + py;

                    Color32 c;
                    Color32 nn = Flat;
                    if (lx == 0 || ly == 0)
                    {
                        c = Palette.FloorDark; // mortar groove
                    }
                    else
                    {
                        float grain = Noise.Fbm(wx * 0.35f, wy * 0.35f, seed, 2) - 0.5f;
                        float t = Mathf.Clamp01(0.5f + grain * 0.6f + slabTint);
                        c = Palette.Lerp(Palette.FloorDark, Palette.FloorLight, t);
                        if (lx == 1) { nn = N(-0.45f, 0, 1); c = Palette.Lerp(c, Palette.FloorLight, 0.35f); }
                        else if (lx == size - 1) nn = N(0.45f, 0, 1);
                        if (ly == size - 1) { nn = N(0, 0.45f, 1); c = Palette.Lerp(c, Palette.FloorLight, 0.35f); }
                        else if (ly == 1) nn = N(0, -0.45f, 1);
                    }

                    // Moss creeping from walls.
                    if ((wallN || wallW || wallE) && Noise.Fbm(wx * 0.15f, wy * 0.15f, seed + 9) > 0.62f)
                        c = Palette.Lerp(c, Palette.Moss, 0.55f);

                    // Ambient occlusion: walls above cast a soft contact shadow (3/4 view).
                    float ao = 1f;
                    if (wallN) ao *= Mathf.Lerp(0.45f, 1f, Mathf.Clamp01((T - 1 - py) / 6f));
                    if (wallW) ao *= Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(px / 3f));
                    if (wallE) ao *= Mathf.Lerp(0.6f, 1f, Mathf.Clamp01((T - 1 - px) / 3f));
                    c = Palette.Lerp(Palette.Void, c, ao);

                    a.Set(wx, wy, c);
                    n.Set(wx, wy, nn);
                }

            // Occasional crack
            if (Noise.Hash(tx, ty, seed + 5) > 0.85f)
            {
                int x = 3 + (int)(Noise.Hash(tx, ty, seed + 6) * 8), y = 12;
                for (int i = 0; i < 9; i++)
                {
                    a.Set(tx * T + x, ty * T + y, Palette.FloorCrack);
                    y--;
                    x += Noise.Hash(tx + i, ty, seed + 7) > 0.5f ? 1 : -1;
                    x = Mathf.Clamp(x, 1, T - 2);
                }
            }
        }

        // ------------------------------------------------------------------ wall face (bricks)
        static void PaintFace(DungeonMap map, PixelCanvas a, PixelCanvas n, int tx, int ty, int seed)
        {
            for (int px = 0; px < T; px++)
                for (int py = 0; py < T; py++)
                {
                    int wx = tx * T + px, wy = ty * T + py;
                    Color32 c;
                    Color32 nn;

                    if (py >= T - 3)
                    {
                        // Top lip of the wall, catches light from above.
                        c = py == T - 1 ? Palette.WallRim : Palette.BrickLight;
                        nn = N(0, 0.2f, 1);
                    }
                    else
                    {
                        int row = py / 4;
                        int offset = (row % 2) * 4 + (tx * 3 % 8);
                        int bx = (wx + offset) % 8;
                        int by = py % 4;
                        if (by == 3 || bx == 0)
                        {
                            c = Palette.Mortar;
                            nn = N(0, -0.6f, 0.8f);
                        }
                        else
                        {
                            float brickTone = Noise.Hash((wx + offset) / 8, ty * 4 + row, seed + 11);
                            c = Palette.Lerp(Palette.BrickDark, Palette.Brick, 0.4f + brickTone * 0.6f);
                            if (by == 2) c = Palette.Lerp(c, Palette.BrickLight, 0.5f);
                            nn = by == 2 ? N(0, -0.25f, 1f) : N(0, -0.85f, 0.55f);
                            if (bx == 1) nn = N(-0.4f, -0.7f, 0.6f);
                            if (bx == 7) nn = N(0.4f, -0.7f, 0.6f);
                        }
                        // Darker at the base where it meets the floor.
                        if (py < 2) c = Palette.Lerp(c, Palette.Void, 0.45f);
                    }
                    a.Set(wx, wy, c);
                    n.Set(wx, wy, nn);
                }
        }

        // ------------------------------------------------------------------ wall top
        static void PaintTop(DungeonMap map, PixelCanvas a, PixelCanvas n, int tx, int ty, bool deep)
        {
            // Rim lines are drawn where the top of this wall borders a walkable area or a face.
            bool edgeW = !deep && IsOpenBelowTop(map, tx - 1, ty);
            bool edgeE = !deep && IsOpenBelowTop(map, tx + 1, ty);
            bool edgeN = !deep && IsOpenBelowTop(map, tx, ty + 1);

            for (int px = 0; px < T; px++)
                for (int py = 0; py < T; py++)
                {
                    int wx = tx * T + px, wy = ty * T + py;
                    Color32 c = deep ? Palette.Void : Palette.WallTop;
                    Color32 nn = Flat;
                    if (!deep && Noise.Hash(wx, wy, 99) > 0.93f) c = Palette.Lerp(c, Palette.WallRim, 0.4f);
                    if (edgeW && px < 2) { c = Palette.WallRim; nn = N(-0.7f, 0, 0.7f); }
                    if (edgeE && px > T - 3) { c = Palette.WallRim; nn = N(0.7f, 0, 0.7f); }
                    if (edgeN && py > T - 3) { c = Palette.WallRim; nn = N(0, 0.7f, 0.7f); }
                    a.Set(wx, wy, c);
                    n.Set(wx, wy, nn);
                }
        }

        static bool IsOpenBelowTop(DungeonMap map, int x, int y) => map.IsFloor(x, y);
    }
}
