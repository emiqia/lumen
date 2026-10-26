using System;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// The tutorial as an old journal: a painted open book (see <see cref="BookArt"/>), an
    /// illustration and title on the left page, the text with a red drop cap on the right.
    /// Opens with a fade, and turning a page folds the sheet over the spine.
    /// </summary>
    public class BookScreen : MonoBehaviour
    {
        public const int PageCount = 10;
        const int TextChars = 24;
        const float OpenTime = 0.28f, FlipTime = 0.36f;

        static readonly Color Ink = new Color32(0x3a, 0x24, 0x16, 255);
        static readonly Color InkFaded = new Color32(0x86, 0x6a, 0x48, 255);
        static readonly Color InkRed = new Color32(0x8c, 0x22, 0x18, 255);

        public bool IsOpen => root.gameObject.activeSelf;
        /// <summary>Raised when the book closes; true if the reader reached the end ("start" mode).</summary>
        public event Action<bool> Closed;

        RectTransform root, bookRect, art, leftPage, rightPage;
        CanvasGroup group;
        Image flipper;
        PixelText title, dropCap, body, pageNumber, hint;
        int page;
        bool startMode, closing, closeResult;
        float openT, flipT = -1f;
        int flipDir;

        public static BookScreen Create(Transform canvas)
        {
            var rt = Ui.Stretch(canvas, "Book");
            var book = rt.gameObject.AddComponent<BookScreen>();
            book.root = rt;
            book.Build();
            rt.gameObject.SetActive(false);
            return book;
        }

        static Vector2 PageCenter(bool right) =>
            new Vector2(BookArt.PageX(right) + BookArt.PageW / 2f - BookArt.Width / 2f, 0f);

        static PixelText InkText(RectTransform parent, Vector2 anchor, Vector2 pos, int scale, PixelText.Align align, Color color, float width)
        {
            var t = Ui.Text(parent, "", anchor, pos, scale, align, color, width);
            t.DropShadow = false; // ink on paper has no glow shadow
            return t;
        }

        void Build()
        {
            group = root.gameObject.AddComponent<CanvasGroup>();
            Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.03f, 0.9f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var cover = Ui.Box(root, Ui.Center, new Vector2(0, 8), new Vector2(BookArt.Width, BookArt.Height), Color.white);
            cover.sprite = BookArt.Book;
            cover.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bookRect = cover.rectTransform;

            leftPage = Ui.Panel(bookRect, "Left", Ui.Center, PageCenter(false), new Vector2(BookArt.PageW, BookArt.PageH));
            leftPage.pivot = new Vector2(0.5f, 0.5f);
            rightPage = Ui.Panel(bookRect, "Right", Ui.Center, PageCenter(true), new Vector2(BookArt.PageW, BookArt.PageH));
            rightPage.pivot = new Vector2(0.5f, 0.5f);

            // Left page: title, an ornamental rule, a framed illustration.
            title = InkText(leftPage, Ui.Top, new Vector2(0, -12), 2, PixelText.Align.Center, Ink, BookArt.PageW);
            Rule(leftPage, new Vector2(0, -34));
            var frame = Ui.Box(leftPage, Ui.Center, new Vector2(0, -14), new Vector2(124, 100), InkFaded);
            frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var inner = Ui.Box(frame.rectTransform, Ui.Center, Vector2.zero, new Vector2(122, 98), new Color32(0xdc, 0xca, 0x9c, 255));
            inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            art = Ui.Panel(inner.rectTransform, "Art", Ui.Center, Vector2.zero, new Vector2(122, 98));
            art.pivot = new Vector2(0.5f, 0.5f);

            // Right page: drop cap and body text.
            dropCap = InkText(rightPage, Ui.TopLeft, new Vector2(10, -12), 2, PixelText.Align.Left, InkRed, 20);
            body = InkText(rightPage, Ui.TopLeft, new Vector2(10, -14), 1, PixelText.Align.Left, Ink, BookArt.PageW - 16);
            pageNumber = InkText(rightPage, Ui.Bottom, new Vector2(0, 8), 1, PixelText.Align.Center, InkFaded, BookArt.PageW);
            Rule(rightPage, new Vector2(0, -BookArt.PageH + 22));

            // The sheet that folds over the spine when turning a page.
            flipper = Ui.Box(bookRect, Ui.Center, Vector2.zero, new Vector2(BookArt.PageW, BookArt.PageH), Color.white);
            flipper.sprite = BookArt.Page;
            flipper.enabled = false;

            hint = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 8), 1, PixelText.Align.Center, Palette.UiDim, 380);
        }

        /// <summary>A short ink rule with a diamond in the middle.</summary>
        static void Rule(RectTransform parent, Vector2 pos)
        {
            var line = Ui.Box(parent, Ui.Top, pos, new Vector2(90, 1), InkFaded);
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var diamond = Ui.Box(line.rectTransform, Ui.Center, Vector2.zero, new Vector2(3, 3), InkRed);
            diamond.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        /// <param name="beforeRun">True when opened before the first run: the last page says "begin".</param>
        public void Open(bool beforeRun)
        {
            startMode = beforeRun;
            page = 0;
            closing = false;
            openT = 0f;
            flipT = -1f;
            flipper.enabled = false;
            root.gameObject.SetActive(true);
            ShowLeft();
            ShowRight();
            Animate(0f);
        }

        /// <summary>Jumps to a page (1-based). Used by the screenshot tool.</summary>
        public void GoToPage(int number)
        {
            page = Mathf.Clamp(number - 1, 0, PageCount - 1);
            openT = OpenTime;
            Animate(1f);
            ShowLeft();
            ShowRight();
        }

        public void CloseSilently() => root.gameObject.SetActive(false);

        void BeginClose(bool finished)
        {
            closing = true;
            closeResult = finished;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (closing)
            {
                openT -= dt * 2f;
                Animate(Mathf.Clamp01(openT / OpenTime));
                if (openT <= 0f)
                {
                    root.gameObject.SetActive(false);
                    Closed?.Invoke(closeResult);
                }
                return;
            }
            if (openT < OpenTime) { openT += dt; Animate(Mathf.Clamp01(openT / OpenTime)); return; }
            if (flipT >= 0f) { UpdateFlip(dt); return; }

            var input = MenuInput.Read();
            bool last = page == PageCount - 1;
            if (input.Right || (input.Confirm && !last)) Turn(+1);
            else if (input.Left) Turn(-1);
            else if (input.Confirm && last) { Sfx.I?.Blip(); BeginClose(true); }
            else if (input.Back) { Sfx.I?.Blip(); BeginClose(startMode); }
        }

        /// <summary>Opening: fade in while the book rises and settles to full size.</summary>
        void Animate(float k)
        {
            float e = 1f - (1f - k) * (1f - k);
            group.alpha = e;
            bookRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
            bookRect.anchoredPosition = new Vector2(0, Mathf.Round(Mathf.Lerp(-6f, 8f, e)));
        }

        void Turn(int dir)
        {
            int next = Mathf.Clamp(page + dir, 0, PageCount - 1);
            if (next == page) return;
            page = next;
            flipDir = dir;
            flipT = 0f;
            Sfx.I?.Blip();
            // The page being revealed underneath is ready immediately; the other side changes when
            // the turning sheet passes the spine.
            if (dir > 0) ShowRight(); else ShowLeft();
            flipper.enabled = true;
            UpdateFlip(0f);
        }

        /// <summary>The sheet shrinks toward the spine, then grows out on the other side.</summary>
        void UpdateFlip(float dt)
        {
            float before = flipT;
            flipT += dt / FlipTime;
            bool fromRight = flipDir > 0;
            bool firstHalf = flipT < 0.5f;
            if (before < 0.5f && !firstHalf) { if (fromRight) ShowLeft(); else ShowRight(); }

            bool onRight = fromRight ? firstHalf : !firstHalf;
            float width = Mathf.Clamp01(firstHalf ? 1f - flipT * 2f : (flipT - 0.5f) * 2f);
            var rt = flipper.rectTransform;
            // Pivot on the spine so the sheet folds around it.
            rt.pivot = new Vector2(onRight ? 0f : 1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Mathf.Max(1f, Mathf.Round(BookArt.PageW * width)), BookArt.PageH);
            // Darker while it stands upright (catching less light).
            float shade = Mathf.Lerp(0.55f, 1f, width);
            flipper.color = new Color(shade, shade * 0.97f, shade * 0.92f, 1f);

            if (flipT >= 1f) { flipT = -1f; flipper.enabled = false; }
        }

        void ShowLeft()
        {
            int n = page + 1;
            title.Text = Locale.Get("book." + n + ".title");
            // Long titles drop to scale 1 so they never spill off the page.
            title.Scale = PixelFont.Measure(title.Text) * 2 > BookArt.PageW - 16 ? 1 : 2;
            BuildIllustration(n);
        }

        void ShowRight()
        {
            int n = page + 1;
            string text = Locale.Get("book." + n + ".body");
            // Red drop cap: the first letter, large; the first two lines leave room for it.
            dropCap.Text = text.Substring(0, 1);
            body.Text = Ui.Wrap(text.Substring(1), TextChars, indentLines: 2, indent: "   ");
            Ui.FitHeight(body);
            pageNumber.Text = "- " + n + " -";
            hint.Text = page == PageCount - 1 && startMode
                ? Locale.Get("book.last") + "      " + Locale.Get("book.hint")
                : Locale.Get("book.hint");
        }

        // ------------------------------------------------------------------ illustrations

        void BuildIllustration(int n)
        {
            for (int i = art.childCount - 1; i >= 0; i--) Destroy(art.GetChild(i).gameObject);
            switch (n)
            {
                case 1: Draw(SpriteBank.Player[0], Vector2.zero, 5); break;
                case 2:
                    Draw(SpriteBank.Brazier, new Vector2(-30, 0), 3); Draw(SpriteBank.BrazierCoals, new Vector2(-30, 0), 3);
                    Draw(SpriteBank.Brazier, new Vector2(30, 0), 3); Draw(SpriteBank.BrazierFire[0], new Vector2(30, 0), 3);
                    Caption("->", Vector2.zero);
                    break;
                case 3:
                    Draw(SpriteBank.Player[1], new Vector2(-30, 4), 2).color = new Color(1f, 1f, 1f, 0.6f);
                    Caption("CTRL", new Vector2(-30, -34));
                    Draw(SpriteBank.Player[2], new Vector2(30, 6), 4);
                    Caption("SHIFT", new Vector2(30, -34));
                    break;
                case 4:
                    Draw(SpriteBank.Player[0], Vector2.zero, 3);
                    Ring(18, Palette.FireYellow, 22);
                    Ring(32, Palette.FireOrange, 34);
                    break;
                case 5: Draw(SpriteBank.StalkerBody[0], Vector2.zero, 5); Draw(SpriteBank.StalkerEyes[SaveData.Colorblind ? 1 : 0], Vector2.zero, 5); break;
                case 6: Draw(SpriteBank.Moth[0], Vector2.zero, 5); Draw(SpriteBank.MothGlow, Vector2.zero, 5); break;
                case 7:
                    Draw(SpriteBank.DevourerBody[0], new Vector2(0, -8), 3);
                    Draw(SpriteBank.DevourerEyes[SaveData.Colorblind ? 1 : 0], new Vector2(0, -8), 3);
                    for (int i = 0; i < 3; i++)
                        Box(new Vector2(-14 + i * 14, 38), new Vector2(7, 7), i == 0 ? Palette.StoneDark : Palette.FireOrange);
                    break;
                case 8:
                    for (int i = 0; i < 4; i++) Draw(SpriteBank.Item((Level.ItemKind)i), new Vector2(-45 + i * 30, 0), 2);
                    break;
                case 9:
                    Draw(SpriteBank.DoorVertical, new Vector2(-40, 0), 2);
                    Box(new Vector2(4, 0), new Vector2(30, 20), BiomePalette.Flooded.Water);
                    Box(new Vector2(4, 4), new Vector2(30, 2), BiomePalette.Flooded.WaterLight);
                    for (int i = 0; i < 4; i++) Box(new Vector2(38 + i * 5, -8 + i * 6), new Vector2(9, 1), new Color(0.45f, 0.5f, 0.55f));
                    break;
                case 10:
                    Box(Vector2.zero, new Vector2(48, 66), Palette.FireYellow);
                    Box(Vector2.zero, new Vector2(44, 62), new Color32(0x2a, 0x1e, 0x30, 255));
                    Caption("*", new Vector2(0, 6), 4, Palette.FireYellow);
                    break;
            }
        }

        Image Draw(Sprite sprite, Vector2 pos, int scale) => Ui.Sprite(art, sprite, Ui.Center, pos, scale);

        void Box(Vector2 pos, Vector2 size, Color color)
        {
            var b = Ui.Box(art, Ui.Center, pos, size, color);
            b.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void Caption(string text, Vector2 pos, int scale = 1, Color? color = null)
        {
            var t = InkText(art, Ui.Center, pos, scale, PixelText.Align.Center, color ?? Ink, 80);
            t.Text = text;
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void Ring(int radius, Color color, int points)
        {
            for (int i = 0; i < points; i++)
            {
                float a = i / (float)points * Mathf.PI * 2f;
                Box(new Vector2(Mathf.Round(Mathf.Cos(a) * radius), Mathf.Round(Mathf.Sin(a) * radius)), new Vector2(2, 2), color);
            }
        }
    }
}
