using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lumen.Core;
using Lumen.Lighting;
using UnityEngine;

namespace Lumen.Bot
{
    /// <summary>
    /// Automated playtesting. Launch a build with:
    ///   Lumen.x86_64 -playtest 20 -playtestOut ./playtest [-shots] [-maxRunSeconds 900] [-batchmode -nographics]
    /// The autopilot plays N runs with fixed seeds and writes a JSON report (and optional
    /// screenshots) used for balance and review passes.
    /// </summary>
    public class PlaytestRunner : MonoBehaviour
    {
        public static float SimSpeed { get; private set; } = 1f;

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

        int runs = 10;
        string outDir = "playtest";
        bool shots;
        float maxRunSeconds = 900f;

        public static bool RequestedFromCommandLine() => Array.IndexOf(Environment.GetCommandLineArgs(), "-playtest") >= 0;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-playtest" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n)) runs = n;
                if (args[i] == "-playtestOut" && i + 1 < args.Length) outDir = args[i + 1];
                if (args[i] == "-shots") shots = true;
                if (args[i] == "-maxRunSeconds" && i + 1 < args.Length && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float m)) maxRunSeconds = m;
            }
            Directory.CreateDirectory(outDir);
            SimSpeed = shots ? 1f : 8f;
        }

        IEnumerator Start()
        {
            var gm = GameManager.Instance;
            gm.Headless = !shots;
            gm.InputOverride = () => new AutoPilot();
            yield return null;

            for (int i = 0; i < runs; i++)
            {
                int seed = 1000 + i * 7919;
                UnityEngine.Random.InitState(seed);
                gm.StartRun(seed);
                gm.Telemetry.Current.seed = seed;
                float start = Time.unscaledTime;
                int shotFloor = 0;
                bool shotBrazier = false;

                while (gm.Current != GameManager.State.GameOver)
                {
                    if (gm.RunTime > maxRunSeconds)
                    {
                        // Safety valve for a stuck bot: count it as a timeout.
                        gm.Telemetry.EndRun("timeout", gm.Floor, gm.Score, gm.RunTime);
                        break;
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
                    }
                    yield return null;
                }
                if (shots && i < 3)
                {
                    yield return new WaitForSecondsRealtime(1.6f);
                    Capture($"run{i}_gameover");
                }
                WriteReport(gm.Telemetry.Runs); // after every run, so a killed process still leaves data
                Debug.Log($"[Playtest] run {i + 1}/{runs} floor={gm.Floor} score={gm.Score} t={gm.RunTime:0}s ({Time.unscaledTime - start:0.0}s real)");
                yield return null;
            }

            Debug.Log("[Playtest] done -> " + Path.GetFullPath(outDir));
            Application.Quit();
        }

        void Capture(string name)
        {
            string path = Path.Combine(outDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
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
