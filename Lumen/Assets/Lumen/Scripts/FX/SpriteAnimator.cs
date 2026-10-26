using UnityEngine;

namespace Lumen.FX
{
    /// <summary>Plays a looping sequence of procedurally generated sprites.</summary>
    public class SpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public SpriteRenderer target;
        float t;

        void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            t = Random.value * 10f; // desync identical props
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || target == null) return;
            t += Time.deltaTime * fps;
            target.sprite = frames[(int)t % frames.Length];
        }
    }

    /// <summary>Y-sorting for a top-down 3/4 view: lower on screen = drawn in front.</summary>
    public class YSort : MonoBehaviour
    {
        public int offset;
        SpriteRenderer[] renderers;
        int[] baseOrders;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseOrders[i] = renderers[i].sortingOrder;
        }

        void LateUpdate()
        {
            int order = 1000 - Mathf.RoundToInt(transform.position.y * 10f) + offset;
            for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = order * 4 + baseOrders[i];
        }
    }
}
