using System.Collections.Generic;
using Lumen.Core;
using Lumen.Level;
using UnityEngine;

namespace Lumen.FX
{
    /// <summary>
    /// Procedural background music: one seamless ~27 s loop per place (title + each biome),
    /// synthesized on first use from a chord progression, a scale and an instrument palette.
    /// Pads, bass, a wandering melody and a circular reverb (so the loop has no seam).
    /// Crossfades when the place changes.
    /// </summary>
    public class Music : MonoBehaviour
    {
        public enum Track { Title, Crypts, Flooded, Roots }

        const int Rate = 22050;
        const float Bpm = 72f;
        const int Bars = 8;
        const float Gain = 0.5f;

        public static Music I { get; private set; }

        readonly Dictionary<Track, AudioClip> clips = new Dictionary<Track, AudioClip>();
        AudioSource a, b;
        AudioSource current;
        Track? playing;
        float fade = 1f;

        void Awake()
        {
            I = this;
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { a, b }) { s.loop = true; s.playOnAwake = false; s.volume = 0f; }
            current = a;
        }

        public static Track ForBiome(Biome biome) => biome switch
        {
            Biome.Flooded => Track.Flooded,
            Biome.Roots => Track.Roots,
            _ => Track.Crypts,
        };

        /// <summary>Switches to a track with a short crossfade (no-op if it is already playing).</summary>
        public void Play(Track track)
        {
            if (playing == track) return;
            playing = track;
            var next = current == a ? b : a;
            next.clip = Get(track);
            next.volume = 0f;
            next.Play();
            current = next;
            fade = 0f;
        }

        void Update()
        {
            float target = Gain * SaveData.MusicVolume;
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 2.5f);
            var other = current == a ? b : a;
            current.volume = target * fade;
            other.volume = target * (1f - fade);
            if (fade >= 1f && other.isPlaying) other.Stop();
        }

        AudioClip Get(Track track)
        {
            if (!clips.TryGetValue(track, out var clip))
            {
                clip = Compose(track);
                clips[track] = clip;
            }
            return clip;
        }

        // ------------------------------------------------------------------ composition

        sealed class Style
        {
            public int[][] Chords;         // MIDI notes, one chord per bar (cycled)
            public int[] Scale;            // melody notes
            public float Pad, Bass, Melody, Arp;
            public bool Pluck;             // melody as plucked string instead of bell
            public float Vibrato;          // watery wobble on pads
        }

        static Style StyleFor(Track t) => t switch
        {
            // A minor: Am F C G — slow celesta over warm pads.
            Track.Title => new Style
            {
                Chords = new[] { new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 } },
                Scale = new[] { 69, 71, 72, 74, 76, 79, 81 }, Pad = 0.05f, Bass = 0.09f, Melody = 0.06f, Arp = 0f,
            },
            // D harmonic minor: Dm Bb Gm A — organ-like pads, sparse bells.
            Track.Crypts => new Style
            {
                Chords = new[] { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 43, 46, 50 }, new[] { 45, 49, 52 } },
                Scale = new[] { 62, 64, 65, 67, 69, 70, 73, 74 }, Pad = 0.055f, Bass = 0.1f, Melody = 0.05f, Arp = 0f,
            },
            // E minor: Em Cmaj7 Am B — watery arpeggios with vibrato.
            Track.Flooded => new Style
            {
                Chords = new[] { new[] { 52, 55, 59 }, new[] { 48, 52, 55, 59 }, new[] { 45, 48, 52 }, new[] { 47, 51, 54 } },
                Scale = new[] { 64, 66, 67, 69, 71, 72, 74, 76 }, Pad = 0.04f, Bass = 0.08f, Melody = 0.04f, Arp = 0.035f, Vibrato = 0.006f,
            },
            // C# minor: C#m A E B — low drones and plucked strings.
            Track.Roots => new Style
            {
                Chords = new[] { new[] { 49, 52, 56 }, new[] { 45, 49, 52 }, new[] { 52, 56, 59 }, new[] { 47, 51, 54 } },
                Scale = new[] { 61, 63, 64, 66, 68, 69, 71, 73 }, Pad = 0.045f, Bass = 0.11f, Melody = 0.07f, Arp = 0f, Pluck = true,
            },
            _ => null,
        };

        static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static AudioClip Compose(Track track)
        {
            var buf = Render(track);
            var clip = AudioClip.Create("music_" + track, buf.Length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        /// <summary>Synthesizes a track's samples (mono, 22050 Hz). Public so tests can check the audio.</summary>
        public static float[] Render(Track track)
        {
            var style = StyleFor(track);
            float beat = 60f / Bpm, bar = beat * 4f;
            int length = Mathf.RoundToInt(bar * Bars * Rate);
            var buf = new float[length];
            var rng = new System.Random(1000 + (int)track);

            for (int barIndex = 0; barIndex < Bars; barIndex++)
            {
                var chord = style.Chords[barIndex % style.Chords.Length];
                int barStart = Mathf.RoundToInt(barIndex * bar * Rate);

                // Pads: each chord tone as two slightly detuned sines, swelling in and out over the bar.
                foreach (int note in chord)
                    AddPad(buf, barStart, bar + 0.8f, Hz(note), style.Pad, style.Vibrato);

                // Bass: chord root an octave down, on beats 1 and 3.
                for (int k = 0; k < 2; k++)
                    AddBass(buf, barStart + Mathf.RoundToInt(k * 2 * beat * Rate), beat * 2f, Hz(chord[0] - 12), style.Bass);

                // Arpeggio (flooded): eighth notes climbing the chord.
                if (style.Arp > 0f)
                    for (int e = 0; e < 8; e++)
                        AddBell(buf, barStart + Mathf.RoundToInt(e * beat * 0.5f * Rate), 0.7f, Hz(chord[e % chord.Length] + 12), style.Arp);
            }

            // Melody: a slow random walk on the scale, resting often so it breathes.
            int degree = style.Scale.Length / 2;
            for (int beatIndex = 0; beatIndex < Bars * 4; beatIndex++)
            {
                if (rng.NextDouble() > 0.42) continue;
                degree = Mathf.Clamp(degree + rng.Next(-2, 3), 0, style.Scale.Length - 1);
                int start = Mathf.RoundToInt(beatIndex * beat * Rate);
                float f = Hz(style.Scale[degree]);
                if (style.Pluck) AddPluck(buf, start, 1.6f, f, style.Melody, rng);
                else AddBell(buf, start, 1.8f, f, style.Melody);
            }

            Reverb(buf);
            Normalize(buf, 0.8f);
            return buf;
        }

        // ------------------------------------------------------------------ instruments
        // All voices write with wrap-around, so notes that run past the end continue at the start:
        // that is what makes the loop seamless.

        static void Write(float[] buf, int index, float value) => buf[((index % buf.Length) + buf.Length) % buf.Length] += value;

        static void AddPad(float[] buf, int start, float seconds, float hz, float gain, float vibrato)
        {
            int n = Mathf.RoundToInt(seconds * Rate);
            float ph1 = 0f, ph2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.9f) * Mathf.Clamp01((seconds - t) / 0.9f);
                float wobble = 1f + vibrato * Mathf.Sin(t * 5.5f);
                ph1 += 2f * Mathf.PI * hz * wobble / Rate;
                ph2 += 2f * Mathf.PI * hz * 1.004f * wobble / Rate;
                float s = Mathf.Sin(ph1) + Mathf.Sin(ph2) + 0.3f * Mathf.Sin(ph1 * 2f);
                Write(buf, start + i, s * env * gain);
            }
        }

        static void AddBass(float[] buf, int start, float seconds, float hz, float gain)
        {
            int n = Mathf.RoundToInt(seconds * Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-t * 1.6f);
                float s = Mathf.Sin(2f * Mathf.PI * hz * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * hz * t);
                Write(buf, start + i, s * env * gain);
            }
        }

        static void AddBell(float[] buf, int start, float seconds, float hz, float gain)
        {
            int n = Mathf.RoundToInt(seconds * Rate);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-t * 3f);
                // Inharmonic partial gives the glassy, celesta-like color.
                float s = Mathf.Sin(2f * Mathf.PI * hz * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * hz * 2.76f * t) * Mathf.Exp(-t * 6f);
                Write(buf, start + i, s * env * gain);
            }
        }

        /// <summary>Karplus-Strong plucked string.</summary>
        static void AddPluck(float[] buf, int start, float seconds, float hz, float gain, System.Random rng)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / hz));
            var ring = new float[period];
            for (int i = 0; i < period; i++) ring[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            int n = Mathf.RoundToInt(seconds * Rate);
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (idx + 1) % period;
                float v = ring[idx];
                ring[idx] = 0.996f * 0.5f * (ring[idx] + ring[next]);
                idx = next;
                Write(buf, start + i, v * gain);
            }
        }

        /// <summary>Two feedback delays read and written circularly, so the tail wraps into the loop start.</summary>
        static void Reverb(float[] buf)
        {
            foreach (var (seconds, feedback) in new[] { (0.137f, 0.38f), (0.211f, 0.32f), (0.293f, 0.25f) })
            {
                int d = Mathf.RoundToInt(seconds * Rate);
                for (int pass = 0; pass < 2; pass++) // second pass lets the wrapped tail settle
                    for (int i = 0; i < buf.Length; i++)
                        buf[(i + d) % buf.Length] += buf[i] * feedback * 0.5f;
            }
        }

        static void Normalize(float[] buf, float peak)
        {
            float max = 0f;
            foreach (float v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            if (max < 1e-5f) return;
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }
    }
}
