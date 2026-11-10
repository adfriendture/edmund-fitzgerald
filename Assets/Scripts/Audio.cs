using System;
using UnityEngine;

namespace Fitz
{
    /// <summary>Simple biquad filter (RBJ cookbook).</summary>
    public struct Biquad
    {
        float b0, b1, b2, a1, a2, z1, z2;

        public static Biquad BandPass(float sr, float f, float q) { var b = new Biquad(); b.SetBandPass(sr, f, q); return b; }
        public static Biquad LowPass(float sr, float f, float q = 0.707f) { var b = new Biquad(); b.SetLowPass(sr, f, q); return b; }
        public static Biquad HighPass(float sr, float f, float q = 0.707f) { var b = new Biquad(); b.SetHighPass(sr, f, q); return b; }

        public void SetBandPass(float sr, float f, float q)
        {
            float w = 2 * Mathf.PI * Mathf.Clamp(f, 10, sr * 0.45f) / sr, alpha = Mathf.Sin(w) / (2 * q), a0 = 1 + alpha;
            b0 = alpha / a0; b1 = 0; b2 = -alpha / a0; a1 = -2 * Mathf.Cos(w) / a0; a2 = (1 - alpha) / a0;
        }

        public void SetLowPass(float sr, float f, float q = 0.707f)
        {
            float w = 2 * Mathf.PI * Mathf.Clamp(f, 10, sr * 0.45f) / sr, alpha = Mathf.Sin(w) / (2 * q), c = Mathf.Cos(w), a0 = 1 + alpha;
            b0 = (1 - c) / 2 / a0; b1 = (1 - c) / a0; b2 = b0; a1 = -2 * c / a0; a2 = (1 - alpha) / a0;
        }

        public void SetHighPass(float sr, float f, float q = 0.707f)
        {
            float w = 2 * Mathf.PI * Mathf.Clamp(f, 10, sr * 0.45f) / sr, alpha = Mathf.Sin(w) / (2 * q), c = Mathf.Cos(w), a0 = 1 + alpha;
            b0 = (1 + c) / 2 / a0; b1 = -(1 + c) / a0; b2 = b0; a1 = -2 * c / a0; a2 = (1 - alpha) / a0;
        }

        public float Run(float x)
        {
            float y = b0 * x + z1;
            z1 = b1 * x - a1 * y + z2;
            z2 = b2 * x - a2 * y;
            return y;
        }
    }

    /// <summary>
    /// Base for synthesised sources. The AudioSource plays a constant clip and we
    /// overwrite the buffer on the audio thread.
    /// </summary>
    public abstract class ProcSource : MonoBehaviour
    {
        protected float Sr;
        protected readonly System.Random Rng = new();
        public AudioSource Source;
        static AudioClip _ones;

        protected virtual void Awake()
        {
            Sr = AudioSettings.outputSampleRate;
            if (_ones == null)
            {
                _ones = AudioClip.Create("ones", 4096, 1, (int)Sr, false);
                var d = new float[4096];
                for (int i = 0; i < d.Length; i++) d[i] = 1;
                _ones.SetData(d, 0);
            }
            Source = gameObject.AddComponent<AudioSource>();
            Source.clip = _ones;
            Source.loop = true;
            Source.spatialBlend = 0;
            Source.Play();
        }

        protected float Noise() => (float)(Rng.NextDouble() * 2 - 1);

        public static int Blocks; // audio buffers rendered, for the smoke test

        void OnAudioFilterRead(float[] data, int channels)
        {
            Blocks++;
            Render(data, channels);
        }

        protected abstract void Render(float[] data, int channels);
    }

    /// <summary>Wind, sea, rain and engine as heard from inside the pilothouse.</summary>
    public class Ambience : ProcSource
    {
        public volatile float Wind = 0.5f, Gust, SeaWash, Rain, Engine = 1f, Muffle, Master = 1f, Rumble;
        Biquad _howl1, _howl2, _windLp, _seaLp, _rainHp, _muffle1, _muffle2;
        float _brown, _ph1, _ph2, _ph3, _wobble, _rainEnv;

        protected override void Awake()
        {
            base.Awake();
            _howl1 = Biquad.BandPass(Sr, 420, 8);
            _howl2 = Biquad.BandPass(Sr, 690, 10);
            _windLp = Biquad.LowPass(Sr, 900);
            _seaLp = Biquad.LowPass(Sr, 280);
            _rainHp = Biquad.HighPass(Sr, 2500);
            _muffle1 = Biquad.LowPass(Sr, 18000);
            _muffle2 = Biquad.LowPass(Sr, 18000);
        }

        int _coef;

        protected override void Render(float[] data, int ch)
        {
            float wind = Wind, gust = Gust, wash = SeaWash, rain = Rain, eng = Engine, muffle = Muffle, rumble = Rumble;
            if (++_coef % 4 == 0)
            {
                _howl1.SetBandPass(Sr, 380 + gust * 260 + wind * 80, 9);
                _howl2.SetBandPass(Sr, 620 + gust * 420, 12);
                _windLp.SetLowPass(Sr, 500 + wind * 900 + gust * 900);
                float cut = Mathf.Lerp(18000, 260, muffle);
                _muffle1.SetLowPass(Sr, cut);
                _muffle2.SetLowPass(Sr, cut);
            }
            float rpmHz = 18f + eng * 10f;
            for (int i = 0; i < data.Length; i += ch)
            {
                float n = Noise();
                _brown = Mathf.Clamp(_brown * 0.995f + n * 0.04f, -1, 1);
                // wind: broad rush plus whistling in the rigging and window frames
                float rush = _windLp.Run(n) * (0.25f + wind * 0.5f + gust * 0.5f);
                float howl = (_howl1.Run(n) * 0.9f + _howl2.Run(n) * 0.6f) * (0.15f + gust * 0.9f) * wind;
                // sea: deep surge, and the wash of water over the deck
                _wobble += 0.12f / Sr;
                float surge = _seaLp.Run(_brown) * (0.9f + 0.5f * Mathf.Sin(_wobble * 6.283f)) * (0.6f + wind);
                float washN = _brown * wash * 1.4f + n * wash * 0.15f;
                // snow and spray ticking on the glass
                _rainEnv = Rng.NextDouble() < rain * 0.004 ? 1f : _rainEnv * 0.9f;
                float tick = _rainHp.Run(n) * (_rainEnv * 0.5f + rain * 0.08f);
                // engine and shaft: felt more than heard up forward
                _ph1 += rpmHz / Sr; _ph2 += rpmHz * 2.01f / Sr; _ph3 += rpmHz * 6.5f / Sr;
                float engine = (Mathf.Sin(_ph1 * 6.283f) * 0.5f + Mathf.Sin(_ph2 * 6.283f) * 0.3f + Mathf.Sin(_ph3 * 6.283f) * 0.06f) * eng * 0.16f;
                float rum = _brown * rumble * 2.2f;

                float mono = rush * 0.7f + howl + surge * 1.6f + washN + engine + rum;
                float l = _muffle1.Run(mono + tick * 0.8f);
                float r = _muffle2.Run(mono + tick * 0.6f + rush * 0.1f);
                data[i] = l * Master * 0.5f;
                if (ch > 1) data[i + 1] = r * Master * 0.5f;
            }
        }
    }

    /// <summary>The ship's steam whistle: a chord of reedy tones with breath.</summary>
    public class WhistleSynth : ProcSource
    {
        public volatile bool On;
        float _env, _p1, _p2, _p3;
        Biquad _lp;

        protected override void Awake() { base.Awake(); _lp = Biquad.LowPass(Sr, 1600); }

        protected override void Render(float[] data, int ch)
        {
            for (int i = 0; i < data.Length; i += ch)
            {
                _env += ((On ? 1f : 0f) - _env) * (On ? 0.0004f : 0.00015f);
                _p1 = (_p1 + 131f / Sr) % 1f; _p2 = (_p2 + 165f / Sr) % 1f; _p3 = (_p3 + 196f / Sr) % 1f;
                float saw = (_p1 * 2 - 1) + (_p2 * 2 - 1) * 0.8f + (_p3 * 2 - 1) * 0.7f;
                float s = _lp.Run(saw * 0.25f + Noise() * 0.08f) * _env;
                data[i] = s * 0.6f;
                if (ch > 1) data[i + 1] = s * 0.6f;
            }
        }
    }

    /// <summary>
    /// The AM broadcast receiver: band static, and two stations of original,
    /// procedurally played guitar music. A real recording placed in
    /// StreamingAssets/Radio replaces a station's music (see docs/EASTER_EGG.md).
    /// </summary>
    public class AmSynth : ProcSource
    {
        public volatile float Static = 1f;     // 0..1
        public volatile float StationA;       // 0..1 how well tuned to the music station
        public volatile float StationB;
        public volatile float Volume = 0.8f;
        public volatile bool UseCustomA;
        Biquad _bw, _bw2, _crackleHp;
        readonly Guitar _a = new(seed: 11, bpm: 76, progression: new[] { 0, 1, 2, 3, 0, 4, 5, 0 }, voicings: Voicings.Minor);
        readonly Guitar _b = new(seed: 5, bpm: 104, progression: new[] { 0, 1, 2, 0, 3, 1, 2, 0 }, voicings: Voicings.Major);
        readonly Guitar _c = new(seed: 29, bpm: 92, progression: new[] { 0, 2, 3, 1, 0, 2, 1, 0 }, voicings: Voicings.Major);
        long _songSamples;
        bool _songC;
        float _crackle, _hetPh;

        protected override void Awake()
        {
            base.Awake();
            _bw = Biquad.LowPass(Sr, 3800);
            _bw2 = Biquad.HighPass(Sr, 160);
            _crackleHp = Biquad.HighPass(Sr, 1200);
            _a.Init(Sr);
            _b.Init(Sr);
            _c.Init(Sr);
        }

        protected override void Render(float[] data, int ch)
        {
            float st = Static, a = StationA, b = StationB, vol = Volume;
            bool customA = UseCustomA;
            for (int i = 0; i < data.Length; i += ch)
            {
                float n = Noise();
                _crackle = Rng.NextDouble() < 0.0012 ? Noise() * 2f : _crackle * 0.97f;
                _hetPh += (900f + st * 400f) / Sr;
                float stat = n * 0.35f + _crackleHp.Run(_crackle) * 0.6f + Mathf.Sin(_hetPh * 6.283f) * 0.02f * st;
                float music = 0;
                if (a > 0.01f && !customA) music += _a.Next() * a; else if (!customA) _a.Next();
                // 850 plays two tunes turn about, with a breath between them for the DJ
                long songLen = (long)(Sr * 140), gap = (long)(Sr * 4);
                if (++_songSamples > songLen + gap) { _songSamples = 0; _songC = !_songC; }
                float songGain = _songSamples < songLen ? 1f : 0f;
                var cur = _songC ? _c : _b;
                var idle = _songC ? _b : _c;
                idle.Next();
                if (b > 0.01f) music += cur.Next() * b * songGain; else cur.Next();
                float s = _bw2.Run(_bw.Run(music * 0.9f + stat * st * 0.5f)) * vol;
                data[i] = s;
                if (ch > 1) data[i + 1] = s;
            }
        }
    }

    public static class Voicings
    {
        // bass, alternate bass, then three upper notes (MIDI)
        public static readonly int[][] Minor =
        {
            new[] { 40, 47, 55, 59, 64 }, // Em
            new[] { 48, 43, 52, 55, 60 }, // C
            new[] { 43, 50, 55, 59, 62 }, // G
            new[] { 50, 45, 54, 57, 62 }, // D
            new[] { 45, 52, 57, 60, 64 }, // Am
            new[] { 47, 42, 51, 57, 59 }, // B7
        };
        public static readonly int[][] Major =
        {
            new[] { 50, 45, 54, 57, 62 }, // D
            new[] { 43, 50, 55, 59, 62 }, // G
            new[] { 45, 52, 55, 57, 61 }, // A7
            new[] { 47, 42, 50, 54, 59 }, // Bm
        };
    }

    /// <summary>Fingerpicked Karplus-Strong guitar following a chord loop.</summary>
    public class Guitar
    {
        readonly int _seed;
        readonly float _bpm;
        readonly int[] _prog;
        readonly int[][] _voicings;
        float _sr;
        readonly KsString[] _strings = new KsString[6];
        int _next;
        long _sample;
        long _nextNoteAt;
        int _step;
        System.Random _rng;
        int _melody = 2;

        public Guitar(int seed, float bpm, int[] progression, int[][] voicings)
        {
            _seed = seed; _bpm = bpm; _prog = progression; _voicings = voicings;
        }

        public void Init(float sr)
        {
            _sr = sr;
            _rng = new System.Random(_seed);
            for (int i = 0; i < _strings.Length; i++) _strings[i] = new KsString((int)sr);
        }

        static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        public float Next()
        {
            if (_sample >= _nextNoteAt)
            {
                long eighth = (long)(_sr * 60f / _bpm / 2f);
                _nextNoteAt = _sample + eighth;
                int bar = (_step / 8) % _prog.Length;
                int beat = _step % 8;
                var v = _voicings[_prog[bar]];
                int[] pattern = { 0, 2, 3, 4, 1, 2, 3, 4 };
                int note = v[pattern[beat]];
                Pluck(Hz(note), beat == 0 || beat == 4 ? 0.9f : 0.55f);
                // a sparse melody above, a slow random walk on the chord's upper notes
                if ((beat == 0 || beat == 3 || beat == 6) && _rng.NextDouble() < 0.55)
                {
                    _melody = Mathf.Clamp(_melody + _rng.Next(-1, 2), 2, 4);
                    Pluck(Hz(v[_melody] + 12), 0.5f);
                }
                _step++;
            }
            _sample++;
            float s = 0;
            foreach (var st in _strings) s += st.Next();
            return s * 0.35f;
        }

        void Pluck(float hz, float amp)
        {
            _strings[_next].Pluck(hz, amp, _sr, _rng);
            _next = (_next + 1) % _strings.Length;
        }

        class KsString
        {
            readonly float[] _buf;
            int _len, _pos;
            float _last;

            public KsString(int sr) { _buf = new float[sr / 30]; }

            public void Pluck(float hz, float amp, float sr, System.Random rng)
            {
                _len = Mathf.Clamp((int)(sr / hz), 2, _buf.Length - 1);
                for (int i = 0; i < _len; i++) _buf[i] = (float)(rng.NextDouble() * 2 - 1) * amp;
                _pos = 0;
            }

            public float Next()
            {
                if (_len == 0) return 0;
                int nxt = (_pos + 1) % _len;
                float v = _buf[_pos];
                _buf[_pos] = (v + _buf[nxt]) * 0.4985f;
                _pos = nxt;
                _last = _last * 0.2f + v * 0.8f;
                return _last;
            }
        }
    }

    /// <summary>One-shot sounds generated at start-up, and the mixer for everything.</summary>
    public class GameAudio : MonoBehaviour
    {
        public Ambience Ambience;
        public WhistleSynth WhistleSynth;
        public AmSynth Am;
        public AudioSource Sfx, Voice, AmVoice, CustomStation;
        AudioLowPassFilter _voiceLp;
        AudioHighPassFilter _voiceHp;
        AudioDistortionFilter _voiceDist;
        public AudioClip ClickClip, BellClip, TelegraphClip, SquelchClip, SlamClip, ScrapeClip, GroanClip, PhoneClip, CrashClip, CrackClip;
        float _whistleUntil;
        public bool WhistleOn => WhistleSynth != null && WhistleSynth.On;
        public float VoiceVolume = 1f, SfxVolume = 1f;

        public void Build()
        {
            int sr = AudioSettings.outputSampleRate;
            Ambience = Child<Ambience>("Ambience");
            WhistleSynth = Child<WhistleSynth>("Whistle");
            Am = Child<AmSynth>("AmRadio");
            Sfx = gameObject.AddComponent<AudioSource>();
            var vgo = new GameObject("Voice");
            vgo.transform.SetParent(transform, false);
            Voice = vgo.AddComponent<AudioSource>();
            _voiceHp = vgo.AddComponent<AudioHighPassFilter>();
            _voiceLp = vgo.AddComponent<AudioLowPassFilter>();
            _voiceDist = vgo.AddComponent<AudioDistortionFilter>();
            var avgo = new GameObject("AmVoice");
            avgo.transform.SetParent(transform, false);
            AmVoice = avgo.AddComponent<AudioSource>();
            avgo.AddComponent<AudioHighPassFilter>().cutoffFrequency = 250;
            avgo.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3500;
            var cgo = new GameObject("CustomStation");
            cgo.transform.SetParent(transform, false);
            CustomStation = cgo.AddComponent<AudioSource>();
            CustomStation.loop = true;
            CustomStation.playOnAwake = false;
            cgo.AddComponent<AudioLowPassFilter>().cutoffFrequency = 4500;

            ClickClip = Make("click", 0.05f, sr, (t, n) => n * Mathf.Exp(-t * 200f) * 0.5f);
            TelegraphClip = Make("telegraph", 1.6f, sr, (t, n) =>
            {
                float s = 0;
                foreach (var start in new[] { 0f, 0.35f })
                {
                    float u = t - start;
                    if (u < 0) continue;
                    s += (Mathf.Sin(u * 2093f * 6.283f) + 0.5f * Mathf.Sin(u * 2960f * 6.283f) + 0.3f * Mathf.Sin(u * 5120f * 6.283f)) * Mathf.Exp(-u * 4f);
                }
                return s * 0.25f;
            });
            SquelchClip = Make("squelch", 0.22f, sr, (t, n) => n * (t < 0.2f ? 0.35f : 0) * (1 - t / 0.22f));
            float seed = 0;
            SlamClip = Make("slam", 2.2f, sr, (t, n) =>
            {
                seed = seed * 0.997f + n * 0.05f;
                float thump = Mathf.Sin(6.283f * (55f * t - 8f * t * t)) * Mathf.Exp(-t * 3.5f);
                return (thump * 0.9f + seed * 2.5f * Mathf.Exp(-t * 1.5f)) * 0.8f;
            });
            float sc = 0;
            ScrapeClip = Make("scrape", 5f, sr, (t, n) =>
            {
                sc = sc * 0.99f + n * 0.1f;
                float grind = Mathf.PerlinNoise(t * 40f, 0.3f);
                float env = Mathf.Clamp01(t * 2f) * Mathf.Clamp01((5f - t) * 0.8f);
                return (sc * 2.5f * grind + Mathf.Sin(t * 6.283f * (38f + grind * 9f)) * 0.4f) * env;
            });
            // steel letting go: a snap, then the hull ringing
            CrackClip = Make("crack", 3f, sr, (t, n) =>
            {
                float snap = n * Mathf.Exp(-t * 22f) * 1.4f;
                float ring = Mathf.Sin(t * 6.283f * 143f) * Mathf.Exp(-t * 2.6f) * 0.5f + Mathf.Sin(t * 6.283f * 97f) * Mathf.Exp(-t * 1.8f) * 0.45f;
                float thud = Mathf.Sin(t * 6.283f * 42f) * Mathf.Exp(-t * 5f) * 0.9f;
                return Mathf.Clamp(snap + ring + thud, -1f, 1f);
            });
            float gp = 0;
            GroanClip = Make("groan", 3.5f, sr, (t, n) =>
            {
                float f = 48f + Mathf.Sin(t * 1.3f) * 9f + Mathf.PerlinNoise(t * 3f, 0) * 14f;
                gp += f / sr;
                float saw = (gp % 1f) * 2 - 1;
                float env = Mathf.Sin(Mathf.Clamp01(t / 3.5f) * Mathf.PI);
                return (saw * 0.3f + n * 0.05f) * env;
            });
            PhoneClip = Make("phone", 0.9f, sr, (t, n) => (Mathf.Sin(t * 6.283f * 70f) > 0 ? 0.18f : -0.18f) * ((int)(t * 6) % 2 == 0 ? 1 : 0));
            BellClip = Make("bell", 6f, sr, (t, n) =>
            {
                float f = 392f;
                return (Mathf.Sin(t * f * 6.283f) * Mathf.Exp(-t * 0.6f)
                    + 0.6f * Mathf.Sin(t * f * 2.0f * 6.283f) * Mathf.Exp(-t * 0.9f)
                    + 0.45f * Mathf.Sin(t * f * 2.76f * 6.283f) * Mathf.Exp(-t * 1.4f)
                    + 0.25f * Mathf.Sin(t * f * 5.4f * 6.283f) * Mathf.Exp(-t * 2.5f)
                    + 0.3f * Mathf.Sin(t * f * 0.5f * 6.283f) * Mathf.Exp(-t * 0.4f)) * 0.22f * Mathf.Clamp01(t * 400f);
            });
            float cr = 0;
            CrashClip = Make("crash", 6f, sr, (t, n) =>
            {
                cr = cr * 0.998f + n * 0.04f;
                return (cr * 3f + n * 0.3f * Mathf.Exp(-t * 2f)) * Mathf.Exp(-t * 0.5f);
            });
        }

        T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        static AudioClip Make(string name, float seconds, int sr, Func<float, float, float> f)
        {
            int n = (int)(seconds * sr);
            var d = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++) d[i] = f(i / (float)sr, (float)(rng.NextDouble() * 2 - 1));
            var c = AudioClip.Create(name, n, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }

        public void Play(AudioClip c, float vol = 1f) { if (c != null) Sfx.PlayOneShot(c, vol * SfxVolume); }
        public void Click() => Play(ClickClip, 0.6f);

        public void Whistle(float seconds) => _whistleUntil = Mathf.Max(_whistleUntil, Time.unscaledTime + seconds);

        public void SetVoiceStyle(Channel ch)
        {
            bool radio = ch == Channel.Radio || ch == Channel.Phone;
            _voiceHp.enabled = radio;
            _voiceLp.enabled = radio;
            _voiceDist.enabled = ch == Channel.Radio;
            _voiceHp.cutoffFrequency = ch == Channel.Radio ? 400 : 300;
            _voiceLp.cutoffFrequency = ch == Channel.Radio ? 2800 : 3200;
            _voiceDist.distortionLevel = 0.35f;
        }

        public void Tick(bool holdWhistle)
        {
            WhistleSynth.On = holdWhistle || Time.unscaledTime < _whistleUntil;
            Voice.volume = VoiceVolume;
            AmVoice.volume = VoiceVolume;
        }
    }
}
