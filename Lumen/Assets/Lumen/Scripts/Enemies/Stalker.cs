using Lumen.Art;
using Lumen.Core;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// Wanders the dark; hunts the player on sight. Sight range grows with the player's
    /// light, so dimming is the counter. Burns in strong light and avoids braziers.
    /// </summary>
    public class Stalker : Shade
    {
        enum State { Wander, Chase, Search, Flee }

        public override EnemyKind Kind => EnemyKind.Stalker;

        State state;
        Vector2 wanderTarget;
        Vector2 lastSeen;
        float stateTimer;
        float alertFlash;

        public static Stalker Create(Vector2 pos, Transform parent) =>
            Build<Stalker>("Stalker", pos, parent, SpriteBank.StalkerBody, SpriteBank.StalkerEyes, 0.3f, 6f, 0.28f, 0.38f);

        protected override void Awake()
        {
            base.Awake();
            wanderTarget = transform.position;
        }

        protected override void Think(float dt)
        {
            stateTimer -= dt;
            alertFlash -= dt;
            Vector2 pos = transform.position;

            if (LightField.InBrazierSafeZone(pos, out var brazier))
            {
                state = State.Flee;
                Vector2 away = (pos - (Vector2)brazier.transform.position).normalized;
                desiredVelocity = away * Tuning.stalkerChaseSpeed;
                return;
            }
            if (state == State.Flee) state = State.Wander;

            bool sees = LightField.CanSeePlayer(pos, Tuning.stalkerSightBase, Tuning.stalkerSightLightFactor);
            if (sees)
            {
                if (state != State.Chase)
                {
                    alertFlash = 0.4f; // eyes flare: "I see you"
                    GameEvents.RaiseStalkerAlerted(pos);
                }
                state = State.Chase;
                lastSeen = Player.transform.position;
                stateTimer = Tuning.stalkerMemory;
            }

            switch (state)
            {
                case State.Wander:
                    if (MoveTo(wanderTarget, Tuning.stalkerWanderSpeed) || stateTimer <= 0f)
                    {
                        wanderTarget = RandomNearbyFloor(6);
                        stateTimer = Random.Range(3f, 6f);
                    }
                    break;

                case State.Chase:
                    // Don't follow the player into a lit brazier.
                    if (LightField.InBrazierSafeZone(lastSeen, out _)) { state = State.Search; stateTimer = 2f; break; }
                    MoveTo(lastSeen, Tuning.stalkerChaseSpeed, 0.3f);
                    if (!sees && stateTimer <= 0f) { state = State.Search; stateTimer = 2.5f; }
                    if (!sees && ((Vector2)transform.position - lastSeen).sqrMagnitude < 0.3f) { state = State.Search; stateTimer = 2.5f; }
                    break;

                case State.Search:
                    if (MoveTo(lastSeen, Tuning.stalkerWanderSpeed * 1.4f) || stateTimer <= 0f)
                    {
                        state = State.Wander;
                        wanderTarget = RandomNearbyFloor(5);
                    }
                    break;
            }

            if (eyeSprite != null)
            {
                float s = alertFlash > 0 ? 1.4f : 1f;
                eyeSprite.color = state == State.Chase ? Color.white : new Color(1f, 1f, 1f, 0.75f);
                eyeSprite.transform.localScale = new Vector3(s, s, 1f);
            }
        }

        public override void Alert(Vector2 position)
        {
            lastSeen = position;
            state = State.Search;
            stateTimer = 6f;
        }

        protected override void OnTouchPlayer(PlayerFlame player)
        {
            player.TakeHit(Tuning.stalkerDamage, Kind, transform.position);
        }
    }
}
