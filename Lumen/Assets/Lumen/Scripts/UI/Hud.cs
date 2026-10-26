using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.Lighting;
using Lumen.World;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// In-game HUD. Visual hierarchy follows what matters to survive: the fuel gauge and the
    /// objective are the loudest elements; the score is secondary. An edge arrow points to the
    /// nearest unlit brazier (or the open exit) when it is off-screen.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        const int FuelBarWidth = 100, FuelBarHeight = 7;
        const int PipSize = 8, PipGap = 12;
        // Arrow margins keep it clear of the HUD bands (gauges on top, hints at the bottom).
        const float EdgeInsetSide = 16f, EdgeInsetTop = 44f, EdgeInsetBottom = 46f;
        const int ArrowSize = 13;

        RectTransform root;
        Camera cam;
        Canvas canvas;
        Image fuelFill, fuelBack, flareFill, objectiveArrow, hintBand;
        PixelText fuelLabel, flareIcon, floorText, scoreText, brazierText, announceText, subAnnounceText, hintText, warningText;
        readonly List<Image> pipFills = new List<Image>();
        readonly List<GameObject> pipObjects = new List<GameObject>();
        float announceTimer, hintTimer, displayedScore, scorePop;
        int shownScore = -1, shownFloor = -1, shownLit = -1, shownTotal = -1;
        bool shownExitOpen;
        string shownFuelLabel;

        class Popup { public PixelText text; public Vector2 world; public float t; }
        readonly List<Popup> popups = new List<Popup>();
        RectTransform popupLayer;
        float lastPopupTime;
        int popupStack;

        public static Hud Create(Transform canvasRoot, Camera camera)
        {
            var root = Ui.Stretch(canvasRoot, "HUD");
            var hud = root.gameObject.AddComponent<Hud>();
            hud.root = root;
            hud.cam = camera;
            hud.canvas = canvasRoot.GetComponent<Canvas>();
            hud.Build();
            return hud;
        }

        void Build()
        {
            popupLayer = Ui.Stretch(root, "Popups");

            // Fuel gauge (top-left): flame icon, bar with quarter ticks, state label underneath.
            // The panel also covers the state label underneath, so it never sits on top of the world.
            Ui.Box(root, Ui.TopLeft, new Vector2(6, -6), new Vector2(FuelBarWidth + 26, 32), Palette.UiPanel);
            Ui.Text(root, "#", Ui.TopLeft, new Vector2(9, -9), 1, PixelText.Align.Left, Palette.FireOrange, 8);
            fuelBack = Ui.Box(root, Ui.TopLeft, new Vector2(18, -10), new Vector2(FuelBarWidth, FuelBarHeight), new Color(0.12f, 0.08f, 0.1f));
            fuelFill = Ui.Box(root, Ui.TopLeft, new Vector2(18, -10), new Vector2(FuelBarWidth, FuelBarHeight), Palette.FireYellow);
            for (int i = 1; i < 4; i++)
                Ui.Box(root, Ui.TopLeft, new Vector2(18 + FuelBarWidth * i / 4, -10), new Vector2(1, FuelBarHeight), Palette.Void);

            // Flare charge: a 2px bar under the gauge with a star that lights up when ready.
            flareFill = Ui.Box(root, Ui.TopLeft, new Vector2(18, -19), new Vector2(FuelBarWidth, 2), Palette.FireWhite);
            flareIcon = Ui.Text(root, "*", Ui.TopLeft, new Vector2(FuelBarWidth + 20, -16), 1, PixelText.Align.Left, Palette.FireWhite, 8);
            fuelLabel = Ui.Text(root, "", Ui.TopLeft, new Vector2(7, -27), 1, PixelText.Align.Left, Palette.UiDim, 160);

            // Floor and score (top-right): informative, deliberately quieter than the gauge.
            floorText = Ui.Text(root, "", Ui.TopRight, new Vector2(-6, -6), 1, PixelText.Align.Right, Palette.UiDim, 140);
            scoreText = Ui.Text(root, "", Ui.TopRight, new Vector2(-6, -17), 1, PixelText.Align.Right, Palette.FireYellow, 140);

            // Objective tracker (top-center).
            brazierText = Ui.Text(root, "", Ui.Top, new Vector2(0, -6), 1, PixelText.Align.Center, Palette.UiText, 220);

            // Edge arrow toward the current objective.
            objectiveArrow = Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(ArrowSize, ArrowSize), Palette.FireOrange);
            objectiveArrow.sprite = SpriteBank.Arrow;
            objectiveArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Announcements (center), warnings and hints (bottom). Hints sit on a dark band for contrast.
            announceText = Ui.Text(root, "", Ui.Center, new Vector2(0, 40), 3, PixelText.Align.Center, Palette.UiText, 380);
            subAnnounceText = Ui.Text(root, "", Ui.Center, new Vector2(0, 18), 1, PixelText.Align.Center, Palette.UiDim, 380);
            warningText = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 30), 1, PixelText.Align.Center, Palette.UiDanger, 380);
            hintBand = Ui.Box(root, Ui.Bottom, new Vector2(0, 6), new Vector2(392, 16), Palette.UiPanel);
            hintText = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 10), 1, PixelText.Align.Center, Palette.UiText, 380);
        }

        public void SetBrazierCount(int total)
        {
            foreach (var p in pipObjects) Destroy(p);
            pipObjects.Clear();
            pipFills.Clear();
            float start = -(total - 1) * PipGap * 0.5f;
            for (int i = 0; i < total; i++)
            {
                var pos = new Vector2(start + i * PipGap - PipSize * 0.5f, -17);
                // Empty pip = outline only; lit pip = solid orange with a highlight row.
                var outline = Ui.Box(root, Ui.Top, pos, new Vector2(PipSize, PipSize), Palette.UiDim);
                outline.rectTransform.pivot = new Vector2(0, 1);
                Ui.Box(outline.rectTransform, Ui.TopLeft, new Vector2(1, -1), new Vector2(PipSize - 2, PipSize - 2), Palette.Void);
                var fill = Ui.Box(outline.rectTransform, Ui.TopLeft, new Vector2(1, -1), new Vector2(PipSize - 2, PipSize - 2), Palette.FireOrange);
                Ui.Box(fill.rectTransform, Ui.TopLeft, Vector2.zero, new Vector2(PipSize - 2, 1), Palette.FireYellow);
                pipObjects.Add(outline.gameObject);
                pipFills.Add(fill);
            }
            shownLit = shownTotal = -1;
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
            // Stagger popups that arrive together so they never overlap each other.
            popupStack = Time.unscaledTime - lastPopupTime < 0.3f ? popupStack + 1 : 0;
            lastPopupTime = Time.unscaledTime;
            var color = amount >= 50 ? Palette.FireYellow : Palette.UiText;
            var t = Ui.Text(popupLayer, "+" + amount, Ui.BottomLeft, Vector2.zero, 1, PixelText.Align.Center, color, 40);
            popups.Add(new Popup { text = t, world = world + Vector2.up * (1.3f + popupStack * 0.4f), t = 0 });
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

            if (player != null) UpdateGauges(player);

            // Score: counts up, pops briefly when it changes.
            if (gm.Score != shownScore) { if (shownScore >= 0) scorePop = 0.15f; shownScore = gm.Score; }
            displayedScore = Mathf.MoveTowards(displayedScore, gm.Score, Mathf.Max(30f, Mathf.Abs(gm.Score - displayedScore) * 6f) * dt);
            scoreText.Text = Mathf.RoundToInt(displayedScore).ToString();
            // A brief white flash instead of doubling in size: noticeable, but never the loudest thing on screen.
            scorePop -= dt;
            scoreText.color = scorePop > 0f ? Palette.FireWhite : Palette.FireYellow;

            if (gm.Floor != shownFloor) { shownFloor = gm.Floor; floorText.Text = Locale.Get("hud.floor") + " " + gm.Floor; }

            int lit = gm.BraziersLit, total = gm.BraziersTotal;
            if (lit != shownLit || total != shownTotal || gm.ExitOpen != shownExitOpen)
            {
                shownLit = lit; shownTotal = total; shownExitOpen = gm.ExitOpen;
                // Short label up top; the full sentence is the one-time banner in the center.
                brazierText.Text = gm.ExitOpen ? Locale.Get("hud.exitShort") : Locale.Get("hud.braziers") + " " + lit + "/" + total;
                brazierText.color = gm.ExitOpen ? Palette.Portal : Palette.UiText;
                for (int i = 0; i < pipFills.Count; i++) pipFills[i].enabled = i < lit;
            }

            UpdateObjectiveArrow(gm);

            announceTimer -= dt;
            float a = Mathf.Clamp01(announceTimer * 2f);
            announceText.canvasRenderer.SetAlpha(a);
            subAnnounceText.canvasRenderer.SetAlpha(a);
            hintTimer -= dt;
            float hintAlpha = Mathf.Clamp01(hintTimer * 2f);
            hintText.canvasRenderer.SetAlpha(hintAlpha);
            hintBand.canvasRenderer.SetAlpha(hintAlpha);

            UpdatePopups(dt);
        }

        void UpdateGauges(Player.PlayerFlame player)
        {
            var tuning = GameTuning.I;
            float f = player.FuelNormalized;
            bool low = player.Fuel <= tuning.lowFuelWarning;
            // 0..1 heartbeat while low. It pulses brightness, never alpha: the gauge must stay
            // fully visible exactly when the player is about to die.
            float pulse = low ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f) : 0f;
            // Never shrink to nothing while there is fuel left: 2 px minimum.
            float width = player.Fuel > 0f ? Mathf.Max(2f, Mathf.Round(FuelBarWidth * f)) : 0f;
            fuelFill.rectTransform.sizeDelta = new Vector2(width, FuelBarHeight);
            // Discrete thresholds read faster than a gradient: yellow > 50%, orange > 25%, red below.
            Color fuelColor = f > 0.5f ? Palette.FireYellow : f > 0.25f ? Palette.FireOrange : Palette.FireRed;
            fuelFill.color = Color.Lerp(fuelColor, Palette.FireWhite, pulse * 0.6f);
            Color idleBack = new Color(0.12f, 0.08f, 0.1f);
            fuelBack.color = player.IsRefueling ? new Color(0.25f, 0.15f, 0.05f)
                : low ? Color.Lerp(idleBack, Palette.FireDeep, pulse) : idleBack;

            string label = !player.IsAlive ? "" : player.IsRefueling ? Locale.Get("hud.refueling") : player.IsDimmed ? Locale.Get("hud.dimmed") : "";
            if (label != shownFuelLabel)
            {
                shownFuelLabel = label;
                fuelLabel.Text = label;
                // Dimmed is a protective state the player chose: cool "stealth" blue, not disabled grey.
                fuelLabel.color = player.IsRefueling ? Palette.FireYellow : (Color)Palette.Stealth;
            }

            float charge = 1f - player.FlareCooldownLeft / tuning.flareCooldown;
            bool canAfford = player.CanAffordFlare;
            bool ready = charge >= 1f && canAfford;
            flareFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(FuelBarWidth * charge), 2);
            flareFill.color = canAfford ? Palette.FireWhite : Palette.UiDanger;
            flareIcon.color = ready ? Palette.FireWhite : new Color(0.35f, 0.32f, 0.38f);

            warningText.Text = low && player.IsAlive ? Locale.Get("hud.low") : "";
            warningText.color = Color.Lerp(Palette.UiDanger, Palette.FireWhite, pulse * 0.5f);
        }

        void UpdateObjectiveArrow(GameManager gm)
        {
            var player = LightField.Player;
            Transform target = null;
            if (gm.Current == GameManager.State.Playing && player != null && gm.Level != null)
            {
                if (gm.ExitOpen) target = gm.Level.Exit.transform;
                else target = NearestUnlit(gm.Level.Braziers, player.transform.position);
            }
            if (target == null || cam == null) { objectiveArrow.enabled = false; return; }

            Vector3 vp = cam.WorldToViewportPoint(target.position);
            bool onScreen = vp.x > 0.04f && vp.x < 0.96f && vp.y > 0.06f && vp.y < 0.94f;
            objectiveArrow.enabled = !onScreen;
            if (onScreen) return;

            Vector2 size = root.rect.size;
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            // Usable rectangle (relative to screen center), shrunk by the HUD margins.
            float maxX = size.x * 0.5f - EdgeInsetSide;
            float maxY = dir.y > 0 ? size.y * 0.5f - EdgeInsetTop : size.y * 0.5f - EdgeInsetBottom;
            Vector2 ray = new Vector2(dir.x * size.x, dir.y * size.y);
            float scale = Mathf.Min(maxX / Mathf.Max(1e-4f, Mathf.Abs(ray.x)), maxY / Mathf.Max(1e-4f, Mathf.Abs(ray.y)));
            Vector2 pos = ray * scale;
            float bob = Mathf.Sin(Time.unscaledTime * 6f) * 1.5f;
            pos += dir.normalized * bob;
            objectiveArrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            // Braziers: rotated orange arrow. Exit: blue diamond (different shape, not just color).
            objectiveArrow.sprite = gm.ExitOpen ? SpriteBank.ExitMarker : SpriteBank.Arrow;
            objectiveArrow.rectTransform.localRotation = gm.ExitOpen ? Quaternion.identity
                : Quaternion.Euler(0, 0, Mathf.Atan2(dir.y * size.y, dir.x * size.x) * Mathf.Rad2Deg);
            objectiveArrow.color = gm.ExitOpen ? Palette.Portal : Palette.FireOrange;
        }

        static Transform NearestUnlit(List<Brazier> braziers, Vector2 from)
        {
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var b in braziers)
            {
                if (b.IsLit) continue;
                float d = ((Vector2)b.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b.transform; }
            }
            return best;
        }

        void UpdatePopups(float dt)
        {
            if (cam == null || canvas == null) return;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.t += dt;
                if (p.t > 0.8f) { Destroy(p.text.gameObject); popups.RemoveAt(i); continue; }
                Vector2 screen = cam.WorldToScreenPoint(p.world + Vector2.up * p.t * 0.8f);
                p.text.rectTransform.anchoredPosition = new Vector2(Mathf.Round(screen.x / canvas.scaleFactor) - 20, Mathf.Round(screen.y / canvas.scaleFactor));
                p.text.canvasRenderer.SetAlpha(1f - Mathf.Clamp01((p.t - 0.4f) / 0.4f));
            }
        }
    }
}
