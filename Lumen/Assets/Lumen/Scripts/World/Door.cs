using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>
    /// An iron gate across a doorway that opens and closes on a cycle. Closed, it is a wall:
    /// it blocks movement, light and line of sight (it lives on the Walls layer and casts shadow).
    /// It never closes on someone standing in it, and it rattles for a second before slamming.
    /// </summary>
    public class Door : MonoBehaviour
    {
        const float WarningTime = 1f;

        BoxCollider2D solid;
        SpriteRenderer bars;
        Behaviour shadow;
        Vector2 size;
        float timer;
        bool closed;

        public bool Closed => closed;

        public static Door Create(DoorSpawn spawn, Transform parent, float phase)
        {
            var r = spawn.Cells;
            var go = new GameObject("Door");
            go.layer = Layers.Walls;
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = r.height > r.width ? SpriteBank.DoorVertical : SpriteBank.DoorHorizontal;
            sr.sharedMaterial = LumenAssets.I.Lit;
            go.AddComponent<FX.YSort>();

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(r.width, r.height);
            col.offset = Vector2.zero; // AddComponent auto-fits colliders to the sprite; override it

            var caster = ShadowCasterCompat.CreateBox(go.transform, new Rect(go.transform.position - (Vector3)(col.size * 0.5f), col.size));

            var door = go.AddComponent<Door>();
            door.Init(sr, col, caster, col.size, phase);
            return door;
        }

        void Init(SpriteRenderer sprite, BoxCollider2D box, Behaviour caster, Vector2 doorSize, float phase)
        {
            bars = sprite;
            solid = box;
            shadow = caster;
            size = doorSize;
            timer = phase * (GameTuning.I.doorOpenTime + GameTuning.I.doorClosedTime);
            SetClosed(false);
        }

        void Update()
        {
            var t = GameTuning.I;
            timer += Time.deltaTime;
            if (!closed)
            {
                float untilClose = t.doorOpenTime - timer;
                // Rattle as a warning: the bars shake and spark before they slam.
                bars.transform.localPosition = untilClose < WarningTime ? (Vector3)(Random.insideUnitCircle * 0.03f) : Vector3.zero;
                if (untilClose < WarningTime && Random.value < Time.deltaTime * 12f)
                    PixelParticles.Emit((Vector2)transform.position + Random.insideUnitCircle * 0.4f, Vector2.up, Palette.FireOrange, 0.3f);
                if (timer >= t.doorOpenTime && !Occupied()) { timer = 0f; SetClosed(true); }
            }
            else if (timer >= t.doorClosedTime) { timer = 0f; SetClosed(false); }
        }

        bool Occupied()
        {
            int mask = (1 << Layers.Player) | (1 << Layers.Enemies);
            return Physics2D.OverlapBox(transform.position, size * 0.95f, 0f, mask) != null;
        }

        void SetClosed(bool value)
        {
            closed = value;
            solid.enabled = value;
            shadow.enabled = value;
            bars.color = value ? Color.white : new Color(1f, 1f, 1f, 0.18f); // open: only a faint frame remains
            bars.transform.localPosition = Vector3.zero;
            if (value) CameraRig.Shake(0.05f);
        }
    }
}
