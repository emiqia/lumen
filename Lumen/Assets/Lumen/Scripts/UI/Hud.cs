using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.Lighting;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>In-game HUD: fuel gauge, flare charge, floor/score, brazier tracker, announcements, hints, score popups.</summary>
    public class Hud : MonoBehaviour
    {
        RectTransform root;
        Image fuelFill, fuelBack, flareFill;
        PixelText fuelLabel, floorText, scoreText, brazierText, announceText, subAnnounceText, hintText, warningText;
        readonly List<Image> brazierPips = new List<Image>();
        float announceTimer, hintTimer, displayedScore;

        class Popup { public PixelText text; public Vector2 world; public float t; }
        readonly List<Popup> popups = new List<Popup>();
        RectTransform popupLayer;

        public static Hud Create(Transform canvas)
        {
            var root = Ui.Stretch(canvas, "HUD");
            var hud = root.gameObject.AddComponent<Hud>();
            hud.root = root;
            hud.Build();
            return hud;
        }

        void Build()
        {
            popupLayer = Ui.Stretch(root, "Popups");

            // Fuel gauge (top-left)
            Ui.Box(root, Ui.TopLeft, new Vector2(6, -6), new Vector2(94, 14), Palette.UiPanel);
            Ui.Text(root, "#", Ui.TopLeft, new Vector2(9, -9), 1, PixelText.Align.Left, Palette.FireOrange, 8);
            fuelBack = Ui.Box(root, Ui.TopLeft, new Vector2(17, -10), new Vector2(80, 6), new Color(0.12f, 0.08f, 0.1f));
            fuelFill = Ui.Box(root, Ui.TopLeft, new Vector2(17, -10), new Vector2(80, 6), Palette.FireYellow);
            fuelLabel = Ui.Text(root, "", Ui.TopLeft, new Vector2(6, -23), 1, PixelText.Align.Left, Palette.UiDim, 120);

            // Flare charge (below gauge)
            flareFill = Ui.Box(root, Ui.TopLeft, new Vector2(17, -17), new Vector2(80, 1), Palette.FireWhite);

            // Floor and score (top-right)
            floorText = Ui.Text(root, "", Ui.TopRight, new Vector2(-6, -6), 1, PixelText.Align.Right, Palette.UiText, 140);
            scoreText = Ui.Text(root, "", Ui.TopRight, new Vector2(-6, -17), 2, PixelText.Align.Right, Palette.FireYellow, 140);

            // Brazier tracker (top-center)
            brazierText = Ui.Text(root, "", Ui.Top, new Vector2(0, -6), 1, PixelText.Align.Center, Palette.UiDim, 200);

            // Announcements (center) and hints (bottom)
            announceText = Ui.Text(root, "", Ui.Center, new Vector2(0, 40), 3, PixelText.Align.Center, Palette.UiText, 380);
            subAnnounceText = Ui.Text(root, "", Ui.Center, new Vector2(0, 18), 1, PixelText.Align.Center, Palette.UiDim, 380);
            warningText = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 30), 1, PixelText.Align.Center, Palette.UiDanger, 380);
            Ui.Box(root, Ui.Bottom, new Vector2(0, 6), new Vector2(0, 0), Color.clear);
            hintText = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 10), 1, PixelText.Align.Center, Palette.UiText, 380);
        }

        public void SetBrazierCount(int total)
        {
            foreach (var p in brazierPips) Destroy(p.gameObject);
            brazierPips.Clear();
            float start = -(total - 1) * 5f;
            for (int i = 0; i < total; i++)
                brazierPips.Add(Ui.Box(root, Ui.Top, new Vector2(start + i * 10, -17), new Vector2(6, 6), Palette.StoneDark));
        }

        public void Announce(string big, string small = "", float duration = 2.5f, Color? color = null)
        {
            announceText.Text = big;
            announceText.color = color ?? Palette.UiText;
            subAnnounceText.Text = small;
            announceTimer = duration;
        }

        public void Hint(string text, float duration = 6f)
        {
            hintText.Text = text;
            hintTimer = duration;
        }

        public void ScorePopup(Vector2 world, int amount)
        {
            var t = Ui.Text(popupLayer, "+" + amount, Ui.BottomLeft, Vector2.zero, 1, PixelText.Align.Center, Palette.FireYellow, 40);
            popups.Add(new Popup { text = t, world = world + Vector2.up * 0.8f, t = 0 });
        }

        public void ClearTransient()
        {
            announceTimer = 0; hintTimer = 0;
            announceText.Text = subAnnounceText.Text = hintText.Text = "";
            foreach (var p in popups) Destroy(p.text.gameObject);
            popups.Clear();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var player = LightField.Player;
            float dt = Time.unscaledDeltaTime;

            if (player != null)
            {
                float f = player.FuelNormalized;
                bool low = player.Fuel <= GameTuning.I.lowFuelWarning;
                float pulse = low ? 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 12f) : 1f;
                fuelFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(80 * f), 6);
                fuelFill.color = Color.Lerp(Palette.FireRed, Palette.FireYellow, f * 1.4f) * pulse;
                fuelBack.color = player.IsRefueling ? new Color(0.25f, 0.15f, 0.05f) : new Color(0.12f, 0.08f, 0.1f);
                fuelLabel.Text = player.IsRefueling ? "+ + +" : player.IsDimmed ? "- - -" : "";
                fuelLabel.color = player.IsRefueling ? Palette.FireYellow : Palette.UiDim;

                float cd = GameTuning.I.flareCooldown;
                float charge = 1f - player.FlareCooldownLeft / cd;
                bool canAfford = player.Fuel > GameTuning.I.flareCost + 1f;
                flareFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(80 * charge), 1);
                flareFill.color = canAfford ? Palette.FireWhite : Palette.UiDanger;

                warningText.Text = low && player.IsAlive ? Locale.Get("hud.low") : "";
                warningText.color = new Color(1, 0.3f, 0.35f, pulse);
            }

            displayedScore = Mathf.MoveTowards(displayedScore, gm.Score, Mathf.Max(30f, Mathf.Abs(gm.Score - displayedScore) * 6f) * dt);
            scoreText.Text = Mathf.RoundToInt(displayedScore).ToString();
            floorText.Text = Locale.Get("hud.floor") + " " + gm.Floor;

            int lit = gm.BraziersLit, total = gm.BraziersTotal;
            brazierText.Text = gm.ExitOpen ? Locale.Get("hud.exit") : Locale.Get("hud.braziers") + " " + lit + "/" + total;
            brazierText.color = gm.ExitOpen ? Palette.Portal : Palette.UiDim;
            for (int i = 0; i < brazierPips.Count; i++)
                brazierPips[i].color = i < lit ? (Color)Palette.FireOrange : (Color)Palette.StoneDark;

            announceTimer -= dt;
            float a = Mathf.Clamp01(announceTimer * 2f);
            announceText.canvasRenderer.SetAlpha(a);
            subAnnounceText.canvasRenderer.SetAlpha(a);
            hintTimer -= dt;
            hintText.canvasRenderer.SetAlpha(Mathf.Clamp01(hintTimer * 2f));

            UpdatePopups(dt);
        }

        void UpdatePopups(float dt)
        {
            var cam = Camera.main;
            var canvas = GetComponentInParent<Canvas>();
            if (cam == null || canvas == null) return;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.t += dt;
                if (p.t > 0.9f) { Destroy(p.text.gameObject); popups.RemoveAt(i); continue; }
                Vector2 screen = cam.WorldToScreenPoint(p.world + Vector2.up * p.t * 0.8f);
                p.text.rectTransform.anchoredPosition = new Vector2(Mathf.Round(screen.x / canvas.scaleFactor) - 20,Mathf.Round(screen.y / canvas.scaleFactor));
                p.text.canvasRenderer.SetAlpha(1f - Mathf.Clamp01((p.t - 0.5f) / 0.4f));
            }
        }
    }
}
