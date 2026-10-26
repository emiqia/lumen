using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>
    /// A corridor where the wind blows in periodic gusts. Telegraphed by drifting leaves before it
    /// hits; during a gust the flame is beaten down (smaller light) and pushed along the corridor.
    /// </summary>
    public class WindGust : MonoBehaviour
    {
        const float Calm = 4.5f, Warning = 1f, Gust = 2.2f;

        Rect area;
        Vector2 direction;
        float timer;

        public bool Blowing => timer >= Calm + Warning;
        public Vector2 Push => direction * GameTuning.I.windGustPush;
        public bool Contains(Vector2 p) => area.Contains(p);

        public static WindGust Create(WindSpawn zone, Transform parent, float phase)
        {
            var go = new GameObject("Wind");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<WindGust>();
            g.Init(zone, phase);
            return g;
        }

        void Init(WindSpawn zone, float phase)
        {
            area = new Rect(zone.Cells.x, zone.Cells.y, zone.Cells.width, zone.Cells.height);
            direction = ((Vector2)zone.Direction).normalized;
            timer = phase * (Calm + Warning + Gust);
            LevelHazards.Register(this);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer > Calm + Warning + Gust) timer = 0f;

            bool warning = timer >= Calm && !Blowing;
            float rate = Blowing ? 40f : warning ? 6f : 1.2f;
            if (Random.value < Time.deltaTime * rate)
            {
                var start = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                var color = Blowing ? new Color(0.75f, 0.8f, 0.85f, 0.7f) : new Color(0.45f, 0.5f, 0.35f, 0.8f); // streaks vs leaves
                PixelParticles.Emit(start, direction * (Blowing ? 7f : 1.5f) + Random.insideUnitCircle * 0.3f, color, Blowing ? 0.35f : 1.2f, false);
            }
        }
    }
}
