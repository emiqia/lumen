using System;
using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>Between floors: pick one of three blessings as cards. Left/right selects, confirm takes.</summary>
    public class BlessingScreen : MonoBehaviour
    {
        const int CardW = 112, CardH = 92, Gap = 8;
        const float InputDelay = 0.7f; // flare-mashing at the end of a floor must not pick a card

        public bool IsOpen => root.gameObject.activeSelf;
        public event Action<Blessing> Chosen;

        RectTransform root;
        readonly List<Image> frames = new List<Image>();
        readonly List<PixelText> names = new List<PixelText>(), descs = new List<PixelText>();
        List<Blessing> offer;
        int selected;
        float delay;

        public static BlessingScreen Create(Transform canvas)
        {
            var rt = Ui.Stretch(canvas, "Blessings");
            var s = rt.gameObject.AddComponent<BlessingScreen>();
            s.root = rt;
            s.Build();
            rt.gameObject.SetActive(false);
            return s;
        }

        void Build()
        {
            Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.82f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Ui.Text(root, Locale.Get("blessing.title"), Ui.Center, new Vector2(0, 74), 2, PixelText.Align.Center, Palette.FireYellow, 380);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * (CardW + Gap);
                var frame = Ui.Box(root, Ui.Center, new Vector2(x, 0), new Vector2(CardW, CardH), Palette.UiDim);
                frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var inner = Ui.Box(frame.rectTransform, Ui.Center, Vector2.zero, new Vector2(CardW - 4, CardH - 4), new Color32(0x1c, 0x15, 0x24, 255));
                inner.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                Ui.Text(inner.rectTransform, "*", Ui.Top, new Vector2(0, -8), 2, PixelText.Align.Center, Palette.FireOrange, CardW);
                names.Add(Ui.Text(inner.rectTransform, "", Ui.Top, new Vector2(0, -30), 1, PixelText.Align.Center, Palette.UiText, CardW - 8));
                descs.Add(Ui.Text(inner.rectTransform, "", Ui.Top, new Vector2(0, -52), 1, PixelText.Align.Center, Palette.UiDim, CardW - 8));
                frames.Add(frame);
            }
            Ui.Text(root, "", Ui.Center, new Vector2(0, -64), 1, PixelText.Align.Center, Palette.UiDim, 380).Text = Locale.Get("blessing.hint");
        }

        public void Open(List<Blessing> choices)
        {
            offer = choices;
            selected = 0;
            delay = InputDelay;
            for (int i = 0; i < 3; i++)
            {
                bool has = i < offer.Count;
                frames[i].gameObject.SetActive(has);
                if (!has) continue;
                names[i].Text = Ui.Wrap(Locale.Get(offer[i].NameKey), 17);
                descs[i].Text = Ui.Wrap(Locale.Get(offer[i].DescKey), 17);
            }
            root.gameObject.SetActive(true);
            Refresh();
        }

        void Update()
        {
            delay -= Time.unscaledDeltaTime;
            if (delay > 0f || offer == null || offer.Count == 0) return;
            var input = MenuInput.Read();
            int before = selected;
            if (input.Left) selected = (selected - 1 + offer.Count) % offer.Count;
            if (input.Right) selected = (selected + 1) % offer.Count;
            if (selected != before) { Sfx.I?.Blip(); Refresh(); }
            if (input.Confirm)
            {
                Sfx.I?.Blip();
                root.gameObject.SetActive(false);
                Chosen?.Invoke(offer[selected]);
            }
        }

        void Refresh()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                bool on = i == selected;
                frames[i].color = on ? (Color)Palette.FireYellow : (Color)Palette.StoneDark;
                names[i].color = on ? Palette.FireYellow : Palette.UiText;
            }
        }
    }
}
