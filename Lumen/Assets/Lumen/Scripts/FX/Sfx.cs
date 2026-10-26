using System;
using Lumen.Core;
using Lumen.Level;
using UnityEngine;

namespace Lumen.FX
{
    /// <summary>
    /// Procedural sound: every effect is synthesized into an AudioClip at startup
    /// (oscillators + noise + envelopes). Listens to GameEvents, so gameplay code
    /// never calls audio directly.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }
        const int Rate = 44100;

        AudioClip flare, ember, brazier, hit, kill, exitOpen, floorClear, death, extinguish, heartbeat, ambient, blip;
        AudioSource[] voices;
        AudioSource ambientSource, heartSource;
        int nextVoice;
        readonly System.Random rng = new System.Random(1234);

        void Awake()
        {
            I = this;
            voices = new AudioSource[12];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = 0.16f * SaveData.MusicVolume;
            heartSource = gameObject.AddComponent<AudioSource>();
            heartSource.loop = true;
            heartSource.volume = 0f;

            Build();
            ambientSource.clip = ambient;
            ambientSource.Play();
            heartSource.clip = heartbeat;
            heartSource.Play();

            GameEvents.Flare += (p, r) => Play(flare, 0.55f);
            GameEvents.EmberCollected += p => Play(ember, 0.4f, UnityEngine.Random.Range(0.95f, 1.1f));
            GameEvents.BrazierLit += p => Play(brazier, 0.6f);
            GameEvents.BrazierExtinguished += p => Play(extinguish, 0.6f);
            GameEvents.PlayerHit += (p, a, k) => Play(hit, 0.7f);
            GameEvents.EnemyKilled += (p, k, _) => Play(kill, 0.45f, k == EnemyKind.Devourer ? 0.6f : UnityEngine.Random.Range(0.9f, 1.2f));
            GameEvents.ExitOpened += () => Play(exitOpen, 0.6f);
            GameEvents.FloorCleared += f => Play(floorClear, 0.6f);
            GameEvents.PlayerDied += c => Play(death, 0.7f);
        }

        public void Blip() => Play(blip, 0.3f);

        /// <summary>Music = ambient drone + heartbeat; effects = everything else.</summary>
        // The drone now sits quietly under the procedural music (see Music).
        public void ApplyVolumes() => ambientSource.volume = 0.16f * SaveData.MusicVolume;

        /// <summary>0 = calm, 1 = about to die. Drives the heartbeat layer.</summary>
        public void SetDanger(float danger)
        {
            heartSource.volume = Mathf.Lerp(heartSource.volume, danger * 0.6f * SaveData.MusicVolume, Time.unscaledDeltaTime * 4f);
            heartSource.pitch = 0.9f + danger * 0.5f;
        }

        void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = pitch;
            v.PlayOneShot(clip, volume * SaveData.SfxVolume);
        }

        // ------------------------------------------------------------------ synthesis
        void Build()
        {
            flare = Synth("flare", 0.6f, (t, d) =>
                Noise() * Env(t, 0.005f, d) * (1 - t / d) * 0.6f +
                Sine(t, 200 + 900 * t / d) * Env(t, 0.01f, d) * 0.5f);

            ember = Synth("ember", 0.18f, (t, d) =>
                Square(t, t < 0.07f ? 880 : 1320) * Env(t, 0.002f, d) * 0.3f);

            blip = Synth("blip", 0.06f, (t, d) => Square(t, 660) * Env(t, 0.002f, d) * 0.25f);

            brazier = Synth("brazier", 1.1f, (t, d) =>
                Noise() * Env(t, 0.15f, d) * 0.35f * (0.6f + 0.4f * Mathf.Sin(t * 40f)) +
                Sine(t, 110) * Env(t, 0.05f, d) * 0.4f +
                (rng.NextDouble() > 0.9985 ? 0.8f : 0f));

            extinguish = Synth("extinguish", 0.8f, (t, d) =>
                Noise() * Env(t, 0.01f, d) * 0.4f * (1 - t / d) +
                Sine(t, 300 - 220 * t / d) * Env(t, 0.01f, d) * 0.3f);

            hit = Synth("hit", 0.35f, (t, d) =>
                Sine(t, 90 - 40 * t / d) * Env(t, 0.002f, d) * 0.9f + Noise() * Env(t, 0.001f, 0.08f) * 0.5f);

            kill = Synth("kill", 0.45f, (t, d) =>
                Noise() * Env(t, 0.005f, d) * 0.35f + Saw(t, 520 - 400 * t / d) * Env(t, 0.005f, d) * 0.25f);

            exitOpen = Synth("exit", 1.2f, (t, d) => Arp(t, d, new[] { 523f, 659f, 784f, 1046f }) * 0.3f);
            floorClear = Synth("clear", 1.0f, (t, d) => Arp(t, d, new[] { 392f, 523f, 659f, 784f, 1046f }) * 0.3f);

            death = Synth("death", 1.6f, (t, d) =>
                (Sine(t, 440 * Mathf.Pow(0.25f, t / d)) + Saw(t, 220 * Mathf.Pow(0.25f, t / d)) * 0.3f) * Env(t, 0.01f, d) * 0.5f);

            heartbeat = Synth("heart", 0.9f, (t, d) =>
                Sine(t, 55) * (Env(t, 0.005f, 0.12f) + Env(Mathf.Max(0, t - 0.22f), 0.005f, 0.14f) * 0.7f) * 0.9f);

            // 12 s ambient drone: detuned low sines with a slow swell + rare water drips.
            ambient = Synth("ambient", 12f, (t, d) =>
            {
                float swell = 0.6f + 0.4f * Mathf.Sin(t / d * Mathf.PI * 2f);
                float drone = (Sine(t, 55f) + Sine(t, 55.4f) * 0.8f + Sine(t, 82.5f) * 0.3f) * 0.18f * swell;
                float dripT = t % 3.7f;
                float drip = t > 1f ? Sine(dripT, 1400 - dripT * 2000) * Env(dripT, 0.001f, 0.12f) * 0.08f : 0f;
                return drone + drip;
            }, fadeEdges: true);
        }

        AudioClip Synth(string name, float duration, Func<float, float, float> f, bool fadeEdges = false)
        {
            int n = (int)(duration * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float s = f(t, duration);
                if (fadeEdges) s *= Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.05f);
                data[i] = Mathf.Clamp(s, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float t, float hz) => Mathf.Sin(t * hz * Mathf.PI * 2f);
        static float Square(float t, float hz) => Sine(t, hz) >= 0 ? 1f : -1f;
        static float Saw(float t, float hz) => 2f * (t * hz - Mathf.Floor(t * hz + 0.5f));
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        /// <summary>Attack / exponential-decay envelope.</summary>
        static float Env(float t, float attack, float length)
        {
            if (t < 0 || t > length) return 0f;
            if (t < attack) return t / attack;
            return Mathf.Exp(-4f * (t - attack) / length);
        }

        static float Arp(float t, float d, float[] notes)
        {
            float step = d / (notes.Length + 2);
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float lt = t - i * step;
                if (lt >= 0) s += Square(lt, notes[i]) * Env(lt, 0.003f, step * 3f) * 0.5f + Sine(lt, notes[i] * 2) * Env(lt, 0.003f, step * 3f) * 0.3f;
            }
            return s;
        }
    }
}
