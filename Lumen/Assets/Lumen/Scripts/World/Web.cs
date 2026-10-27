using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>
    /// A web spun across a corridor. Crossing it slows you down and smothers your flame a little.
    /// Flares and light bombs burn webs away.
    /// </summary>
    public class Web : MonoBehaviour
    {
        Rect area;

        public bool Contains(Vector2 p) => area.Contains(p);

        public static Web Create(Vector2Int cell, Transform parent)
        {
            var go = new GameObject("Web");
            go.transform.SetParent(parent, false);
            go.transform.position = DungeonMap.CellToWorld(cell);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Web;
            sr.sharedMaterial = LumenAssets.I.Lit;
            sr.sortingOrder = -10000;
            go.transform.localScale = Vector3.one * 1.3f;
            var web = go.AddComponent<Web>();
            web.area = new Rect(cell.x - 0.15f, cell.y - 0.15f, 1.3f, 1.3f);
            LevelHazards.Register(web);
            GameEvents.Flare += web.OnBurst;
            GameEvents.Blast += web.OnBurst;
            return web;
        }

        void OnBurst(Vector2 origin, float radius)
        {
            Vector2 p = transform.position;
            if ((p - origin).sqrMagnitude > radius * radius || !LightField.LineOfSight(origin, p)) return;
            PixelParticles.Burst(p, 16, new Color(0.9f, 0.9f, 0.95f, 0.8f), 2f, 0.6f, false, 0.5f);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            LevelHazards.Unregister(this);
            GameEvents.Flare -= OnBurst;
            GameEvents.Blast -= OnBurst;
        }
    }
}
