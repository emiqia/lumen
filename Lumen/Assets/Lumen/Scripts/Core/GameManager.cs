using System.Collections;
using Lumen.Art;
using Lumen.Bot;
using Lumen.Enemies;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using Lumen.UI;
using Lumen.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Core
{
    /// <summary>
    /// Composition root and game-flow state machine. The scene contains only this component:
    /// it builds the camera, post-processing, audio and UI, then owns the
    /// Title → Playing ⇄ Paused → GameOver flow. Game rules live in the systems it wires up
    /// (ScoreService, DarknessSpawner, TutorialDirector, PostFxController) and in the actors,
    /// which talk back through <see cref="GameEvents"/>.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Title, Playing, Transition, Paused, ChoosingBlessing, GameOver }

        public static GameManager Instance { get; private set; }

        public State Current { get; private set; }
        public int Floor { get; private set; }
        public int Score => score.Score;
        public float RunTime { get; private set; }
        public float FloorTime { get; private set; }
        public int BraziersLit { get; private set; }
        public int BraziersTotal => level?.Braziers.Count ?? 0;
        public bool ExitOpen => level?.Exit != null && level.Exit.IsOpen;
        public LevelInstance Level => level;
        public PlayerFlame Player => player;
        public RunTelemetry Telemetry { get; private set; }
        public bool GameOverVisible => screens.GameOverVisible;
        /// <summary>The run in progress (difficulty, blessings, inventory). Null on the title screen.</summary>
        public RunState Run => run;
        public FrontEnd FrontEnd => frontEnd;
        public BookScreen Book => book;

        /// <summary>Set by automated playtests: an AI drives the player instead of the keyboard.</summary>
        public System.Func<IPlayerInput> InputOverride { get; set; }
        /// <summary>Headless runs skip fades, UI and hit-stop, and simulate at <see cref="SimSpeed"/>.</summary>
        public bool Headless { get; set; }
        public float SimSpeed { get; set; } = 1f;

        Camera cam;
        CameraRig rig;
        Hud hud;
        Screens screens;
        FrontEnd frontEnd;
        BookScreen book;
        BlessingScreen blessings;
        RunState run;
        PostFxController postFx;
        ScoreService score;
        DarknessSpawner darkness;
        TutorialDirector tutorial;
        LevelInstance level;
        PlayerFlame player;
        int runSeed;
        int flowVersion;       // bumped on every run start / title entry, so stale coroutines can tell
        bool gameOverShown;    // game-over input is ignored until the screen is actually up
        static float hitStopUntil;

        static GameTuning Tuning => GameTuning.I;

        // ------------------------------------------------------------------ boot

        void Awake()
        {
            Instance = this;
            GameEvents.ClearAll();
            Shade.All.Clear();
            Application.targetFrameRate = 120;

            SpriteBank.Build();
            PixelFont.Build();

            BuildCamera();
            postFx = PostFxController.Create(transform);
            new GameObject("Particles").AddComponent<PixelParticles>().transform.SetParent(transform);
            new GameObject("Audio").AddComponent<Sfx>().transform.SetParent(transform);

            var canvas = Ui.CreateCanvas("UI", 100);
            canvas.transform.SetParent(transform);
            hud = Hud.Create(canvas.transform, cam);
            screens = Screens.Create(canvas.transform);
            blessings = BlessingScreen.Create(canvas.transform);
            book = BookScreen.Create(canvas.transform);
            frontEnd = FrontEnd.Create(canvas.transform, book);
            book.transform.SetAsLastSibling(); // the book opens on top of the menus

            frontEnd.StartRequested += d => { Sfx.I?.Blip(); StartRun(d); };
            frontEnd.ResumeRequested += () => SetPaused(false);
            frontEnd.AbandonRequested += () => { SetPaused(false); EnterTitle(); };
            frontEnd.QuitRequested += Application.Quit;
            frontEnd.SettingsChanged += ApplySettings;
            blessings.Chosen += OnBlessingChosen;
            ItemPickup.Collector = TryCollect;

            Telemetry = new RunTelemetry();
            score = new ScoreService(Tuning);
            darkness = new DarknessSpawner(Tuning);
            tutorial = new TutorialDirector(hud);

            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.ScoreGained += (amount, pos) => hud.ScorePopup(pos, amount);
            GameEvents.BrazierLit += _ => RecountBraziers();
            GameEvents.BrazierExtinguished += _ =>
            {
                RecountBraziers();
                if (Current == State.Playing) hud.Announce("", Locale.Get("hud.relit"), 2f);
            };
            GameEvents.PlayerReachedExit += CompleteFloor;
            GameEvents.ItemCollected += (_, kind) => hud.Announce("", Locale.Get("item." + kind), 1.6f);
            GameEvents.PlayerRevived += _ => hud.Announce(Locale.Get("hud.revived"), "", 2f, Palette.FireYellow);
        }

        /// <summary>Applies persisted settings: brightness, audio volumes and window mode.</summary>
        void ApplySettings()
        {
            postFx.ApplyBrightness();
            Sfx.I?.ApplyVolumes();
            if (!Application.isBatchMode && !Application.isEditor)
                Screen.fullScreenMode = SaveData.Windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        }

        void OnDestroy()
        {
            score?.Dispose();
            tutorial?.Dispose();
            level?.Dispose();
            run?.Dispose();
            GameTuning.Active = null;
            if (ItemPickup.Collector == (System.Func<ItemKind, bool>)TryCollect) ItemPickup.Collector = null;
        }

        void Start()
        {
            ApplySettings();
            if (PlaytestRunner.RequestedFromCommandLine()) gameObject.AddComponent<PlaytestRunner>();
            else EnterTitle();
        }

        void BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(transform);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Void;
            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;

            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = SpriteBank.PPU;
            ppc.refResolutionX = 384;
            ppc.refResolutionY = 216;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;

            rig = go.AddComponent<CameraRig>();
        }

        // ------------------------------------------------------------------ flow

        public void EnterTitle()
        {
            flowVersion++;
            Current = State.Title;
            Time.timeScale = 1f;
            score.Counting = false;
            EndRunState();
            screens.HideGameOver();
            frontEnd.ShowTitle();
            hud.gameObject.SetActive(false);
            // Attract mode: the autopilot plays a floor behind the title screen.
            LoadFloor(2, Random.Range(0, 99999), () => new AutoPilot());
            screens.SetFade(0f);
        }

        /// <summary>Starts a run at the default difficulty (used by tests and playtests).</summary>
        public void StartRun(int seed) => StartRun(Difficulty.Antorcha, seed);

        public void StartRun(Difficulty difficulty, int seed = -1)
        {
            flowVersion++;
            runSeed = seed < 0 ? Random.Range(0, 999999) : seed;
            EndRunState();
            run = new RunState(difficulty, GameTuning.Base);
            GameTuning.Active = run.Tuning;   // every system now reads the difficulty-adjusted numbers
            darkness.Tuning = run.Tuning;
            frontEnd.Hide();
            score.Reset();
            score.Counting = false; // starts counting once floor 1 is loaded, not during the fade from the demo
            RunTime = 0f;
            bool arrows = DifficultyProfile.For(difficulty).ShowsBrazierArrows;
            hud.BrazierArrows = arrows;
            tutorial.BeginRun(arrows);
            screens.HideGameOver();
            hud.gameObject.SetActive(!Headless);
            StartCoroutine(TransitionTo(1, fadeOut: !Headless));
        }

        IEnumerator TransitionTo(int floor, bool fadeOut)
        {
            Current = State.Transition;
            if (fadeOut)
            {
                screens.SetFade(1f);
                while (!screens.FadeDone) yield return null;
            }

            float carried = player != null && player.IsAlive && floor > 1 ? player.Fuel : Tuning.maxFuel;
            int seed = FloorSettings.SeedFor(runSeed, floor);
            LoadFloor(floor, seed, InputOverride);
            // Fuel carries over between floors with a small bonus, so efficiency matters.
            player.SetFuel(FuelModel.CarryToNextFloor(carried, Tuning.floorCarryBonus, Tuning.floorCarryMinimum, Tuning.maxFuel));

            // Score and telemetry start once the run's own floor exists, never during the fade from the demo.
            if (floor == 1) Telemetry.BeginRun(runSeed, run.Difficulty);
            score.Counting = true;
            hud.ClearTransient();
            hud.Announce(FloorNames.Title(floor, seed), Locale.Get("floor.enter") + " " + floor + "  -  " + FloorNames.Subtitle(floor), 3.2f);
            tutorial.BeginFloor();
            GameEvents.RaiseFloorStarted(floor);
            Telemetry.BeginFloor(floor);

            screens.SetFade(0f);
            Current = State.Playing;
        }

        void EndRunState()
        {
            GameTuning.Active = null;
            darkness.Tuning = GameTuning.Base;
            run?.Dispose();
            run = null;
        }

        /// <summary>Called by item pickups: adds the item to the run if it can be carried.</summary>
        bool TryCollect(ItemKind kind)
        {
            if (run == null || Current != State.Playing) return false;
            switch (kind)
            {
                case ItemKind.Lantern:
                    if (run.Lantern) return Full();
                    run.Lantern = true;
                    return true;
                case ItemKind.Oil:
                    if (run.Oil >= RunState.MaxOil) return Full();
                    run.Oil++;
                    return true;
                case ItemKind.Lure:
                    if (run.Lures >= RunState.MaxLures) return Full();
                    run.Lures++;
                    return true;
                default:
                    hud.RevealAllBraziers = true;
                    hud.Hint(Locale.Get("item.map.used"), 3f);
                    return true;
            }
        }

        bool Full()
        {
            hud.Hint(Locale.Get("item.full"), 1.5f);
            return false;
        }

        void LoadFloor(int floor, int seed, System.Func<IPlayerInput> input)
        {
            UnloadLevel();
            Floor = floor;
            FloorTime = 0f;
            darkness.Reset();

            var settings = FloorSettings.ForFloor(floor);
            var map = DungeonGenerator.Generate(seed, settings);
            level = LevelBuilder.Build(map, settings, seed, transform);
            player = PlayerFlame.Create(DungeonMap.CellToWorld(map.PlayerSpawn));
            player.transform.SetParent(level.Root.transform);
            if (input != null) player.Controls = input();
            player.Run = run;
            hud.RevealAllBraziers = false;
            postFx.SetAmbient(BiomePalette.For(map.Biome).Ambient);

            rig.target = player.transform;
            rig.SnapTo(player.transform.position);
            float halfH = cam.orthographicSize, halfW = halfH * 16f / 9f;
            rig.bounds = Rect.MinMaxRect(halfW - 1, halfH - 1, map.Width - halfW + 1, map.Height - halfH + 1);
            if (rig.bounds.width < 0 || rig.bounds.height < 0) rig.bounds = new Rect();

            hud.SetBrazierCount(level.Braziers.Count);
            BraziersLit = 0;
        }

        void UnloadLevel()
        {
            PixelParticles.I?.ClearAll();
            level?.Dispose();
            level = null;
            player = null;
            Shade.All.Clear();
            LightField.Clear();
            LevelHazards.Clear();
        }

        /// <summary>Event-driven: recounted only when a brazier is lit or snuffed out.</summary>
        void RecountBraziers()
        {
            if (level == null) return;
            int before = BraziersLit;
            BraziersLit = level.LitBraziers;
            if (Current == State.Playing) Telemetry.OnBraziersChanged(BraziersLit, before);

            bool allLit = BraziersLit == level.Braziers.Count;
            bool wasOpen = level.Exit.IsOpen;
            level.Exit.SetOpen(allLit);
            if (allLit && !wasOpen && Current == State.Playing) hud.Announce("", Locale.Get("hud.exit"), 2.5f);
        }

        void CompleteFloor()
        {
            if (Current == State.Title) { EnterTitle(); return; } // attract-mode bot finished: new demo floor
            if (Current != State.Playing) return;
            score.Add(score.FloorBonus(Floor, FloorTime), player.transform.position);
            Telemetry.EndFloor(FloorTime, player.Fuel);
            GameEvents.RaiseFloorCleared(Floor);
            player.Controllable = false;

            // Choose a blessing before going down. Headless playtests take the first offer.
            var offer = Blessing.Offer(runSeed * 31 + Floor, 3, run.Blessings);
            if (Headless || offer.Count == 0)
            {
                if (offer.Count > 0) { run.Take(offer[0]); Telemetry.OnBlessing(offer[0].Id); }
                StartCoroutine(TransitionTo(Floor + 1, fadeOut: !Headless));
                return;
            }
            Current = State.ChoosingBlessing;
            Time.timeScale = 0f;
            hud.ClearTransient(); // no stray hints under the cards
            blessings.Open(offer);
            // A bot can't read cards: show them briefly (for screenshots) and take the first.
            if (InputOverride != null) StartCoroutine(BotPicksBlessing());
        }

        IEnumerator BotPicksBlessing()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Current == State.ChoosingBlessing) blessings.Choose(0);
        }

        void OnBlessingChosen(Blessing blessing)
        {
            if (Current != State.ChoosingBlessing || run == null) return;
            run.Take(blessing);
            Telemetry.OnBlessing(blessing.Id);
            Time.timeScale = 1f;
            StartCoroutine(TransitionTo(Floor + 1, fadeOut: true));
        }

        void OnPlayerDied(string cause)
        {
            if (Current == State.Title)
            {
                StartCoroutine(RestartAttract());
                return;
            }
            if (Current != State.Playing) return;
            Current = State.GameOver;
            gameOverShown = false;
            score.Counting = false;
            Telemetry.EndRun(cause, Floor, Score, RunTime, FloorTime);
            // Only human runs count for records: bot playtests must never touch the player's save.
            bool record = InputOverride == null && SaveData.SubmitRun(run.Difficulty, Score, Floor);
            StartCoroutine(ShowGameOverDelayed(cause, record));
        }

        /// <summary>Ends the current run without a death (used by playtests that hit their time limit).</summary>
        public void AbortRun(string reason)
        {
            if (Current != State.Playing && Current != State.Transition) return;
            StopAllCoroutines();
            Current = State.GameOver;
            score.Counting = false;
            Telemetry.EndRun(reason, Floor, Score, RunTime, FloorTime, player != null ? player.Fuel : 0f);
        }

        IEnumerator ShowGameOverDelayed(string cause, bool record)
        {
            int version = flowVersion;
            yield return new WaitForSecondsRealtime(Headless ? 0.1f : 1.4f);
            // The player may have left (title) or a playtest may have restarted meanwhile.
            if (version != flowVersion || Current != State.GameOver) yield break;
            if (!Headless) screens.ShowGameOver(cause, Score, Floor, RunTime, record, run.Difficulty);
            gameOverShown = true;
        }

        IEnumerator RestartAttract()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Current == State.Title) EnterTitle();
        }

        public static void HitStop(float duration)
        {
            if (Instance == null || Instance.Headless) return;
            hitStopUntil = Time.unscaledTime + duration;
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            HandleInput(MenuInput.Read());
            if (Current == State.Playing || Current == State.Title) TickFloor(Time.deltaTime);

            // Hit-stop: a few frames of near-freeze on impact for weight.
            if (Current == State.Playing)
                Time.timeScale = Time.unscaledTime < hitStopUntil ? 0.05f : (Headless ? SimSpeed : 1f);

            if (player != null)
            {
                float danger = Current == State.Playing ? Mathf.Clamp01(1f - player.Fuel / (Tuning.lowFuelWarning * 1.6f)) : 0f;
                postFx.SetDanger(danger);
                Sfx.I?.SetDanger(player.IsAlive ? danger : 0f);
            }
        }

        void TickFloor(float dt)
        {
            if (level == null || player == null) return;
            FloorTime += dt;
            if (Current != State.Playing) return;

            RunTime += dt;
            Telemetry.Sample(player.Fuel, player.IsDimmed, player.IsSprinting, dt);
            if (!Headless) tutorial.Tick(dt, level, player);

            if (player.IsAlive && darkness.Tick(dt, Floor) && darkness.Spawn(level, player.transform.position) != null)
                Telemetry.DarknessSpawns++;
        }

        void HandleInput(MenuInput input)
        {
            // Title and pause menus handle their own input (FrontEnd); the blessing screen too.
            switch (Current)
            {
                case State.Playing:
                    if (input.Pause) SetPaused(true);
                    break;
                case State.GameOver:
                    // Ignore input during the death beat: mashing the flare button must not skip the screen.
                    if (!gameOverShown) break;
                    if (input.Retry || MenuInput.GameOverConfirm()) { Sfx.I?.Blip(); StartRun(run.Difficulty); }
                    else if (input.Back) EnterTitle();
                    break;
            }
        }

        void SetPaused(bool paused)
        {
            Current = paused ? State.Paused : State.Playing;
            Time.timeScale = paused ? 0f : 1f;
            // Input devices still report presses at timeScale 0, so the player must stop reading them.
            if (player != null) player.Controllable = !paused;
            if (paused) frontEnd.ShowPause();
            else frontEnd.Hide();
            AudioListener.pause = paused;
        }
    }
}
