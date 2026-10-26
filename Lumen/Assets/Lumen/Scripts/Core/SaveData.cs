using UnityEngine;

namespace Lumen.Core
{
    /// <summary>Persistent player data (records per difficulty, settings) backed by PlayerPrefs.</summary>
    public static class SaveData
    {
        const string KeyBestScore = "lumen.best.score";
        const string KeyBestFloor = "lumen.best.floor";
        const string KeyLanguage = "lumen.lang";
        const string KeyRuns = "lumen.runs";
        const string KeyShake = "lumen.shake";
        const string KeyBrightness = "lumen.brightness";
        const string KeyColorblind = "lumen.colorblind";
        const string KeyMusic = "lumen.volume.music";
        const string KeySfx = "lumen.volume.sfx";
        const string KeyWindowed = "lumen.windowed";
        const string KeyTimer = "lumen.timer";
        const string KeyTutorial = "lumen.tutorial.seen";
        const string KeyDifficulty = "lumen.difficulty";

        /// <summary>Brightness steps offered in the menus (multiplier on ambient light).</summary>
        public static readonly float[] BrightnessSteps = { 0.8f, 1f, 1.3f, 1.7f, 2.1f, 2.5f };
        public static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }
        static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); }

        static float NextStep(float[] steps, float current)
        {
            int i = System.Array.FindIndex(steps, s => Mathf.Approximately(s, current));
            return steps[(i + 1) % steps.Length];
        }

        // ------------------------------------------------------------------ records (per difficulty)
        static string Suffix(Difficulty d) => "." + d.ToString().ToLowerInvariant();

        public static int BestScore(Difficulty d) => PlayerPrefs.GetInt(KeyBestScore + Suffix(d), 0);
        public static int BestFloor(Difficulty d) => PlayerPrefs.GetInt(KeyBestFloor + Suffix(d), 0);
        public static int Runs => PlayerPrefs.GetInt(KeyRuns, 0);

        /// <summary>Records a finished run. Returns true if it set a new best score for that difficulty.</summary>
        public static bool SubmitRun(Difficulty d, int score, int floor)
        {
            PlayerPrefs.SetInt(KeyRuns, Runs + 1);
            bool record = score > BestScore(d);
            if (record) PlayerPrefs.SetInt(KeyBestScore + Suffix(d), score);
            if (floor > BestFloor(d)) PlayerPrefs.SetInt(KeyBestFloor + Suffix(d), floor);
            PlayerPrefs.Save();
            return record;
        }

        public static Difficulty LastDifficulty
        {
            get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(KeyDifficulty, (int)Difficulty.Antorcha), 0, 3);
            set { PlayerPrefs.SetInt(KeyDifficulty, (int)value); PlayerPrefs.Save(); }
        }

        public static bool TutorialSeen { get => GetBool(KeyTutorial, false); set => SetBool(KeyTutorial, value); }

        // ------------------------------------------------------------------ settings
        public static string Language
        {
            get => PlayerPrefs.GetString(KeyLanguage, "es");
            set { PlayerPrefs.SetString(KeyLanguage, value); PlayerPrefs.Save(); }
        }

        public static bool ScreenShake { get => GetBool(KeyShake, true); set => SetBool(KeyShake, value); }
        public static bool SpeedrunTimer { get => GetBool(KeyTimer, false); set => SetBool(KeyTimer, value); }
        public static bool Windowed { get => GetBool(KeyWindowed, false); set => SetBool(KeyWindowed, value); }

        public static float Brightness { get => PlayerPrefs.GetFloat(KeyBrightness, 1f); set => SetFloat(KeyBrightness, value); }
        public static float CycleBrightness() => Brightness = NextStep(BrightnessSteps, Brightness);

        public static float MusicVolume { get => PlayerPrefs.GetFloat(KeyMusic, 0.75f); set => SetFloat(KeyMusic, value); }
        public static float SfxVolume { get => PlayerPrefs.GetFloat(KeySfx, 1f); set => SetFloat(KeySfx, value); }
        public static float CycleMusic() => MusicVolume = NextStep(VolumeSteps, MusicVolume);
        public static float CycleSfx() => SfxVolume = NextStep(VolumeSteps, SfxVolume);

        static int colorblindCache = -1; // read every frame by every enemy: don't hit PlayerPrefs each time

        /// <summary>Swaps the red/green enemy eye pair for magenta/cyan.</summary>
        public static bool Colorblind
        {
            get
            {
                if (colorblindCache < 0) colorblindCache = PlayerPrefs.GetInt(KeyColorblind, 0);
                return colorblindCache == 1;
            }
            set
            {
                colorblindCache = value ? 1 : 0;
                SetBool(KeyColorblind, value);
            }
        }
    }
}
