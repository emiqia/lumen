using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Enemies
{
    /// <summary>
    /// Poses as an ember. The tell: it pulses out of rhythm and twitches now and then. Get close
    /// and it bites, chases you for a few seconds, then settles back into its disguise.
    /// Your bright light reveals it; a flare kills it.
    /// </summary>
    public class Mimic : Shade
    {
        const float ChaseTime = 5f;

        public override EnemyKind Kind => EnemyKind.Mimic;
        public bool Disguised { get; private set; } = true;

        SpriteAnimator anim;
        Light2D lure;
        float revealedFor, twitch;

        public static Mimic Create(Vector2 pos, Transform parent)
        {
            var m = Build<Mimic>("Mimic", pos, parent, SpriteBank.Mimic, SpriteBank.MimicEyes, 0.3f, 6f, 0.25f, 0.3f);
            m.InitDisguise();
            return m;
        }

        void InitDisguise()
        {
            anim = bodySprite.GetComponent<SpriteAnimator>();
            lure = LightFactory.Point(transform, Palette.FireOrange, 1.4f, 0.6f, false); // same glow as a real ember
            SetDisguised(true);
        }

        void SetDisguised(bool value)
        {
            Disguised = value;
            anim.frames = value ? SpriteBank.Ember : SpriteBank.Mimic;
            anim.fps = value ? 6f : 8f;
            bodySprite.sharedMaterial = value ? LumenAssets.I.Unlit : LumenAssets.I.Lit;
            if (eyeSprite != null) eyeSprite.gameObject.SetActive(!value);
            lure.enabled = value;
            if (!value) PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.3f, 14, Palette.FireRed, 2.5f, 0.4f);
        }

        // Embers bob gently; the mimic bobs a little too fast, and sometimes twitches.
        protected override Vector3 BodyOffset => Disguised
            ? new Vector3(0, Mathf.Sin(Time.time * 4.1f + GetInstanceID()) * 0.12f + (twitch > 0f ? 0.06f : 0f), 0)
            : Vector3.zero;

        protected override void Think(float dt)
        {
            twitch -= dt;
            if (Disguised)
            {
                desiredVelocity = Vector2.zero;
                if (Random.value < dt * 0.25f) twitch = 0.12f;
                if (Player == null || !Player.IsAlive || Player.IsInvisible) return;
                float d = Vector2.Distance(Player.transform.position, transform.position);
                if (d < Tuning.mimicRevealDistance)
                {
                    SetDisguised(false);
                    revealedFor = 0f;
                    if (d < 1.1f) Player.TakeHit(Tuning.mimicDamage, Kind, transform.position); // the ambush bite
                }
                return;
            }

            revealedFor += dt;
            if (Player != null && Player.IsAlive && revealedFor < ChaseTime)
                MoveTo(Player.transform.position, Tuning.mimicChaseSpeed, 0.3f);
            else if (Player == null || Vector2.Distance(Player.transform.position, transform.position) > 6f)
            {
                desiredVelocity = Vector2.zero;
                SetDisguised(true); // lost interest: back to pretending
            }
            else desiredVelocity = Vector2.zero;
        }

        protected override void UpdateBurn(float dt)
        {
            // Bright light exposes the fake ember before it can bite.
            if (Disguised && LightField.PlayerBurns(transform.position)) { SetDisguised(false); revealedFor = 0f; }
            base.UpdateBurn(dt);
        }

        protected override float BurnTime => 1.2f;

        protected override void OnTouchPlayer(PlayerFlame player)
        {
            if (!Disguised) player.TakeHit(Tuning.mimicDamage, Kind, transform.position);
        }
    }
}
