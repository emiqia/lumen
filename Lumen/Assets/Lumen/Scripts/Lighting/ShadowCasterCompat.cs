using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Lighting
{
    /// <summary>
    /// ShadowCaster2D only exposes its shape to the editor. Since walls are generated at
    /// runtime, the shape path is injected before the component wakes up.
    /// </summary>
    public static class ShadowCasterCompat
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo ShapePath = typeof(ShadowCaster2D).GetField("m_ShapePath", Flags);
        static readonly FieldInfo ShapePathHash = typeof(ShadowCaster2D).GetField("m_ShapePathHash", Flags);

        public static ShadowCaster2D CreateBox(Transform parent, Rect rect)
        {
            var go = new GameObject("Caster");
            go.SetActive(false); // defer Awake until the shape is set
            go.transform.SetParent(parent, false);
            go.transform.position = rect.center;

            var caster = go.AddComponent<ShadowCaster2D>();
            Vector2 e = rect.size * 0.5f;
            var path = new[]
            {
                new Vector3(-e.x, -e.y), new Vector3(e.x, -e.y),
                new Vector3(e.x, e.y), new Vector3(-e.x, e.y),
            };
            ShapePath?.SetValue(caster, path);
            ShapePathHash?.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));
            caster.selfShadows = false;
            caster.castsShadows = true;

            go.SetActive(true);
            return caster;
        }
    }
}
