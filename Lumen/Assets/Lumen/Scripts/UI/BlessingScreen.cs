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
    /// Between floors: pick one of three blessings. The veil fades in and the cards are dealt one
    /// by one (flip, rise, fade). The highlighted card lifts and glows; taking it makes the others
    /// fall away before the screen closes.
    /// </summary>
    public class BlessingScreen : MonoBehaviour
    {
        const int CardW = 112, CardH = 96, Gap = 10;
        const float InputDelay = 0.9f;  // flare-mashing at the end of a floor must not pick a card
        const float DealTime = 0.38f, DealStagger = 0.14f, OutTime = 0.45f;

        public bool IsOpen => root.gameObject.activeSelf;
        public event Action<Blessing> Chosen;

        RectTransform root;
        CanvasGroup group;
        PixelText title, hint;
        readonly List<RectTransform> cards = new List<RectTransform>();
        readonly List<CanvasGroup> cardGroups = new List<CanvasGroup>();
        readonly List<Image> frames = new List<Image>(), glows = new List<Image>(), icons = new List<Image>(), backs = new List<Image>();
        readonly List<PixelText> names = new List<PixelText>(), descs = new List<PixelText>();
        List<Blessing> offer;
        int selected;
        float t, outT = -1f;

        public static BlessingScreen Create(Transform canvas)
        {
            var rt = Ui.Stretch(canvas, "Blessings");
            var s = rt.gameObject.AddComponent<BlessingScreen>();
            s.root = rt;
            s.Build();
            rt.gameObject.SetActive(false);
            return s;
        }

        static float CardX(int i) => (i - 1) * (CardW + Gap);

        void Build()
        {
            group = root.gameObject.AddComponent<CanvasGroup>();
            Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.84f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title = Ui.Text(root, "", Ui.Center, new Vector2(0, 78), 2, PixelText.Align.Center, Palette.FireYellow, 380);
            for (int i = 0; i < 3; i++)
            {
                var card = Ui.Panel(root, "Card", Ui.Center, new Vector2(CardX(i), 0), new Vector2(CardW, CardH));
                card.pivot = new Vector2(0.5f, 0.5f);
                cardGroups.Add(card.gameObject.AddComponent<CanvasGroup>());

                // Soft warm glow behind the highlighted card.
                var glow = Ui.Box(card, Ui.Center, Vector2.zero, new Vector2(CardW + 10, CardH + 10), new Color(1f, 0.7f, 0.3f, 0f));
                glow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                glow.sprite = SpriteBank.SoftCircle;
                var frame = Ui.Box(card, Ui.Center, Vector2.zero, new Vector2(CardW, CardH), Palette.StoneDark);
                frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var inner = Ui.Box(frame.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 4, CardH - 4), new Color32(0x1c, 0x15, 0x24, 255));
                inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                // A thin inner border makes it read as a card, not a box.
                var border = Ui.Box(inner.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 10, CardH - 10), new Color32(0x3a, 0x2c, 0x44, 255));
                border.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var face = Ui.Box(border.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 12, CardH - 12), new Color32(0x1c, 0x15, 0x24, 255));
                face.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                var icon = Ui.Sprite(face.rectTransform, Icon("wick"), Ui.Top, new Vector2(0, -14), 2);
                icons.Add(icon);
                names.Add(Ui.Text(face.rectTransform, "", Ui.Top, new Vector2(0, -28), 1, PixelText.Align.Center, Palette.UiText, CardW - 12));
                descs.Add(Ui.Text(face.rectTransform, "", Ui.Top, new Vector2(0, -50), 1, PixelText.Align.Center, Palette.UiDim, CardW - 12));
                // Dealt face-down: the back covers the face until the flip passes its midpoint.
                var back = Ui.Box(frame.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 4, CardH - 4), new Color32(0x2a, 0x1a, 0x30, 255));
                back.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var backBorder = Ui.Box(back.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 14, CardH - 14), new Color32(0x4a, 0x30, 0x4e, 255));
                backBorder.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var backInner = Ui.Box(backBorder.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 16, CardH - 16), new Color32(0x2a, 0x1a, 0x30, 255));
                backInner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Ui.Text(backInner.rectTransform, "#", Ui.Center, new Vector2(0, 0), 3, PixelText.Align.Center, Palette.FireOrange, 40);
                backs.Add(back);
                cards.Add(card);
                frames.Add(frame);
                glows.Add(glow);
            }
            hint = Ui.Text(root, "", Ui.Center, new Vector2(0, -70), 1, PixelText.Align.Center, Palette.UiDim, 380);
        }

        public void Open(List<Blessing> choices)
        {
            offer = choices;
            selected = 0;
            t = 0f;
            outT = -1f;
            title.Text = Locale.Get("blessing.title");
            hint.Text = Locale.Get("blessing.hint");
            for (int i = 0; i < cards.Count; i++)
            {
                bool has = i < offer.Count;
                cards[i].gameObject.SetActive(has);
                if (!has) continue;
                icons[i].sprite = Icon(offer[i].Id);
                names[i].Text = Ui.Wrap(Locale.Get(offer[i].NameKey), 16);
                descs[i].Text = Ui.Wrap(Locale.Get(offer[i].DescKey), 16);
                Ui.FitHeight(names[i]);
                Ui.FitHeight(descs[i]);
            }
            root.gameObject.SetActive(true);
            Animate();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (outT >= 0f)
            {
                outT += dt;
                Animate();
                if (outT >= OutTime)
                {
                    root.gameObject.SetActive(false);
                    Chosen?.Invoke(offer[selected]);
                }
                return;
            }
            Animate();
            if (t < InputDelay || offer == null || offer.Count == 0) return;

            var input = MenuInput.Read();
            int before = selected;
            if (input.Left) selected = (selected - 1 + offer.Count) % offer.Count;
            if (input.Right) selected = (selected + 1) % offer.Count;
            if (selected != before) Sfx.I?.Blip();
            if (input.Confirm) Take();
        }

        /// <summary>Takes a card programmatically (used by the bot).</summary>
        public void Choose(int index)
        {
            if (!IsOpen || offer == null || offer.Count == 0 || outT >= 0f) return;
            selected = Mathf.Clamp(index, 0, offer.Count - 1);
            Take();
        }

        void Take()
        {
            Sfx.I?.Blip();
            outT = 0f;
        }

        static float EaseOut(float k) => 1f - (1f - k) * (1f - k) * (1f - k);

        void Animate()
        {
            group.alpha = outT >= 0f ? 1f - Mathf.Clamp01((outT - OutTime * 0.5f) / (OutTime * 0.5f)) : Mathf.Clamp01(t / 0.25f);
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].gameObject.activeSelf) continue;
                bool on = i == selected;
                float k = EaseOut(Mathf.Clamp01((t - 0.12f - i * DealStagger) / DealTime));
                float lift = on && outT < 0f ? 4f + Mathf.Sin(t * 4f) * 1f : 0f;
                float y = Mathf.Lerp(-36f, 0f, k) + lift;
                // Dealt face-down: the back narrows to nothing, then the face opens out.
                float scaleX = Mathf.Abs(1f - 2f * k);
                backs[i].enabled = k < 0.5f;
                backs[i].transform.GetChild(0).gameObject.SetActive(k < 0.5f);
                float scale = 1f;
                float alpha = k;

                if (outT >= 0f)
                {
                    float o = Mathf.Clamp01(outT / (OutTime * 0.6f));
                    if (on) { scale = 1f + 0.12f * EaseOut(o); y += 6f * o; }
                    else { alpha *= 1f - o; y -= 24f * o; }
                }
                cards[i].anchoredPosition = new Vector2(CardX(i), Mathf.Round(y));
                cards[i].localScale = new Vector3(scaleX * scale, scale, 1f);
                cardGroups[i].alpha = alpha;

                frames[i].color = on ? (Color)Palette.FireYellow : (Color)Palette.StoneDark;
                names[i].color = on ? Palette.FireYellow : Palette.UiText;
                glows[i].color = new Color(1f, 0.7f, 0.3f, on ? 0.35f + 0.1f * Mathf.Sin(t * 5f) : 0f);
            }
        }

        // ---- icons: one shape per kind of blessing, so a hand of three reads at a glance.
        static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        static readonly string[] Star = { "....#....", ".#..#..#.", "..#.#.#..", "...###...", "#########", "...###...", "..#.#.#..", ".#..#..#.", "....#...." };
        static readonly string[] Drop = { "....#....", "....#....", "...###...", "...###...", "..#####..", ".#######.", ".###.###.", "..#...#..", "...###..." };
        static readonly string[] Bowl = { "..#.#.#..", "...#.#...", ".........", "#########", ".#######.", "..#####..", "...###...", "..#...#..", ".#.....#." };
        static readonly string[] Sun = { "#...#...#", ".#.....#.", "...###...", "..#####..", "#.#####.#", "..#####..", "...###...", ".#.....#.", "#...#...#" };
        static readonly string[] Moon = { "...####..", "..##.....", ".##......", ".##......", ".##......", ".##......", ".##......", "..##.....", "...####.." };
        static readonly string[] Wing = { ".........", "#...#....", ".#...#...", "..#...#..", "...#...#.", "..#...#..", ".#...#...", "#...#....", "........." };
        static readonly string[] Heart = { ".........", ".##...##.", "####.####", "#########", "#########", ".#######.", "..#####..", "...###...", "....#...." };

        static Sprite Icon(string id)
        {
            if (iconCache.TryGetValue(id, out var cached)) return cached;
            var (rows, color) = id switch
            {
                "cheapflare" or "quickflare" or "echo" => (Star, (Color32)Palette.FireWhite),
                "fierce" or "dragon" => (Sun, (Color32)Palette.FireOrange),
                "hearth" or "bond" => (Bowl, (Color32)Palette.FireOrange),
                "shadow" or "owl" => (Moon, (Color32)Palette.Stealth),
                "swift" or "runner" => (Wing, new Color32(0xe8, 0xe0, 0xd0, 255)),
                "phoenix" => (Heart, new Color32(0xe8, 0x3a, 0x3a, 255)),
                _ => (Drop, (Color32)Palette.FireYellow),
            };
            var tex = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[81];
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    px[(8 - r) * 9 + c] = rows[r][c] == '#' ? color : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0.5f), 1f);
            iconCache[id] = sprite;
            return sprite;
        }
    }
}
