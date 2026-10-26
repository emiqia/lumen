using System;
using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using UnityEngine;

namespace Lumen.UI
{
    /// <summary>
    /// Title, difficulty select, options and pause menus. Owns menu input while visible and
    /// reports the player's choices through events; the GameManager decides what they mean.
    /// </summary>
    public class FrontEnd : MonoBehaviour
    {
        public enum Mode { Hidden, Title, Difficulty, Options, Pause }

        public Mode Current { get; private set; } = Mode.Hidden;
        public event Action<Difficulty> StartRequested;
        public event Action ResumeRequested, AbandonRequested, QuitRequested, SettingsChanged;

        RectTransform root, titlePanel, difficultyPanel, optionsPanel, pausePanel;
        UnityEngine.UI.Image veil;
        MenuList titleMenu, difficultyMenu, optionsMenu, pauseMenu;
        PixelText logo, difficultyDesc, difficultyRecord, hint, difficultyTitle, optionsTitle, pauseTitle;
        BookScreen book;
        Mode optionsReturn;
        Difficulty chosen;

        public static FrontEnd Create(Transform canvas, BookScreen book)
        {
            var rt = Ui.Stretch(canvas, "FrontEnd");
            var fe = rt.gameObject.AddComponent<FrontEnd>();
            fe.root = rt;
            fe.book = book;
            fe.Build();
            book.Closed += fe.OnBookClosed;
            fe.SetMode(Mode.Hidden);
            return fe;
        }

        void Build()
        {
            veil = Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.2f));
            veil.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Title: just the logo and a menu. The rules live in the book.
            titlePanel = Ui.Stretch(root, "Title");
            logo = Ui.Text(titlePanel, "LUMEN", Ui.Center, new Vector2(0, 44), 6, PixelText.Align.Center, Palette.FireYellow, 380);
            titleMenu = MenuList.Create(titlePanel, Ui.Center, new Vector2(0, -6));
            titleMenu.SetItems(new List<MenuItem>
            {
                new MenuItem(() => Locale.Get("menu.play"), () => SetMode(Mode.Difficulty)),
                new MenuItem(() => Locale.Get("menu.guide"), () => book.Open(beforeRun: false)),
                new MenuItem(() => Locale.Get("menu.options"), () => OpenOptions(Mode.Title)),
                new MenuItem(() => Locale.Get("menu.quit"), () => QuitRequested?.Invoke()),
            });

            // Difficulty select with a description and the record for the highlighted fire.
            difficultyPanel = Ui.Stretch(root, "Difficulty");
            difficultyTitle = Ui.Text(difficultyPanel, "", Ui.Center, new Vector2(0, 70), 2, PixelText.Align.Center, Palette.FireYellow, 380);
            difficultyMenu = MenuList.Create(difficultyPanel, Ui.Center, new Vector2(0, 40));
            var items = new List<MenuItem>();
            foreach (var d in DifficultyProfile.All)
            {
                var level = d;
                items.Add(new MenuItem(() => Locale.Get(DifficultyProfile.LocaleKey(level)), () => ChooseDifficulty(level)));
            }
            items.Add(new MenuItem(() => Locale.Get("menu.back"), () => SetMode(Mode.Title)));
            difficultyMenu.SetItems(items, (int)SaveData.LastDifficulty);
            difficultyMenu.SelectionChanged += _ => RefreshDifficultyInfo();
            difficultyDesc = Ui.Text(difficultyPanel, "", Ui.Center, new Vector2(0, -36), 1, PixelText.Align.Center, Palette.UiText, 380);
            difficultyRecord = Ui.Text(difficultyPanel, "", Ui.Center, new Vector2(0, -60), 1, PixelText.Align.Center, Palette.UiDim, 380);

            // Options.
            optionsPanel = Ui.Stretch(root, "Options");
            optionsTitle = Ui.Text(optionsPanel, "", Ui.Center, new Vector2(0, 80), 2, PixelText.Align.Center, Palette.FireYellow, 380);
            optionsMenu = MenuList.Create(optionsPanel, Ui.Center, new Vector2(0, 58));
            optionsMenu.SetItems(new List<MenuItem>
            {
                Value("options.music", () => Percent(SaveData.MusicVolume), _ => SaveData.CycleMusic()),
                Value("options.sfx", () => Percent(SaveData.SfxVolume), _ => SaveData.CycleSfx()),
                Value("options.screen", () => Locale.Get(SaveData.Windowed ? "options.windowed" : "options.fullscreen"), _ => SaveData.Windowed = !SaveData.Windowed),
                Value("options.brightness", () => Percent(SaveData.Brightness), _ => SaveData.CycleBrightness()),
                Value("options.shake", () => OnOff(SaveData.ScreenShake), _ => SaveData.ScreenShake = !SaveData.ScreenShake),
                Value("options.colorblind", () => OnOff(SaveData.Colorblind), _ => SaveData.Colorblind = !SaveData.Colorblind),
                Value("options.timer", () => OnOff(SaveData.SpeedrunTimer), _ => SaveData.SpeedrunTimer = !SaveData.SpeedrunTimer),
                Value("options.language", () => Locale.Get("options.languageName"), _ => { Locale.Toggle(); RefreshTexts(); }),
                new MenuItem(() => Locale.Get("menu.back"), () => SetMode(optionsReturn)),
            });

            // Pause.
            pausePanel = Ui.Stretch(root, "Pause");
            pauseTitle = Ui.Text(pausePanel, "", Ui.Center, new Vector2(0, 40), 3, PixelText.Align.Center, Palette.UiText, 380);
            pauseMenu = MenuList.Create(pausePanel, Ui.Center, new Vector2(0, 8));
            pauseMenu.SetItems(new List<MenuItem>
            {
                new MenuItem(() => Locale.Get("menu.resume"), () => ResumeRequested?.Invoke()),
                new MenuItem(() => Locale.Get("menu.options"), () => OpenOptions(Mode.Pause)),
                new MenuItem(() => Locale.Get("menu.guide"), () => book.Open(beforeRun: false)),
                new MenuItem(() => Locale.Get("menu.abandon"), () => AbandonRequested?.Invoke()),
            });

            hint = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 8), 1, PixelText.Align.Center, Palette.UiDim, 380);
            RefreshTexts();
        }

        MenuItem Value(string key, Func<string> value, Action<int> change) =>
            new MenuItem(() => Locale.Get(key) + ": " + value(), null, dir => { change(dir); SettingsChanged?.Invoke(); });

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
        static string OnOff(bool v) => Locale.Get(v ? "settings.on" : "settings.off");

        void RefreshTexts()
        {
            difficultyTitle.Text = Locale.Get("difficulty.title");
            optionsTitle.Text = Locale.Get("options.title");
            pauseTitle.Text = Locale.Get("pause.title");
            hint.Text = Locale.Get("menu.hint");
            titleMenu.Refresh(); difficultyMenu.Refresh(); optionsMenu.Refresh(); pauseMenu.Refresh();
            RefreshDifficultyInfo();
        }

        void RefreshDifficultyInfo()
        {
            int i = difficultyMenu.Selected;
            if (i >= DifficultyProfile.All.Length) { difficultyDesc.Text = difficultyRecord.Text = ""; return; }
            var d = DifficultyProfile.All[i];
            difficultyDesc.Text = Ui.Wrap(Locale.Get(DifficultyProfile.LocaleKey(d) + ".desc"), 56);
            int best = SaveData.BestScore(d);
            difficultyRecord.Text = best > 0
                ? Locale.Get("difficulty.record") + " " + best + "   " + Locale.Get("title.floor") + " " + SaveData.BestFloor(d)
                : Locale.Get("difficulty.norecord");
        }

        public void ShowTitle() => SetMode(Mode.Title);
        public void ShowPause() => SetMode(Mode.Pause);
        public void Hide() => SetMode(Mode.Hidden);

        void OpenOptions(Mode returnTo)
        {
            optionsReturn = returnTo;
            SetMode(Mode.Options);
        }

        void ChooseDifficulty(Difficulty d)
        {
            chosen = d;
            SaveData.LastDifficulty = d;
            // First time ever: read the book before the first floor.
            if (!SaveData.TutorialSeen) book.Open(beforeRun: true);
            else StartRequested?.Invoke(d);
        }

        void OnBookClosed(bool finishedBeforeRun)
        {
            if (Current != Mode.Difficulty) return; // opened from Guía: just return to the menu
            SaveData.TutorialSeen = true;
            StartRequested?.Invoke(chosen);
        }

        void SetMode(Mode mode)
        {
            Current = mode;
            titlePanel.gameObject.SetActive(mode == Mode.Title);
            difficultyPanel.gameObject.SetActive(mode == Mode.Difficulty);
            optionsPanel.gameObject.SetActive(mode == Mode.Options);
            pausePanel.gameObject.SetActive(mode == Mode.Pause);
            veil.gameObject.SetActive(mode != Mode.Hidden);
            hint.gameObject.SetActive(mode != Mode.Hidden);
            // Over the attract demo the veil is light; over a paused run or submenus, darker.
            veil.color = new Color(0.02f, 0.01f, 0.04f, mode == Mode.Title ? 0.2f : 0.75f);
            if (mode == Mode.Difficulty) RefreshDifficultyInfo();
            RefreshTexts();
        }

        void Update()
        {
            if (Current == Mode.Hidden || book.IsOpen) return;

            if (Current == Mode.Title)
            {
                // The logo flickers like a candle.
                float f = Mathf.PerlinNoise(Time.unscaledTime * 3f, 0f);
                logo.color = Color.Lerp(Palette.FireOrange, Palette.FireWhite, f);
            }

            var input = MenuInput.Read();
            var menu = Current switch
            {
                Mode.Title => titleMenu,
                Mode.Difficulty => difficultyMenu,
                Mode.Options => optionsMenu,
                _ => pauseMenu,
            };
            if (input.Back)
            {
                Sfx.I?.Blip();
                if (Current == Mode.Pause) ResumeRequested?.Invoke();
                else if (Current == Mode.Options) SetMode(optionsReturn);
                else if (Current == Mode.Difficulty) SetMode(Mode.Title);
                return;
            }
            if (menu.Navigate(input)) Sfx.I?.Blip();
        }
    }
}
