using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Rough coastlines of eastern Lake Superior, in nautical miles east/north of the
    /// wreck site (46°59.9'N, 85°06.6'W). Good enough for a radar picture and a chart.
    /// </summary>
    public static class Charts
    {
        const double OriginLat = 46.998, OriginLon = -85.110;

        public static Vector2 ToNm(double lat, double lon)
        {
            double north = (lat - OriginLat) * 60.0;
            double east = (lon - OriginLon) * 60.0 * System.Math.Cos(OriginLat * System.Math.PI / 180.0);
            return new Vector2((float)east, (float)north);
        }

        public static readonly double[][] Michipicoten = { new[] { 47.73, -85.98 }, new[] { 47.78, -85.85 }, new[] { 47.80, -85.70 }, new[] { 47.77, -85.60 }, new[] { 47.72, -85.70 }, new[] { 47.70, -85.88 }, new[] { 47.73, -85.98 } };
        public static readonly double[][] Caribou = { new[] { 47.36, -85.84 }, new[] { 47.355, -85.80 }, new[] { 47.315, -85.79 }, new[] { 47.32, -85.84 }, new[] { 47.36, -85.84 } };
        public static readonly double[][] Ontario = { new[] { 48.20, -86.00 }, new[] { 48.05, -85.40 }, new[] { 48.00, -84.88 }, new[] { 47.85, -84.92 }, new[] { 47.70, -84.97 }, new[] { 47.60, -85.03 }, new[] { 47.48, -84.98 }, new[] { 47.40, -84.88 }, new[] { 47.28, -84.75 }, new[] { 47.15, -84.78 }, new[] { 47.06, -84.78 }, new[] { 46.98, -84.80 }, new[] { 46.85, -84.72 }, new[] { 46.70, -84.60 }, new[] { 46.55, -84.57 } };
        public static readonly double[][] Michigan = { new[] { 46.48, -84.63 }, new[] { 46.56, -84.85 }, new[] { 46.65, -84.98 }, new[] { 46.77, -84.96 }, new[] { 46.765, -85.10 }, new[] { 46.75, -85.30 }, new[] { 46.70, -85.60 }, new[] { 46.67, -85.98 }, new[] { 46.64, -86.30 }, new[] { 46.60, -86.70 } };

        public static readonly Vector2 WhitefishPoint = ToNm(46.771, -84.957);
        public static readonly Vector2 CaribouIsland = ToNm(47.337, -85.815);
        public static readonly Vector2 SixFathomShoal = ToNm(47.385, -85.78);
        public static readonly Vector2 StartPosition = new(-33.4f, 26.5f);   // the line to Whitefish passes Caribou close aboard

        static List<Vector2> _landPoints;

        /// <summary>Points along every coast, about every 0.25 nm, for radar echoes.</summary>
        public static List<Vector2> LandPoints
        {
            get
            {
                if (_landPoints != null) return _landPoints;
                _landPoints = new List<Vector2>();
                foreach (var line in new[] { Michipicoten, Caribou, Ontario, Michigan })
                    for (int i = 1; i < line.Length; i++)
                    {
                        var a = ToNm(line[i - 1][0], line[i - 1][1]);
                        var b = ToNm(line[i][0], line[i][1]);
                        int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.25f));
                        for (int k = 0; k < n; k++) _landPoints.Add(Vector2.Lerp(a, b, k / (float)n));
                    }
                return _landPoints;
            }
        }

        public static float DepthFathoms(Vector2 nm)
        {
            float toShoal = Vector2.Distance(nm, SixFathomShoal);
            float toCaribou = Vector2.Distance(nm, CaribouIsland);
            float d = 95f + Mathf.Sin(nm.x * 0.3f) * 12f + Mathf.Sin(nm.y * 0.21f) * 9f;
            d = Mathf.Min(d, 6f + toShoal * 22f);
            d = Mathf.Min(d, 1f + toCaribou * 18f);
            float toWhitefish = Vector2.Distance(nm, WhitefishPoint);
            d = Mathf.Min(d, 3f + toWhitefish * 6f);
            return Mathf.Max(1f, d);
        }

        // ---- the paper chart on the chart table ----

        const double ChartLatMin = 46.45, ChartLatMax = 48.05, ChartLonMin = -86.45, ChartLonMax = -84.45;

        public static Vector2 ChartUv(Vector2 nm)
        {
            Vector2 min = ToNm(ChartLatMin, ChartLonMin), max = ToNm(ChartLatMax, ChartLonMax);
            return new Vector2(Mathf.InverseLerp(min.x, max.x, nm.x), Mathf.InverseLerp(min.y, max.y, nm.y));
        }

        public static Painter DrawChartBase(int size)
        {
            // a lake chart the way the survey prints them: white deep water, blue near the shore, buff land
            var water = new Color(0.93f, 0.92f, 0.88f);
            var p = new Painter(size, water);
            var ink = new Color(0.2f, 0.18f, 0.17f);
            var land = new Color(0.9f, 0.82f, 0.6f);
            // fill land crudely: everything beyond the Ontario/Michigan shore lines
            FillBeyond(p, Ontario, true, land);
            FillBeyond(p, Michigan, false, land);
            foreach (var poly in new[] { Michipicoten, Caribou }) FillPoly(p, poly, land);
            var near = ShoreDistance(p, land, size / 28);
            for (int i = 0; i < p.Px.Length; i++)
                if (near[i] > 0) p.Px[i] = Color.Lerp(water, new Color(0.66f, 0.79f, 0.86f), Mathf.Clamp01(near[i] * 2.2f));
            Soundings(p, near, ink);
            foreach (var line in new[] { Michipicoten, Caribou, Ontario, Michigan })
                for (int i = 1; i < line.Length; i++)
                    p.Line(ChartUv(ToNm(line[i - 1][0], line[i - 1][1])), ChartUv(ToNm(line[i][0], line[i][1])), 0.003f, ink);
            // grid
            for (int i = 1; i < 8; i++)
            {
                p.Line(new Vector2(i / 8f, 0), new Vector2(i / 8f, 1), 0.0012f, new Color(0.4f, 0.45f, 0.5f, 0.5f));
                p.Line(new Vector2(0, i / 8f), new Vector2(1, i / 8f), 0.0012f, new Color(0.4f, 0.45f, 0.5f, 0.5f));
            }
            p.Text("LAKE SUPERIOR", new Vector2(0.38f, 0.62f), 0.03f, new Color(0.2f, 0.3f, 0.45f));
            p.Text("EASTERN PART", new Vector2(0.38f, 0.585f), 0.016f, new Color(0.2f, 0.3f, 0.45f));
            p.Text("WHITEFISH PT", ChartUv(WhitefishPoint) + new Vector2(0.02f, -0.03f), 0.016f, ink);
            p.Text("CARIBOU I", ChartUv(CaribouIsland) + new Vector2(-0.07f, 0.0f), 0.016f, ink);
            p.Text("MICHIPICOTEN I", ChartUv(ToNm(47.75, -85.78)) + new Vector2(0, 0.045f), 0.016f, ink);
            p.Text("ONTARIO", ChartUv(ToNm(47.7, -84.7)), 0.02f, ink);
            p.Text("MICHIGAN", ChartUv(ToNm(46.55, -85.6)), 0.02f, ink);
            p.Text("6 FM", ChartUv(SixFathomShoal) + new Vector2(0.03f, 0.01f), 0.012f, new Color(0.6f, 0.1f, 0.1f));
            p.Ring(ChartUv(SixFathomShoal), 0.006f, 0.009f, new Color(0.6f, 0.1f, 0.1f));
            // compass rose
            var rc = new Vector2(0.15f, 0.25f);
            p.Ring(rc, 0.085f, 0.088f, new Color(0.55f, 0.15f, 0.15f));
            p.Ticks(rc, 0.085f, 0, 360, 36, 0.012f, 0.0015f, new Color(0.55f, 0.15f, 0.15f));
            p.Text("N", rc + new Vector2(0, 0.1f), 0.016f, new Color(0.55f, 0.15f, 0.15f));
            p.Ring(rc, 0.06f, 0.0615f, new Color(0.55f, 0.15f, 0.15f));
            p.Ticks(rc, 0.06f, 0, 360, 72, 0.006f, 0.001f, new Color(0.55f, 0.15f, 0.15f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45 * Mathf.Deg2Rad, len = i % 2 == 0 ? 0.055f : 0.035f;
                p.Line(rc, rc + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * len, i % 2 == 0 ? 0.004f : 0.0025f, new Color(0.55f, 0.15f, 0.15f));
            }
            p.Disc(rc, 0.006f, new Color(0.55f, 0.15f, 0.15f));
            Neatline(p, ink);
            PaperFinish(p);
            return p;
        }

        /// <summary>
        /// For each water pixel, how close the nearest land is: 1 on the shore falling to 0
        /// at <paramref name="reach"/> pixels out (land itself is -1). A box blur of the land mask, run twice.
        /// </summary>
        static float[] ShoreDistance(Painter p, Color land, int reach)
        {
            int n = p.Size;
            Color32 l = land;
            var m = new float[n * n];
            for (int i = 0; i < m.Length; i++) m[i] = p.Px[i].r == l.r && p.Px[i].g == l.g && p.Px[i].b == l.b ? 1 : 0;
            var b = (float[])m.Clone();
            var tmp = new float[n * n];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 0; y < n; y++)
                {
                    float sum = 0;
                    for (int x = -reach; x < n + reach; x++)
                    {
                        if (x + reach < n && x + reach >= 0) sum += b[y * n + x + reach];
                        if (x - reach - 1 >= 0 && x - reach - 1 < n) sum -= b[y * n + x - reach - 1];
                        if (x >= 0 && x < n) tmp[y * n + x] = sum / (2 * reach + 1);
                    }
                }
                for (int x = 0; x < n; x++)
                {
                    float sum = 0;
                    for (int y = -reach; y < n + reach; y++)
                    {
                        if (y + reach < n && y + reach >= 0) sum += tmp[(y + reach) * n + x];
                        if (y - reach - 1 >= 0 && y - reach - 1 < n) sum -= tmp[(y - reach - 1) * n + x];
                        if (y >= 0 && y < n) b[y * n + x] = sum / (2 * reach + 1);
                    }
                }
            }
            for (int i = 0; i < m.Length; i++) b[i] = m[i] > 0 ? -1 : Mathf.Clamp01(b[i] * 2.5f);
            return b;
        }

        /// <summary>Depths in fathoms scattered over the water, shoaler towards the land.</summary>
        static void Soundings(Painter p, float[] near, Color ink)
        {
            var rng = new System.Random(1975);
            var col = new Color(ink.r, ink.g, ink.b, 0.75f);
            int n = p.Size, placed = 0;
            for (int tries = 0; tries < 4000 && placed < 140; tries++)
            {
                float u = (float)rng.NextDouble() * 0.9f + 0.05f, v = (float)rng.NextDouble() * 0.9f + 0.05f;
                // keep clear of the titles and the compass rose
                if (u > 0.33f && u < 0.72f && v > 0.56f && v < 0.66f) continue;
                if (Vector2.Distance(new Vector2(u, v), new Vector2(0.15f, 0.25f)) < 0.12f) continue;
                int x = (int)(u * n), y = (int)(v * n);
                float d = near[y * n + x];
                if (d < 0) continue;
                // only lay a sounding where the water around it is open for its width
                if (near[y * n + Mathf.Min(n - 1, x + n / 60)] < 0 || near[y * n + Mathf.Max(0, x - n / 60)] < 0) continue;
                int fathoms = d > 0.02f ? 2 + (int)((1 - d) * 30 + rng.Next(0, 6)) : 60 + rng.Next(0, 110);
                p.Text(fathoms.ToString(), new Vector2(u, v), 0.0095f, col);
                placed++;
            }
        }

        /// <summary>Double border with alternating minute bars, the way a printed chart is framed.</summary>
        static void Neatline(Painter p, Color ink)
        {
            const float o = 0.012f, i = 0.024f;
            foreach (var e in new[] { o, i })
            {
                p.Line(new Vector2(e, e), new Vector2(1 - e, e), 0.0016f, ink);
                p.Line(new Vector2(e, 1 - e), new Vector2(1 - e, 1 - e), 0.0016f, ink);
                p.Line(new Vector2(e, e), new Vector2(e, 1 - e), 0.0016f, ink);
                p.Line(new Vector2(1 - e, e), new Vector2(1 - e, 1 - e), 0.0016f, ink);
            }
            for (int k = 0; k < 40; k += 2)
            {
                float a = i + k * (1 - 2 * i) / 40f, b = i + (k + 1) * (1 - 2 * i) / 40f, m = (o + i) / 2;
                p.Line(new Vector2(a, m), new Vector2(b, m), o - 0.002f, ink);
                p.Line(new Vector2(a, 1 - m), new Vector2(b, 1 - m), o - 0.002f, ink);
                p.Line(new Vector2(m, a), new Vector2(m, b), o - 0.002f, ink);
                p.Line(new Vector2(1 - m, a), new Vector2(1 - m, b), o - 0.002f, ink);
            }
        }

        /// <summary>Handled paper: fibre, a cross of fold creases, foxing, a mug ring and edges gone yellow.</summary>
        static void PaperFinish(Painter p)
        {
            int n = p.Size;
            var age = new Color(0.78f, 0.68f, 0.48f);
            var mug = new Vector2(0.86f, 0.18f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                    Color c = p.Px[y * n + x];
                    float lum = 1f + (Surfaces.Fbm(u, v, 0.004f, 21, 2) - 0.5f) * 0.06f + (Surfaces.Fbm(u, v, 0.3f, 22, 4) - 0.5f) * 0.07f;
                    // the folds it came in: a dark valley with a lit ridge beside it
                    float fu = Mathf.Abs(u - 0.5f), fv = Mathf.Abs(v - 0.5f);
                    if (fu < 0.0015f || fv < 0.0015f) lum *= 0.84f;
                    else if (fu < 0.004f || fv < 0.004f) lum *= 1.04f;
                    // yellowed towards the edges, foxing in spots
                    float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                    float yellow = Mathf.Clamp01(1 - edge / 0.07f) * 0.35f + (Surfaces.Fbm(u, v, 0.25f, 23, 3) - 0.5f) * 0.12f;
                    float fox = Surfaces.Fbm(u, v, 0.012f, 24, 2);
                    if (fox > 0.72f) yellow += (fox - 0.72f) * 2.5f;
                    // a mug ring, a darker rim round a faint stain
                    float r = Vector2.Distance(new Vector2(u, v), mug);
                    float ring = Mathf.Clamp01(1 - Mathf.Abs(r - 0.045f) / 0.0035f) * 0.35f + (r < 0.045f ? 0.05f : 0);
                    c = Color.Lerp(c * lum, age, Mathf.Clamp01(yellow));
                    c = Color.Lerp(c, new Color(0.5f, 0.33f, 0.16f), ring * (0.6f + Surfaces.Fbm(u, v, 0.02f, 25, 2) * 0.6f));
                    c.a = 1;
                    p.Px[y * n + x] = c;
                }
        }

        static void FillBeyond(Painter p, double[][] line, bool east, Color c)
        {
            // For each row, fill from the coastline to the chart edge.
            for (int y = 0; y < p.Size; y++)
            {
                float v = (y + 0.5f) / p.Size;
                float? edge = null;
                for (int i = 1; i < line.Length; i++)
                {
                    var a = ChartUv(ToNm(line[i - 1][0], line[i - 1][1]));
                    var b = ChartUv(ToNm(line[i][0], line[i][1]));
                    if ((v >= a.y && v <= b.y) || (v <= a.y && v >= b.y))
                    {
                        float t = Mathf.Abs(b.y - a.y) < 1e-6f ? 0 : (v - a.y) / (b.y - a.y);
                        edge = Mathf.Lerp(a.x, b.x, t);
                        break;
                    }
                }
                if (edge == null) continue;
                if (east)
                {
                    p.Rect(new Vector2(edge.Value, v - 0.5f / p.Size), new Vector2(1, v + 0.5f / p.Size), c);
                }
                else
                {
                    // Michigan: land is south of the line, so fill columns instead
                }
            }
            if (!east)
            {
                for (int x = 0; x < p.Size; x++)
                {
                    float u = (x + 0.5f) / p.Size;
                    for (int i = 1; i < line.Length; i++)
                    {
                        var a = ChartUv(ToNm(line[i - 1][0], line[i - 1][1]));
                        var b = ChartUv(ToNm(line[i][0], line[i][1]));
                        if ((u >= a.x && u <= b.x) || (u <= a.x && u >= b.x))
                        {
                            float t = Mathf.Abs(b.x - a.x) < 1e-6f ? 0 : (u - a.x) / (b.x - a.x);
                            float edge = Mathf.Lerp(a.y, b.y, t);
                            p.Rect(new Vector2(u - 0.5f / p.Size, 0), new Vector2(u + 0.5f / p.Size, edge), c);
                            break;
                        }
                    }
                }
            }
        }

        static void FillPoly(Painter p, double[][] poly, Color c)
        {
            var pts = new List<Vector2>();
            foreach (var q in poly) pts.Add(ChartUv(ToNm(q[0], q[1])));
            for (int y = 0; y < p.Size; y++)
            {
                float v = (y + 0.5f) / p.Size;
                var xs = new List<float>();
                for (int i = 1; i < pts.Count; i++)
                {
                    var a = pts[i - 1];
                    var b = pts[i];
                    if ((v >= a.y && v < b.y) || (v < a.y && v >= b.y))
                        xs.Add(Mathf.Lerp(a.x, b.x, (v - a.y) / (b.y - a.y)));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    p.Rect(new Vector2(xs[k], v - 0.5f / p.Size), new Vector2(xs[k + 1], v + 0.5f / p.Size), c);
            }
        }
    }
}
