using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// An eye set in the wall. It never moves or bites, but if it watches your light for a
    /// moment it raises the alarm and every stalker nearby comes for you. Slip past dimmed,
    /// or burst it with a flare.
    /// </summary>
    public class Watcher : Shade
    {
        const float Cooldown = 8f;

        public override EnemyKind Kind => EnemyKind.Watcher;

        float charge, cooldown;

        public static Watcher Create(Vector2 pos, Transform parent)
        {
            var w = Build<Watcher>("Watcher", pos, parent, SpriteBank.Watcher, SpriteBank.WatcherEyes, 0.35f, 1.5f, 0.3f, 0.12f);
            w.body.bodyType = RigidbodyType2D.Kinematic; // bolted to the wall
            return w;
        }

        protected override void Think(float dt)
        {
            desiredVelocity = Vector2.zero;
            cooldown -= dt;
            bool sees = Player != null && !Player.IsDimmed && LightField.CanSeePlayer(transform.position, 2f, 0.9f);
            charge = sees && cooldown <= 0f ? charge + dt : Mathf.Max(0f, charge - dt * 0.5f);

            // The eye swells and brightens as it fixes on you: a readable warning.
            float k = Mathf.Clamp01(charge / Tuning.watcherAlarmTime);
            if (eyeSprite != null) eyeSprite.transform.localScale = Vector3.one * (1f + k * 0.5f);

            if (charge >= Tuning.watcherAlarmTime) RaiseAlarm();
        }

        void RaiseAlarm()
        {
            charge = 0f;
            cooldown = Cooldown;
            Vector2 target = Player.transform.position;
            float r = Tuning.watcherAlarmRadius;
            foreach (var s in All)
                if (s is Stalker && !s.IsDead && ((Vector2)s.transform.position - (Vector2)transform.position).sqrMagnitude < r * r)
                    s.Alert(target);
            PixelParticles.Ring(transform.position, 40, Palette.EyeColor(Kind), 7f, 0.6f);
            CameraRig.Shake(0.15f);
            GameEvents.RaiseWatcherAlarm(transform.position);
        }

        protected override float BurnTime => 2.2f;
        protected override void FixedUpdate() { if (!IsDead) body.linearVelocity = Vector2.zero; }
        protected override void OnTouchPlayer(PlayerFlame player) { } // harmless to touch; dangerous to be seen by
    }
}
