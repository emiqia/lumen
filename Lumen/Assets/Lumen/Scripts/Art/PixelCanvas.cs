using UnityEngine;

namespace Lumen.Art
{
    /// <summary>
    /// Tiny CPU raster used to paint procedural pixel art before uploading it to a Texture2D.
    /// Origin is bottom-left, matching Unity texture coordinates.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height];
        }

        public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Set(int x, int y, Color32 c)
        {
            if (Inside(x, y)) Pixels[y * Width + x] = c;
        }

        public Color32 Get(int x, int y) => Inside(x, y) ? Pixels[y * Width + x] : Palette.Clear;

        public void Fill(Color32 c)
        {
            for (int i = 0; i < Pixels.Length; i++) Pixels[i] = c;
        }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int ix = x; ix < x + w; ix++)
                for (int iy = y; iy < y + h; iy++)
                    Set(ix, iy, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        /// <summary>Darkens opaque pixels that border transparency, giving sprites a readable outline.</summary>
        public void Outline(Color32 c)
        {
            var copy = (Color32[])Pixels.Clone();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (copy[y * Width + x].a != 0) continue;
                    bool near = false;
                    for (int d = 0; d < 4 && !near; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                        int ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (Inside(nx, ny) && copy[ny * Width + nx].a != 0) near = true;
                    }
                    if (near) Pixels[y * Width + x] = c;
                }
        }

        public Texture2D ToTexture(string name)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(Pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Same as <see cref="ToTexture"/> but keeps the texture linear, as required for normal maps.</summary>
        public Texture2D ToLinearTexture(string name)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(Pixels);
            tex.Apply(false, true);
            return tex;
        }
    }

    /// <summary>Hash-based value noise. Deterministic, allocation free.</summary>
    public static class Noise
    {
        public static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        public static float Value(float x, float y, int seed = 0)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed);
            float c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        public static float Fbm(float x, float y, int seed = 0, int octaves = 3)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(x, y, seed + i * 31) * amp;
                norm += amp;
                x *= 2; y *= 2; amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}
