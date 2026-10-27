using System.Collections.Generic;
using Lumen.Art;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using Lumen.World;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// A spider that creeps through corridors spinning webs. It shies away from strong light
    /// (and burns quickly in it), so it lays traps where you can't see.
    /// </summary>
    public class Weaver : Shade
    {
        const int MaxWebs = 3;

        public override EnemyKind Kind => EnemyKind.Weaver;

        readonly List<Web> webs = new List<Web>();
        Vector2 wanderTarget;
        float spinTimer;

        public static Weaver Create(Vector2 pos, Transform parent) =>
            Build<Weaver>("Weaver", pos, parent, SpriteBank.Weaver, SpriteBank.WeaverEyes, 0.28f, 8f, 0.2f, 0.45f);

        protected override void Awake()
        {
            base.Awake();
            wanderTarget = transform.position;
            spinTimer = Random.Range(2f, 5f);
        }

        protected override void Think(float dt)
        {
            Vector2 pos = transform.position;
            // Recoil from a bright flame.
            if (Player != null && Player.IsAlive && !Player.IsDimmed &&
                Vector2.Distance(pos, Player.transform.position) < Player.LightRadius * 0.6f)
            {
                desiredVelocity = (pos - (Vector2)Player.transform.position).normalized * Tuning.weaverSpeed * 1.6f * speedMultiplier;
                return;
            }

            if (MoveTo(wanderTarget, Tuning.weaverSpeed)) wanderTarget = RandomNearbyFloor(7);

            spinTimer -= dt;
            if (spinTimer > 0f) return;
            spinTimer = Tuning.weaverWebInterval;
            webs.RemoveAll(w => w == null);
            var cell = Cell;
            if (webs.Count >= MaxWebs || !map.IsFloor(cell) || LevelHazards.WebNear(DungeonMap.CellToWorld(cell), 2.5f)) return;
            webs.Add(Web.Create(cell, transform.parent));
        }

        protected override float BurnTime => 1f;

        protected override void OnTouchPlayer(PlayerFlame player) => player.TakeHit(Tuning.weaverDamage, Kind, transform.position);
    }
}
