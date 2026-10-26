using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>Objective + checkpoint: lit braziers open the exit, refuel the player and repel shades.</summary>
    public class Brazier : MonoBehaviour
    {
        public bool IsLit { get; private set; }
        /// <summary>0..1 progress of a Devourer snuffing this brazier out.</summary>
        public float DrainProgress { get; private set; }

        SpriteRenderer fire;
        Light2D light2D;
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

            var solid = go.AddComponent<CircleCollider2D>();
            solid.radius = 0.35f;
            solid.offset = new Vector2(0, 0.1f);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            trigger.offset = solid.offset; // AddComponent auto-fits colliders to the sprite; override it

            go.AddComponent<YSort>();
            // Awake runs inside AddComponent, so the fire child must already exist.
            return go.AddComponent<Brazier>();
        }

        void Awake()
        {
            fire = transform.Find("Fire").GetComponent<SpriteRenderer>();
            light2D = LightFactory.Point(transform, Palette.BrazierLight, GameTuning.I.brazierLightRadius, 1.1f, true);
            light2D.transform.localPosition = new Vector3(0, 0.6f, 0);
            flicker = light2D.gameObject.AddComponent<Flicker>();
            flicker.baseRadius = GameTuning.I.brazierLightRadius;
            flicker.baseIntensity = 1.1f;
            SetLit(false, silent: true);
            LightField.Register(this);
        }

        void OnDestroy() => LightField.Unregister(this);

        void OnTriggerEnter2D(Collider2D other) => TryIgnite(other);
        void OnTriggerStay2D(Collider2D other) => TryIgnite(other);

        void TryIgnite(Collider2D other)
        {
            if (IsLit) return;
            var player = other.GetComponent<PlayerFlame>();
            if (player == null || !player.IsAlive) return;
            SetLit(true);
            GameManager.AddScore(GameTuning.I.scoreBrazier, transform.position);
        }

        public void SetLit(bool lit, bool silent = false)
        {
            IsLit = lit;
            DrainProgress = 0f;
            fire.enabled = lit;
            light2D.enabled = lit;
            flicker.enabled = lit;
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
            if (!IsLit) return;
            drainedThisFrame -= Time.deltaTime;
            if (drainedThisFrame <= 0f) DrainProgress = Mathf.Max(0f, DrainProgress - Time.deltaTime * 0.3f);
            // While being drained the fire gutters visibly — a telegraph for the player.
            flicker.intensityAmount = 0.12f + DrainProgress * 0.8f;
            flicker.baseRadius = GameTuning.I.brazierLightRadius * (1f - DrainProgress * 0.6f);
        }
    }
}
