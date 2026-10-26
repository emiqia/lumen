using System;
using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// The tutorial as an old book: an illustration on the left page, the rule on the right,
    /// pages turned with left/right. Opens before the first run and from the "Guía" menu.
    /// </summary>
    public class BookScreen : MonoBehaviour
    {
        const int PageCount = 10;
        const int BookW = 344, BookH = 184, PageW = 162, PageH = 168;
        const int TextChars = 25;

        static readonly Color Leather = new Color32(0x3b, 0x26, 0x1a, 255);
        static readonly Color ParchmentL = new Color32(0xd9, 0xc8, 0xa0, 255);
        static readonly Color ParchmentR = new Color32(0xd2, 0xc0, 0x96, 255);
        static readonly Color Ink = new Color32(0x3a, 0x26, 0x18, 255);
        static readonly Color InkFaded = new Color32(0x7a, 0x60, 0x44, 255);

        public bool IsOpen => root.gameObject.activeSelf;
        /// <summary>Raised when the book closes; true if the reader reached the end ("start" mode).</summary>
        public event Action<bool> Closed;

        RectTransform root, art;
        PixelText title, body, pageNumber, hint;
        int page;
        bool startMode;
        float inputDelay;

        public static BookScreen Create(Transform canvas)
        {
            var rt = Ui.Stretch(canvas, "Book");
            var book = rt.gameObject.AddComponent<BookScreen>();
            book.root = rt;
            book.Build();
            rt.gameObject.SetActive(false);
            return book;
        }

        void Build()
        {
            Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.03f, 0.88f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var cover = Ui.Box(root, Ui.Center, new Vector2(0, 6), new Vector2(BookW, BookH), Leather);
            cover.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var left = Ui.Box(cover.rectTransform, Ui.Center, new Vector2(-PageW / 2 - 2, 0), new Vector2(PageW, PageH), ParchmentL);
            left.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var right = Ui.Box(cover.rectTransform, Ui.Center, new Vector2(PageW / 2 + 2, 0), new Vector2(PageW, PageH), ParchmentR);
            right.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // The spine's shadow between the pages.
            var spine = Ui.Box(cover.rectTransform, Ui.Center, Vector2.zero, new Vector2(4, PageH), new Color(0.2f, 0.12f, 0.08f, 1f));
            spine.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            title = Ui.Text(left.rectTransform, "", Ui.Top, new Vector2(0, -8), 2, PixelText.Align.Center, Ink, PageW);
            art = Ui.Panel(left.rectTransform, "Art", Ui.Center, new Vector2(0, -10), new Vector2(PageW, 110));
            body = Ui.Text(right.rectTransform, "", Ui.TopLeft, new Vector2(8, -10), 1, PixelText.Align.Left, Ink, PageW - 12);
            pageNumber = Ui.Text(right.rectTransform, "", Ui.Bottom, new Vector2(0, 6), 1, PixelText.Align.Center, InkFaded, PageW);
            hint = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 8), 1, PixelText.Align.Center, Palette.UiDim, 380);
        }

        /// <param name="beforeRun">True when opened before the first run: the last page says "begin".</param>
        public void Open(bool beforeRun)
        {
            startMode = beforeRun;
            page = 0;
            inputDelay = 0.3f;
            root.gameObject.SetActive(true);
            ShowPage();
        }

        void Close(bool finished)
        {
            root.gameObject.SetActive(false);
            Closed?.Invoke(finished);
        }

        void Update()
        {
            inputDelay -= Time.unscaledDeltaTime;
            if (inputDelay > 0f) return;
            var input = MenuInput.Read();
            bool last = page == PageCount - 1;
            if (input.Right || (input.Confirm && !last)) Turn(+1);
            else if (input.Left) Turn(-1);
            else if (input.Confirm && last) { Sfx.I?.Blip(); Close(true); }
            else if (input.Back) { Sfx.I?.Blip(); Close(startMode); }
        }

        void Turn(int dir)
        {
            int next = Mathf.Clamp(page + dir, 0, PageCount - 1);
            if (next == page) return;
            page = next;
            Sfx.I?.Blip();
            ShowPage();
        }

        void ShowPage()
        {
            int n = page + 1;
            title.Text = Locale.Get("book." + n + ".title");
            body.Text = Ui.Wrap(Locale.Get("book." + n + ".body"), TextChars);
            pageNumber.Text = n + "/" + PageCount;
            hint.Text = page == PageCount - 1 && startMode ? Locale.Get("book.last") + "     " + Locale.Get("book.hint") : Locale.Get("book.hint");
            BuildIllustration(n);
        }

        // ------------------------------------------------------------------ illustrations

        void BuildIllustration(int n)
        {
            for (int i = art.childCount - 1; i >= 0; i--) Destroy(art.GetChild(i).gameObject);
            switch (n)
            {
                case 1: Draw(SpriteBank.Player[0], Vector2.zero, 5); break;
                case 2:
                    Draw(SpriteBank.Brazier, new Vector2(-34, 0), 3); Draw(SpriteBank.BrazierCoals, new Vector2(-34, 0), 3);
                    Draw(SpriteBank.Brazier, new Vector2(34, 0), 3); Draw(SpriteBank.BrazierFire[0], new Vector2(34, 0), 3);
                    Caption("->", Vector2.zero);
                    break;
                case 3:
                    Draw(SpriteBank.Player[1], new Vector2(-36, -6), 2).color = new Color(1f, 1f, 1f, 0.6f);
                    Caption("CTRL", new Vector2(-36, -36));
                    Draw(SpriteBank.Player[2], new Vector2(36, 0), 4);
                    Caption("SHIFT", new Vector2(36, -36));
                    break;
                case 4:
                    Draw(SpriteBank.Player[0], Vector2.zero, 3);
                    Ring(18, Palette.FireYellow, 22);
                    Ring(32, Palette.FireOrange, 34);
                    break;
                case 5: Draw(SpriteBank.StalkerBody[0], Vector2.zero, 5); Draw(SpriteBank.StalkerEyes[SaveData.Colorblind ? 1 : 0], Vector2.zero, 5); break;
                case 6: Draw(SpriteBank.Moth[0], Vector2.zero, 5); Draw(SpriteBank.MothGlow, Vector2.zero, 5); break;
                case 7:
                    Draw(SpriteBank.DevourerBody[0], new Vector2(0, -6), 3);
                    Draw(SpriteBank.DevourerEyes[SaveData.Colorblind ? 1 : 0], new Vector2(0, -6), 3);
                    for (int i = 0; i < 3; i++)
                        Box(new Vector2(-14 + i * 14, 44), new Vector2(8, 8), i == 0 ? Palette.StoneDark : Palette.FireOrange);
                    break;
                case 8:
                    for (int i = 0; i < 4; i++) Draw(SpriteBank.Item((Level.ItemKind)i), new Vector2(-54 + i * 36, 0), 2);
                    break;
                case 9:
                    Draw(SpriteBank.DoorVertical, new Vector2(-48, 0), 2);
                    Box(new Vector2(0, 0), new Vector2(34, 22), BiomePalette.Flooded.Water);
                    Box(new Vector2(0, 4), new Vector2(34, 2), BiomePalette.Flooded.WaterLight);
                    for (int i = 0; i < 4; i++) Box(new Vector2(40 + i * 6, -8 + i * 6), new Vector2(10, 1), new Color(0.75f, 0.8f, 0.85f));
                    break;
                case 10:
                    Box(Vector2.zero, new Vector2(54, 72), Palette.UiPanel);
                    Box(Vector2.zero, new Vector2(50, 68), new Color32(0x2a, 0x1e, 0x30, 255));
                    Caption("*", new Vector2(0, 8), 4, Palette.FireYellow);
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
            var t = Ui.Text(art, text, Ui.Center, pos, scale, PixelText.Align.Center, color ?? Ink, 80);
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
