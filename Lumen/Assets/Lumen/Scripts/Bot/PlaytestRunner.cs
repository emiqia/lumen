using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lumen.Core;
using Lumen.Enemies;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.UI;
using UnityEngine;

namespace Lumen.Bot
{
    /// <summary>
    /// Automated playtesting. Launch a build with:
    ///   Lumen.x86_64 -playtest 20 -playtestOut ./playtest [-difficulty Hoguera] [-shots] [-maxRunSeconds 900] [-batchmode -nographics]
    /// The autopilot plays N runs with fixed seeds and writes a JSON report (and optional
    /// screenshots) used for balance and review passes.
    /// </summary>
    public class PlaytestRunner : MonoBehaviour
    {
        float simSpeed = 8f;

        [Serializable]
        class Report
        {
            public string version;
            public string date;
            public int runs;
            public float avgFloor, avgScore, avgSeconds;
            public int maxFloor;
            public List<string> deathCauses = new List<string>();
            public List<RunTelemetry.RunRecord> records;
        }

        /// <summary>Timestamped highlights (Unix epoch seconds) used to cut gameplay clips from a screen recording.</summary>
        [Serializable]
        class Moment { public double epoch; public string what; }
        [Serializable]
        class Moments { public List<Moment> items = new List<Moment>(); }

        int runs = 10;
        string outDir = "playtest";
        bool shots;
        float maxRunSeconds = 900f;
        Difficulty difficulty = Difficulty.Antorcha;
        readonly Moments moments = new Moments();

        public static bool RequestedFromCommandLine() => Array.IndexOf(Environment.GetCommandLineArgs(), "-playtest") >= 0;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-playtest" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n)) runs = n;
                if (args[i] == "-playtestOut" && i + 1 < args.Length) outDir = args[i + 1];
                if (args[i] == "-shots") shots = true;
                if (args[i] == "-difficulty" && i + 1 < args.Length && Enum.TryParse(args[i + 1], true, out Difficulty d)) difficulty = d;
                if (args[i] == "-maxRunSeconds" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float m)) maxRunSeconds = m;
            }
            Directory.CreateDirectory(outDir);
            simSpeed = shots ? 1f : 8f;
            // Automated runs happen in the background: never play the game's audio through the speakers.
            AudioListener.volume = 0f;
        }

        IEnumerator Start()
        {
            var gm = GameManager.Instance;
            gm.Headless = !shots;
            gm.SimSpeed = simSpeed;
            gm.InputOverride = () => new AutoPilot();
            yield return null;

            if (shots)
            {
                GameEvents.EnemyKilled += (_, k, __) => Mark("kill_" + k);
                GameEvents.BrazierLit += _ => Mark("brazier_lit");
                GameEvents.BrazierExtinguished += _ => Mark("brazier_out");
                GameEvents.ExitOpened += () => Mark("exit_open");
                GameEvents.PlayerHit += (_, __, k) => Mark("hit_" + k);
                // Front end: title (with the attract demo behind), difficulty, options and the book.
                gm.EnterTitle();
                yield return new WaitForSecondsRealtime(5f);
                yield return Shot("title");
                gm.FrontEnd.ShowDifficulty();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("menu_difficulty");
                gm.FrontEnd.ShowOptions();
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Shot("menu_options");
                foreach (var mode in new[] { FrontEnd.Mode.Characters, FrontEnd.Mode.Provisions, FrontEnd.Mode.Unlocks, FrontEnd.Mode.Challenges })
                {
                    gm.FrontEnd.ShowScreen(mode);
                    yield return new WaitForSecondsRealtime(0.6f);
                    yield return Shot("menu_" + mode.ToString().ToLowerInvariant());
                }
                gm.FrontEnd.ShowTitle();
                gm.Book.Open(beforeRun: false);
                yield return new WaitForSecondsRealtime(0.12f);
                yield return Shot("book_opening");
                foreach (int page in new[] { 1, 2, 7, 10, 11, 12 })
                {
                    gm.Book.GoToPage(page);
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Shot("book_page" + page);
                }
                gm.Book.CloseSilently();
                yield return null;
            }

            for (int i = 0; i < runs; i++)
            {
                int seed = 1000 + i * 7919;
                UnityEngine.Random.InitState(seed);
                gm.StartRun(difficulty, seed); // the run seed is recorded in telemetry by GameManager
                float start = Time.unscaledTime;
                int shotFloor = 0, enemyShots = 0, shotEventFloor = 0;
                bool shotBrazier = false, shotBlessing = false;
                float nextEnemyShot = 0f;

                while (gm.Current != GameManager.State.GameOver)
                {
                    if (gm.RunTime > maxRunSeconds)
                    {
                        // Safety valve for a stuck (or simply excellent) bot: count it as a timeout.
                        gm.AbortRun("timeout");
                        break;
                    }
                    if (shots && !shotBlessing && gm.Current == GameManager.State.ChoosingBlessing)
                    {
                        shotBlessing = true;
                        yield return new WaitForSecondsRealtime(0.25f); // mid-deal
                        Capture($"run{i}_blessing_dealing");
                        yield return new WaitForSecondsRealtime(1f);
                        Capture($"run{i}_blessing");
                    }
                    if (shots && i < 3 && gm.Current == GameManager.State.Playing)
                    {
                        if (gm.Floor != shotFloor && gm.FloorTime > 3f)
                        {
                            shotFloor = gm.Floor;
                            Capture($"run{i}_floor{gm.Floor}_start");
                        }
                        if (!shotBrazier && gm.BraziersLit > 0 && gm.FloorTime > 1f)
                        {
                            shotBrazier = true;
                            Capture($"run{i}_floor{gm.Floor}_brazier");
                        }
                        if (LightField.Player != null && LightField.Player.Fuel < 25f && UnityEngine.Random.value < 0.002f)
                            Capture($"run{i}_floor{gm.Floor}_lowfuel_{(int)gm.RunTime}");
                        if (gm.FloorEvent != FloorEventKind.None && shotEventFloor != gm.Floor && gm.FloorTime > 8f)
                        {
                            shotEventFloor = gm.Floor;
                            Capture($"run{i}_floor{gm.Floor}_event_{gm.FloorEvent}");
                        }
                        if (Time.unscaledTime > nextEnemyShot && EnemyInView(out var kind) && (enemyShots < 3 || seenKinds.Add(kind)))
                        {
                            enemyShots++;
                            seenKinds.Add(kind);
                            nextEnemyShot = Time.unscaledTime + 8f;
                            Capture($"run{i}_floor{gm.Floor}_enemy_{kind}");
                        }
                    }
                    yield return null;
                }
                bool died = gm.Telemetry.Runs.Count > 0 && gm.Telemetry.Runs[gm.Telemetry.Runs.Count - 1].deathCause != "timeout";
                if (shots && i < 3 && died)
                {
                    yield return new WaitForSecondsRealtime(2f);
                    Capture($"run{i}_gameover");
                }
                WriteReport(gm.Telemetry.Runs); // after every run, so a killed process still leaves data
                Debug.Log($"[Playtest] run {i + 1}/{runs} floor={gm.Floor} score={gm.Score} t={gm.RunTime:0}s ({Time.unscaledTime - start:0.0}s real)");
                yield return null;
            }

            if (shots) File.WriteAllText(Path.Combine(outDir, "moments.json"), JsonUtility.ToJson(moments, true));
            Debug.Log("[Playtest] done -> " + Path.GetFullPath(outDir));
            Application.Quit();
        }

        void Mark(string what) =>
            moments.items.Add(new Moment { epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0, what = what });

        /// <summary>A visible, close non-moth enemy in line of sight: a good "threat" screenshot.</summary>
        static bool EnemyInView(out EnemyKind kind)
        {
            kind = EnemyKind.Stalker;
            var player = LightField.Player;
            if (player == null) return false;
            Vector2 pp = player.transform.position;
            foreach (var s in Shade.All)
            {
                if (s == null || s.IsDead) continue;
                Vector2 sp = s.transform.position;
                if ((sp - pp).sqrMagnitude < 4.5f * 4.5f && LightField.LineOfSight(pp, sp)) { kind = s.Kind; return true; }
            }
            return false;
        }

        readonly HashSet<EnemyKind> seenKinds = new HashSet<EnemyKind>();

        // ScreenCapture.CaptureScreenshot is deferred and can land a screen later; grab this frame instead.
        /// <summary>Waits until this frame is captured, so the next state change can't leak into it.</summary>
        IEnumerator Shot(string name) => CaptureAtEndOfFrame(Path.Combine(outDir, name + ".png"));

        void Capture(string name) => StartCoroutine(CaptureAtEndOfFrame(Path.Combine(outDir, name + ".png")));

        static IEnumerator CaptureAtEndOfFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }

        void WriteReport(List<RunTelemetry.RunRecord> records)
        {
            var r = new Report
            {
                version = Application.version,
                date = DateTime.Now.ToString("u"),
                runs = records.Count,
                records = records,
            };
            foreach (var rec in records)
            {
                r.avgFloor += rec.floorReached;
                r.avgScore += rec.score;
                r.avgSeconds += rec.seconds;
                r.maxFloor = Mathf.Max(r.maxFloor, rec.floorReached);
                r.deathCauses.Add(rec.deathCause);
            }
            if (records.Count > 0)
            {
                r.avgFloor /= records.Count;
                r.avgScore /= records.Count;
                r.avgSeconds /= records.Count;
            }
            File.WriteAllText(Path.Combine(outDir, "report.json"), JsonUtility.ToJson(r, true));
        }
    }
}
