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
    /// Boss every five floors, waiting in the exit room; the exit stays sealed until it falls.
    /// Light can't hurt it: it takes six flares, with a short immunity after each. It fights in
    /// three phases (every two wounds), each adding an attack, all telegraphed:
    ///  1. Charge: a red line shows where it will dash.
    ///  2. Shadow wave: its eyes flare, then a ring expands; it only hurts you if you are NOT dimmed.
    ///  3. Summon: it calls two stalkers out of the dark.
    /// </summary>
    public class Guardian : Shade
    {
        enum Attack { None, ChargeWindup, Charging, WaveWindup, Recover }

        const float WoundGap = 2.5f, WindupCharge = 0.9f, ChargeTime = 0.55f, ChargeSpeed = 8.5f;
        const float WindupWave = 0.8f, WaveSpeed = 6f, WaveMaxRadius = 9f;

        public override EnemyKind Kind => EnemyKind.Guardian;
        public int Wounds => wounds.Wounds;
        public int MaxWounds => Tuning.guardianWounds;
        public bool Vulnerable => wounds.CanWound(Time.time);
        public int Phase => 1 + Mathf.Min(2, Wounds / 2);
        public bool IsAwake { get; private set; }

        WoundGate wounds;
        Attack attack;
        float attackTimer, nextAttack = 2.5f;
        Vector2 chargeDir;
        SpriteRenderer telegraph;
        float waveRadius = -1f;
        Vector2 waveOrigin;
        bool waveHit;
        int attackCycle;

        public static Guardian Create(Vector2 pos, Transform parent)
        {
            var g = Build<Guardian>("Guardian", pos, parent, SpriteBank.Guardian, SpriteBank.GuardianEyes, 0.8f, 3f, 0.3f, 1.3f);
            g.body.mass = 60f;
            g.shadowSprite.transform.localScale = new Vector3(3f, 2f, 1f);
            // The charge telegraph: a thin red bar that points where it will dash.
            var line = new GameObject("Telegraph").AddComponent<SpriteRenderer>();
            line.transform.SetParent(g.transform, false);
            line.sprite = SpriteBank.Pixel;
            line.sharedMaterial = LumenAssets.I.Unlit;
            line.sortingOrder = 31000;
            line.enabled = false;
            g.telegraph = line;
            return g;
        }

        protected override void Awake()
        {
            base.Awake();
            wounds = new WoundGate(WoundGap);
        }

        protected override void Think(float dt)
        {
            Vector2 pos = transform.position;
            if (Player == null || !Player.IsAlive) { desiredVelocity = Vector2.zero; return; }
            Vector2 pp = Player.transform.position;

            // It wakes when it first sees you, and announces itself.
            if (!IsAwake)
            {
                desiredVelocity = Vector2.zero;
                if (LightField.CanSeePlayer(pos, 6f, 0.5f)) { IsAwake = true; CameraRig.Shake(0.4f); GameEvents.RaiseGuardianAwoke(pos); }
                return;
            }

            UpdateWave(dt, pp);
            attackTimer -= dt;
            switch (attack)
            {
                case Attack.None:
                    MoveTo(pp, Tuning.devourerSpeed * (0.9f + Phase * 0.15f), 0.4f);
                    nextAttack -= dt;
                    if (nextAttack <= 0f) StartAttack(pos, pp);
                    break;

                case Attack.ChargeWindup:
                    desiredVelocity = Vector2.zero;
                    ShowTelegraph(pos, Mathf.Repeat(attackTimer * 8f, 1f) > 0.5f);
                    if (attackTimer <= 0f) { attack = Attack.Charging; attackTimer = ChargeTime; telegraph.enabled = false; }
                    break;

                case Attack.Charging:
                    desiredVelocity = chargeDir * ChargeSpeed;
                    body.linearVelocity = desiredVelocity;
                    if (attackTimer <= 0f) { attack = Attack.Recover; attackTimer = 0.8f; }
                    break;

                case Attack.WaveWindup:
                    desiredVelocity = Vector2.zero;
                    if (eyeSprite != null) eyeSprite.transform.localScale = Vector3.one * (1.2f + Mathf.Sin(attackTimer * 30f) * 0.2f);
                    if (attackTimer <= 0f)
                    {
                        if (eyeSprite != null) eyeSprite.transform.localScale = Vector3.one;
                        waveOrigin = pos; waveRadius = 0.5f; waveHit = false;
                        attack = Attack.Recover; attackTimer = 0.6f;
                    }
                    break;

                case Attack.Recover:
                    desiredVelocity = Vector2.Lerp(desiredVelocity, Vector2.zero, dt * 4f);
                    if (attackTimer <= 0f) { attack = Attack.None; nextAttack = Mathf.Max(1.6f, 3.6f - Phase * 0.5f); }
                    break;
            }
        }

        void StartAttack(Vector2 pos, Vector2 pp)
        {
            attackCycle++;
            // Phase 1: charges. Phase 2: alternates charge / wave. Phase 3: adds summoning.
            if (Phase >= 3 && attackCycle % 3 == 0) { Summon(pos); nextAttack = 2.5f; return; }
            if (Phase >= 2 && attackCycle % 2 == 0) { attack = Attack.WaveWindup; attackTimer = WindupWave; return; }
            chargeDir = (pp - pos).normalized;
            attack = Attack.ChargeWindup;
            attackTimer = WindupCharge;
        }

        void ShowTelegraph(Vector2 pos, bool bright)
        {
            const float length = ChargeSpeed * ChargeTime;
            telegraph.enabled = true;
            telegraph.transform.position = pos + chargeDir * length * 0.5f + Vector2.up * 0.3f;
            telegraph.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(chargeDir.y, chargeDir.x) * Mathf.Rad2Deg);
            telegraph.transform.localScale = new Vector3(length * 16f, 2f, 1f); // pixel sprite: 16 px per unit
            telegraph.color = new Color(1f, 0.2f, 0.25f, bright ? 0.9f : 0.45f);
        }

        /// <summary>Expanding shadow ring. Dimming your flame lets it pass through you harmlessly.</summary>
        void UpdateWave(float dt, Vector2 pp)
        {
            if (waveRadius < 0f) return;
            float before = waveRadius;
            waveRadius += WaveSpeed * dt;
            if (Random.value < 0.9f)
                PixelParticles.RingToRadius(waveOrigin, 6, new Color(0.45f, 0.25f, 0.7f), 0.01f, 0.05f);
            float d = Vector2.Distance(waveOrigin, pp);
            if (!waveHit && d >= before && d <= waveRadius + 0.3f)
            {
                waveHit = true;
                if (!Player.IsDimmed && !Player.IsInvisible) Player.TakeHit(Tuning.guardianWaveDamage, Kind, waveOrigin);
            }
            // Visual: a ring of particles sweeping outward.
            int points = 28;
            for (int i = 0; i < points; i++)
            {
                if (Random.value > 0.35f) continue;
                float a = i / (float)points * Mathf.PI * 2f;
                PixelParticles.Emit(waveOrigin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * waveRadius, Vector2.zero,
                    new Color(0.55f, 0.3f, 0.85f), 0.25f, true);
            }
            if (waveRadius > WaveMaxRadius) waveRadius = -1f;
        }

        void Summon(Vector2 pos)
        {
            var level = GameManager.Instance != null ? GameManager.Instance.Level : null;
            if (level == null) return;
            for (int i = 0; i < 2; i++)
            {
                var spot = RandomNearbyFloor(4);
                var s = LevelBuilder.SpawnEnemy(level, EnemyKind.Stalker, spot);
                if (Player != null) s.Alert(Player.transform.position);
                PixelParticles.Burst(spot, 20, new Color(0.15f, 0.1f, 0.2f), 2.5f, 0.6f, false);
            }
            CameraRig.Shake(0.3f);
        }

        protected override void UpdateBurn(float dt) { } // light can't hurt it

        protected override void OnFlare(Vector2 origin, float radius)
        {
            if (IsDead) return;
            Vector2 p = transform.position;
            if ((p - origin).sqrMagnitude > radius * radius || !LightField.LineOfSight(origin, p)) return;
            if (!wounds.TryWound(Time.time))
            {
                PixelParticles.Burst(p + Vector2.up, 8, Palette.DevourerEdge, 1.5f, 0.4f, false);
                return;
            }
            IsAwake = true;
            stunned = 1.2f;
            attack = Attack.Recover; attackTimer = 1.2f; telegraph.enabled = false;
            bodySprite.color = new Color(1f, 0.55f, 0.6f);
            PixelParticles.Burst(p + Vector2.up, 30, Palette.EyeColor(Kind), 4f, 0.7f);
            CameraRig.Shake(0.45f);
            if (wounds.Wounds >= MaxWounds) Kill(KillCause.Flare);
        }

        protected override void OnBlast(Vector2 origin, float radius) => OnFlare(origin, radius);

        protected override void Update()
        {
            base.Update();
            if (stunned <= 0f) bodySprite.color = Color.Lerp(bodySprite.color, Vulnerable ? Color.white : new Color(0.7f, 0.65f, 0.8f), Time.deltaTime * 3f);
        }

        protected override void OnTouchPlayer(PlayerFlame player) => player.TakeHit(Tuning.guardianDamage, Kind, transform.position);
    }
}
