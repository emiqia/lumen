using UnityEngine;

namespace Lumen.Core
{
    /// <summary>Persistent player data (best run, settings) backed by PlayerPrefs.</summary>
    public static class SaveData
    {
        const string KeyBestScore = "lumen.best.score";
        const string KeyBestFloor = "lumen.best.floor";
        const string KeyLanguage = "lumen.lang";
        const string KeyRuns = "lumen.runs";
        const string KeyShake = "lumen.shake";
        const string KeyBrightness = "lumen.brightness";

        /// <summary>Brightness steps offered in the menus (multiplier on ambient light).</summary>
        public static readonly float[] BrightnessSteps = { 0.8f, 1f, 1.25f, 1.5f, 1.8f };

        public static int BestScore => PlayerPrefs.GetInt(KeyBestScore, 0);
        public static int BestFloor => PlayerPrefs.GetInt(KeyBestFloor, 0);
        public static int Runs => PlayerPrefs.GetInt(KeyRuns, 0);

        public static string Language
        {
            get => PlayerPrefs.GetString(KeyLanguage, "es");
            set { PlayerPrefs.SetString(KeyLanguage, value); PlayerPrefs.Save(); }
        }

        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(KeyShake, 1) == 1;
            set { PlayerPrefs.SetInt(KeyShake, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(KeyBrightness, 1f);
            set { PlayerPrefs.SetFloat(KeyBrightness, value); PlayerPrefs.Save(); }
        }

        /// <summary>Cycles to the next brightness step and returns it.</summary>
        public static float CycleBrightness()
        {
            int i = System.Array.FindIndex(BrightnessSteps, s => Mathf.Approximately(s, Brightness));
            Brightness = BrightnessSteps[(i + 1) % BrightnessSteps.Length];
            return Brightness;
        }

        /// <summary>Records a finished run. Returns true if it set a new best score.</summary>
        public static bool SubmitRun(int score, int floor)
        {
            PlayerPrefs.SetInt(KeyRuns, Runs + 1);
            bool record = score > BestScore;
            if (record) PlayerPrefs.SetInt(KeyBestScore, score);
            if (floor > BestFloor) PlayerPrefs.SetInt(KeyBestFloor, floor);
            PlayerPrefs.Save();
            return record;
        }
    }
}
