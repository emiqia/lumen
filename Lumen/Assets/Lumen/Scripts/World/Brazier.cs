using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>
    /// Objective + checkpoint: lit braziers open the exit, refuel the player and repel shades.
    /// Each holds a finite fuel reserve; once drained it stays lit (it still counts for the exit)
    /// but burns low, stops refuelling and its safe zone shrinks.
    /// </summary>
    public class Brazier : MonoBehaviour
    {
        public bool IsLit { get; private set; }
        /// <summary>0..1 progress of a Devourer snuffing this brazier out.</summary>
        public float DrainProgress { get; private set; }
        public float Reserve { get; private set; }
        public bool Depleted => Reserve <= 0f;
        public float SafeRadius => Depleted ? Tuning.brazierDepletedSafeRadius : Tuning.brazierSafeRadius;

        static GameTuning Tuning => GameTuning.I;

        SpriteRenderer fire, coals;
        Light2D light2D, coalLight;
        Flicker flicker;
        float drainedThisFrame;

        public static Brazier Create(Vector2 position, Transform parent)
        {
            var go = new GameObject("Brazier");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var baseSr = go.AddComponent<SpriteRenderer>();
            baseSr.sprite = SpriteBank.Brazier;
            baseSr.sharedMaterial = LumenAssets.I.Lit;

            var fireSr = new GameObject("Fire").AddComponent<SpriteRenderer>();
            fireSr.transform.SetParent(go.transform, false);
            fireSr.sharedMaterial = LumenAssets.I.Unlit;
            fireSr.sortingOrder = 1;
            var anim = fireSr.gameObject.AddComponent<SpriteAnimator>();
            anim.frames = SpriteBank.BrazierFire;
            anim.fps = 10f;

            // Unlit embers in the bowl: an unlit brazier still glows faintly, asking to be lit.
            var coalSr = new GameObject("Coals").AddComponent<SpriteRenderer>();
            coalSr.transform.SetParent(go.transform, false);
            coalSr.sprite = SpriteBank.BrazierCoals;
            coalSr.sharedMaterial = LumenAssets.I.Unlit;
            coalSr.sortingOrder = 1;

            var solid = go.AddComponent<CircleCollider2D>();
            solid.radius = 0.35f;
            solid.offset = new Vector2(0, 0.1f);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            trigger.offset = solid.offset; // AddComponent auto-fits colliders to the sprite; override it

            go.AddComponent<YSort>();
            var brazier = go.AddComponent<Brazier>();
            brazier.Init(fireSr, coalSr);
            return brazier;
        }

        void Init(SpriteRenderer fireRenderer, SpriteRenderer coalRenderer)
        {
            fire = fireRenderer;
            coals = coalRenderer;
            Reserve = Tuning.brazierReserve;

            light2D = LightFactory.Point(transform, Palette.BrazierLight, Tuning.brazierLightRadius, 1.1f, true);
            light2D.transform.localPosition = new Vector3(0, 0.6f, 0);
            flicker = light2D.gameObject.AddComponent<Flicker>();
            flicker.baseRadius = Tuning.brazierLightRadius;
            flicker.baseIntensity = 1.1f;

            coalLight = LightFactory.Point(transform, Palette.FireRed, 1.3f, 0.45f, false);
            coalLight.transform.localPosition = new Vector3(0, 0.5f, 0);

            SetLit(false, silent: true);
            LightField.Register(this);
        }

        void OnDestroy() => LightField.Unregister(this);

        void OnTriggerEnter2D(Collider2D other) => TryIgnite(other);
        void OnTriggerStay2D(Collider2D other) => TryIgnite(other);

        void TryIgnite(Collider2D other)
        {
            if (IsLit || other.gameObject.layer != Layers.Player) return;
            if (!other.TryGetComponent(out PlayerFlame player) || !player.IsAlive) return;
            SetLit(true);
        }

        public void SetLit(bool lit, bool silent = false)
        {
            IsLit = lit;
            DrainProgress = 0f;
            fire.enabled = lit;
            light2D.enabled = lit;
            flicker.enabled = lit;
            coals.enabled = !lit;
            coalLight.enabled = !lit;
            if (silent) return;

            if (lit)
            {
                PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.6f, 35, Palette.FireYellow, 4f, 0.8f, true, 2f);
                CameraRig.Shake(0.2f);
                GameEvents.RaiseBrazierLit(transform.position);
            }
            else
            {
                PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.6f, 25, new Color(0.35f, 0.33f, 0.4f), 1.5f, 1.4f, false, 1.2f);
                GameEvents.RaiseBrazierExtinguished(transform.position);
            }
        }

        /// <summary>Takes up to <paramref name="amount"/> fuel from the reserve. Returns what was taken.</summary>
        public float Withdraw(float amount)
        {
            if (!IsLit) return 0f;
            float taken = Mathf.Clamp(amount, 0f, Reserve);
            Reserve -= taken;
            if (taken > 0f && Depleted)
                PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.7f, 14, new Color(0.4f, 0.36f, 0.42f), 1.2f, 1f, false, 0.8f);
            return taken;
        }

        /// <summary>Called every frame by a Devourer feeding on this brazier.</summary>
        public void Drain(float amount)
        {
            if (!IsLit) return;
            drainedThisFrame = 0.2f;
            DrainProgress += amount;
            if (DrainProgress >= 1f) SetLit(false);
        }

        void Update()
        {
            if (!IsLit)
            {
                // Coals breathe slowly (~1.2 Hz) so unlit braziers read as "waiting", not as props.
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 7.5f + transform.position.x);
                coals.color = new Color(1f, 1f, 1f, 0.35f + 0.55f * pulse);
                coalLight.intensity = 0.25f + 0.25f * pulse;
                return;
            }
            drainedThisFrame -= Time.deltaTime;
            if (drainedThisFrame <= 0f) DrainProgress = Mathf.Max(0f, DrainProgress - Time.deltaTime * 0.3f);

            // A dry brazier burns low; being drained by a Devourer makes it gutter (a telegraph).
            float strength = (Depleted ? 0.55f : 1f) * (1f - DrainProgress * 0.6f);
            flicker.intensityAmount = 0.12f + DrainProgress * 0.8f;
            flicker.baseRadius = Tuning.brazierLightRadius * strength;
            flicker.baseIntensity = Depleted ? 0.75f : 1.1f;
            fire.transform.localScale = Vector3.Lerp(fire.transform.localScale, Vector3.one * (Depleted ? 0.6f : 1f), Time.deltaTime * 3f);
        }
    }
}
