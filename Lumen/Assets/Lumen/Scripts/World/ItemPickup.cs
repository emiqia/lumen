using Lumen.Art;
using Lumen.Core;
using Lumen.FX;
using Lumen.Level;
using Lumen.Lighting;
using Lumen.Player;
using UnityEngine;

namespace Lumen.World
{
    /// <summary>A collectible item lying in a room. Collected only if the run can carry it.</summary>
    public class ItemPickup : MonoBehaviour
    {
        /// <summary>Set by the GameManager: tries to add the item to the run; false if it can't be carried.</summary>
        public static System.Func<ItemKind, bool> Collector;

        ItemKind kind;
        Vector2 home;
        float phase;

        public static ItemPickup Create(ItemSpawn spawn, Transform parent)
        {
            var go = new GameObject("Item_" + spawn.Kind);
            go.layer = Layers.Pickups;
            go.transform.SetParent(parent, false);
            go.transform.position = DungeonMap.CellToWorld(spawn.Cell);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Item(spawn.Kind);
            sr.sharedMaterial = LumenAssets.I.Unlit;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            col.offset = Vector2.zero;
            var light = LightFactory.Point(go.transform, ItemColor(spawn.Kind), 1.6f, 0.55f, false);
            light.gameObject.AddComponent<Flicker>().baseRadius = 1.6f;
            go.AddComponent<FX.YSort>();
            var item = go.AddComponent<ItemPickup>();
            item.kind = spawn.Kind;
            return item;
        }

        public static Color ItemColor(ItemKind k) => k switch
        {
            ItemKind.Lantern => new Color(1f, 0.85f, 0.5f),
            ItemKind.Lure => new Color(1f, 0.95f, 0.85f),
            ItemKind.Oil => new Color(0.95f, 0.6f, 0.2f),
            _ => new Color(0.6f, 0.8f, 1f),
        };

        void Start()
        {
            home = transform.position;
            phase = Random.value * 10f;
        }

        void Update() => transform.position = home + new Vector2(0, Mathf.Sin(Time.time * 2f + phase) * 0.08f);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != Layers.Player || !other.TryGetComponent(out PlayerFlame player) || !player.IsAlive) return;
            if (Collector == null || !Collector(kind)) return;
            GameEvents.RaiseItemCollected(transform.position, kind);
            PixelParticles.Burst(transform.position, 16, ItemColor(kind), 2.5f, 0.5f, true, 1f);
            Destroy(gameObject);
        }
    }
}
