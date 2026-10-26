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

        protected static GameTuning Tuning => GameTuning.I;
        protected Rigidbody2D body;
        protected SpriteRenderer bodySprite;
        protected SpriteRenderer eyeSprite;
        protected DungeonMap map;
        protected GridPathfinder pathfinder;
        protected PlayerFlame Player => LightField.Player;

        protected Vector2 desiredVelocity;
        protected float stunned;
        protected float burn; // accumulated seconds in burning light

        readonly List<Vector2Int> path = new List<Vector2Int>();
        int pathIndex;
        Vector2Int pathGoal;
        float repathTimer;

        protected static T Build<T>(string name, Vector2 pos, Transform parent, Sprite[] frames, Sprite eyes, float radius, float fps)
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
            }

            go.AddComponent<YSort>();
            var shade = go.AddComponent<T>();
            shade.bodySprite = sr;
            shade.eyeSprite = eyeSr;
            return shade;
        }

        public void Init(DungeonMap map, GridPathfinder pathfinder)
        {
            this.map = map;
            this.pathfinder = pathfinder;
        }

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
            bodySprite.transform.localPosition = stunned > 0 ? (Vector3)(Random.insideUnitCircle * 0.04f) : Vector3.zero;
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead) return;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, desiredVelocity, 30f * Time.fixedDeltaTime);
        }

        /// <summary>Default light behaviour: brazier zones repel, the player's bright core burns.</summary>
        protected virtual void UpdateBurn(float dt)
        {
            bool burning = LightField.PlayerBurns(transform.position) || LightField.InBrazierSafeZone(transform.position, out _);
            burn = burning ? burn + dt : Mathf.Max(0f, burn - dt * 0.5f);
            float t = Mathf.Clamp01(burn / Tuning.stalkerBurnTime);
            bodySprite.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), t);
            if (burning && Random.value < dt * 25f)
                PixelParticles.Emit((Vector2)transform.position + Random.insideUnitCircle * 0.3f + Vector2.up * 0.4f,
                    Vector2.up, Palette.ShadeEdge, 0.6f, false, 1f);
            if (burn >= Tuning.stalkerBurnTime) Kill();
        }

        protected virtual void OnFlare(Vector2 origin, float radius)
        {
            if (IsDead) return;
            Vector2 p = transform.position;
            if ((p - origin).sqrMagnitude <= radius * radius && LightField.LineOfSight(origin, p))
                Kill();
        }

        public virtual void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.4f, 28, new Color(0.15f, 0.1f, 0.2f), 3f, 0.9f, false, 0.6f, 1f, 0.3f);
            PixelParticles.Burst((Vector2)transform.position + Vector2.up * 0.4f, 8, Palette.ShadeEye, 2f, 0.5f);
            GameEvents.RaiseEnemyKilled(transform.position, Kind);
            GameManager.AddScore(ScoreValue, transform.position);
            Destroy(gameObject);
        }

        protected abstract int ScoreValue { get; }

        protected virtual void OnCollisionStay2D(Collision2D c)
        {
            if (IsDead || stunned > 0f) return;
            var player = c.collider.GetComponent<PlayerFlame>();
            if (player != null) OnTouchPlayer(player);
        }

        protected abstract void OnTouchPlayer(PlayerFlame player);

        // ------------------------------------------------------------------ navigation helpers
        protected Vector2Int Cell => DungeonMap.WorldToCell(transform.position);

        /// <summary>Steers along an A* path to a goal cell. Returns true once arrived.</summary>
        protected bool MoveTo(Vector2 goal, float speed, float repathInterval = 0.5f)
        {
            Vector2 pos = transform.position;
            if ((goal - pos).sqrMagnitude < 0.04f) { desiredVelocity = Vector2.zero; return true; }

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
