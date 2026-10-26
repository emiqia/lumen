using Lumen.Art;
using Lumen.FX;
using Lumen.Core;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using Lumen.World;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// Slow, huge, immune to normal light. Hunts lit braziers and snuffs them out,
    /// re-sealing the exit. Flares stun it; it takes several to kill.
    /// </summary>
    public class Devourer : Shade
    {
        public override EnemyKind Kind => EnemyKind.Devourer;

        WoundGate wounds;
        SpriteRenderer[] woundMarks;
        Brazier targetBrazier;
        float retarget;
        Vector2 wanderTarget;

        public static Devourer Create(Vector2 pos, Transform parent)
        {
            var d = Build<Devourer>("Devourer", pos, parent, SpriteBank.DevourerBody, SpriteBank.DevourerEyes, 0.55f, 4f, 0.22f, 0.62f);
            d.body.mass = 20f;
            d.shadowSprite.transform.localScale = new Vector3(2f, 1.6f, 1f);

            // Three marks over its head: one per flare still needed. They go dark while it is
            // immune (enraged) and light up again when it can be hurt: the fight explains itself.
            var pips = new SpriteRenderer[Tuning.devourerFlaresToKill];
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = new GameObject("WoundMark").AddComponent<SpriteRenderer>();
                pip.transform.SetParent(d.transform, false);
                pip.transform.localPosition = new Vector3((i - (pips.Length - 1) * 0.5f) * 0.35f, 1.75f, 0f);
                pip.transform.localScale = Vector3.one * 3.5f; // the 1x1 px sprite -> 3.5 px square
                pip.sprite = SpriteBank.Pixel;
                pip.sharedMaterial = LumenAssets.I.Unlit;
                pip.sortingOrder = 32000;
                pips[i] = pip;
            }
            d.woundMarks = pips;
            return d;
        }

        protected override void Awake()
        {
            base.Awake();
            wounds = new WoundGate(Tuning.devourerStunTime + Tuning.devourerWoundImmunity);
        }

        /// <summary>Whether a flare would wound it right now (the bot uses this to save fuel).</summary>
        public bool Vulnerable => wounds.CanWound(Time.time);

        /// <summary>Charging after a wound. One landed hit sates it and it goes back to the braziers:
        /// otherwise a 30-damage body glued to the player kills in a few seconds.</summary>
        public bool Enraged => !wounds.CanWound(Time.time) && stunned <= 0f && !sated;
        bool sated;

        protected override void Think(float dt)
        {
            // Freshly wounded: ignore braziers and charge whoever burned it.
            if (Enraged && Player != null && Player.IsAlive)
            {
                MoveTo(Player.transform.position, Tuning.devourerSpeed * Tuning.devourerEnragedSpeed, 0.3f);
                return;
            }

            retarget -= dt;
            if (retarget <= 0f || targetBrazier == null || !targetBrazier.IsLit)
            {
                retarget = 2f;
                targetBrazier = NearestLitBrazier();
            }

            Vector2 pos = transform.position;
            if (targetBrazier != null)
            {
                Vector2 bp = (Vector2)targetBrazier.transform.position + Vector2.down * 0.6f;
                bool arrived = (bp - pos).sqrMagnitude < 0.8f * 0.8f;
                if (!arrived) MoveTo(bp, Tuning.devourerSpeed, 1f);
                else
                {
                    desiredVelocity = Vector2.zero;
                    targetBrazier.Drain(dt / Tuning.devourerExtinguishTime);
                    if (Random.value < dt * 30f)
                        PixelParticles.Emit((Vector2)targetBrazier.transform.position + Vector2.up * 0.7f,
                            (pos - (Vector2)targetBrazier.transform.position) * 1.5f, Palette.FireOrange, 0.5f);
                }
            }
            else if (Player != null && LightField.CanSeePlayer(pos, 3f, 0.6f))
            {
                MoveTo(Player.transform.position, Tuning.devourerSpeed * 1.2f, 0.6f);
            }
            else
            {
                if (MoveTo(wanderTarget, Tuning.devourerSpeed * 0.7f)) wanderTarget = RandomNearbyFloor(8);
            }
        }

        protected override void UpdateBurn(float dt) { } // immune to normal light

        protected override void OnFlare(Vector2 origin, float radius)
        {
            if (IsDead) return;
            Vector2 p = transform.position;
            if ((p - origin).sqrMagnitude > radius * radius || !LightField.LineOfSight(origin, p)) return;
            // A wound buys a stun, then several seconds of immunity while it charges you:
            // killing it takes separate engagements, not three flares back to back.
            if (!wounds.TryWound(Time.time))
            {
                PixelParticles.Burst(p + Vector2.up * 0.6f, 8, Palette.DevourerEdge, 1.5f, 0.4f, false);
                return;
            }
            stunned = Tuning.devourerStunTime;
            sated = false;
            bodySprite.color = new Color(1f, 0.6f, 0.6f);
            PixelParticles.Burst(p + Vector2.up * 0.6f, 20, Palette.DevourerEye, 3f, 0.6f);
            CameraRig.Shake(0.3f);
            if (wounds.Wounds >= Tuning.devourerFlaresToKill) Kill(KillCause.Flare);
        }

        protected override void Update()
        {
            base.Update();
            UpdateWoundMarks();
            // Enraged: glows angry red; otherwise fades back to normal after a stun.
            Color target = Enraged ? new Color(1f, 0.45f, 0.45f) : Color.white;
            if (stunned <= 0f) bodySprite.color = Color.Lerp(bodySprite.color, target, Time.deltaTime * 3f);
        }

        void UpdateWoundMarks()
        {
            if (woundMarks == null) return;
            int remaining = Tuning.devourerFlaresToKill - wounds.Wounds;
            bool vulnerable = Vulnerable;
            bool blink = Mathf.Repeat(Time.time * 4f, 1f) > 0.5f;
            for (int i = 0; i < woundMarks.Length; i++)
            {
                bool alive = i < remaining;
                Color c = !alive ? new Color(0.25f, 0.22f, 0.28f, 0.6f)          // spent
                    : vulnerable ? (Color)Palette.FireOrange                       // hit it now
                    : new Color(0.45f, 0.4f, 0.5f, blink ? 0.9f : 0.4f);          // immune: dark, flickering
                woundMarks[i].color = c;
            }
        }

        Brazier NearestLitBrazier()
        {
            Brazier best = null;
            float bestD = float.MaxValue;
            foreach (var b in LightField.Braziers)
            {
                if (!b.IsLit) continue;
                float d = ((Vector2)b.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        protected override void OnTouchPlayer(PlayerFlame player)
        {
            if (player.TakeHit(Tuning.devourerDamage, Kind, transform.position)) sated = true;
        }
    }
}
