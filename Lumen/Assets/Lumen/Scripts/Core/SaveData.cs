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
