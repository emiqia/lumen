using Lumen.Enemies;
using Lumen.Level;
using Lumen.Lighting;
using UnityEngine;

namespace Lumen.Core
{
    /// <summary>
    /// Anti-camping pressure: the longer a floor drags on, the more stalkers the darkness
    /// births. New stalkers wake up already searching toward the player's position.
    /// The interval shortens on deep floors so the late game keeps tightening.
    /// </summary>
    public class DarknessSpawner
    {
        GameTuning tuning;
        float timer;

        public DarknessSpawner(GameTuning tuning) => this.tuning = tuning;

        /// <summary>Swapped per run so difficulty multipliers apply.</summary>
        public GameTuning Tuning { get => tuning; set => tuning = value; }

        public void Reset() => timer = 0f;

        public float IntervalFor(int floor) =>
            Mathf.Max(tuning.darknessSpawnMinInterval,
                tuning.darknessSpawnInterval - tuning.darknessIntervalDropPerFloor * Mathf.Max(0, floor - tuning.darknessRampFromFloor));

        /// <summary>Advances the clock. Returns true when a spawn is due.</summary>
        public bool Tick(float dt, int floor)
        {
            timer += dt;
            if (timer < IntervalFor(floor)) return false;
            timer = 0f;
            return true;
        }

        /// <summary>Spawns a stalker far from the player, outside any brazier's light.</summary>
        public Shade Spawn(LevelInstance level, Vector2 playerPos)
        {
            var map = level.Map;
            var dist = map.DistanceField(DungeonMap.WorldToCell(playerPos));
            for (int i = 0; i < 40; i++)
            {
                var c = new Vector2Int(Random.Range(0, map.Width), Random.Range(0, map.Height));
                if (dist[c.x, c.y] < tuning.darknessSpawnMinSteps) continue;
                Vector2 p = DungeonMap.CellToWorld(c);
                if (LightField.InBrazierSafeZone(p, out _)) continue;
                var shade = LevelBuilder.SpawnEnemy(level, EnemyKind.Stalker, p);
                shade.Alert(playerPos);
                return shade;
            }
            return null;
        }
    }
}
