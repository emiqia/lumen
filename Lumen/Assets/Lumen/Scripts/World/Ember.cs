using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>Floating fuel pickup. Drifts toward the player when close (pickup magnet).</summary>
    public class Ember : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<Ember> all = new System.Collections.Generic.List<Ember>();
        public static System.Collections.Generic.IReadOnlyList<Ember> All => all;

        Vector2 home;
        float phase;
        bool collected;

        public static Ember Create(Vector2 position, Transform parent)
        {
            var go = new GameObject("Ember");
            go.layer = Layers.Pickups;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = LumenAssets.I.Unlit;
            var anim = go.AddComponent<SpriteAnimator>();
            anim.frames = SpriteBank.Ember;
            anim.fps = 6f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;
            col.offset = Vector2.zero;
            var light = LightFactory.Point(go.transform, Palette.FireOrange, 1.4f, 0.6f, false);
            light.gameObject.AddComponent<Flicker>().baseRadius = 1.4f;
            go.AddComponent<YSort>();
            return go.AddComponent<Ember>();
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            home = transform.position;
            phase = Random.value * 10f;
        }

        void Update()
        {
            var player = LightField.Player;
            Vector2 target = home + new Vector2(0, Mathf.Sin(Time.time * 2.5f + phase) * 0.12f);
            if (player != null && player.IsAlive)
            {
                Vector2 pp = player.transform.position;
                float d = Vector2.Distance(pp, home);
                float magnet = 1.8f * GameTuning.I.emberMagnetMultiplier;
                if (d < magnet) { home = Vector2.MoveTowards(home, pp, Time.deltaTime * Mathf.Max(2f, 6f - d * 2f / GameTuning.I.emberMagnetMultiplier)); }
            }
            transform.position = target;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.gameObject.layer != Layers.Player) return;
            if (!other.TryGetComponent(out PlayerFlame player) || !player.IsAlive) return;
            collected = true;
            player.AddFuel(GameTuning.I.emberFuel);
            GameEvents.RaiseEmberCollected(transform.position);
            PixelParticles.Burst(transform.position, 12, Palette.FireYellow, 2.5f, 0.4f, true, 1f);
            Destroy(gameObject);
        }
    }
}
