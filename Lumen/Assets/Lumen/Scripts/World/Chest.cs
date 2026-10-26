using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>A locked chest. Touch it while carrying this floor's key to open it.</summary>
    public class Chest : MonoBehaviour
    {
        /// <summary>Set by the GameManager: tries to open with the run's key; returns true if it opened.</summary>
        public static System.Func<Vector2, bool> Opener;

        SpriteRenderer sr;
        bool open;

        public static Chest Create(Vector2Int cell, Transform parent)
        {
            var go = new GameObject("Chest");
            go.transform.SetParent(parent, false);
            go.transform.position = DungeonMap.CellToWorld(cell);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteBank.ChestClosed;
            renderer.sharedMaterial = LumenAssets.I.Lit;
            var solid = go.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(0.75f, 0.45f);
            solid.offset = new Vector2(0f, 0.05f);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            trigger.offset = Vector2.zero;
            // A faint golden glint so it can be spotted in the dark.
            LightFactory.Point(go.transform, new Color(1f, 0.8f, 0.4f), 1.2f, 0.35f, false);
            go.AddComponent<YSort>();
            var chest = go.AddComponent<Chest>();
            chest.sr = renderer;
            return chest;
        }

        void OnTriggerEnter2D(Collider2D other) => TryOpen(other);
        void OnTriggerStay2D(Collider2D other) => TryOpen(other);

        void TryOpen(Collider2D other)
        {
            if (open || other.gameObject.layer != Layers.Player) return;
            if (!other.TryGetComponent(out PlayerFlame player) || !player.IsAlive) return;
            if (Opener == null || !Opener(transform.position)) return;
            open = true;
            sr.sprite = SpriteBank.ChestOpen;
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.4f, 30, Palette.FireYellow, 3.5f, 0.7f, true, 1.5f);
            CameraRig.Shake(0.2f);
        }
    }
}
