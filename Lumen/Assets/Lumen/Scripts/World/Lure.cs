using Lumen.Art;
using Lumen.Core;
using Lumen.Enemies;
using Lumen.FX;
using Lumen.Lighting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>
    /// A thrown light decoy. While it shines, moths prefer it over everything and stalkers
    /// nearby go to investigate it, giving the player a window to slip past.
    /// </summary>
    public class Lure : MonoBehaviour
    {
        float life;
        Light2D light2D;

        public static Lure Throw(Vector2 from, Vector2 direction, Transform parent)
        {
            var t = GameTuning.I;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;
            direction.Normalize();
            // Land short of walls.
            var hit = Physics2D.CircleCast(from, 0.2f, direction, t.lureThrowDistance, Layers.WallMask);
            Vector2 at = hit.collider != null ? hit.centroid - direction * 0.1f : from + direction * t.lureThrowDistance;

            var go = new GameObject("Lure");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Item(Level.ItemKind.Lure);
            sr.sharedMaterial = LumenAssets.I.Unlit;
            go.AddComponent<FX.YSort>();
            var lure = go.AddComponent<Lure>();
            lure.Init();
            PixelParticles.Burst(at, 20, Palette.FireWhite, 3f, 0.5f);
            return lure;
        }

        void Init()
        {
            life = GameTuning.I.lureDuration;
            light2D = LightFactory.Point(transform, new Color(1f, 0.95f, 0.85f), 3.2f, 1.2f, true);
            LevelHazards.Register(this);
            CallStalkers();
        }

        /// <summary>Stalkers within range go to investigate the new light.</summary>
        void CallStalkers()
        {
            float r = GameTuning.I.lureAttractRadius;
            Vector2 p = transform.position;
            foreach (var s in Shade.All)
                if (s is Stalker && !s.IsDead && ((Vector2)s.transform.position - p).sqrMagnitude < r * r)
                    s.Alert(p);
        }

        void Update()
        {
            life -= Time.deltaTime;
            // Gutters out over its last two seconds.
            float k = Mathf.Clamp01(life / 2f);
            light2D.intensity = 1.2f * k * (0.9f + 0.1f * Mathf.Sin(Time.time * 20f));
            if (life <= 0f) Destroy(gameObject);
        }

        void OnDestroy() => LevelHazards.Unregister(this);
    }
}
