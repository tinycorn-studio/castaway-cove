using UnityEngine;

namespace Cove
{
    public class AudioLibrary : MonoBehaviour
    {
        public const int Rate = 44100;
        private AudioSource global;
        private AudioSource footsteps;
        private System.Random rng = new System.Random(1234);

        public AudioClip Footstep;
        public AudioClip Splash;
        public AudioClip Cast;
        public AudioClip Plop;
        public AudioClip Nibble;
        public AudioClip Bite;
        public AudioClip Hook;
        public AudioClip ReelTick;
        public AudioClip Fanfare;
        public AudioClip Snap;
        public AudioClip Click;
        public AudioClip Buy;
        public AudioClip Error;
        public AudioClip Shot;
        public AudioClip Hit;
        public AudioClip Punch;
        public AudioClip Reload;
        public AudioClip Hurt;
        public AudioClip Death;
        public AudioClip BossRoar;

        private float lastReelTick;

        public void Init()
        {
            global = gameObject.AddComponent<AudioSource>();
            global.playOnAwake = false;
            global.spatialBlend = 0f;
            footsteps = gameObject.AddComponent<AudioSource>();
            footsteps.playOnAwake = false;
            footsteps.spatialBlend = 0f;

            Footstep = BuildNoise("footstep", 0.11f, 700f, 120f, 0.35f);
            Splash = BuildNoise("splash", 0.36f, 1600f, 300f, 0.5f);
            Cast = BuildSweep("cast", 0.28f, 400f, 1400f, 0.25f);
            Plop = BuildTone("plop", 0.16f, 320f, 140f, 0.5f);
            Nibble = BuildBlips("nibble", new float[] { 1200f, 1500f }, 0.05f, 0.05f, 0.18f);
            Bite = BuildBlips("bite", new float[] { 880f, 1320f, 880f }, 0.07f, 0.055f, 0.3f);
            Hook = BuildSweep("hook", 0.2f, 500f, 1000f, 0.3f);
            ReelTick = BuildNoise("reeltick", 0.025f, 2400f, 1200f, 0.16f);
            Fanfare = BuildBlips("fanfare", new float[] { 523f, 659f, 784f, 1047f }, 0.12f, 0.09f, 0.26f);
            Snap = BuildNoise("snap", 0.22f, 3000f, 500f, 0.4f);
            Click = BuildTone("click", 0.045f, 900f, 700f, 0.15f);
            Buy = BuildBlips("buy", new float[] { 660f, 990f }, 0.08f, 0.07f, 0.22f);
            Error = BuildBlips("error", new float[] { 220f, 180f }, 0.12f, 0.05f, 0.2f);
            Shot = BuildNoise("shot", 0.13f, 1100f, 220f, 0.5f);
            Hit = BuildTone("hit", 0.09f, 260f, 110f, 0.4f);
            Punch = BuildNoise("punch", 0.08f, 600f, 160f, 0.35f);
            Reload = BuildBlips("reload", new float[] { 420f, 320f }, 0.06f, 0.05f, 0.2f);
            Hurt = BuildTone("hurt", 0.2f, 190f, 80f, 0.35f);
            Death = BuildBlips("death", new float[] { 300f, 220f, 140f }, 0.16f, 0.09f, 0.3f);
            BossRoar = BuildNoise("bossroar", 0.7f, 170f, 55f, 0.5f);
        }

        private float[] Noise(float duration, float startHz, float endHz, float amp, int seed)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(duration * Rate));
            var data = new float[n];
            var r = new System.Random(seed);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += hz / Rate;
                if (phase > 1f) phase -= 1f;
                float noise = (float)r.NextDouble() * 2f - 1f;
                float generate = Mathf.Sin(phase * Mathf.PI * 2f) * 0.4f + noise * 0.6f;
                float env = Mathf.Pow(1f - t, 2.2f);
                data[i] = generate * env * amp;
            }
            return data;
        }

        private AudioClip BuildNoise(string name, float dur, float hzA, float hzB, float amp)
        {
            return MakeClip(name, Noise(dur, hzA, hzB, amp, rng.Next()));
        }

        private AudioClip BuildTone(string name, float dur, float hzA, float hzB, float amp)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(dur * Rate));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float hz = Mathf.Lerp(hzA, hzB, t);
                phase += hz / Rate;
                float env = Mathf.Pow(1f - t, 2f);
                data[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * amp;
            }
            return MakeClip(name, data);
        }

        private AudioClip BuildSweep(string name, float dur, float hzA, float hzB, float amp)
        {
            return MakeClip(name, Noise(dur, hzA, hzB, amp, rng.Next()));
        }

        private AudioClip BuildBlips(string name, float[] freq, float each, float gap, float amp)
        {
            int per = Mathf.Max(1, Mathf.RoundToInt(each * Rate));
            int gapSamples = Mathf.RoundToInt(gap * Rate);
            int total = freq.Length * (per + gapSamples);
            var data = new float[total];
            int cursor = 0;
            for (int f = 0; f < freq.Length; f++)
            {
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / per;
                    float env = Mathf.Sin(t * Mathf.PI);
                    data[cursor + i] = Mathf.Sin(2f * Mathf.PI * freq[f] * i / Rate) * env * amp;
                }
                cursor += per + gapSamples;
            }
            return MakeClip(name, data);
        }

        private AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void Play(AudioClip clip, float vol = 1f, float pitch = 1f, bool foot = false)
        {
            if (clip == null) return;
            var src = foot ? footsteps : global;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
        }

        public void PlayFootstep(bool water)
        {
            if (footsteps == null) return;
            var clip = water ? Splash : Footstep;
            footsteps.pitch = 0.92f + (float)rng.NextDouble() * 0.16f;
            footsteps.PlayOneShot(clip, water ? 0.32f : 0.38f);
        }

        public void PlayReelTick()
        {
            if (Time.unscaledTime - lastReelTick < 0.11f) return;
            lastReelTick = Time.unscaledTime;
            Play(ReelTick, 0.35f, 0.9f + (float)rng.NextDouble() * 0.25f);
        }
    }
}
