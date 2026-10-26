using Lumen.Art;
using Lumen.Core;
using Lumen.Lighting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lumen.FX
{
    /// <summary>
    /// Owns the ambient light and post-processing stack. Reacts to danger (low fuel tints
    /// and tightens the vignette) and to the player's brightness setting.
    /// </summary>
    public class PostFxController : MonoBehaviour
    {
        Light2D ambient;
        Vignette vignette;
        ColorAdjustments colorAdjust;
        static GameTuning Tuning => GameTuning.I;

        public static PostFxController Create(Transform parent)
        {
            var go = new GameObject("PostFX");
            go.transform.SetParent(parent, false);
            return go.AddComponent<PostFxController>();
        }

        void Awake()
        {
            ambient = LightFactory.Global(transform, Palette.AmbientLight, Tuning.ambientIntensity);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.75f);
            bloom.scatter.Override(0.65f);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(Tuning.vignetteBase);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.08f);

            colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.contrast.Override(10f);
            colorAdjust.saturation.Override(8f);
            colorAdjust.postExposure.Override(0f);

            ApplyBrightness();
        }

        /// <summary>0 = safe, 1 = about to go out.</summary>
        public void SetDanger(float danger)
        {
            vignette.intensity.value = Mathf.Lerp(Tuning.vignetteBase, Tuning.vignetteDanger, danger);
            vignette.color.value = Color.Lerp(Color.black, new Color(0.35f, 0f, 0.02f), danger);
        }

        public void ApplyBrightness()
        {
            float b = SaveData.Brightness;
            ambient.intensity = Tuning.ambientIntensity * b;
            colorAdjust.postExposure.value = Mathf.Log(b, 2f) * 0.5f;
        }
    }
}
