using Lumen.Art;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// Drawn to light from far away (no line of sight needed). Circles its target, then dives.
    /// Lit braziers also attract them — light one to lure moths away from you.
    /// </summary>
    public class Moth : Shade
    {
        enum State { Idle, Approach, Orbit, Dive, Recover }

        public override EnemyKind Kind => EnemyKind.Moth;
        protected override int ScoreValue => Tuning.scoreMoth;

        State state;
        float timer;
        float orbitAngle;
        Vector2 diveDir;
        Transform target;

        public static Moth Create(Vector2 pos, Transform parent)
        {
            var m = Build<Moth>("Moth", pos, parent, SpriteBank.Moth, SpriteBank.MothGlow, 0.2f, 14f);
            m.bodySprite.transform.localPosition = Vector3.up * 0.3f;
            return m;
        }

        protected override void Think(float dt)
        {
            timer -= dt;
            target = PickTarget();
            Vector2 pos = transform.position;

            switch (state)
            {
                case State.Idle:
                    desiredVelocity = Vector2.Lerp(desiredVelocity, Random.insideUnitCircle * 0.8f, dt * 2f);
                    if (target != null) state = State.Approach;
                    break;

                case State.Approach:
                    if (target == null) { state = State.Idle; break; }
                    Vector2 tp = target.position;
                    MoveTo(tp, Tuning.mothSpeed, 0.4f);
                    if ((tp - pos).sqrMagnitude < 2.6f * 2.6f && LightField.LineOfSight(pos, tp))
                    {
                        state = State.Orbit;
                        timer = Random.Range(1.4f, 2.6f);
                        orbitAngle = Mathf.Atan2(pos.y - tp.y, pos.x - tp.x);
                    }
                    break;

                case State.Orbit:
                    if (target == null) { state = State.Idle; break; }
                    orbitAngle += dt * 2.4f;
                    Vector2 c = target.position;
                    Vector2 orbitPos = c + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * 2.2f;
                    desiredVelocity = (orbitPos - pos) * 4f;
                    // Only dive at the player; braziers are just circled forever.
                    if (timer <= 0f && Player != null && target == Player.transform)
                    {
                        state = State.Dive;
                        timer = 0.55f;
                        diveDir = ((Vector2)Player.transform.position - pos).normalized;
                        PixelParticles.Burst(pos, 6, Palette.MothWing, 1.5f, 0.3f, false);
                    }
                    break;

                case State.Dive:
                    desiredVelocity = diveDir * Tuning.mothDiveSpeed;
                    body.linearVelocity = desiredVelocity;
                    if (timer <= 0f) { state = State.Recover; timer = 1.2f; }
                    break;

                case State.Recover:
                    desiredVelocity = Vector2.Lerp(desiredVelocity, Vector2.zero, dt * 3f);
                    if (timer <= 0f) state = State.Approach;
                    break;
            }

            // Bob up and down while flying.
            bodySprite.transform.localPosition = new Vector3(0, 0.3f + Mathf.Sin(Time.time * 9f + GetInstanceID()) * 0.06f, 0);
        }

        Transform PickTarget()
        {
            Vector2 pos = transform.position;
            Transform best = null;
            float bestScore = 0f;

            if (Player != null && Player.IsAlive)
            {
                // Attraction scales with the player's light: dim and they lose interest.
                float range = Player.IsDimmed ? 2.5f : Player.LightRadius * 2.2f;
                float d = Vector2.Distance(pos, Player.transform.position);
                if (d < range) { best = Player.transform; bestScore = range - d; }
            }
            foreach (var b in LightField.Braziers)
            {
                if (!b.IsLit) continue;
                float range = Tuning.brazierLightRadius * 2f;
                float d = Vector2.Distance(pos, b.transform.position);
                if (d < range && range - d > bestScore) { best = b.transform; bestScore = range - d; }
            }
            return best;
        }

        // Moths love light: they are not hurt by the player's glow or braziers, only by flares.
        protected override void UpdateBurn(float dt) { }

        protected override void OnTouchPlayer(PlayerFlame player)
        {
            if (player.TakeHit(Tuning.mothDamage, Kind, transform.position)) Kill();
        }

        protected override void OnCollisionStay2D(Collision2D c)
        {
            base.OnCollisionStay2D(c);
            if (state == State.Dive && c.collider.gameObject.layer == Core.Layers.Walls)
            {
                state = State.Recover;
                timer = 0.8f;
                stunned = 0.5f;
            }
        }
    }
}
