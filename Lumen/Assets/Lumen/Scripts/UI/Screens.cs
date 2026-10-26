using Lumen.Art;
using Lumen.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>Title, pause, game-over and fade overlays.</summary>
    public class Screens : MonoBehaviour
    {
        RectTransform title, pause, gameOver;
        PixelText titleLogo, titlePrompt, titleBest, titleLang, titleSub, titleControls, titleQuit, titleSettings;
        PixelText pauseTitle, pauseResume, pauseQuit, pauseSettings;
        PixelText overTitle, overCause, overStats, overRecord, overRetry;
        Image fade;
        float fadeTarget, fadeValue;

        public static Screens Create(Transform canvas)
        {
            var go = Ui.Stretch(canvas, "Screens");
            var s = go.gameObject.AddComponent<Screens>();
            s.Build(go);
            return s;
        }

        void Build(RectTransform root)
        {
            // Title
            title = Ui.Stretch(root, "Title");
            // Light veil only: the attract-mode demo behind the title should stay visible.
            Ui.Box(title, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, 0.2f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleLogo = Ui.Text(title, "LUMEN", Ui.Center, new Vector2(0, 42), 6, PixelText.Align.Center, Palette.FireYellow, 380);
            titleSub = Ui.Text(title, "", Ui.Center, new Vector2(0, 14), 1, PixelText.Align.Center, Palette.FireOrange, 380);
            titlePrompt = Ui.Text(title, "", Ui.Center, new Vector2(0, -16), 1, PixelText.Align.Center, Palette.UiText, 380);
            titleBest = Ui.Text(title, "", Ui.Center, new Vector2(0, -34), 1, PixelText.Align.Center, Palette.UiDim, 380);
            titleControls = Ui.Text(title, "", Ui.Bottom, new Vector2(0, 22), 1, PixelText.Align.Center, Palette.UiDim, 380);
            titleSettings = Ui.Text(title, "", Ui.Bottom, new Vector2(0, 34), 1, PixelText.Align.Center, Palette.UiDim, 380);
            titleLang = Ui.Text(title, "", Ui.BottomLeft, new Vector2(6, 6), 1, PixelText.Align.Left, Palette.UiDim, 150);
            titleQuit = Ui.Text(title, "", Ui.BottomRight, new Vector2(-6, 6), 1, PixelText.Align.Right, Palette.UiDim, 150);

            // Pause
            pause = Ui.Stretch(root, "Pause");
            Ui.Box(pause, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.75f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pauseTitle = Ui.Text(pause, "", Ui.Center, new Vector2(0, 30), 3, PixelText.Align.Center, Palette.UiText);
            pauseResume = Ui.Text(pause, "", Ui.Center, new Vector2(0, 0), 1, PixelText.Align.Center, Palette.UiText);
            pauseSettings = Ui.Text(pause, "", Ui.Center, new Vector2(0, -14), 1, PixelText.Align.Center, Palette.UiDim);
            pauseQuit = Ui.Text(pause, "", Ui.Center, new Vector2(0, -28), 1, PixelText.Align.Center, Palette.UiDim);

            // Game over
            gameOver = Ui.Stretch(root, "GameOver");
            Ui.Box(gameOver, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.7f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            overTitle = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, 50), 3, PixelText.Align.Center, Palette.FireRed);
            overCause = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, 28), 1, PixelText.Align.Center, Palette.UiDim);
            overStats = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, -2), 1, PixelText.Align.Center, Palette.UiText);
            overRecord = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, -30), 2, PixelText.Align.Center, Palette.FireYellow);
            overRetry = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, -54), 1, PixelText.Align.Center, Palette.UiText);

            fade = Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), Color.black);
            fade.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            ShowTitle(false);
            ShowPause(false);
            HideGameOver();
        }

        public void ShowTitle(bool show)
        {
            title.gameObject.SetActive(show);
            if (!show) return;
            titleSub.Text = Locale.Get("title.subtitle");
            titlePrompt.Text = Locale.Get("title.start");
            titleControls.Text = Locale.Get("title.controls");
            titleLang.Text = Locale.Get("title.lang");
            titleQuit.Text = Locale.Get("title.quit");
            titleSettings.Text = SettingsLine();
            titleBest.Text = SaveData.BestScore > 0
                ? Locale.Get("title.best") + " " + SaveData.BestScore + "   " + Locale.Get("title.floor") + " " + SaveData.BestFloor
                : "";
        }

        public void ShowPause(bool show)
        {
            pause.gameObject.SetActive(show);
            if (!show) return;
            pauseTitle.Text = Locale.Get("pause.title");
            pauseResume.Text = Locale.Get("pause.resume");
            pauseQuit.Text = Locale.Get("pause.quit");
            RefreshPause();
        }

        public void RefreshPause() => pauseSettings.Text = SettingsLine();

        static string SettingsLine() =>
            Locale.Get("settings.shake") + " " + Locale.Get(SaveData.ScreenShake ? "settings.on" : "settings.off") + "    " +
            Locale.Get("settings.brightness") + " " + Mathf.RoundToInt(SaveData.Brightness * 100f) + "%";

        public void ShowGameOver(string causeKey, int score, int floor, float time, bool record)
        {
            gameOver.gameObject.SetActive(true);
            overTitle.Text = Locale.Get("over.title");
            overCause.Text = Locale.Get(causeKey);
            int m = (int)(time / 60), s = (int)(time % 60);
            overStats.Text = Locale.Get("over.score") + "  " + score + "\n" +
                             Locale.Get("over.floor") + "  " + floor + "\n" +
                             Locale.Get("over.time") + "  " + m + ":" + s.ToString("00");
            overRecord.Text = record ? Locale.Get("over.record") : "";
            overRetry.Text = Locale.Get("over.retry");
        }

        public void HideGameOver() => gameOver.gameObject.SetActive(false);

        public void SetFade(float target, bool instant = false)
        {
            fadeTarget = target;
            if (instant) fadeValue = target;
        }

        public bool FadeDone => Mathf.Abs(fadeValue - fadeTarget) < 0.01f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fadeValue = Mathf.MoveTowards(fadeValue, fadeTarget, dt * 2.5f);
            fade.color = new Color(0, 0, 0, fadeValue);
            fade.enabled = fadeValue > 0.001f;

            if (title.gameObject.activeSelf)
            {
                titlePrompt.canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f));
                // Logo flickers like a candle.
                float f = Mathf.PerlinNoise(Time.unscaledTime * 3f, 0f);
                titleLogo.color = Color.Lerp(Palette.FireOrange, Palette.FireWhite, f);
            }
            if (gameOver.gameObject.activeSelf)
                overRetry.canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }
}
