using System.Collections;
using Lumen.Art;
using Lumen.Bot;
using Lumen.Enemies;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using Lumen.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lumen.Core
{
    /// <summary>
    /// Composition root and game-flow state machine. The scene contains only this component;
    /// camera, lighting, post-processing, audio, UI and levels are all assembled here.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State { Title, Playing, Transition, Paused, GameOver }

        public static GameManager Instance { get; private set; }

        public State Current { get; private set; }
        public int Floor { get; private set; }
        public int Score { get; private set; }
        public float RunTime { get; private set; }
        public float FloorTime { get; private set; }
        public int BraziersLit { get; private set; }
        public int BraziersTotal => level?.Braziers.Count ?? 0;
        public bool ExitOpen => level?.Exit != null && level.Exit.IsOpen;
        public LevelInstance Level => level;
        public PlayerFlame Player => player;
        public RunTelemetry Telemetry { get; private set; }

        /// <summary>Set by the playtest runner: an AI drives the player instead of the keyboard.</summary>
        public System.Func<IPlayerInput> InputOverride { get; set; }
        public bool Headless { get; set; }

        Camera cam;
        CameraRig rig;
        Hud hud;
        Screens screens;
        LevelInstance level;
        PlayerFlame player;
        Vignette vignette;
        float darknessTimer;
        bool exitAnnounced;
        int lastBraziersLit;
        static float hitStopUntil;

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
            BuildPostProcessing();
            LightFactory.Global(transform, Palette.AmbientLight, 0.07f);
            new GameObject("Particles").AddComponent<PixelParticles>().transform.SetParent(transform);
            new GameObject("Audio").AddComponent<Sfx>().transform.SetParent(transform);

            var canvas = Ui.CreateCanvas("UI", 100);
            canvas.transform.SetParent(transform);
            hud = Hud.Create(canvas.transform);
            screens = Screens.Create(canvas.transform);

            Telemetry = new RunTelemetry();
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.ScoreGained += (amount, pos) => hud.ScorePopup(pos, amount);
            GameEvents.BrazierExtinguished += p => hud.Announce("", Locale.Get("hud.relit"), 2f);
        }

        void Start()
        {
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

        void BuildPostProcessing()
        {
            var go = new GameObject("PostFX");
            go.transform.SetParent(transform);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.75f);
            bloom.scatter.Override(0.65f);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.18f);

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(12f);
            color.saturation.Override(6f);
        }

        // ------------------------------------------------------------------ flow

        public void EnterTitle()
        {
            Current = State.Title;
            Time.timeScale = 1f;
            screens.HideGameOver();
            screens.ShowPause(false);
            screens.ShowTitle(true);
            hud.gameObject.SetActive(false);
            Floor = 2;
            // Attract mode: the autopilot plays a floor behind the title screen.
            LoadFloor(2, Random.Range(0, 99999), () => new AutoPilot());
            screens.SetFade(0f);
        }

        public void StartRun(int seed = -1)
        {
            Score = 0;
            RunTime = 0f;
            Telemetry.BeginRun();
            screens.ShowTitle(false);
            screens.HideGameOver();
            hud.gameObject.SetActive(!Headless);
            StartCoroutine(TransitionTo(1, seed < 0 ? Random.Range(0, 999999) : seed, fadeOut: !Headless));
        }

        IEnumerator TransitionTo(int floor, int seed, bool fadeOut)
        {
            Current = State.Transition;
            if (fadeOut)
            {
                screens.SetFade(1f);
                while (!screens.FadeDone) yield return null;
            }

            float carriedFuel = player != null && player.IsAlive && floor > 1 ? player.Fuel : GameTuning.I.maxFuel;
            LoadFloor(floor, seed, InputOverride);
            // Fuel carries over between floors (with a small bonus), making efficiency matter.
            player.SetFuel(Mathf.Max(carriedFuel + 25f, 50f));

            hud.ClearTransient();
            hud.Announce(Locale.Get("floor.enter") + " " + floor, IntroFor(floor), 2.8f);
            if (floor == 1) StartCoroutine(Tutorial());
            GameEvents.RaiseFloorStarted(floor);
            Telemetry.BeginFloor(floor);

            screens.SetFade(0f);
            Current = State.Playing;
        }

        static string IntroFor(int floor)
        {
            if (floor == 2) return Locale.Get("floor.newEnemy.Moth");
            if (floor == 3) return Locale.Get("floor.newEnemy.Devourer");
            return "";
        }

        IEnumerator Tutorial()
        {
            string[] keys = { "tut.move", "tut.brazier", "tut.fuel", "tut.dim", "tut.flare" };
            yield return new WaitForSeconds(2.5f);
            foreach (var k in keys)
            {
                if (Current != State.Playing || Floor != 1) yield break;
                hud.Hint(Locale.Get(k), 5.5f);
                yield return new WaitForSeconds(6f);
            }
        }

        void LoadFloor(int floor, int seed, System.Func<IPlayerInput> input)
        {
            UnloadLevel();
            Floor = floor;
            FloorTime = 0f;
            BraziersLit = 0;
            darknessTimer = 0f;
            exitAnnounced = false;

            var map = DungeonGenerator.Generate(seed, FloorSettings.ForFloor(floor));
            level = LevelBuilder.Build(map, seed, transform);
            player = PlayerFlame.Create(DungeonMap.CellToWorld(map.PlayerSpawn));
            player.transform.SetParent(level.Root.transform);
            if (input != null) player.Input = input();

            rig.target = player.transform;
            rig.SnapTo(player.transform.position);
            float halfH = cam.orthographicSize, halfW = halfH * 16f / 9f;
            rig.bounds = Rect.MinMaxRect(halfW - 1, halfH - 1, map.Width - halfW + 1, map.Height - halfH + 1);
            if (rig.bounds.width < 0 || rig.bounds.height < 0) rig.bounds = new Rect();

            hud.SetBrazierCount(level.Braziers.Count);
            lastBraziersLit = 0;
        }

        void UnloadLevel()
        {
            PixelParticles.I?.ClearAll();
            if (level != null) Destroy(level.Root);
            level = null;
            player = null;
            Shade.All.Clear();
            LightField.Clear();
        }

        public void CompleteFloor()
        {
            if (Current == State.Title) { EnterTitle(); return; } // attract-mode bot finished: new demo floor
            if (Current != State.Playing) return;
            int bonus = GameTuning.I.scoreFloorBase * Floor +
                        Mathf.Max(0, Mathf.RoundToInt((GameTuning.I.floorParTime - FloorTime) * GameTuning.I.scorePerSecondUnderPar));
            AddScore(bonus, player.transform.position);
            Telemetry.EndFloor(FloorTime, player.Fuel);
            GameEvents.RaiseFloorCleared(Floor);
            player.Controllable = false;
            StartCoroutine(TransitionTo(Floor + 1, Random.Range(0, 999999), fadeOut: !Headless));
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
            Telemetry.EndRun(cause, Floor, Score, RunTime);
            bool record = !Headless && SaveData.SubmitRun(Score, Floor);
            StartCoroutine(ShowGameOverDelayed(cause, record));
        }

        IEnumerator ShowGameOverDelayed(string cause, bool record)
        {
            yield return new WaitForSecondsRealtime(Headless ? 0.1f : 1.4f);
            if (!Headless) screens.ShowGameOver(cause, Score, Floor, RunTime, record);
        }

        IEnumerator RestartAttract()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (Current == State.Title) EnterTitle();
        }

        public static void AddScore(int amount, Vector2 pos)
        {
            var gm = Instance;
            if (gm == null || gm.Current != State.Playing) return;
            gm.Score += amount;
            GameEvents.RaiseScoreGained(amount, pos);
        }

        public static void HitStop(float duration)
        {
            if (Instance == null || Instance.Headless) return;
            hitStopUntil = Time.unscaledTime + duration;
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            HandleInput();
            if (Current == State.Playing || Current == State.Title) TickFloor();

            // Hit-stop: a few frames of near-freeze on impact for weight.
            if (Current == State.Playing)
                Time.timeScale = Time.unscaledTime < hitStopUntil ? 0.05f : (Headless ? PlaytestRunner.SimSpeed : 1f);

            // Vignette tightens and turns red as the flame fades.
            if (player != null && vignette != null)
            {
                float danger = Current == State.Playing ? Mathf.Clamp01(1f - player.Fuel / (GameTuning.I.lowFuelWarning * 1.6f)) : 0f;
                vignette.intensity.value = Mathf.Lerp(0.38f, 0.55f, danger);
                vignette.color.value = Color.Lerp(Color.black, new Color(0.35f, 0f, 0.02f), danger);
                Sfx.I?.SetDanger(player.IsAlive ? danger : 0f);
            }
        }

        void TickFloor()
        {
            if (level == null || player == null) return;
            float dt = Time.deltaTime;
            FloorTime += dt;
            if (Current == State.Playing)
            {
                RunTime += dt;
                Telemetry.Sample(player.Fuel, player.IsDimmed, dt);
            }

            int lit = 0;
            foreach (var b in level.Braziers) if (b.IsLit) lit++;
            BraziersLit = lit;
            if (lit != lastBraziersLit) Telemetry.OnBraziersChanged(lit, lastBraziersLit);
            lastBraziersLit = lit;

            bool allLit = lit == level.Braziers.Count;
            level.Exit.SetOpen(allLit);
            if (allLit && !exitAnnounced && Current == State.Playing)
            {
                exitAnnounced = true;
                hud.Announce("", Locale.Get("hud.exit"), 2.5f);
            }
            if (!allLit) exitAnnounced = false;

            // Pressure valve: lingering too long lets the darkness birth new stalkers.
            darknessTimer += dt;
            if (darknessTimer >= GameTuning.I.darknessSpawnInterval && player.IsAlive)
            {
                darknessTimer = 0f;
                SpawnFromDarkness();
            }
        }

        void SpawnFromDarkness()
        {
            var map = level.Map;
            var dist = map.DistanceField(DungeonMap.WorldToCell(player.transform.position));
            for (int i = 0; i < 40; i++)
            {
                var c = new Vector2Int(Random.Range(0, map.Width), Random.Range(0, map.Height));
                if (dist[c.x, c.y] < 14) continue;
                Vector2 p = DungeonMap.CellToWorld(c);
                if (LightField.InBrazierSafeZone(p, out _)) continue;
                LevelBuilder.SpawnEnemy(level, EnemyKind.Stalker, p);
                Telemetry.DarknessSpawns++;
                return;
            }
        }

        void HandleInput()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if (kb == null && pad == null) return;
            bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (pad != null && pad.startButton.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame);

            switch (Current)
            {
                case State.Title:
                    if (confirm) { Sfx.I?.Blip(); StartRun(); }
                    else if (kb != null && kb.lKey.wasPressedThisFrame) { Locale.Toggle(); screens.ShowTitle(true); Sfx.I?.Blip(); }
                    else if (back) Application.Quit();
                    break;
                case State.Playing:
                    if (back || (pad != null && pad.startButton.wasPressedThisFrame)) SetPaused(true);
                    break;
                case State.Paused:
                    if (back || confirm) SetPaused(false);
                    else if (kb != null && kb.qKey.wasPressedThisFrame) { SetPaused(false); EnterTitle(); }
                    else if (kb != null && kb.sKey.wasPressedThisFrame) { SaveData.ScreenShake = !SaveData.ScreenShake; screens.RefreshPause(); }
                    break;
                case State.GameOver:
                    if ((kb != null && kb.rKey.wasPressedThisFrame) || confirm) { Sfx.I?.Blip(); StartRun(); }
                    else if (back) EnterTitle();
                    break;
            }
        }

        void SetPaused(bool paused)
        {
            Current = paused ? State.Paused : State.Playing;
            Time.timeScale = paused ? 0f : 1f;
            screens.ShowPause(paused);
            AudioListener.pause = paused;
        }
    }
}
