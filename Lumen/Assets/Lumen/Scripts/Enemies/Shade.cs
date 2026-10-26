using System.Collections.Generic;
using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.Enemies
{
    /// <summary>
    /// Base class for every creature of the dark. Handles physics movement, path following,
    /// burning in light, flare reactions and contact damage. Subclasses implement Think().
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Shade : MonoBehaviour
    {
        public static readonly List<Shade> All = new List<Shade>();

        public abstract EnemyKind Kind { get; }
        public bool IsDead { get; private set; }
        public bool IsStunned => stunned > 0f;

        protected static GameTuning Tuning => GameTuning.I;
        protected Rigidbody2D body;
        protected SpriteRenderer bodySprite;
        protected SpriteRenderer eyeSprite;
        protected SpriteRenderer shadowSprite;
        protected DungeonMap map;
        protected GridPathfinder pathfinder;
        protected PlayerFlame Player => LightField.Player;

        protected Vector2 desiredVelocity;
        protected float speedMultiplier = 1f;
        protected float stunned;
        protected float burn; // accumulated seconds in burning light
        /// <summary>True while light is burning this shade (updated each frame by UpdateBurn).</summary>
        protected bool Burning { get; private set; }

        readonly List<Vector2Int> path = new List<Vector2Int>();
        int pathIndex;
        Vector2Int pathGoal;
        float repathTimer;

        protected static T Build<T>(string name, Vector2 pos, Transform parent, Sprite[] frames, Sprite eyes, float radius, float fps,
                                    Color haloColor, float haloHeight)
            where T : Shade
        {
            var go = new GameObject(name);
            go.layer = Layers.Enemies;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius;

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(go.transform, false);
            shadow.sprite = SpriteBank.BlobShadow;
            shadow.sharedMaterial = LumenAssets.I.Lit;
            shadow.sortingOrder = -2;

            var sr = new GameObject("Body").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(go.transform, false);
            sr.sprite = frames[0];
            sr.sharedMaterial = LumenAssets.I.Lit; // only visible where light falls
            var anim = sr.gameObject.AddComponent<SpriteAnimator>();
            anim.frames = frames;
            anim.fps = fps;

            SpriteRenderer eyeSr = null;
            if (eyes != null)
            {
                eyeSr = new GameObject("Eyes").AddComponent<SpriteRenderer>();
                eyeSr.transform.SetParent(sr.transform, false);
                eyeSr.sprite = eyes;
                eyeSr.sharedMaterial = LumenAssets.I.Unlit; // glowing eyes in the dark
                eyeSr.sortingOrder = 1;

                // Soft unlit halo so the eyes survive the vignette at the screen edges.
                var halo = new GameObject("EyeHalo").AddComponent<SpriteRenderer>();
                halo.transform.SetParent(eyeSr.transform, false);
                halo.transform.localPosition = new Vector3(0, haloHeight, 0);
                halo.transform.localScale = Vector3.one * 0.55f;
                halo.sprite = SpriteBank.SoftCircle;
                halo.sharedMaterial = LumenAssets.I.Unlit;
                halo.color = haloColor;
                halo.sortingOrder = 0;
            }

            go.AddComponent<YSort>();
            // Factory pattern used by every actor: build the hierarchy, add the component,
            // then hand it its references through Setup (Awake only caches its own components).
            var shade = go.AddComponent<T>();
            shade.Setup(sr, eyeSr, shadow);
            return shade;
        }

        void Setup(SpriteRenderer body, SpriteRenderer eyes, SpriteRenderer shadow)
        {
            bodySprite = body;
            eyeSprite = eyes;
            shadowSprite = shadow;
        }

        public void Init(DungeonMap map, GridPathfinder pathfinder, float speedMultiplier = 1f)
        {
            this.map = map;
            this.pathfinder = pathfinder;
            this.speedMultiplier = speedMultiplier;
        }

        /// <summary>Makes the shade aware of a position (e.g. born from the darkness hunting you).</summary>
        public virtual void Alert(Vector2 position) { }

        /// <summary>Where the body sprite sits relative to the root (fliers hover above their shadow).</summary>
        protected virtual Vector3 BodyOffset => Vector3.zero;

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            All.Add(this);
            GameEvents.Flare += OnFlare;
        }

        protected virtual void OnDestroy()
        {
            All.Remove(this);
            GameEvents.Flare -= OnFlare;
        }

        protected abstract void Think(float dt);

        protected virtual void Update()
        {
            if (IsDead) return;
            float dt = Time.deltaTime;
            stunned -= dt;

            if (stunned > 0f) desiredVelocity = Vector2.zero;
            else Think(dt);

            UpdateBurn(dt);

            // Face movement direction; jitter when stunned.
            if (Mathf.Abs(body.linearVelocity.x) > 0.1f) bodySprite.flipX = body.linearVelocity.x < 0;
            Vector3 jitter = stunned > 0 ? (Vector3)(Random.insideUnitCircle * 0.04f) : Vector3.zero;
            bodySprite.transform.localPosition = BodyOffset + jitter;
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead) return;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, desiredVelocity, 30f * Time.fixedDeltaTime);
        }

        /// <summary>Default light behaviour: brazier zones repel, the player's bright core burns.</summary>
        protected virtual void UpdateBurn(float dt)
        {
            bool byPlayer = LightField.PlayerBurns(transform.position);
            bool burning = byPlayer || LightField.InBrazierSafeZone(transform.position, out _);
            Burning = burning;
            burn = burning ? burn + dt : Mathf.Max(0f, burn - dt * 0.5f);
            float t = Mathf.Clamp01(burn / Tuning.stalkerBurnTime);
            bodySprite.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), t);
            if (burning && Random.value < dt * 25f)
                PixelParticles.Emit((Vector2)transform.position + Random.insideUnitCircle * 0.3f + Vector2.up * 0.4f,
                    Vector2.up, Palette.ShadeEdge, 0.6f, false, 1f);
            if (burn >= Tuning.stalkerBurnTime) Kill(byPlayer ? KillCause.PlayerLight : KillCause.Brazier);
        }

        /// <summary>Flares kill up close and only stun further out, so they reward commitment.</summary>
        protected virtual void OnFlare(Vector2 origin, float radius)
        {
            if (IsDead) return;
            Vector2 p = transform.position;
            float d2 = (p - origin).sqrMagnitude;
            if (d2 > radius * radius || !LightField.LineOfSight(origin, p)) return;
            float kill = Tuning.flareKillRadius;
            if (d2 <= kill * kill) Kill(KillCause.Flare);
            else Stun(Tuning.flareStunTime);
        }

        public void Stun(float seconds)
        {
            stunned = Mathf.Max(stunned, seconds);
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.5f, 6, Palette.FireWhite, 1.5f, 0.35f);
        }

        public virtual void Kill(KillCause cause)
        {
            if (IsDead) return;
            IsDead = true;
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.4f, 28, new Color(0.15f, 0.1f, 0.2f), 3f, 0.9f, false, 0.6f, 1f, 0.3f);
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.4f, 8, Palette.ShadeEye, 2f, 0.5f);
            GameEvents.RaiseEnemyKilled(transform.position, Kind, cause);
            Destroy(gameObject);
        }

        protected virtual void OnCollisionStay2D(Collision2D c)
        {
            if (IsDead || stunned > 0f || c.collider.gameObject.layer != Layers.Player) return;
            if (c.collider.TryGetComponent(out PlayerFlame player)) OnTouchPlayer(player);
        }

        protected abstract void OnTouchPlayer(PlayerFlame player);

        // ------------------------------------------------------------------ navigation helpers
        protected Vector2Int Cell => DungeonMap.WorldToCell(transform.position);

        /// <summary>Steers along an A* path to a goal cell. Returns true once arrived.</summary>
        protected bool MoveTo(Vector2 goal, float speed, float repathInterval = 0.5f)
        {
            Vector2 pos = transform.position;
            if ((goal - pos).sqrMagnitude < 0.04f) { desiredVelocity = Vector2.zero; return true; }
            speed *= speedMultiplier;

            // Straight line if nothing is in the way: cheaper and smoother than a path.
            if (LightField.LineOfSight(pos, goal) && !Physics2D.CircleCast(pos, 0.25f, goal - pos, Vector2.Distance(pos, goal), Layers.WallMask))
            {
                desiredVelocity = (goal - pos).normalized * speed;
                path.Clear();
                return false;
            }

            var goalCell = DungeonMap.WorldToCell(goal);
            repathTimer -= Time.deltaTime;
            if (path.Count == 0 || goalCell != pathGoal || repathTimer <= 0f)
            {
                repathTimer = repathInterval + Random.value * 0.2f;
                pathGoal = goalCell;
                pathfinder.FindPath(Cell, goalCell, path, 1500);
                pathIndex = 0;
            }
            if (pathIndex >= path.Count) { desiredVelocity = Vector2.zero; return path.Count == 0; }

            Vector2 wp = DungeonMap.CellToWorld(path[pathIndex]);
            if ((wp - pos).sqrMagnitude < 0.09f) pathIndex++;
            desiredVelocity = (wp - pos).normalized * speed;
            return false;
        }

        protected Vector2 RandomNearbyFloor(int radius)
        {
            var c = Cell;
            for (int i = 0; i < 12; i++)
            {
                var t = c + new Vector2Int(Random.Range(-radius, radius + 1), Random.Range(-radius, radius + 1));
                if (map.IsFloor(t)) return DungeonMap.CellToWorld(t);
            }
            return transform.position;
        }
    }
}
