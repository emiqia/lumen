using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>Sealed until every brazier on the floor is lit.</summary>
    public class ExitPortal : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        SpriteRenderer sr;
        SpriteAnimator anim;
        Light2D light2D;

        public static ExitPortal Create(Vector2 position, Transform parent)
        {
            var go = new GameObject("Exit");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var p = go.AddComponent<ExitPortal>();
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sprite = SpriteBank.PortalClosed;
            p.sr.sharedMaterial = LumenAssets.I.Lit;
            p.sr.sortingOrder = -50; // flat on the floor
            p.anim = go.AddComponent<SpriteAnimator>();
            p.anim.frames = SpriteBank.PortalOpen;
            p.anim.fps = 8f;
            p.anim.enabled = false;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            col.offset = Vector2.zero; // AddComponent auto-fits colliders to the sprite; override it
            p.light2D = LightFactory.Point(go.transform, Palette.PortalLight, 3.5f, 1.2f, true);
            p.light2D.enabled = false;
            var f = p.light2D.gameObject.AddComponent<Flicker>();
            f.baseRadius = 3.5f; f.baseIntensity = 1.2f; f.speed = 2f;
            return p;
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen) return;
            IsOpen = open;
            anim.enabled = open;
            sr.sprite = open ? SpriteBank.PortalOpen[0] : SpriteBank.PortalClosed;
            sr.sharedMaterial = open ? LumenAssets.I.Unlit : LumenAssets.I.Lit;
            light2D.enabled = open;
            if (open)
            {
                PixelParticles.Ring(transform.position, 30, Palette.Portal, 5f, 0.7f);
                GameEvents.RaiseExitOpened();
            }
        }

        void Update()
        {
            if (IsOpen && Random.value < Time.deltaTime * 20f)
                PixelParticles.Emit((Vector2)transform.position + Random.insideUnitCircle * 0.4f, Vector2.up * 0.6f, Palette.Portal, 0.8f, true, 0.5f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!IsOpen) return;
            var player = other.GetComponent<PlayerFlame>();
            if (player != null && player.IsAlive) GameManager.Instance.CompleteFloor();
        }
    }
}
