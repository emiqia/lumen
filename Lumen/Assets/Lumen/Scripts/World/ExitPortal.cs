using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.World
{
    /// <summary>Sealed until every brazier on the floor is lit. Reaching it raises PlayerReachedExit.</summary>
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
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.PortalClosed;
            sr.sharedMaterial = LumenAssets.I.Lit;
            sr.sortingOrder = -50; // flat on the floor
            var anim = go.AddComponent<SpriteAnimator>();
            anim.frames = SpriteBank.PortalOpen;
            anim.fps = 8f;
            anim.enabled = false;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            col.offset = Vector2.zero; // AddComponent auto-fits colliders to the sprite; override it
            var light = LightFactory.Point(go.transform, Palette.PortalLight, 3.5f, 1.2f, true);
            light.enabled = false;
            var f = light.gameObject.AddComponent<Flicker>();
            f.baseRadius = 3.5f; f.baseIntensity = 1.2f; f.speed = 2f;

            var portal = go.AddComponent<ExitPortal>();
            portal.Init(sr, anim, light);
            return portal;
        }

        void Init(SpriteRenderer renderer, SpriteAnimator animator, Light2D light)
        {
            sr = renderer;
            anim = animator;
            light2D = light;
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
            if (!IsOpen || other.gameObject.layer != Layers.Player) return;
            if (other.TryGetComponent(out PlayerFlame player) && player.IsAlive) GameEvents.RaisePlayerReachedExit();
        }
    }
}
