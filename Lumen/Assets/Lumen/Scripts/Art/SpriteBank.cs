using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Art
{
    /// <summary>
    /// Generates every character/prop sprite procedurally at startup (no imported art).
    /// All sprites use 16 pixels per unit so one dungeon tile == 16x16 px.
    /// </summary>
    public static class SpriteBank
    {
        public const int PPU = 16;

        public static Sprite[] Player { get; private set; }
        public static Sprite[] StalkerBody { get; private set; }
        /// <summary>[0] = normal palette, [1] = colorblind-safe palette.</summary>
        public static Sprite[] StalkerEyes { get; private set; }
        public static Sprite[] Moth { get; private set; }
        public static Sprite MothGlow { get; private set; }
        public static Sprite[] DevourerBody { get; private set; }
        /// <summary>[0] = normal palette, [1] = colorblind-safe palette.</summary>
        public static Sprite[] DevourerEyes { get; private set; }
        public static Sprite Brazier { get; private set; }
        public static Sprite[] BrazierFire { get; private set; }
        public static Sprite BrazierCoals { get; private set; }
        public static Sprite CandleFlame { get; private set; }
        public static Sprite Arrow { get; private set; }
        public static Sprite ExitMarker { get; private set; }
        public static Sprite[] Ember { get; private set; }
        public static Sprite PortalClosed { get; private set; }
        public static Sprite[] PortalOpen { get; private set; }
        public static Sprite[] Decorations { get; private set; }
        public static Sprite BlobShadow { get; private set; }
        public static Sprite Pixel { get; private set; }
        public static Sprite SoftCircle { get; private set; }

        static bool built;

        public static void Build()
        {
            if (built) return;
            built = true;

            Pixel = Make(Solid(1, 1, Color.white), "pixel", new Vector2(0.5f, 0.5f));
            SoftCircle = Make(SoftDisc(32), "soft", new Vector2(0.5f, 0.5f), 32);
            BlobShadow = Make(Blob(), "blob", new Vector2(0.5f, 0.5f));

            Player = Frames(6, i => PlayerFrame(i), "player", new Vector2(0.5f, 0.25f));
            StalkerBody = Frames(4, i => StalkerFrame(i), "stalker", new Vector2(0.5f, 0.2f));
            StalkerEyes = new[]
            {
                Make(StalkerEyesCanvas(Palette.ShadeEye), "stalker_eyes", new Vector2(0.5f, 0.2f)),
                Make(StalkerEyesCanvas(Palette.ShadeEyeAlt), "stalker_eyes_cb", new Vector2(0.5f, 0.2f)),
            };
            Moth = Frames(2, i => MothFrame(i), "moth", new Vector2(0.5f, 0.5f));
            MothGlow = Make(MothGlowCanvas(), "moth_glow", new Vector2(0.5f, 0.5f));
            DevourerBody = Frames(4, i => DevourerFrame(i), "devourer", new Vector2(0.5f, 0.15f));
            DevourerEyes = new[]
            {
                Make(DevourerEyesCanvas(Palette.DevourerEye), "devourer_eyes", new Vector2(0.5f, 0.15f)),
                Make(DevourerEyesCanvas(Palette.DevourerEyeAlt), "devourer_eyes_cb", new Vector2(0.5f, 0.15f)),
            };
            Brazier = Make(BrazierCanvas(), "brazier", new Vector2(0.5f, 0.2f));
            BrazierFire = Frames(4, i => BrazierFireFrame(i), "brazier_fire", new Vector2(0.5f, 0.2f));
            BrazierCoals = Make(BrazierCoalsCanvas(), "brazier_coals", new Vector2(0.5f, 0.2f));
            CandleFlame = Make(CandleFlameCanvas(), "candle_flame", new Vector2(0.5f, 0.5f));
            Arrow = Make(ArrowCanvas(), "arrow", new Vector2(0.5f, 0.5f));
            ExitMarker = Make(DiamondCanvas(), "exit_marker", new Vector2(0.5f, 0.5f));
            Ember = Frames(4, i => EmberFrame(i), "ember", new Vector2(0.5f, 0.5f));
            PortalClosed = Make(PortalCanvas(false, 0), "portal_closed", new Vector2(0.5f, 0.5f));
            PortalOpen = Frames(4, i => PortalCanvas(true, i), "portal_open", new Vector2(0.5f, 0.5f));
            Decorations = Frames(5, i => DecorationCanvas(i), "deco", new Vector2(0.5f, 0.5f));
        }

        static Sprite Make(PixelCanvas c, string name, Vector2 pivot, int ppu = PPU)
        {
            var tex = c.ToTexture(name);
            return Sprite.Create(tex, new Rect(0, 0, c.Width, c.Height), pivot, ppu, 0, SpriteMeshType.FullRect);
        }

        static Sprite[] Frames(int count, System.Func<int, PixelCanvas> paint, string name, Vector2 pivot)
        {
            var list = new Sprite[count];
            for (int i = 0; i < count; i++) list[i] = Make(paint(i), name + "_" + i, pivot);
            return list;
        }

        static PixelCanvas Solid(int w, int h, Color32 c)
        {
            var p = new PixelCanvas(w, h);
            p.Fill(c);
            return p;
        }

        static PixelCanvas SoftDisc(int size)
        {
            var p = new PixelCanvas(size, size);
            float r = size / 2f;
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    byte a = (byte)(Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 255);
                    p.Set(x, y, new Color32(255, 255, 255, a));
                }
            return p;
        }

        static PixelCanvas Blob()
        {
            var p = new PixelCanvas(12, 6);
            p.Ellipse(6, 3, 6, 3, new Color32(0, 0, 0, 90));
            p.Ellipse(6, 3, 4, 2, new Color32(0, 0, 0, 140));
            return p;
        }

        // ---------------------------------------------------------------- Player
        // A little living flame: teardrop shape, noise-animated tip, two dark eyes.
        static PixelCanvas PlayerFrame(int frame)
        {
            var p = new PixelCanvas(16, 16);
            float phase = frame / 6f * Mathf.PI * 2f;
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    float fx = (x + 0.5f - 8f) / 5.2f;
                    float fy = (y + 0.5f - 5.5f) / 9.5f;          // body sits low, tip rises
                    float sway = Mathf.Sin(phase + y * 0.55f) * 0.12f * Mathf.Clamp01(fy * 1.5f);
                    float width = fy < 0 ? Mathf.Sqrt(Mathf.Max(0, 1 - fy * fy * 9f)) : (1f - fy) * 1.05f;
                    float d = Mathf.Abs(fx - sway) / Mathf.Max(0.001f, width);
                    float n = Noise.Value(x * 0.7f, y * 0.7f - frame * 1.3f, 7) * 0.35f;
                    float heat = 1f - d - n * Mathf.Clamp01(fy + 0.2f);
                    if (fy > 1f || heat <= 0f) continue;
                    Color32 c = heat > 0.72f ? Palette.FireWhite
                              : heat > 0.5f ? Palette.FireYellow
                              : heat > 0.28f ? Palette.FireOrange
                              : Palette.FireRed;
                    p.Set(x, y, c);
                }
            // Eyes (blink on frame 3)
            if (frame != 3)
            {
                p.Set(6, 5, Palette.Eye); p.Set(6, 6, Palette.Eye);
                p.Set(9, 5, Palette.Eye); p.Set(9, 6, Palette.Eye);
            }
            else
            {
                p.Set(6, 5, Palette.Eye); p.Set(9, 5, Palette.Eye);
            }
            return p;
        }

        // ---------------------------------------------------------------- Stalker
        // Hooded wraith with wavy tendrils. Drawn with the lit material, so it is only
        // visible when light touches it; the eyes are a separate unlit sprite.
        static PixelCanvas StalkerFrame(int frame)
        {
            var p = new PixelCanvas(16, 16);
            float phase = frame / 4f * Mathf.PI * 2f;
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    float fx = (x + 0.5f - 8f);
                    float top = 14f - fx * fx * 0.18f;                       // rounded hood
                    float bottom = 1.5f + Mathf.Sin(phase + x * 1.3f) * 1.3f; // tendrils
                    if (y > top || y < bottom || Mathf.Abs(fx) > 6.2f - Mathf.Max(0, 6 - y) * 0.15f) continue;
                    float shade = (y - bottom) / (top - bottom);
                    Color32 c = Mathf.Abs(fx) > 4.6f || y > top - 1.2f ? Palette.ShadeEdge
                              : shade < 0.35f ? Palette.ShadeCore : Palette.ShadeBody;
                    p.Set(x, y, c);
                }
            // Hollow face
            p.Rect(5, 8, 6, 3, Palette.ShadeCore);
            return p;
        }

        // Horizontal 3x1 slits with a white-hot center: identifiable by shape and value, not only hue.
        static PixelCanvas StalkerEyesCanvas(Color32 eye)
        {
            var p = new PixelCanvas(16, 16);
            p.Rect(4, 9, 3, 1, eye);
            p.Rect(9, 9, 3, 1, eye);
            p.Set(5, 9, Palette.FireWhite);
            p.Set(10, 9, Palette.FireWhite);
            return p;
        }

        // ---------------------------------------------------------------- Moth
        // Wide triangular forewings (14 px span when open) and feathery antennae: reads as a moth,
        // not as a pill or a jar, even in silhouette.
        static PixelCanvas MothFrame(int frame)
        {
            var p = new PixelCanvas(16, 12);
            float open = frame == 0 ? 1f : 0.5f;
            const float cx = 7.5f;
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 12; y++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) / open; // horizontal distance, squeezed when closed
                    // forewing: wide at the top, tapering down
                    bool fore = y >= 4 && y <= 10 && dx <= 7f && dx >= 0.8f && (10 - y) * 1.1f >= dx - 2.5f;
                    // hindwing: small rounded lobe under the forewing
                    bool hind = y >= 1 && y < 5 && dx >= 0.8f && dx <= 4.2f - (4 - y) * 0.6f;
                    if (fore) p.Set(x, y, dx > 5.5f || y == 10 ? Palette.MothWingDark : Palette.MothWing);
                    else if (hind) p.Set(x, y, Palette.MothWingDark);
                }
            p.Rect(7, 1, 2, 9, Palette.MothBody);                      // body
            p.Set(6, 10, Palette.MothBody); p.Set(5, 11, Palette.MothBody); // antennae
            p.Set(9, 10, Palette.MothBody); p.Set(10, 11, Palette.MothBody);
            if (open > 0.75f) { p.Set(3, 8, Palette.MothBody); p.Set(12, 8, Palette.MothBody); } // eye spots
            return p;
        }

        static PixelCanvas MothGlowCanvas()
        {
            var p = new PixelCanvas(16, 12);
            p.Set(7, 9, new Color32(255, 240, 200, 255));
            p.Set(8, 9, new Color32(255, 240, 200, 255));
            return p;
        }

        // ---------------------------------------------------------------- Devourer
        static PixelCanvas DevourerFrame(int frame)
        {
            var p = new PixelCanvas(24, 24);
            float breathe = Mathf.Sin(frame / 4f * Mathf.PI * 2f);
            float rx = 10.5f + breathe * 0.6f, ry = 9f - breathe * 0.5f;
            p.Ellipse(12, 10, rx, ry, Palette.DevourerEdge);
            p.Ellipse(12, 10, rx - 1.2f, ry - 1.2f, Palette.DevourerBody);
            // spikes on the back
            for (int i = 0; i < 5; i++)
            {
                int sx = 5 + i * 3 + (frame % 2);
                for (int k = 0; k < 2 + i % 2; k++) p.Set(sx, (int)(10 + ry) + k - 1, Palette.DevourerEdge);
            }
            // gaping maw
            int mawH = 3 + (frame % 2);
            p.Rect(7, 5, 10, mawH, Palette.DevourerMaw);
            for (int x = 7; x < 17; x += 2)
            {
                p.Set(x, 5 + mawH - 1, Palette.Bone);  // upper teeth
                p.Set(x + 1, 5, Palette.Bone);          // lower teeth
            }
            return p;
        }

        static PixelCanvas DevourerEyesCanvas(Color32 e)
        {
            var p = new PixelCanvas(24, 24);
            p.Rect(7, 12, 2, 2, e); p.Rect(11, 13, 2, 2, e); p.Rect(15, 12, 2, 2, e);
            p.Set(9, 15, e); p.Set(14, 15, e);
            return p;
        }

        // ---------------------------------------------------------------- Brazier
        // A wide bowl on a splayed tripod: reads as "fire goes here", not as an anvil.
        static PixelCanvas BrazierCanvas()
        {
            var p = new PixelCanvas(16, 16);
            int[] legY = { 0, 1, 2, 3, 4, 5 };
            int[] legX = { 3, 4, 4, 5, 5, 6 };
            for (int i = 0; i < legY.Length; i++)
            {
                p.Set(legX[i], legY[i], Palette.StoneDark);
                p.Set(15 - legX[i], legY[i], Palette.StoneDark);
            }
            p.Rect(7, 2, 2, 4, Palette.Metal);              // central stem
            p.Rect(4, 6, 8, 1, Palette.StoneDark);          // bowl bottom
            p.Rect(3, 7, 10, 1, Palette.Stone);
            p.Rect(2, 8, 12, 1, Palette.Stone);
            p.Rect(2, 9, 12, 1, Palette.StoneLight);        // rim catches the light
            p.Rect(4, 9, 8, 1, Palette.Metal);              // dark coal bed inside the rim
            p.Outline(new Color32(10, 8, 14, 255));
            return p;
        }

        static PixelCanvas BrazierCoalsCanvas()
        {
            var p = new PixelCanvas(16, 16);
            Color32[] row = { Palette.FireDeep, Palette.FireRed, Palette.FireDeep, Palette.FireOrange,
                              Palette.FireRed, Palette.FireDeep, Palette.FireRed, Palette.FireDeep };
            for (int i = 0; i < row.Length; i++) p.Set(4 + i, 9, row[i]);
            p.Set(6, 10, Palette.FireRed);
            p.Set(9, 10, Palette.FireDeep);
            p.Set(7, 10, Palette.FireDeep);
            return p;
        }

        static PixelCanvas CandleFlameCanvas()
        {
            var p = new PixelCanvas(16, 16);
            p.Set(7, 7, Palette.FireOrange);
            p.Set(7, 8, Palette.FireYellow);
            p.Set(7, 9, Palette.FireWhite);
            return p;
        }

        static PixelCanvas ArrowCanvas()
        {
            var p = new PixelCanvas(13, 13);
            for (int x = 1; x < 12; x++)
            {
                int half = (11 - x) / 2;
                for (int y = 6 - half; y <= 6 + half; y++) p.Set(x, y, Color.white);
            }
            p.Outline(new Color32(10, 8, 14, 255));
            return p;
        }

        // The exit gets a different *shape* (diamond), so it is not told apart by color alone.
        static PixelCanvas DiamondCanvas()
        {
            var p = new PixelCanvas(13, 13);
            for (int x = 1; x < 12; x++)
                for (int y = 1; y < 12; y++)
                    if (Mathf.Abs(x - 6) + Mathf.Abs(y - 6) <= 5) p.Set(x, y, Color.white);
            p.Set(6, 6, new Color32(10, 8, 14, 255)); // hollow center reads as a portal ring
            p.Outline(new Color32(10, 8, 14, 255));
            return p;
        }

        static PixelCanvas BrazierFireFrame(int frame)
        {
            var p = new PixelCanvas(16, 16);
            for (int x = 3; x < 13; x++)
                for (int y = 8; y < 16; y++)
                {
                    float fx = (x + 0.5f - 8f) / 4.5f;
                    float fy = (y - 8f) / 7.5f;
                    float n = Noise.Value(x * 0.8f, y * 0.6f - frame * 1.7f, 3);
                    float heat = (1f - Mathf.Abs(fx) * 1.1f) * (1f - fy) * 1.5f - n * 0.6f;
                    if (heat <= 0.05f) continue;
                    p.Set(x, y, heat > 0.75f ? Palette.FireWhite : heat > 0.5f ? Palette.FireYellow
                                : heat > 0.25f ? Palette.FireOrange : Palette.FireRed);
                }
            return p;
        }

        // ---------------------------------------------------------------- Ember
        static PixelCanvas EmberFrame(int frame)
        {
            var p = new PixelCanvas(8, 8);
            Color32 core = frame % 2 == 0 ? Palette.FireWhite : Palette.FireYellow;
            p.Rect(3, 3, 2, 2, core);
            p.Set(3, 5, Palette.FireOrange); p.Set(4, 2, Palette.FireOrange);
            p.Set(2, 3 + frame % 2, Palette.FireRed); p.Set(5, 4 - frame % 2, Palette.FireRed);
            if (frame >= 2) p.Set(4, 6, Palette.FireYellow);
            return p;
        }

        // ---------------------------------------------------------------- Portal
        static PixelCanvas PortalCanvas(bool open, int frame)
        {
            var p = new PixelCanvas(16, 16);
            p.Ellipse(8, 8, 7.5f, 7.5f, Palette.StoneDark);
            p.Ellipse(8, 8, 6.5f, 6.5f, Palette.Stone);
            if (!open)
            {
                p.Ellipse(8, 8, 5f, 5f, Palette.StoneDark);
                // sealed rune cross
                p.Rect(7, 4, 2, 8, Palette.Metal);
                p.Rect(4, 7, 8, 2, Palette.Metal);
                return p;
            }
            p.Ellipse(8, 8, 5.2f, 5.2f, Palette.PortalDeep);
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                {
                    float dx = x + 0.5f - 8, dy = y + 0.5f - 8;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 5f) continue;
                    float ang = Mathf.Atan2(dy, dx) + frame * 0.8f + r * 0.7f;
                    if (Mathf.Sin(ang * 3f) > 0.3f) p.Set(x, y, Palette.Portal);
                    if (r < 1.8f) p.Set(x, y, Palette.PortalCore);
                }
            return p;
        }

        // ---------------------------------------------------------------- Decorations
        static PixelCanvas DecorationCanvas(int kind)
        {
            var p = new PixelCanvas(16, 16);
            switch (kind)
            {
                case 0: // skull: square-ish cranium, big sockets, teeth and a separate jaw (not a round ghost)
                    p.Rect(5, 5, 7, 5, Palette.Bone);
                    p.Rect(6, 10, 5, 1, Palette.Bone);
                    p.Rect(6, 6, 2, 2, Palette.FloorCrack); p.Rect(9, 6, 2, 2, Palette.FloorCrack);
                    p.Set(8, 5, Palette.BoneDark);
                    for (int x = 6; x <= 10; x++) p.Set(x, 4, x % 2 == 0 ? Palette.Bone : Palette.BoneDark);
                    p.Rect(6, 2, 5, 1, Palette.BoneDark);
                    p.Outline(Palette.FloorCrack);
                    break;
                case 1: // crossed bones
                    for (int i = 0; i < 8; i++)
                    {
                        p.Set(4 + i, 3 + i / 2, Palette.Bone);
                        p.Set(11 - i, 3 + i / 2, Palette.BoneDark);
                    }
                    p.Set(3, 3, Palette.Bone); p.Set(12, 3, Palette.BoneDark);
                    break;
                case 2: // rubble
                    p.Ellipse(6, 4, 2.5f, 2f, Palette.StoneDark);
                    p.Ellipse(10, 3, 2f, 1.5f, Palette.Stone);
                    p.Set(8, 6, Palette.StoneLight); p.Set(12, 5, Palette.StoneDark);
                    break;
                case 3: // cobweb hanging from the top-left corner: radials fanned *inside* the corner
                        // (not along the edges, which read as a letter) plus two sagging arcs.
                    var web = new Color32(Palette.BoneDark.r, Palette.BoneDark.g, Palette.BoneDark.b, 128);
                    foreach (float deg in new[] { 18f, 45f, 72f })
                    {
                        float ang = deg * Mathf.Deg2Rad;
                        for (float d = 0; d <= 11f; d += 0.5f)
                            p.Set(Mathf.RoundToInt(Mathf.Cos(ang) * d), 15 - Mathf.RoundToInt(Mathf.Sin(ang) * d), web);
                    }
                    foreach (float r in new[] { 4.5f, 8.5f })
                        for (float deg = 10f; deg <= 80f; deg += 3f)
                        {
                            float ang = deg * Mathf.Deg2Rad;
                            float sag = Mathf.Sin((deg - 10f) / 70f * Mathf.PI * 3f) * 0.6f; // droops between radials
                            float rr = r - Mathf.Abs(sag);
                            p.Set(Mathf.RoundToInt(Mathf.Cos(ang) * rr), 15 - Mathf.RoundToInt(Mathf.Sin(ang) * rr), web);
                        }
                    break;
                default: // candle stub
                    p.Rect(7, 2, 2, 4, Palette.Bone);
                    p.Rect(6, 1, 4, 1, Palette.BoneDark);
                    p.Set(7, 6, Palette.Metal);
                    break;
            }
            return p;
        }
    }
}
