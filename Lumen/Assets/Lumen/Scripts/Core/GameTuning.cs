using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Every gameplay number in one designer-editable asset.
    /// Loaded from Resources/GameTuning; falls back to these defaults if missing.
    /// The per-floor difficulty curve lives in <see cref="Level.FloorSettings.ForFloor"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Lumen/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Player movement")]
        public float moveSpeed = 4.2f;
        public float dimMoveSpeed = 3.1f;
        public float acceleration = 40f;

        [Header("Sprint (costs light)")]
        public float sprintSpeedMultiplier = 1.6f;
        public float sprintDrainMultiplier = 3f;   // burns this much faster than full light
        public float sprintLightBoost = 1.2f;      // the flame flares up: enemies see you from farther

        [Header("Fuel (= health = time)")]
        public float maxFuel = 100f;
        public float drainBright = 1.5f;
        public float drainDim = 0.6f;
        public float emberFuel = 12f;
        public float hitInvulnerability = 1.1f;
        public float lowFuelWarning = 25f;
        public float floorCarryBonus = 15f;    // fuel added when entering the next floor
        public float floorCarryMinimum = 45f;  // never start a floor below this
        public float deathCauseWindow = 3f;    // dying this soon after a hit blames the enemy
        public int extraLives = 0;             // granted by the "Segunda chispa" blessing

        [Header("Character abilities (set per run by the chosen flame)")]
        public float emberMagnetMultiplier = 1f;
        public bool mothsIgnoreDimmed = false;
        public bool ignoreWebs = false;
        public bool lanternEachFloor = false;
        public bool waterImmune = false;       // Ash Boots
        public float lightBombRadius = 4.5f;
        public float shadowDustTime = 5f;      // seconds of invisibility
        public float reviveFuel = 35f;

        [Header("Player light")]
        public float minLightRadius = 3.2f;
        public float maxLightRadius = 7.5f;
        public float dimLightRadius = 2.2f;
        public float burnRadiusFactor = 0.4f; // portion of the light that hurts shades

        [Header("Flare")]
        public float flareCost = 22f;
        public float flareRadius = 5.5f;      // stuns everything in line of sight
        public float flareKillRadius = 3.2f;  // kills inside this radius
        public float flareStunTime = 2f;
        public float flareCooldown = 2.2f;

        [Header("Braziers")]
        public float brazierLightRadius = 5f;
        public float brazierSafeRadius = 3.2f;
        public float brazierDepletedSafeRadius = 1.8f;
        public float brazierRefuelPerSecond = 16f;
        public float brazierReserve = 80f;    // fuel a brazier can give before running dry

        [Header("Stalker")]
        public float stalkerWanderSpeed = 1.4f;
        public float stalkerChaseSpeed = 3.6f;
        public float stalkerDamage = 22f;
        public float stalkerBurnTime = 1.3f;   // seconds in burn light to die
        public float stalkerSightBase = 1.5f;  // sight range = base + player light radius * factor
        public float stalkerSightLightFactor = 1f;
        public float stalkerMemory = 3f;       // seconds it keeps chasing after losing sight
        public float stalkerBurnSlow = 0.5f;   // speed multiplier while burning: full light is a weapon

        [Header("Moth")]
        public float mothSpeed = 2.6f;
        public float mothDiveSpeed = 7.5f;
        public float mothDamage = 14f;
        public float mothOrbitRadius = 3f;
        public float mothRetreatTime = 2.5f;   // after a successful dive it backs off, then returns

        [Header("Devourer")]
        public float devourerSpeed = 1.25f;
        public float devourerDamage = 22f;
        public int devourerFlaresToKill = 3;
        public float devourerStunTime = 2f;
        public float devourerWoundImmunity = 3f;   // after the stun wears off it shrugs off flares for this long...
        public float devourerEnragedSpeed = 1.6f;  // ...and charges at this multiplier (still slower than you)
        public float devourerExtinguishTime = 2.5f;

        [Header("Items")]
        public float lureDuration = 7f;        // a thrown decoy shines this long
        public float lureAttractRadius = 11f;  // stalkers within this distance go investigate
        public float lureThrowDistance = 3.5f;

        [Header("Biome hazards")]
        public float waterSprintDrain = 9f;    // extra fuel/s when running through water (walking is free)
        public float windGustDim = 0.45f;      // light radius multiplier while a gust hits you
        public float windGustPush = 2.2f;
        public float doorOpenTime = 7f;
        public float doorClosedTime = 4f;

        [Header("Pressure")]
        public float darknessSpawnInterval = 40f;    // a new stalker emerges if the floor drags on
        public float darknessSpawnMinInterval = 22f;
        public int darknessRampFromFloor = 6;        // interval shrinks after this floor...
        public float darknessIntervalDropPerFloor = 2f; // ...by this much per floor
        public int darknessSpawnMinSteps = 14;       // spawn at least this far (walking) from the player

        [Header("Score")]
        public int scoreEmber = 10;
        public int scoreBrazier = 50;
        public int scoreStalker = 25;
        public int scoreMoth = 15;
        public int scoreDevourer = 150;
        public int scoreFloorBase = 100;
        public float floorParBase = 30f;      // par time = base + perFloor * min(floor, parFloorCap)
        public float floorParPerFloor = 6f;
        public int floorParCap = 9;
        public int scorePerSecondUnderPar = 4;

        [Header("Presentation")]
        public float ambientIntensity = 0.14f;
        public float vignetteBase = 0.28f;
        public float vignetteDanger = 0.5f;

        public float FloorParTime(int floor) => floorParBase + floorParPerFloor * Mathf.Min(floor, floorParCap);

        static GameTuning instance;

        /// <summary>The designer-edited asset (never modified at runtime).</summary>
        public static GameTuning Base
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

        /// <summary>
        /// The run-local copy with difficulty and blessings applied, set by the GameManager while a
        /// run is in progress. Gameplay reads <see cref="I"/> and gets the right numbers either way.
        /// </summary>
        public static GameTuning Active { get; set; }

        public static GameTuning I => Active != null ? Active : Base;
    }
}
