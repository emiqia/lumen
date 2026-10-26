using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Lighting
{
    /// <summary>Creates correctly configured 2D lights at runtime.</summary>
    public static class LightFactory
    {
        public static Light2D Point(Transform parent, Color color, float radius, float intensity, bool shadows, float falloff = 0.6f)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
            light.falloffIntensity = falloff;
            light.shadowsEnabled = shadows;
            light.shadowIntensity = shadows ? 0.92f : 0f;
            LightCompat.Configure(light, shadows);
            return light;
        }

        public static Light2D Global(Transform parent, Color color, float intensity)
        {
            var go = new GameObject("GlobalLight");
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = color;
            light.intensity = intensity;
            LightCompat.Configure(light, false);
            return light;
        }
    }
}
