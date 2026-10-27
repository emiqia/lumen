using System;

namespace Lumen.Core
{
    /// <summary>Four difficulty levels named after the size of the fire.</summary>
    public enum Difficulty { Vela, Antorcha, Hoguera, Ceniza }

    /// <summary>
    /// Multipliers a difficulty applies on top of the base tuning. Antorcha is the reference
    /// (all 1.0). Plain data so it can be unit tested and shown in the menu.
    /// </summary>
    public readonly struct DifficultyProfile
    {
        public readonly float Drain, EnemySpeed, Damage, BrazierReserve, Sight, DarknessInterval, EmberFuel;
        public readonly bool DevourerKillsInOneHit;
        /// <summary>On the harder fires nothing points you to the braziers (only a Map reveals them).</summary>
        public bool ShowsBrazierArrows => Drain <= 1f;

        DifficultyProfile(float drain, float enemySpeed, float damage, float reserve, float sight, float darkness, float ember, bool oneHit = false)
        {
            Drain = drain; EnemySpeed = enemySpeed; Damage = damage; BrazierReserve = reserve;
            Sight = sight; DarknessInterval = darkness; EmberFuel = ember; DevourerKillsInOneHit = oneHit;
        }

        public static DifficultyProfile For(Difficulty d) => d switch
        {
            Difficulty.Vela => new DifficultyProfile(0.75f, 0.85f, 0.7f, 1.3f, 0.85f, 1.3f, 1.25f),
            Difficulty.Hoguera => new DifficultyProfile(1.15f, 1.1f, 1.25f, 0.8f, 1.1f, 0.8f, 0.9f),
            Difficulty.Ceniza => new DifficultyProfile(1.3f, 1.2f, 1.5f, 0.55f, 1.25f, 0.65f, 0.75f, oneHit: true),
            _ => new DifficultyProfile(1f, 1f, 1f, 1f, 1f, 1f, 1f),
        };

        /// <summary>Applies this profile to a run-local copy of the tuning (never to the shared asset).</summary>
        public void ApplyTo(GameTuning t)
        {
            t.drainBright *= Drain;
            t.drainDim *= Drain;
            t.stalkerWanderSpeed *= EnemySpeed;
            t.stalkerChaseSpeed *= EnemySpeed;
            t.mothSpeed *= EnemySpeed;
            t.mothDiveSpeed *= EnemySpeed;
            t.devourerSpeed *= EnemySpeed;
            t.stalkerDamage *= Damage;
            t.mothDamage *= Damage;
            t.mimicDamage *= Damage;
            t.weaverDamage *= Damage;
            t.guardianDamage *= Damage;
            t.guardianWaveDamage *= Damage;
            t.devourerDamage = DevourerKillsInOneHit ? 9999f : t.devourerDamage * Damage;
            t.brazierReserve *= BrazierReserve;
            t.stalkerSightBase *= Sight;
            t.stalkerSightLightFactor *= Sight;
            t.darknessSpawnInterval *= DarknessInterval;
            t.darknessSpawnMinInterval *= DarknessInterval;
            t.emberFuel *= EmberFuel;
        }

        public static string LocaleKey(Difficulty d) => "difficulty." + d.ToString().ToLowerInvariant();
        public static readonly Difficulty[] All = (Difficulty[])Enum.GetValues(typeof(Difficulty));
    }
}
