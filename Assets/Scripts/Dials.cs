using UnityEngine;

namespace Fitz
{
    /// <summary>Instrument faces, painted once at start-up.</summary>
    public static class Dials
    {
        static readonly Color Ink = new(0.06f, 0.06f, 0.06f);
        static readonly Color Face = new(0.9f, 0.88f, 0.8f);
        static readonly Vector2 C = new(0.5f, 0.5f);

        static Painter Blank(Color? bg = null)
        {
            var p = new Painter(512, new Color(0, 0, 0, 0));
            p.Disc(C, 0.5f, bg ?? Face);
            return p;
        }

        public static Texture2D RudderAngle()
        {
            var p = Blank(new Color(0.08f, 0.08f, 0.08f));
            var white = new Color(0.92f, 0.92f, 0.9f);
            // port in red, starboard in green
            for (int i = 0; i < 40; i++)
            {
                p.Line(C + Painter.Dir(-70 + i * 1.75f) * 0.36f, C + Painter.Dir(-70 + (i + 1) * 1.75f) * 0.36f, 0.035f, new Color(0.75f, 0.08f, 0.06f));
                p.Line(C + Painter.Dir(i * 1.75f) * 0.36f, C + Painter.Dir((i + 1) * 1.75f) * 0.36f, 0.035f, new Color(0.08f, 0.6f, 0.15f));
            }
            p.Ticks(C, 0.43f, -70, 70, 16, 0.04f, 0.006f, white);
            p.Ticks(C, 0.43f, -70, 70, 8, 0.07f, 0.01f, white);
            for (int v = -40; v <= 40; v += 10)
                p.Text(Mathf.Abs(v).ToString(), C + Painter.Dir(v * 1.75f) * 0.27f, 0.05f, white);
            p.Text("PORT", new Vector2(0.28f, 0.28f), 0.045f, new Color(0.9f, 0.3f, 0.25f));
            p.Text("STBD", new Vector2(0.72f, 0.28f), 0.045f, new Color(0.3f, 0.9f, 0.4f));
            p.Text("RUDDER", new Vector2(0.5f, 0.17f), 0.04f, white);
            return p.ToTexture();
        }

        public static Texture2D GyroCard()
        {
            var p = Blank(new Color(0.1f, 0.1f, 0.1f));
            var white = new Color(0.95f, 0.95f, 0.92f);
            p.Ticks(C, 0.48f, 0, 360, 72, 0.03f, 0.004f, white);
            p.Ticks(C, 0.48f, 0, 360, 36, 0.06f, 0.008f, white);
            for (int d = 0; d < 360; d += 30)
            {
                string s = d switch { 0 => "N", 90 => "E", 180 => "S", 270 => "W", _ => (d / 10).ToString() };
                var col = d == 0 ? new Color(1f, 0.3f, 0.25f) : white;
                p.Text(s, C + Painter.Dir(d) * 0.34f, d % 90 == 0 ? 0.08f : 0.055f, col, d);
            }
            p.Ring(C, 0.0f, 0.03f, white);
            p.Text("GYRO", C + new Vector2(0, -0.12f), 0.035f, new Color(0.7f, 0.7f, 0.65f));
            return p.ToTexture();
        }

        public static Texture2D Clinometer()
        {
            var p = Blank();
            // arc hangs below the pivot, which sits high on the face
            var pivot = new Vector2(0.5f, 0.62f);
            for (int v = -40; v <= 40; v += 5)
            {
                float a = 180 - v * 1.75f;
                float len = v % 10 == 0 ? 0.06f : 0.03f;
                p.Line(pivot + Painter.Dir(a) * (0.5f - len), pivot + Painter.Dir(a) * 0.5f, v % 10 == 0 ? 0.008f : 0.004f, Ink);
                if (v % 10 == 0) p.Text(Mathf.Abs(v).ToString(), pivot + Painter.Dir(a) * 0.38f, 0.04f, Ink);
            }
            p.Line(pivot + Painter.Dir(180 - 40 * 1.75f) * 0.5f, pivot + Painter.Dir(180 + 40 * 1.75f) * 0.5f, 0.0f, Ink);
            p.Text("PORT", new Vector2(0.78f, 0.24f), 0.04f, new Color(0.7f, 0.1f, 0.1f));
            p.Text("STBD", new Vector2(0.22f, 0.24f), 0.04f, new Color(0.1f, 0.5f, 0.15f));
            p.Text("CLINOMETER", new Vector2(0.5f, 0.8f), 0.038f, Ink);
            return p.ToTexture();
        }

        public static Texture2D Clock()
        {
            var p = Blank(new Color(0.94f, 0.93f, 0.88f));
            p.Ticks(C, 0.46f, 0, 360, 60, 0.025f, 0.004f, Ink);
            p.Ticks(C, 0.46f, 0, 360, 12, 0.06f, 0.012f, Ink);
            for (int h = 1; h <= 12; h++) p.Text(h.ToString(), C + Painter.Dir(h * 30) * 0.33f, 0.07f, Ink);
            p.Text("SHIPS TIME", C + new Vector2(0, -0.17f), 0.03f, new Color(0.3f, 0.3f, 0.3f));
            return p.ToTexture();
        }

        static Texture2D Gauge(float min, float max, int majors, int minorsPer, string title, string unit, float labelScale = 1f, System.Func<float, string> label = null)
        {
            var p = Blank();
            p.Ticks(C, 0.46f, -135, 135, majors * minorsPer, 0.03f, 0.004f, Ink);
            p.Ticks(C, 0.46f, -135, 135, majors, 0.07f, 0.01f, Ink);
            for (int i = 0; i <= majors; i++)
            {
                float v = Mathf.Lerp(min, max, i / (float)majors);
                p.Text(label != null ? label(v) : v.ToString("0"), C + Painter.Dir(Mathf.Lerp(-135, 135, i / (float)majors)) * 0.32f, 0.055f * labelScale, Ink);
            }
            p.Text(title, C + new Vector2(0, -0.16f), 0.045f, Ink);
            p.Text(unit, C + new Vector2(0, -0.24f), 0.032f, new Color(0.3f, 0.3f, 0.3f));
            return p.ToTexture();
        }

        public static Texture2D Rpm() => Gauge(0, 120, 6, 4, "SHAFT", "RPM");
        public static Texture2D Anemometer() => Gauge(0, 100, 5, 4, "WIND", "KNOTS");
        public static Texture2D Fathometer() => Gauge(0, 120, 6, 4, "DEPTH", "FATHOMS");

        public static Texture2D Barometer()
        {
            var p = Blank(new Color(0.92f, 0.9f, 0.82f));
            p.Ticks(C, 0.46f, -135, 135, 30, 0.03f, 0.004f, Ink);
            p.Ticks(C, 0.46f, -135, 135, 3, 0.07f, 0.01f, Ink);
            string[] nums = { "28", "29", "30", "31" };
            for (int i = 0; i < 4; i++) p.Text(nums[i], C + Painter.Dir(-135 + i * 90) * 0.33f, 0.06f, Ink);
            p.Text("STORMY", C + Painter.Dir(-100) * 0.22f, 0.03f, new Color(0.5f, 0.1f, 0.1f));
            p.Text("RAIN", C + Painter.Dir(-35) * 0.22f, 0.03f, Ink);
            p.Text("CHANGE", C + Painter.Dir(15) * 0.2f, 0.03f, Ink);
            p.Text("FAIR", C + Painter.Dir(80) * 0.22f, 0.03f, Ink);
            p.Text("INCHES", C + new Vector2(0, -0.22f), 0.03f, new Color(0.3f, 0.3f, 0.3f));
            return p.ToTexture();
        }

        public static Texture2D TelegraphFace()
        {
            var p = Blank(new Color(0.92f, 0.9f, 0.84f));
            string[] names = { "FULL", "HALF", "SLOW", "STOP", "SLOW", "HALF", "FULL" };
            for (int i = 0; i < 7; i++)
            {
                float a = (i - 3) * 30f;
                var col = i < 3 ? new Color(0.65f, 0.1f, 0.08f) : i == 3 ? Ink : new Color(0.1f, 0.3f, 0.12f);
                p.Text(names[i], C + Painter.Dir(a) * 0.37f, 0.045f, col, a);
                p.Line(C + Painter.Dir(a + 15) * 0.3f, C + Painter.Dir(a + 15) * 0.47f, 0.006f, Ink);
            }
            p.Line(C + Painter.Dir(-105) * 0.3f, C + Painter.Dir(-105) * 0.47f, 0.006f, Ink);
            p.Text("ASTERN", C + new Vector2(-0.2f, -0.12f), 0.04f, new Color(0.65f, 0.1f, 0.08f));
            p.Text("AHEAD", C + new Vector2(0.2f, -0.12f), 0.04f, new Color(0.1f, 0.3f, 0.12f));
            p.Text("ENGINE ROOM", C + new Vector2(0, -0.26f), 0.032f, Ink);
            return p.ToTexture();
        }

        public static Texture2D AmDial()
        {
            var p = new Painter(512, new Color(0.85f, 0.78f, 0.6f));
            var ink = new Color(0.2f, 0.15f, 0.1f);
            for (int k = 0; k <= 22; k++)
            {
                float u = Mathf.Lerp(0.04f, 0.96f, k / 22f);
                p.Line(new Vector2(u, 0.62f), new Vector2(u, k % 2 == 0 ? 0.74f : 0.69f), 0.004f, ink);
            }
            int[] labels = { 55, 60, 70, 80, 100, 120, 140, 160 };
            foreach (var l in labels)
            {
                float u = Mathf.Lerp(0.04f, 0.96f, Mathf.InverseLerp(540f, 1600f, l * 10f));
                p.Text(l.ToString(), new Vector2(u, 0.45f), 0.11f, ink);
            }
            p.Text("KC", new Vector2(0.95f, 0.85f), 0.08f, ink);
            p.Text("BROADCAST", new Vector2(0.5f, 0.88f), 0.08f, ink);
            return p.ToTexture();
        }
    }

    /// <summary>A head-up PPI radar display with phosphor persistence.</summary>
    public class RadarScope
    {
        public readonly Texture2D Texture;
        public Material Material;
        public float RangeNm = 12;
        readonly int _n;
        readonly float[] _glow;
        readonly Color32[] _px;
        float _sweep;
        float _uploadTimer;
        readonly System.Random _rng = new(29);

        public RadarScope(int size)
        {
            _n = size;
            _glow = new float[size * size];
            _px = new Color32[size * size];
            Texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Texture.SetPixels32(_px);
            Texture.Apply();
        }

        void Paint(float bearingRel, float rangeNm, float strength, int radius = 1)
        {
            if (rangeNm > RangeNm || rangeNm < 0) return;
            float r = rangeNm / RangeNm * 0.47f;
            var d = Painter.Dir(bearingRel);
            int cx = Mathf.RoundToInt((0.5f + d.x * r) * _n), cy = Mathf.RoundToInt((0.5f + d.y * r) * _n);
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    int px = cx + x, py = cy + y;
                    if (px < 0 || py < 0 || px >= _n || py >= _n) continue;
                    int i = py * _n + px;
                    _glow[i] = Mathf.Min(1.4f, _glow[i] + strength);
                }
        }

        static bool InSweep(float a, float from, float to)
        {
            float span = Mathf.Repeat(to - from, 360f);
            return Mathf.Repeat(a - from, 360f) <= span;
        }

        public void Tick(float dt, bool on, Ship ship, Story story)
        {
            float decay = Mathf.Pow(0.45f, dt);
            for (int i = 0; i < _glow.Length; i++) _glow[i] *= decay;

            if (on)
            {
                float from = _sweep;
                _sweep = Mathf.Repeat(_sweep + dt * 360f / 2.6f, 360f);
                float to = _sweep;
                float heading = ship.Heading;

                foreach (var pt in Charts.LandPoints)
                {
                    var rel = pt - ship.NavNm;
                    float range = rel.magnitude;
                    if (range > RangeNm) continue;
                    float brg = Mathf.Atan2(rel.x, rel.y) * Mathf.Rad2Deg - heading;
                    if (InSweep(Mathf.Repeat(brg, 360f), from, to)) Paint(brg, range, 0.9f, 1);
                }
                foreach (var target in story.RadarTargets())
                {
                    var rel = target - ship.NavNm;
                    float brg = Mathf.Atan2(rel.x, rel.y) * Mathf.Rad2Deg - heading;
                    if (InSweep(Mathf.Repeat(brg, 360f), from, to)) Paint(brg, rel.magnitude, 1.2f, 1);
                }
                // sea and snow clutter
                float storm = ship.Ocean.Storm;
                int clutter = (int)(dt * 900 * (0.4f + storm));
                for (int k = 0; k < clutter; k++)
                {
                    float a = from + (float)_rng.NextDouble() * Mathf.Repeat(to - from, 360f);
                    float rr = (float)(_rng.NextDouble() * _rng.NextDouble()) * RangeNm * (0.15f + storm * 0.2f);
                    Paint(a, rr, 0.35f, 0);
                }
                // the sweep itself
                var dir = Painter.Dir(to);
                for (int s = 0; s < _n / 2; s++)
                {
                    float r = s / (float)_n;
                    int px = Mathf.RoundToInt((0.5f + dir.x * r) * _n), py = Mathf.RoundToInt((0.5f + dir.y * r) * _n);
                    if (px >= 0 && py >= 0 && px < _n && py < _n) _glow[py * _n + px] = Mathf.Max(_glow[py * _n + px], 0.55f);
                }
            }

            _uploadTimer -= dt;
            if (_uploadTimer > 0) return;
            _uploadTimer = 1f / 30f;
            float half = _n / 2f;
            for (int y = 0; y < _n; y++)
                for (int x = 0; x < _n; x++)
                {
                    int i = y * _n + x;
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float rr = Mathf.Sqrt(dx * dx + dy * dy);
                    if (rr > 0.96f) { _px[i] = new Color32(0, 0, 0, 255); continue; }
                    float g = _glow[i];
                    if (on)
                    {
                        // range rings and heading flash
                        float ring = Mathf.Abs(Mathf.Repeat(rr * 3f / 0.94f, 1f) - 0.0f);
                        if (ring < 0.025f || (Mathf.Abs(dx) < 0.006f && dy > 0)) g += 0.18f;
                        g += 0.03f;
                    }
                    byte v = (byte)Mathf.Clamp(g * 255f, 0, 255);
                    _px[i] = new Color32((byte)(v * 0.35f), v, (byte)(v * 0.45f), 255);
                }
            Texture.SetPixels32(_px);
            Texture.Apply(false);
        }
    }
}
