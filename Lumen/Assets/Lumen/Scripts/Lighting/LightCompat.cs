using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lumen.Lighting
{
    /// <summary>
    /// Normal map settings are only exposed to the inspector. Lights in this game are
    /// created from code, so these are set through reflection in one isolated place.
    /// </summary>
    static class LightCompat
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static readonly FieldInfo NormalQualityField = typeof(Light2D).GetField("m_NormalMapQuality", Flags);
        static readonly FieldInfo NormalDistanceField = typeof(Light2D).GetField("m_NormalMapDistance", Flags);

        public static void Configure(Light2D light, bool normalMaps)
        {
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            light.targetSortingLayers = ids;

            if (normalMaps && NormalQualityField != null)
            {
                NormalQualityField.SetValue(light, Light2D.NormalMapQuality.Accurate);
                NormalDistanceField?.SetValue(light, 1.5f);
            }
        }
    }
}
