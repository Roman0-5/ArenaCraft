using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// All sound is synthesised at runtime so the project ships with zero audio
    /// assets while still satisfying GDD 5.3 / FSR3: SFX for attacks, resource
    /// collection and phase transitions, plus a calm "farming" track and an
    /// intense "battle" track. Swap in real clips later by assigning the public
    /// AudioClip fields before Initialize().
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _sfx;
        private AudioSource _music;

        private AudioClip _attack, _hit, _collect, _break, _transition, _victory;
        private AudioClip _calmMusic, _battleMusic;

        public void Initialize()
        {
            Instance = this;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.35f;
            _music.spatialBlend = 0f;

            _attack = MakeNoiseSweep("attack", 0.16f, 900f, 250f, 0.35f);
            _hit = MakeThud("hit", 0.18f, 150f);
            _collect = MakeChime("collect", new[] { 880f, 1320f }, 0.18f, 0.3f);
            _break = MakeThud("break", 0.30f, 90f);
            _transition = MakeChime("transition", new[] { 440f, 660f, 880f }, 0.6f, 0.4f);
            _victory = MakeChime("victory", new[] { 523f, 659f, 784f, 1047f }, 1.0f, 0.45f);

            _calmMusic = MakeMusic("calm", false);
            _battleMusic = MakeMusic("battle", true);
        }

        // ---- SFX ----
        public void PlayAttack() => _sfx?.PlayOneShot(_attack, 0.5f);
        public void PlayHit() => _sfx?.PlayOneShot(_hit, 0.7f);
        public void PlayCollect() => _sfx?.PlayOneShot(_collect, 0.4f);
        public void PlayNodeBreak() => _sfx?.PlayOneShot(_break, 0.6f);
        public void PlayTransition() => _sfx?.PlayOneShot(_transition, 0.6f);
        public void PlayVictory() => _sfx?.PlayOneShot(_victory, 0.7f);

        // ---- Music ----
        public void PlayCalmMusic() => SwapMusic(_calmMusic);
        public void PlayBattleMusic() => SwapMusic(_battleMusic);
        public void StopMusic() { if (_music != null) _music.Stop(); }

        private void SwapMusic(AudioClip clip)
        {
            if (_music == null || clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            _music.Play();
        }

        // ---------- Synthesis helpers ----------
        private const int SampleRate = 44100;

        private static AudioClip MakeNoiseSweep(string name, float dur, float startFreq, float endFreq, float vol)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / SampleRate;
                float env = Mathf.Sin(Mathf.PI * t); // fade in/out
                float tone = Mathf.Sin(phase * 2f * Mathf.PI);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                data[i] = (tone * 0.5f + noise * 0.5f) * env * vol;
            }
            return ClipFrom(name, data);
        }

        private static AudioClip MakeThud(string name, float dur, float freq)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Exp(-6f * t);
                float f = freq * (1f - 0.5f * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate)) * env * 0.8f;
            }
            return ClipFrom(name, data);
        }

        private static AudioClip MakeChime(string name, float[] freqs, float dur, float vol)
        {
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];
            float per = dur / freqs.Length;
            for (int i = 0; i < n; i++)
            {
                float tSec = i / (float)SampleRate;
                int idx = Mathf.Min(freqs.Length - 1, (int)(tSec / per));
                float localT = (tSec - idx * per) / per;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(localT));
                data[i] = Mathf.Sin(2f * Mathf.PI * freqs[idx] * tSec) * env * vol;
            }
            return ClipFrom(name, data);
        }

        /// <summary>
        /// A short looping bed. Calm = slow major arpeggio + soft pad. Battle =
        /// faster, lower pulse with a percussive kick on every beat.
        /// </summary>
        private static AudioClip MakeMusic(string name, bool battle)
        {
            float dur = battle ? 3.692f : 5.333f; // a few bars long; loops seamlessly enough
            int n = Mathf.RoundToInt(SampleRate * dur);
            var data = new float[n];

            float[] scale = battle
                ? new[] { 110f, 130.8f, 146.8f, 164.8f, 130.8f, 110f, 98f, 130.8f }
                : new[] { 261.6f, 329.6f, 392f, 523.2f, 392f, 329.6f, 293.7f, 329.6f };
            float noteLen = dur / scale.Length;
            float beat = battle ? 0.23f : 0.4f;

            for (int i = 0; i < n; i++)
            {
                float tSec = i / (float)SampleRate;
                int idx = Mathf.Min(scale.Length - 1, (int)(tSec / noteLen));
                float localT = (tSec % noteLen) / noteLen;
                float env = Mathf.Sin(Mathf.PI * localT) * 0.5f + 0.2f;

                float melody = Mathf.Sin(2f * Mathf.PI * scale[idx] * tSec) * env;
                float pad = Mathf.Sin(2f * Mathf.PI * scale[idx] * 0.5f * tSec) * 0.25f;
                float sample = (melody + pad) * (battle ? 0.5f : 0.4f);

                if (battle)
                {
                    // Percussive kick at each beat.
                    float beatPhase = (tSec % beat) / beat;
                    float kickEnv = Mathf.Exp(-18f * beatPhase);
                    float kick = Mathf.Sin(2f * Mathf.PI * 60f * tSec) * kickEnv * 0.7f;
                    float hat = (Random.value * 2f - 1f) * Mathf.Exp(-40f * beatPhase) * 0.15f;
                    sample += kick + hat;
                }

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }
            return ClipFrom(name, data);
        }

        private static AudioClip ClipFrom(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
