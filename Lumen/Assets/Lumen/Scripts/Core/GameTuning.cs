using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Every gameplay number in one designer-editable asset.
    /// Loaded from Resources/GameTuning; falls back to these defaults if missing.
    /// </summary>
    [CreateAssetMenu(menuName = "Lumen/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Player movement")]
        public float moveSpeed = 4.2f;
        public float dimMoveSpeed = 2.9f;
        public float acceleration = 40f;

        [Header("Fuel (= health = time)")]
        public float maxFuel = 100f;
        public float drainBright = 1.5f;
        public float drainDim = 0.55f;
        public float brazierRefuelPerSecond = 22f;
        public float emberFuel = 12f;
        public float hitInvulnerability = 1.1f;
        public float lowFuelWarning = 25f;

        [Header("Player light")]
        public float minLightRadius = 3.2f;
        public float maxLightRadius = 7.5f;
        public float dimLightRadius = 2.2f;
        public float burnRadiusFactor = 0.45f; // portion of the light that hurts shades

        [Header("Flare")]
        public float flareCost = 18f;
        public float flareRadius = 5.5f;
        public float flareCooldown = 1.1f;

        [Header("Braziers")]
        public float brazierLightRadius = 5f;
        public float brazierSafeRadius = 3.2f;

        [Header("Enemies")]
        public float stalkerWanderSpeed = 1.4f;
        public float stalkerChaseSpeed = 3.3f;
        public float stalkerDamage = 22f;
        public float stalkerBurnTime = 1.4f;   // seconds in burn light to die
        public float mothSpeed = 2.6f;
        public float mothDiveSpeed = 7.5f;
        public float mothDamage = 10f;
        public float devourerSpeed = 1.25f;
        public float devourerDamage = 30f;
        public int devourerFlaresToKill = 3;
        public float devourerExtinguishTime = 2.5f;

        [Header("Pressure")]
        public float darknessSpawnInterval = 40f; // a new stalker emerges if the floor drags on

        [Header("Score")]
        public int scoreEmber = 10;
        public int scoreBrazier = 50;
        public int scoreStalker = 25;
        public int scoreMoth = 15;
        public int scoreDevourer = 150;
        public int scoreFloorBase = 100;
        public float floorParTime = 75f;
        public int scorePerSecondUnderPar = 4;

        static GameTuning instance;
        public static GameTuning I
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameTuning>("GameTuning");
                    if (instance == null) instance = CreateInstance<GameTuning>();
                }
                return instance;
            }
        }
    }
}
