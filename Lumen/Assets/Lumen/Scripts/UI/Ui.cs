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
