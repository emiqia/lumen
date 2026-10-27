using System;
using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>What the player chose before a run.</summary>
    public sealed class RunSetup
    {
        public Difficulty Difficulty;
        public Character Character;
        public List<Provision> Provisions = new List<Provision>();
        public bool Daily;
    }

    /// <summary>
    /// Title, run setup (difficulty, flame, provisions), unlocks, challenges, options and pause.
    /// Owns menu input while visible and reports choices through events.
    /// </summary>
    public class FrontEnd : MonoBehaviour
    {
        public enum Mode { Hidden, Title, Difficulty, Characters, Provisions, Unlocks, Challenges, Options, Pause }

        public Mode Current { get; private set; } = Mode.Hidden;
        public event Action<RunSetup> StartRequested;
        public event Action ResumeRequested, AbandonRequested, QuitRequested, SettingsChanged;

        const float FadeTime = 0.22f;

        RectTransform root;
        readonly Dictionary<Mode, RectTransform> panels = new Dictionary<Mode, RectTransform>();
        readonly Dictionary<Mode, CanvasGroup> groups = new Dictionary<Mode, CanvasGroup>();
        readonly Dictionary<Mode, MenuList> menus = new Dictionary<Mode, MenuList>();
        readonly Dictionary<Mode, (PixelText text, string key)> titles = new Dictionary<Mode, (PixelText, string)>();
        Image veil, characterPreview;
        PixelText logo, hint, cenizas;
        PixelText difficultyDesc, difficultyRecord, characterName, characterQuote, characterAbility;
        PixelText provisionsTotal, unlockDesc, challengeDesc;
        BookScreen book;
        Mode optionsReturn;
        RunSetup setup = new RunSetup();
        readonly HashSet<Provision> basket = new HashSet<Provision>();
        bool pendingStart;
        float modeT;

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

        // ------------------------------------------------------------------ building

        RectTransform Panel(Mode mode, string titleKey = null)
        {
            var p = Ui.Stretch(root, mode.ToString());
            panels[mode] = p;
            groups[mode] = p.gameObject.AddComponent<CanvasGroup>();
            if (titleKey != null)
            {
                var t = Ui.Text(p, "", Ui.Center, new Vector2(0, 82), 2, PixelText.Align.Center, Palette.FireYellow, 380);
                titles[mode] = (t, titleKey);
            }
            return p;
        }

        MenuList Menu(Mode mode, Vector2 pos, List<MenuItem> items, int select = 0, int rowHeight = 13)
        {
            var m = MenuList.Create(panels[mode], Ui.Center, pos);
            m.RowHeight = rowHeight;
            m.SetItems(items, select);
            menus[mode] = m;
            return m;
        }

        PixelText Info(Mode mode, Vector2 pos, Color color, float width = 380) =>
            Ui.Text(panels[mode], "", Ui.Center, pos, 1, PixelText.Align.Center, color, width);

        void Build()
        {
            veil = Ui.Box(root, Ui.Center, Vector2.zero, new Vector2(4000, 4000), new Color(0.02f, 0.01f, 0.04f, 0.2f));
            veil.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // ---- title: the logo and a menu. The rules live in the book.
            var title = Panel(Mode.Title);
            logo = Ui.Text(title, "LUMEN", Ui.Center, new Vector2(0, 52), 6, PixelText.Align.Center, Palette.FireYellow, 380);
            Menu(Mode.Title, new Vector2(0, 4), new List<MenuItem>
            {
                new MenuItem(() => Locale.Get("menu.play"), () => { setup = new RunSetup(); SetMode(Mode.Difficulty); }),
                new MenuItem(() => Locale.Get("menu.daily") + DailyBestLabel(), StartDaily),
                new MenuItem(() => Locale.Get("menu.unlocks"), () => SetMode(Mode.Unlocks)),
                new MenuItem(() => Locale.Get("menu.challenges"), () => SetMode(Mode.Challenges)),
                new MenuItem(() => Locale.Get("menu.guide"), () => book.Open(beforeRun: false)),
                new MenuItem(() => Locale.Get("menu.options"), () => OpenOptions(Mode.Title)),
                new MenuItem(() => Locale.Get("menu.quit"), () => QuitRequested?.Invoke()),
            });

            // ---- difficulty
            Panel(Mode.Difficulty, "difficulty.title");
            var dItems = new List<MenuItem>();
            foreach (var d in DifficultyProfile.All)
            {
                var level = d;
                dItems.Add(new MenuItem(() => Locale.Get(DifficultyProfile.LocaleKey(level)), () =>
                {
                    setup.Difficulty = level;
                    SaveData.LastDifficulty = level;
                    SetMode(Mode.Characters);
                }));
            }
            dItems.Add(new MenuItem(() => Locale.Get("menu.back"), () => SetMode(Mode.Title)));
            Menu(Mode.Difficulty, new Vector2(0, 52), dItems, (int)SaveData.LastDifficulty).SelectionChanged += _ => RefreshInfo();
            difficultyDesc = Info(Mode.Difficulty, new Vector2(0, -28), Palette.UiText);
            difficultyRecord = Info(Mode.Difficulty, new Vector2(0, -52), Palette.UiDim);

            // ---- characters: list on the left, portrait and personality on the right
            Panel(Mode.Characters, "char.title");
            var cItems = new List<MenuItem>();
            foreach (var c in Character.All)
            {
                var flame = c;
                cItems.Add(new MenuItem(() => Locale.Get(flame.NameKey) + (Meta.IsUnlocked(flame.Id) ? "" : "  (" + flame.Price + ")"),
                    () => ChooseCharacter(flame)));
            }
            cItems.Add(new MenuItem(() => Locale.Get("menu.back"), () => SetMode(Mode.Difficulty)));
            int sel = Mathf.Max(0, Character.IndexOf(Character.Get(Meta.SelectedCharacter)));
            Menu(Mode.Characters, new Vector2(-96, 52), cItems, sel).SelectionChanged += _ => RefreshInfo();
            var cp = panels[Mode.Characters];
            characterPreview = Ui.Box(cp, Ui.Center, new Vector2(84, 30), new Vector2(80, 80), Color.white);
            characterPreview.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            characterName = Ui.Text(cp, "", Ui.Center, new Vector2(84, -18), 1, PixelText.Align.Center, Palette.FireYellow, 200);
            characterQuote = Ui.Text(cp, "", Ui.Center, new Vector2(84, -30), 1, PixelText.Align.Center, Palette.UiDim, 200);
            characterQuote.rectTransform.pivot = new Vector2(0.5f, 1f);
            characterAbility = Ui.Text(cp, "", Ui.Center, new Vector2(84, -58), 1, PixelText.Align.Center, Palette.UiText, 200);
            characterAbility.rectTransform.pivot = new Vector2(0.5f, 1f);

            // ---- provisions (pre-run shop)
            Panel(Mode.Provisions, "prov.title");
            var pItems = new List<MenuItem>();
            foreach (var p in Provision.All)
            {
                var item = p;
                pItems.Add(new MenuItem(() => (basket.Contains(item) ? "[X] " : "[ ] ") + Locale.Get(item.NameKey) + "   " + item.Price,
                    () => ToggleProvision(item)));
            }
            pItems.Add(new MenuItem(() => Locale.Get("prov.start"), BeginRun));
            pItems.Add(new MenuItem(() => Locale.Get("menu.back"), () => { basket.Clear(); SetMode(Mode.Characters); }));
            Menu(Mode.Provisions, new Vector2(0, 56), pItems).SelectionChanged += _ => RefreshInfo();
            provisionsTotal = Info(Mode.Provisions, new Vector2(0, -50), Palette.UiText);

            // ---- unlocks
            Panel(Mode.Unlocks, "unlock.title");
            var uItems = new List<MenuItem>();
            foreach (var u in Unlock.All)
            {
                var unlock = u;
                uItems.Add(new MenuItem(() => Locale.Get(unlock.NameKey) + "   " + (Meta.IsUnlocked(unlock.Id) ? Locale.Get("unlock.owned") : unlock.Price.ToString()),
                    () => Buy(unlock)));
            }
            uItems.Add(new MenuItem(() => Locale.Get("menu.back"), () => SetMode(Mode.Title)));
            Menu(Mode.Unlocks, new Vector2(0, 66), uItems, rowHeight: 11).SelectionChanged += _ => RefreshInfo();
            unlockDesc = Info(Mode.Unlocks, new Vector2(0, -84), Palette.UiText);

            // ---- challenges
            Panel(Mode.Challenges, "chal.title");
            var chItems = new List<MenuItem>();
            foreach (var c in Challenge.All)
            {
                var ch = c;
                chItems.Add(new MenuItem(() => (Meta.IsCompleted(ch) ? "* " : "- ") + Locale.Get(ch.NameKey) + "   +" + ch.Reward));
            }
            chItems.Add(new MenuItem(() => Locale.Get("menu.back"), () => SetMode(Mode.Title)));
            Menu(Mode.Challenges, new Vector2(0, 62), chItems).SelectionChanged += _ => RefreshInfo();
            challengeDesc = Info(Mode.Challenges, new Vector2(0, -70), Palette.UiText);

            // ---- options
            Panel(Mode.Options, "options.title");
            Menu(Mode.Options, new Vector2(0, 58), new List<MenuItem>
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

            // ---- pause
            Panel(Mode.Pause, "pause.title");
            Menu(Mode.Pause, new Vector2(0, 8), new List<MenuItem>
            {
                new MenuItem(() => Locale.Get("menu.resume"), () => ResumeRequested?.Invoke()),
                new MenuItem(() => Locale.Get("menu.options"), () => OpenOptions(Mode.Pause)),
                new MenuItem(() => Locale.Get("menu.guide"), () => book.Open(beforeRun: false)),
                new MenuItem(() => Locale.Get("menu.abandon"), () => AbandonRequested?.Invoke()),
            });

            cenizas = Ui.Text(root, "", Ui.TopRight, new Vector2(-8, -8), 1, PixelText.Align.Right, Palette.FireOrange, 200);
            hint = Ui.Text(root, "", Ui.Bottom, new Vector2(0, 8), 1, PixelText.Align.Center, Palette.UiDim, 380);
            RefreshTexts();
        }

        MenuItem Value(string key, Func<string> value, Action<int> change) =>
            new MenuItem(() => Locale.Get(key) + ": " + value(), null, dir => { change(dir); SettingsChanged?.Invoke(); });

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
        static string OnOff(bool v) => Locale.Get(v ? "settings.on" : "settings.off");

        static string DailyBestLabel()
        {
            int best = Meta.DailyBest(Meta.TodaySeed);
            return best > 0 ? "  (" + best + ")" : "";
        }

        // ------------------------------------------------------------------ actions

        void ChooseCharacter(Character c)
        {
            if (!Meta.IsUnlocked(c.Id))
            {
                // Buying straight from the selection screen is allowed when affordable.
                foreach (var u in Unlock.All)
                    if (u.Id == c.Id && Meta.TryUnlock(u)) { Sfx.I?.Blip(); RefreshTexts(); }
                return;
            }
            setup.Character = c;
            Meta.SelectedCharacter = c.Id;
            basket.Clear();
            SetMode(Mode.Provisions);
        }

        int BasketTotal()
        {
            int total = 0;
            foreach (var p in basket) total += p.Price;
            return total;
        }

        void ToggleProvision(Provision p)
        {
            if (basket.Contains(p)) basket.Remove(p);
            else if (BasketTotal() + p.Price <= Meta.Cenizas) basket.Add(p);
            RefreshInfo();
        }

        void Buy(Unlock u)
        {
            if (Meta.TryUnlock(u)) Sfx.I?.Blip();
            RefreshTexts();
        }

        void BeginRun()
        {
            if (!Meta.TrySpend(BasketTotal())) return;
            setup.Provisions = new List<Provision>(basket);
            basket.Clear();
            RequestStart();
        }

        void StartDaily()
        {
            setup = new RunSetup { Difficulty = Difficulty.Antorcha, Character = Character.All[0], Daily = true };
            RequestStart();
        }

        void RequestStart()
        {
            // First time ever: read the book before the first floor.
            if (!SaveData.TutorialSeen) { pendingStart = true; book.Open(beforeRun: true); }
            else StartRequested?.Invoke(setup);
        }

        void OnBookClosed(bool finishedBeforeRun)
        {
            if (!pendingStart) return; // opened from Guía: just return to the menu
            pendingStart = false;
            SaveData.TutorialSeen = true;
            StartRequested?.Invoke(setup);
        }

        public void ShowTitle() => SetMode(Mode.Title);
        public void ShowPause() => SetMode(Mode.Pause);
        public void ShowDifficulty() => SetMode(Mode.Difficulty);
        public void ShowOptions() => OpenOptions(Mode.Title);
        public void ShowScreen(Mode mode) => SetMode(mode);
        public void Hide() => SetMode(Mode.Hidden);

        void OpenOptions(Mode returnTo)
        {
            optionsReturn = returnTo;
            SetMode(Mode.Options);
        }

        // ------------------------------------------------------------------ refresh

        void RefreshTexts()
        {
            foreach (var kv in titles) kv.Value.text.Text = Locale.Get(kv.Value.key);
            hint.Text = Locale.Get("menu.hint");
            RefreshInfo();
        }

        void RefreshInfo()
        {
            cenizas.Text = Meta.Cenizas + " " + Locale.Get("meta.cenizas");

            int di = menus[Mode.Difficulty].Selected;
            if (di < DifficultyProfile.All.Length)
            {
                var d = DifficultyProfile.All[di];
                difficultyDesc.Text = Ui.Wrap(Locale.Get(DifficultyProfile.LocaleKey(d) + ".desc"), 56);
                int best = SaveData.BestScore(d);
                difficultyRecord.Text = best > 0
                    ? Locale.Get("difficulty.record") + " " + best + "   " + Locale.Get("title.floor") + " " + SaveData.BestFloor(d)
                    : Locale.Get("difficulty.norecord");
            }
            else difficultyDesc.Text = difficultyRecord.Text = "";

            int ci = menus[Mode.Characters].Selected;
            bool showChar = ci < Character.All.Count;
            characterPreview.enabled = showChar;
            if (showChar)
            {
                var c = Character.All[ci];
                bool owned = Meta.IsUnlocked(c.Id);
                characterPreview.sprite = SpriteBank.PlayerFor(c)[0];
                characterPreview.color = owned ? Color.white : new Color(0.25f, 0.22f, 0.3f, 1f); // locked: a silhouette
                characterName.Text = Locale.Get(c.NameKey).ToUpperInvariant();
                characterQuote.Text = Ui.Wrap("\"" + Locale.Get(c.QuoteKey) + "\"", 32);
                Ui.FitHeight(characterQuote);
                string lockLine = owned ? "" : "\n\n" + Locale.Get("char.locked") + " " + c.Price + " " + Locale.Get("meta.cenizas");
                characterAbility.Text = Ui.Wrap(Locale.Get(c.AbilityKey), 32) + lockLine;
                Ui.FitHeight(characterAbility);
            }
            else characterName.Text = characterQuote.Text = characterAbility.Text = "";

            int pi = menus[Mode.Provisions].Selected;
            string pdesc = pi < Provision.All.Count ? Locale.Get(Provision.All[pi].DescKey) + "\n" : "";
            provisionsTotal.Text = pdesc + Locale.Get("prov.total") + " " + BasketTotal() + " / " + Meta.Cenizas + " " + Locale.Get("meta.cenizas");

            int ui = menus[Mode.Unlocks].Selected;
            unlockDesc.Text = ui < Unlock.All.Count ? Ui.Wrap(Locale.Get(Unlock.All[ui].DescKey), 56) : "";

            int chi = menus[Mode.Challenges].Selected;
            challengeDesc.Text = chi < Challenge.All.Count ? Ui.Wrap(Locale.Get(Challenge.All[chi].DescKey), 56) : "";

            foreach (var m in menus.Values) m.Refresh();
        }

        void SetMode(Mode mode)
        {
            Current = mode;
            modeT = 0f;
            foreach (var kv in panels) kv.Value.gameObject.SetActive(kv.Key == mode);
            veil.gameObject.SetActive(mode != Mode.Hidden);
            hint.gameObject.SetActive(mode != Mode.Hidden);
            cenizas.gameObject.SetActive(mode != Mode.Hidden && mode != Mode.Pause);
            // Over the attract demo the veil is light; over a paused run or submenus, darker.
            veil.color = new Color(0.02f, 0.01f, 0.04f, mode == Mode.Title ? 0.2f : 0.78f);
            RefreshTexts();
        }

        void Update()
        {
            // Panels fade in instead of popping.
            modeT += Time.unscaledDeltaTime;
            if (groups.TryGetValue(Current, out var g)) g.alpha = Mathf.Clamp01(modeT / FadeTime);

            // The book has its own hint line; don't overlap it with the menu's.
            hint.gameObject.SetActive(Current != Mode.Hidden && !book.IsOpen);
            cenizas.gameObject.SetActive(Current != Mode.Hidden && Current != Mode.Pause && !book.IsOpen);
            if (Current == Mode.Hidden || book.IsOpen) return;

            if (Current == Mode.Title)
            {
                // The logo flickers like a candle.
                float f = Mathf.PerlinNoise(Time.unscaledTime * 3f, 0f);
                logo.color = Color.Lerp(Palette.FireOrange, Palette.FireWhite, f);
            }

            var input = MenuInput.Read();
            if (input.Back)
            {
                Sfx.I?.Blip();
                switch (Current)
                {
                    case Mode.Pause: ResumeRequested?.Invoke(); break;
                    case Mode.Options: SetMode(optionsReturn); break;
                    case Mode.Characters: SetMode(Mode.Difficulty); break;
                    case Mode.Provisions: basket.Clear(); SetMode(Mode.Characters); break;
                    case Mode.Title: break;
                    default: SetMode(Mode.Title); break;
                }
                return;
            }
            if (menus.TryGetValue(Current, out var menu) && menu.Navigate(input)) Sfx.I?.Blip();
        }
    }
}
