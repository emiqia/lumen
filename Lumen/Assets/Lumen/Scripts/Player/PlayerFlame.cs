using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Player
{
    /// <summary>
    /// The player: a living flame. Fuel is health, light radius and timer all at once —
    /// the core tension is that seeing more makes you easier to find.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerFlame : MonoBehaviour
    {
        public IPlayerInput Controls { get; set; } = new DeviceInput();

        public float Fuel => fuel.Value;
        public float FuelNormalized => fuel.Normalized;
        public float LightRadius { get; private set; }
        public bool IsDimmed { get; private set; }
        public bool IsAlive { get; private set; } = true;
        public bool IsRefueling { get; private set; }
        public bool IsSprinting { get; private set; }
        /// <summary>Shadow dust: unseen by everything for a few seconds.</summary>
        public bool IsInvisible => invisibleFor > 0f;
        public bool InGust { get; private set; }
        /// <summary>The run this flame belongs to (inventory, run tuning). Null in the attract-mode demo.</summary>
        public RunState Run
        {
            get => run;
            set
            {
                run = value;
                // Each flame casts its own color of light.
                if (run != null && mainLight != null) mainLight.color = run.Character.Light;
            }
        }
        RunState run;
        public float FlareCooldownLeft { get; private set; }
        public bool CanAffordFlare => fuel.CanAfford(Tuning.flareCost);
        public bool Controllable { get; set; } = true;

        // Telemetry for the playtest bot / stats screen.
        public int FlaresUsed { get; private set; }
        public float TimeDimmed { get; private set; }
        public float TimeSprinting { get; private set; }

        static GameTuning Tuning => GameTuning.I;

        Rigidbody2D body;
        FuelModel fuel;
        SpriteRenderer sprite;
        Light2D mainLight;
        Light2D flareLight;
        Flicker flicker;
        float invulnerable;
        float flareGlow;
        float lastHitTime = -10f;
        EnemyKind lastHitSource;
        float sparkTimer;
        float hitFlash;
        float splashTimer;
        float invisibleFor;
        Vector2 gustPush;
        Vector2 lastMoveDir = Vector2.down;

        public static PlayerFlame Create(Vector2 position, Character character = null)
        {
            var go = new GameObject("Player");
            go.layer = Layers.Player;
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;
            col.offset = new Vector2(0f, 0.05f);

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector3(0, -0.05f);
            shadow.sprite = SpriteBank.BlobShadow;
            shadow.sharedMaterial = LumenAssets.I.Lit;
            shadow.sortingOrder = -2;

            var sr = new GameObject("Body").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            var frames = SpriteBank.PlayerFor(character);
            sr.sprite = frames[0];
            sr.sharedMaterial = LumenAssets.I.Unlit; // the flame is its own light source
            var anim = sr.gameObject.AddComponent<SpriteAnimator>();
            anim.frames = frames;
            anim.fps = 10f;

            go.AddComponent<YSort>();
            var flame = go.AddComponent<PlayerFlame>();
            flame.sprite = sr;
            return flame;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            fuel = new FuelModel(Tuning.maxFuel, Tuning.drainBright, Tuning.drainDim);
            LightRadius = Tuning.maxLightRadius;

            mainLight = LightFactory.Point(transform, Palette.PlayerLight, LightRadius, 1.15f, true);
            mainLight.transform.localPosition = new Vector3(0, 0.3f, 0);
            flicker = mainLight.gameObject.AddComponent<Flicker>();
            flicker.baseIntensity = 1.15f;

            flareLight = LightFactory.Point(transform, new Color(1f, 0.9f, 0.7f), Tuning.flareRadius, 0f, true, 0.3f);
            flareLight.transform.localPosition = new Vector3(0, 0.3f, 0);
            flareLight.enabled = false;

            LightField.Player = this;
        }

        public void SetFuel(float value) => fuel.Set(value);
        public void AddFuel(float amount) => fuel.Add(amount);

        void Update()
        {
            if (!IsAlive) return;
            float dt = Time.deltaTime;

            if (Controllable) Controls.Tick();
            Vector2 pos = transform.position;
            bool moving = Controllable && Controls.Move.sqrMagnitude > 0.01f;
            if (moving) lastMoveDir = Controls.Move.normalized;
            IsDimmed = Controllable && Controls.Dim;
            // Running trades light for speed: a speedrunner's tool, and a beacon for everything in the dark.
            IsSprinting = Controllable && Controls.Sprint && !IsDimmed && moving && fuel.Value > 1f;
            if (IsDimmed) TimeDimmed += dt;
            if (IsSprinting) TimeSprinting += dt;

            // Fuel economy: lit braziers refuel from their finite reserve; otherwise the flame burns.
            float gained = 0f;
            if (LightField.InBrazierSafeZone(pos, out var brazier) && !brazier.Depleted)
                gained = brazier.Withdraw(fuel.RefuelFrom(brazier.Reserve, Tuning.brazierRefuelPerSecond, dt));
            IsRefueling = gained > 0f;
            if (!IsRefueling) fuel.Burn(dt * (IsSprinting ? Tuning.sprintDrainMultiplier : 1f), IsDimmed);

            UpdateHazards(pos, dt);

            if (Controllable && Controls.CycleItemPressed && run != null) { run.CycleActive(); Sfx.I?.Blip(); }
            if (Controllable && Controls.UseItemPressed) TryUseItem();
            invisibleFor -= dt;

            // Light radius follows fuel; dimming clamps it down, sprinting flares it up, gusts beat it down.
            float targetRadius = IsDimmed ? Tuning.dimLightRadius
                : Mathf.Lerp(Tuning.minLightRadius, Tuning.maxLightRadius, FuelNormalized) * (IsSprinting ? Tuning.sprintLightBoost : 1f);
            if (InGust) targetRadius *= Tuning.windGustDim;
            if (IsInvisible) targetRadius = Mathf.Min(targetRadius, Tuning.dimLightRadius);
            LightRadius = Mathf.Lerp(LightRadius, targetRadius, dt * 6f);
            flicker.baseRadius = LightRadius;
            flicker.baseIntensity = IsDimmed ? 0.7f : 1.15f;
            flicker.intensityAmount = FuelNormalized < 0.25f ? 0.35f : 0.12f; // sputters when low

            // Flare
            FlareCooldownLeft = Mathf.Max(0f, FlareCooldownLeft - dt);
            if (Controllable && Controls.FlarePressed) TryFlare();
            UpdateFlareGlow(dt);

            // Visual feedback
            invulnerable -= dt;
            hitFlash -= Time.unscaledDeltaTime;
            float blink = invulnerable > 0 && Mathf.Repeat(Time.time * 14f, 1f) > 0.5f ? 0.25f : 1f;
            float scale = IsDimmed ? 0.8f : 1f;
            sprite.transform.localScale = Vector3.Lerp(sprite.transform.localScale, new Vector3(scale, scale, 1), dt * 12f);
            // A hit flashes the flame pale blue-white for a couple of frames: unmistakable against fire colors.
            if (IsInvisible) blink = 0.3f + 0.1f * Mathf.Sin(Time.time * 10f); // ghostly while hidden by shadow dust
            sprite.color = hitFlash > 0f ? new Color(0.75f, 0.85f, 1f, 1f) : new Color(1, 1, 1, blink);
            if (Mathf.Abs(Controls.Move.x) > 0.1f) sprite.flipX = Controls.Move.x < 0;
            EmitSparks(dt);

            if (fuel.Empty) Die();
        }

        void FixedUpdate()
        {
            if (!IsAlive) { body.linearVelocity = Vector2.zero; return; }
            Vector2 move = Controllable ? Controls.Move : Vector2.zero;
            float speed = IsDimmed ? Tuning.dimMoveSpeed : Tuning.moveSpeed * (IsSprinting ? Tuning.sprintSpeedMultiplier : 1f);
            Vector2 target = move * speed + (InGust ? gustPush : Vector2.zero);
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, target, Tuning.acceleration * Time.fixedDeltaTime);
        }

        /// <summary>Biome hazards: running through water costs light; gusts dim and push the flame.</summary>
        void UpdateHazards(Vector2 pos, float dt)
        {
            bool boots = run != null && run.AshBoots;
            bool inWater = LevelHazards.InWater(pos);
            if (inWater && IsSprinting && !boots) fuel.Add(-Tuning.waterSprintDrain * dt);
            splashTimer -= dt;
            if (inWater && body.linearVelocity.sqrMagnitude > 0.5f && splashTimer <= 0f)
            {
                splashTimer = IsSprinting ? 0.05f : 0.18f;
                PixelParticles.Emit(pos + Random.insideUnitCircle * 0.2f, Random.insideUnitCircle * (IsSprinting ? 2.5f : 1f) + Vector2.up,
                    new Color(0.55f, 0.75f, 0.9f, 0.9f), 0.35f, false, -3f);
            }
            InGust = !boots && LevelHazards.GustAt(pos, out gustPush);
        }

        void TryUseItem()
        {
            if (run == null || !run.TryConsumeActive(out var used)) return;
            Vector2 pos = transform.position;
            switch (used)
            {
                case RunState.Active.Lure:
                    Lure.Throw(pos, lastMoveDir, transform.parent);
                    GameEvents.RaiseItemUsed(pos, Level.ItemKind.Lure);
                    break;
                case RunState.Active.ShadowDust:
                    invisibleFor = Tuning.shadowDustTime;
                    PixelParticles.Burst(pos, 30, new Color(0.25f, 0.2f, 0.35f), 2.5f, 0.9f, false, 0.6f);
                    GameEvents.RaiseItemUsed(pos, Level.ItemKind.ShadowDust);
                    break;
                default:
                    // A free, wide, lethal flare.
                    GameEvents.RaiseBlast(pos, Tuning.lightBombRadius);
                    flareGlow = 1.4f;
                    PixelParticles.RingToRadius(pos, 96, Palette.FireWhite, Tuning.lightBombRadius, 0.5f);
                    PixelParticles.Burst(pos, 40, Palette.FireYellow, 6f, 0.6f);
                    CameraRig.Shake(0.6f);
                    GameEvents.RaiseItemUsed(pos, Level.ItemKind.LightBomb);
                    break;
            }
        }

        void TryFlare()
        {
            if (FlareCooldownLeft > 0f) return;
            // Oil makes a flare free; otherwise it costs light.
            bool oil = Run != null && Run.Oil > 0;
            if (oil) Run.Oil--;
            else if (!fuel.TrySpend(Tuning.flareCost))
            {
                Sfx.I?.Blip(); // not enough fuel: fizzle
                PixelParticles.Burst(transform.position, 6, Palette.FireRed, 1.5f, 0.3f);
                return;
            }
            FlareCooldownLeft = Tuning.flareCooldown;
            FlaresUsed++;
            flareGlow = 1f;
            GameEvents.RaiseFlare(transform.position, Tuning.flareRadius);
            // Two rings: the bright inner one marks the kill radius, the faint outer one the stun radius.
            PixelParticles.RingToRadius(transform.position, 72, Palette.FireYellow, Tuning.flareKillRadius, 0.45f);
            PixelParticles.RingToRadius(transform.position, 72, Palette.FireOrange, Tuning.flareRadius, 0.5f);
            PixelParticles.Burst(transform.position, 25, Palette.FireWhite, 5f, 0.5f);
            CameraRig.Shake(0.35f);
        }

        void UpdateFlareGlow(float dt)
        {
            if (flareGlow <= 0f) { flareLight.enabled = false; return; }
            flareLight.enabled = true;
            flareGlow = Mathf.Max(0f, flareGlow - dt * 2.8f);
            flareLight.intensity = flareGlow * 2.6f;
            flareLight.pointLightOuterRadius = Tuning.flareRadius * (1.25f - flareGlow * 0.25f);
        }

        void EmitSparks(float dt)
        {
            sparkTimer -= dt;
            if (sparkTimer > 0) return;
            sparkTimer = IsDimmed ? 0.25f : 0.07f;
            var c = Random.value > 0.5f ? Palette.FireOrange : Palette.FireYellow;
            PixelParticles.Emit((Vector2)transform.position + new Vector2(Random.Range(-0.2f, 0.2f), 0.6f),
                new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1f)), c, Random.Range(0.4f, 0.8f), true, 0.8f);
        }

        public bool TakeHit(float amount, EnemyKind source, Vector2 from)
        {
            if (!IsAlive || invulnerable > 0f) return false;
            invulnerable = Tuning.hitInvulnerability;
            // The lantern's glass takes the blow instead of the flame.
            if (Run != null && Run.Lantern)
            {
                Run.Lantern = false;
                PixelParticles.Burst(transform.position, 24, new Color(1f, 0.9f, 0.6f), 4f, 0.6f, true, 1f);
                GameEvents.RaiseItemUsed(transform.position, Level.ItemKind.Lantern);
                CameraRig.Shake(0.3f);
                return true;
            }
            hitFlash = 0.08f;
            lastHitTime = Time.time;
            lastHitSource = source;
            fuel.Add(-amount);

            Vector2 away = ((Vector2)transform.position - from).normalized;
            body.linearVelocity = away * 9f;

            GameEvents.RaisePlayerHit(transform.position, amount, source);
            PixelParticles.Burst(transform.position, 18, Palette.UiText, 4f, 0.5f);
            CameraRig.Shake(0.5f);
            GameManager.HitStop(0.07f);
            if (fuel.Empty) Die();
            return true;
        }

        void Die()
        {
            if (!IsAlive) return;
            // "Segunda chispa": rekindle once instead of going out.
            if (Run != null && Run.Tuning.extraLives > 0)
            {
                Run.Tuning.extraLives--;
                fuel.Set(Tuning.reviveFuel);
                invulnerable = 2f;
                PixelParticles.Ring(transform.position, 48, Palette.FireYellow, 8f, 0.6f);
                PixelParticles.Burst(transform.position, 40, Palette.FireWhite, 4f, 0.8f, true, 1.5f);
                CameraRig.Shake(0.5f);
                GameEvents.RaisePlayerRevived(transform.position);
                return;
            }
            IsAlive = false;
            IsRefueling = false;
            IsDimmed = false;
            fuel.Set(0f);
            mainLight.enabled = false;
            flareLight.enabled = false;
            sprite.enabled = false;
            PixelParticles.Burst(transform.position, 50, Palette.FireOrange, 3.5f, 1.2f, true, 1.2f);
            PixelParticles.Burst(transform.position, 30, new Color(0.3f, 0.3f, 0.35f), 1.5f, 1.8f, false, 0.8f);
            CameraRig.Shake(0.7f);
            string cause = Time.time - lastHitTime < Tuning.deathCauseWindow ? "cause." + lastHitSource : "cause.fuel";
            GameEvents.RaisePlayerDied(cause);
        }

        void OnDestroy()
        {
            if (LightField.Player == this) LightField.Player = null;
        }
    }
}
