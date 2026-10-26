using Lumen.Art;
using Lumen.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>Game-over screen and the full-screen fade. Menus live in <see cref="FrontEnd"/>.</summary>
    public class Screens : MonoBehaviour
    {
        RectTransform gameOver;
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
            gameOver = Ui.Stretch(root, "GameOver");
            Ui.Box(gameOver, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.7f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            overTitle = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, 54), 3, PixelText.Align.Center, Palette.FireRed);
            overCause = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, 32), 1, PixelText.Align.Center, Palette.UiDim);
            overStats = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, 6), 1, PixelText.Align.Center, Palette.UiText);
            overRecord = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, -34), 2, PixelText.Align.Center, Palette.FireYellow);
            overRetry = Ui.Text(gameOver, "", Ui.Center, new Vector2(0, -58), 1, PixelText.Align.Center, Palette.UiText);

            fade = Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), Color.black);
            fade.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            HideGameOver();
        }

        public void ShowGameOver(string causeKey, int score, int floor, float time, bool record, Difficulty difficulty)
        {
            gameOver.gameObject.SetActive(true);
            overTitle.Text = Locale.Get("over.title");
            overCause.Text = Locale.Get(causeKey);
            int m = (int)(time / 60), s = (int)(time % 60);
            overStats.Text = Locale.Get(DifficultyProfile.LocaleKey(difficulty)) + "\n" +
                             Locale.Get("over.score") + "  " + score + "\n" +
                             Locale.Get("over.floor") + "  " + floor + "\n" +
                             Locale.Get("over.time") + "  " + m + ":" + s.ToString("00");
            overRecord.Text = record ? Locale.Get("over.record") : "";
            overRetry.Text = Locale.Get("over.retry");
        }

        public void HideGameOver() => gameOver.gameObject.SetActive(false);
        public bool GameOverVisible => gameOver.gameObject.activeSelf;

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
            if (gameOver.gameObject.activeSelf)
                overRetry.canvasRenderer.SetAlpha(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f));
        }
    }
}
