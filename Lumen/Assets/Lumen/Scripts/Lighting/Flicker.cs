using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Lighting
{
    /// <summary>Organic fire flicker on a Light2D (noise-driven intensity and radius).</summary>
    [RequireComponent(typeof(Light2D))]
    public class Flicker : MonoBehaviour
    {
        public float baseIntensity = 1f;
        public float baseRadius = 5f;
        public float intensityAmount = 0.12f;
        public float radiusAmount = 0.06f;
        public float speed = 6f;

        Light2D light2D;
        float seed;

        void Awake()
        {
            light2D = GetComponent<Light2D>();
            seed = Random.value * 100f;
        }

        void LateUpdate()
        {
            float t = Time.time * speed + seed;
            float n = Mathf.PerlinNoise(t, seed) * 2f - 1f;
            light2D.intensity = baseIntensity * (1f + n * intensityAmount);
            light2D.pointLightOuterRadius = Mathf.Max(0.01f, baseRadius * (1f + n * radiusAmount));
            light2D.pointLightInnerRadius = light2D.pointLightOuterRadius * 0.15f;
        }
    }
}
