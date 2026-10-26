using Lumen.Art;
using UnityEngine;

namespace Lumen.UI
{
    /// <summary>
    /// Procedurally painted open book for the tutorial: leather covers with stitching and
    /// corner guards, aged paper with grain and stains, a shaded gutter, curved page edges and
    /// the stacked edges of the pages underneath. Also the single page used by the flip animation.
    /// </summary>
    public static class BookArt
    {
        public const int Width = 352, Height = 196;
        public const int PageW = 160, PageH = 176;
        public const int Gutter = 6; // gap between the two pages at the spine

        static readonly Color32 Leather = new Color32(0x4a, 0x2b, 0x1c, 255);
        static readonly Color32 LeatherDark = new Color32(0x2e, 0x1a, 0x11, 255);
        static readonly Color32 LeatherLight = new Color32(0x6b, 0x40, 0x29, 255);
        static readonly Color32 Stitch = new Color32(0xb8, 0x93, 0x5e, 255);
        static readonly Color32 Brass = new Color32(0xc9, 0x9a, 0x45, 255);
        static readonly Color32 Paper = new Color32(0xe6, 0xd6, 0xae, 255);
        static readonly Color32 PaperShade = new Color32(0xc4, 0xae, 0x80, 255);
        static readonly Color32 PageEdge = new Color32(0xa8, 0x92, 0x66, 255);

        static Sprite book, page;

        public static Sprite Book => book != null ? book : (book = Make(PaintBook(), "book"));
        public static Sprite Page => page != null ? page : (page = Make(PaintPage(false), "book_page"));

        static Sprite Make(PixelCanvas c, string name)
        {
            var tex = c.ToTexture(name);
            return Sprite.Create(tex, new Rect(0, 0, c.Width, c.Height), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Left edge (x) of a page inside the book art.</summary>
        public static int PageX(bool right) => right ? Width / 2 + Gutter / 2 : Width / 2 - Gutter / 2 - PageW;
        public const int PageY = (Height - PageH) / 2;

        static PixelCanvas PaintBook()
        {
            var p = new PixelCanvas(Width, Height);

            // Leather cover with a darker rim and subtle grain.
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    bool rim = x < 3 || y < 3 || x >= Width - 3 || y >= Height - 3;
                    float grain = Noise.Fbm(x * 0.2f, y * 0.2f, 7) - 0.5f;
                    var c = rim ? LeatherDark : Palette.Lerp(Leather, LeatherLight, Mathf.Clamp01(0.4f + grain * 0.8f));
                    p.Set(x, y, c);
                }
            // Stitching around the cover.
            for (int x = 6; x < Width - 6; x += 4) { p.Set(x, 5, Stitch); p.Set(x, Height - 6, Stitch); }
            for (int y = 6; y < Height - 6; y += 4) { p.Set(5, y, Stitch); p.Set(Width - 6, y, Stitch); }
            // Brass corner guards.
            foreach (var (cx, cy, dx, dy) in new[] { (0, 0, 1, 1), (Width - 1, 0, -1, 1), (0, Height - 1, 1, -1), (Width - 1, Height - 1, -1, -1) })
                for (int i = 0; i < 9; i++)
                    for (int j = 0; j < 9 - i; j++)
                        p.Set(cx + dx * i, cy + dy * j, (i + j) % 4 == 0 ? LeatherDark : Brass);

            // Stacked page edges peeking out under each page (the book has thickness).
            for (int layer = 3; layer >= 1; layer--)
                foreach (bool right in new[] { false, true })
                {
                    int px = PageX(right) + (right ? layer : -layer);
                    for (int y = PageY + layer; y < PageY + PageH + layer; y++)
                        for (int x = px; x < px + PageW; x++)
                            p.Set(x, y, layer % 2 == 0 ? PageEdge : PaperShade);
                }

            // The two pages.
            foreach (bool right in new[] { false, true })
            {
                var sheet = PaintPage(right);
                int ox = PageX(right);
                for (int x = 0; x < PageW; x++)
                    for (int y = 0; y < PageH; y++)
                    {
                        var c = sheet.Get(x, y);
                        if (c.a > 0) p.Set(ox + x, PageY + y, c);
                    }
            }

            // Spine shadow down the middle.
            for (int y = PageY - 2; y < PageY + PageH + 2; y++)
                for (int x = Width / 2 - Gutter / 2; x < Width / 2 + Gutter / 2; x++)
                    p.Set(x, y, LeatherDark);
            return p;
        }

        /// <summary>One sheet: aged paper, darker toward the spine and the edges, curved top and bottom.</summary>
        public static PixelCanvas PaintPage(bool right)
        {
            var p = new PixelCanvas(PageW, PageH);
            for (int x = 0; x < PageW; x++)
            {
                float fromSpine = right ? x / (float)PageW : 1f - x / (float)PageW;
                // Pages bow upward toward the spine (1-2 px), like a real open book.
                int bow = Mathf.RoundToInt((1f - fromSpine) * 2f);
                for (int y = 0; y < PageH; y++)
                {
                    if (y < bow || y >= PageH - bow) continue;
                    float grain = Noise.Fbm(x * 0.35f + (right ? 100 : 0), y * 0.35f, 3) - 0.5f;
                    float stain = Mathf.Clamp01((Noise.Fbm(x * 0.04f + (right ? 50 : 0), y * 0.04f, 11) - 0.62f) * 4f);
                    float gutter = Mathf.Clamp01(1f - fromSpine * 6f);           // shadow near the spine
                    float edge = Mathf.Clamp01(1f - Mathf.Min(Mathf.Min(x, PageW - 1 - x), Mathf.Min(y, PageH - 1 - y)) / 5f);
                    float shade = Mathf.Clamp01(0.08f + grain * 0.25f + stain * 0.35f + gutter * 0.55f + edge * 0.3f);
                    p.Set(x, y, Palette.Lerp(Paper, PaperShade, shade));
                }
            }
            return p;
        }
    }
}
