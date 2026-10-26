using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using UnityEngine;

namespace Lumen.FX
{
    /// <summary>
    /// Pooled, pixel-snapped particle system. Kept deliberately tiny: each particle is
    /// one SpriteRenderer drawing a 1px sprite, so sparks match the pixel-art grid.
    /// </summary>
    public class PixelParticles : MonoBehaviour
    {
        public static PixelParticles I { get; private set; }

        class P
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float life, maxLife, drag, rise, size;
            public Color color;
        }

        readonly List<P> active = new List<P>(512);
        readonly Stack<P> pool = new Stack<P>(512);

        void Awake() => I = this;

        public static void Burst(Vector2 pos, int count, Color color, float speed, float life,
                                 bool glow = true, float rise = 0f, float size = 1f, float spread = 0.1f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                I.Spawn(pos + Random.insideUnitCircle * spread, dir * speed * Random.Range(0.4f, 1f),
                        color, life * Random.Range(0.6f, 1f), glow, rise, size);
            }
        }

        public static void Ring(Vector2 pos, int count, Color color, float speed, float life)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                I.Spawn(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, color, life, true, 0f, 1f);
            }
        }

        public static void Emit(Vector2 pos, Vector2 vel, Color color, float life, bool glow = true, float rise = 0f, float size = 1f)
        {
            if (I != null) I.Spawn(pos, vel, color, life, glow, rise, size);
        }

        void Spawn(Vector2 pos, Vector2 vel, Color color, float life, bool glow, float rise, float size)
        {
            if (active.Count >= 600) return;
            var p = pool.Count > 0 ? pool.Pop() : Create();
            p.pos = pos; p.vel = vel; p.life = p.maxLife = life;
            p.drag = 3f; p.rise = rise; p.color = color; p.size = size;
            p.sr.sharedMaterial = glow ? LumenAssets.I.Unlit : LumenAssets.I.Lit;
            p.sr.color = color;
            p.sr.transform.localScale = Vector3.one * size;
            p.sr.gameObject.SetActive(true);
            active.Add(p);
        }

        P Create()
        {
            var go = new GameObject("p");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Pixel;
            sr.sortingOrder = 32000;
            return new P { sr = sr };
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.sr.gameObject.SetActive(false);
                    pool.Push(p);
                    active.RemoveAt(i);
                    continue;
                }
                p.vel *= 1f / (1f + p.drag * dt);
                p.vel.y += p.rise * dt;
                p.pos += p.vel * dt;
                // Snap to the 1/16 pixel grid so particles never sit between pixels.
                p.sr.transform.position = new Vector3(Mathf.Round(p.pos.x * 16f) / 16f, Mathf.Round(p.pos.y * 16f) / 16f, 0f);
                var c = p.color;
                c.a *= Mathf.Clamp01(p.life / p.maxLife * 1.5f);
                p.sr.color = c;
            }
        }

        public void ClearAll()
        {
            foreach (var p in active) { p.sr.gameObject.SetActive(false); pool.Push(p); }
            active.Clear();
        }
    }
}
