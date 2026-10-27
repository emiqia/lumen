using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// uGUI Graphic that renders text with <see cref="PixelFont"/>. Builds one quad per glyph
    /// in OnPopulateMesh, with an optional 1px drop shadow for legibility over dark scenes.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelText : MaskableGraphic
    {
        public enum Align { Left, Center, Right }

        [SerializeField] string text = "";
        [SerializeField] int scale = 1;
        [SerializeField] Align align = Align.Left;
        [SerializeField] bool dropShadow = true;
        public Color shadowColor = new Color(0.04f, 0.03f, 0.06f, 0.9f);

        public string Text
        {
            get => text;
            set { if (text != value) { text = value ?? ""; SetVerticesDirty(); } }
        }

        public int Scale
        {
            get => scale;
            set { if (scale != value) { scale = Mathf.Max(1, value); SetVerticesDirty(); } }
        }

        public Align Alignment
        {
            get => align;
            set { align = value; SetVerticesDirty(); }
        }

        public bool DropShadow
        {
            get => dropShadow;
            set { dropShadow = value; SetVerticesDirty(); }
        }

        public override Texture mainTexture
        {
            get { PixelFont.Build(); return PixelFont.Atlas; }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (string.IsNullOrEmpty(text)) return;
            PixelFont.Build();

            var lines = text.Split('\n');
            float lineH = 10 * scale;
            float totalH = lines.Length * lineH - 2 * scale;
            var r = rectTransform.rect;
            float y = r.center.y + totalH / 2f - PixelFont.GlyphH * scale;

            foreach (var line in lines)
            {
                float w = PixelFont.Measure(line) * scale;
                float x = align == Align.Left ? r.xMin : align == Align.Right ? r.xMax - w : r.center.x - w / 2f;
                x = Mathf.Round(x);
                float ly = Mathf.Round(y);
                foreach (char ch in line)
                {
                    if (ch == ' ') { x += 4 * scale; continue; }
                    if (PixelFont.TryGet(ch, out var uv) || PixelFont.TryGet('?', out uv))
                    {
                        if (dropShadow) Quad(vh, x + scale, ly - scale, uv, shadowColor);
                        Quad(vh, x, ly, uv, color);
                    }
                    x += 6 * scale;
                }
                y -= lineH;
            }
        }

        void Quad(VertexHelper vh, float x, float y, Rect uv, Color c)
        {
            float w = PixelFont.GlyphW * scale, h = (PixelFont.GlyphH + PixelFont.Descent) * scale;
            y -= PixelFont.Descent * scale; // the atlas cell starts below the baseline
            int i = vh.currentVertCount;
            Color32 c32 = c;
            vh.AddVert(new Vector3(x, y), c32, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(new Vector3(x, y + h), c32, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(new Vector3(x + w, y + h), c32, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(new Vector3(x + w, y), c32, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
