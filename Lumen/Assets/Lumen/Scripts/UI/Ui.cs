using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>Code-first UI construction helpers. Everything is laid out in "art pixels".</summary>
    public static class Ui
    {
        public static readonly Vector2 TopLeft = new Vector2(0, 1), TopRight = new Vector2(1, 1),
            Top = new Vector2(0.5f, 1), Center = new Vector2(0.5f, 0.5f), Bottom = new Vector2(0.5f, 0),
            BottomLeft = new Vector2(0, 0), BottomRight = new Vector2(1, 0);

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = true;
            go.AddComponent<PixelCanvasScaler>();
            return canvas;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var rt = Panel(parent, name, Center, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        public static PixelText Text(Transform parent, string text, Vector2 anchor, Vector2 pos, int scale = 1,
                                     PixelText.Align align = PixelText.Align.Left, Color? color = null, float width = 380)
        {
            var rt = Panel(parent, "Text", anchor, pos, new Vector2(width, 10 * scale));
            var t = rt.gameObject.AddComponent<PixelText>();
            t.Scale = scale;
            t.Alignment = align;
            t.Text = text;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            return t;
        }

        public static Image Box(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rt = Panel(parent, "Box", anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A sprite shown at an integer multiple of its pixel size (keeps pixel art crisp).</summary>
        public static Image Sprite(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, int scale)
        {
            var img = Box(parent, anchor, pos, sprite.rect.size * scale, Color.white);
            img.sprite = sprite;
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        /// <summary>
        /// PixelText centers its lines vertically in its rect. For top-anchored paragraphs, grow the
        /// rect to the real line count so the text starts at the top and flows down.
        /// </summary>
        public static void FitHeight(PixelText t)
        {
            int lines = string.IsNullOrEmpty(t.Text) ? 1 : t.Text.Split('\n').Length;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, lines * 10 * t.Scale);
        }

        /// <summary>Greedy word wrap for the pixel font, which has no automatic line breaking.</summary>
        public static string Wrap(string text, int maxChars)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var paragraph in text.Split('\n'))
            {
                int lineLen = 0;
                foreach (var word in paragraph.Split(' '))
                {
                    if (word.Length == 0) continue;
                    if (lineLen > 0 && lineLen + 1 + word.Length > maxChars) { sb.Append('\n'); lineLen = 0; }
                    else if (lineLen > 0) { sb.Append(' '); lineLen++; }
                    sb.Append(word);
                    lineLen += word.Length;
                }
                sb.Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }
    }

    /// <summary>
    /// Integer UI scaling: one art pixel = N screen pixels, with N chosen so the virtual
    /// canvas is at least 384x216. Keeps the pixel font perfectly crisp at any resolution.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class PixelCanvasScaler : MonoBehaviour
    {
        public const int RefWidth = 384, RefHeight = 216;
        Canvas canvas;

        void Awake() => canvas = GetComponent<Canvas>();

        void Update()
        {
            int s = Mathf.Max(1, Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight));
            if (!Mathf.Approximately(canvas.scaleFactor, s)) canvas.scaleFactor = s;
        }
    }
}
