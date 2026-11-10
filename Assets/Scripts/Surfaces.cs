using System;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Procedural wear for the pilothouse: mottled enamel, rust running down from the
    /// window corners, a scuffed kick band, the worn track in the linoleum, smoke
    /// stains on the deckhead, tarnish on brass and hammertone on steel. Everything is
    /// painted once at start-up from value noise, so the ship is clean but lived in.
    /// </summary>
    public static class Surfaces
    {
        // ---------------------------------------------------------------- noise

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffffff) / 16777215f;
            }
        }

        static float Noise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal noise in 0..1, centred on 0.5. x, y in metres; scale is the size of the largest feature.</summary>
        public static float Fbm(float x, float y, float scale, int seed, int octaves = 5)
        {
            float v = 0, amp = 0.5f, f = 1f / scale, sum = 0;
            for (int i = 0; i < octaves; i++)
            {
                v += Noise(x * f, y * f, seed + i * 17) * amp;
                sum += amp;
                amp *= 0.5f;
                f *= 2.1f;
            }
            return v / sum;
        }

        static Texture2D Make(Color32[] px, int w, int h, bool tile)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            t.SetPixels32(px);
            t.Apply(true);
            return t;
        }

        static void Blend(Color32[] px, int w, int h, int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= w || y >= h || a <= 0) return;
            px[y * w + x] = Color.Lerp(px[y * w + x], c, Mathf.Clamp01(a));
        }

        // ---------------------------------------------------------------- pictures from the forge

        // Baked by tools/bake_surfaces.py from the forge's texture pictures. Kept as PNG bytes
        // so the pixels can be read here; if they're missing the noise alone is used.
        static bool _picturesLoaded;
        static Color[] _plating, _tiles;
        static int _platingW, _platingH, _tilesW, _tilesH;
        const float PlatingW = 1.5f, PlatingH = 1.26f;   // metres the plating picture covers
        const float TilesSize = 0.6f;                     // two 0.3 m tiles each way

        static void LoadPictures()
        {
            if (_picturesLoaded) return;
            _picturesLoaded = true;
            _plating = LoadPixels("Surfaces/wall_detail", out _platingW, out _platingH);
            _tiles = LoadPixels("Surfaces/floor_tiles", out _tilesW, out _tilesH);
        }

        static Color[] LoadPixels(string path, out int w, out int h)
        {
            w = h = 0;
            var bytes = Resources.Load<TextAsset>(path);
            if (bytes == null) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(bytes.bytes)) return null;
            w = t.width; h = t.height;
            var px = t.GetPixels();
            UnityEngine.Object.Destroy(t);
            return px;
        }

        /// <summary>Bilinear, repeating sample of a picture at u, v in tiles.</summary>
        static Color Sample(Color[] px, int w, int h, float u, float v)
        {
            float x = Mathf.Repeat(u, 1f) * w - 0.5f, y = Mathf.Repeat(v, 1f) * h - 0.5f;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            int xa = (x0 % w + w) % w, xb = (xa + 1) % w, ya = (y0 % h + h) % h, yb = (ya + 1) % h;
            return Color.Lerp(Color.Lerp(px[ya * w + xa], px[ya * w + xb], fx), Color.Lerp(px[yb * w + xa], px[yb * w + xb], fx), fy);
        }

        // ---------------------------------------------------------------- surfaces

        /// <summary>
        /// A painted steel panel seen from inside: metresW by metresH, enamel colour with
        /// slight mottling, scuffs along the bottom, and rust streaks running down from
        /// the given x positions (metres from the left edge), where water finds its way
        /// in at the window corners. With <paramref name="plating"/> (where the panel's
        /// corner sits on its wall, in metres) the riveted plating from the forge picture
        /// runs under the paint, continuous from one panel to the next.
        /// </summary>
        public static Texture2D Paint(float metresW, float metresH, Color paint, int seed, float[] dripsAt = null, float scuff = 0.12f, float grime = 1f, Vector2? plating = null)
        {
            LoadPictures();
            bool plated = plating.HasValue && _plating != null;
            var at = plating ?? Vector2.zero;
            int w = Mathf.Clamp(Mathf.RoundToInt(metresW * 220), 32, 2048), h = Mathf.Clamp(Mathf.RoundToInt(metresH * 220), 16, 1024);
            var px = new Color32[w * h];
            var rng = new System.Random(seed);
            float pxm = metresW / w;    // metres per pixel
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float mx = x * pxm, my = y * pxm;
                    float coarse = Fbm(mx, my, 0.7f, seed) - 0.5f;
                    float fine = Fbm(mx, my, 0.06f, seed + 99, 3) - 0.5f;
                    float lum = 1f + coarse * 0.14f + fine * 0.05f;
                    if (plated) lum *= Mathf.Lerp(1f, Sample(_plating, _platingW, _platingH, (mx + at.x) / PlatingW, (my + at.y) / PlatingH).r * 255f / 160f, 0.85f);
                    // dust and handling darken the paint a little towards the floor
                    if (my < scuff) lum *= Mathf.Lerp(0.78f, 1f, my / scuff) + (Fbm(mx, my, 0.03f, seed + 5, 3) - 0.5f) * 0.25f;
                    px[y * w + x] = paint * lum;
                }
            // rust streaks under leaks
            var rust = new Color(0.42f, 0.2f, 0.07f);
            var stain = new Color(0.3f, 0.26f, 0.2f);
            if (dripsAt != null)
                foreach (var dx in dripsAt)
                {
                    if (rng.NextDouble() > 0.6 * grime) continue;
                    float len = (float)(0.12 + rng.NextDouble() * 0.5) * grime;
                    float wid = (float)(0.007 + rng.NextDouble() * 0.012);
                    float x0 = dx + (float)(rng.NextDouble() - 0.5) * 0.05f;
                    int s2 = rng.Next();
                    for (int y = h - 1; y >= 0; y--)
                    {
                        float down = (h - 1 - y) * pxm;
                        if (down > len) break;
                        float fade = 1f - down / len;
                        float wander = (Fbm(0, down, 0.08f, s2, 3) - 0.5f) * 0.02f;
                        float cx = (x0 + wander) / pxm;
                        float halo = wid * 4 / pxm, core = wid / pxm;
                        for (int x = Mathf.FloorToInt(cx - halo); x <= cx + halo; x++)
                        {
                            float d = Mathf.Abs(x - cx);
                            Blend(px, w, h, x, y, stain, (1 - d / halo) * 0.35f * fade);
                            if (d < core) Blend(px, w, h, x, y, rust, Mathf.Clamp01(1.4f - d / core) * 0.95f * Mathf.Sqrt(fade));
                        }
                    }
                }
            // a few soft grime patches and finger marks
            int spots = Mathf.RoundToInt(3 * grime * metresW);
            for (int i = 0; i < spots; i++)
            {
                float cx = (float)rng.NextDouble() * w, cy = (float)rng.NextDouble() * h;
                float r = (0.04f + (float)rng.NextDouble() * 0.1f) / pxm;
                float a = 0.16f + (float)rng.NextDouble() * 0.2f;
                var dark = paint * 0.6f;
                for (int y = Mathf.FloorToInt(cy - r); y <= cy + r; y++)
                    for (int x = Mathf.FloorToInt(cx - r); x <= cx + r; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                        if (d < 1) Blend(px, w, h, x, y, dark, (1 - d) * (1 - d) * a * Fbm(x * pxm, y * pxm, 0.05f, seed + i, 3) * 2);
                    }
            }
            return Make(px, w, h, false);
        }

        /// <summary>Cream deckhead with smoke yellowing around the light fixtures (positions in metres from the corner).</summary>
        public static Texture2D Ceiling(float metresW, float metresH, Color paint, Vector2[] fixtures, int seed)
        {
            const float ppm = 160;
            int w = Mathf.RoundToInt(metresW * ppm), h = Mathf.RoundToInt(metresH * ppm);
            var px = new Color32[w * h];
            var smoke = new Color(0.55f, 0.47f, 0.32f);
            const float plateW = 1.22f, plateH = 0.61f, pitch = 0.1f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float mx = x / ppm, my = y / ppm;
                    float lum = 1f + (Fbm(mx, my, 0.8f, seed) - 0.5f) * 0.1f;
                    // lapped plates: a dark seam with a lit lip beside it, and a row of rivet heads along each
                    float sx = Mathf.Repeat(mx, plateW), sy = Mathf.Repeat(my, plateH);
                    float seam = Mathf.Min(sx, sy);
                    if (seam < 0.005f) lum *= 0.72f;
                    else if (seam < 0.012f) lum *= 1.06f;
                    float ax = Mathf.Min(sx, plateW - sx), ay = Mathf.Min(sy, plateH - sy);
                    if (ax < 0.04f || ay < 0.04f)
                    {
                        // rivet centres 0.025 m in from the seam, every 0.1 m along it
                        var rc = ax < ay ? new Vector2(ax - 0.025f, Mathf.Repeat(my, pitch) - pitch / 2) : new Vector2(Mathf.Repeat(mx, pitch) - pitch / 2, ay - 0.025f);
                        float d = rc.magnitude / 0.0065f;
                        if (d < 1) lum *= 1.08f - 0.22f * Mathf.Clamp01((rc.x + rc.y) / 0.0065f + 0.3f);
                        else if (d < 1.4f) lum *= 0.9f;
                    }
                    Color c = paint * lum;
                    float s = 0;
                    foreach (var f in fixtures)
                    {
                        float d = Vector2.Distance(new Vector2(mx, my), f);
                        s = Mathf.Max(s, Mathf.Clamp01(1 - d / 0.7f));
                    }
                    s = s * s * (0.5f + Fbm(mx, my, 0.15f, seed + 3, 3) * 0.6f);
                    // and a general dinginess towards the edges where nobody wipes
                    float edge = Mathf.Min(Mathf.Min(mx, metresW - mx), Mathf.Min(my, metresH - my));
                    s = Mathf.Max(s, Mathf.Clamp01(1 - edge / 0.3f) * 0.2f);
                    px[y * w + x] = Color.Lerp(c, smoke, s);
                }
            return Make(px, w, h, false);
        }

        /// <summary>
        /// Dark linoleum with the track worn lighter where people stand and walk: a list
        /// of path segments in metres from the corner, and the places people stand.
        /// </summary>
        public static Texture2D Floor(float metresW, float metresH, Color lino, Vector2[] path, Vector2[] standing, int seed)
        {
            LoadPictures();
            const float ppm = 200;
            int w = Mathf.RoundToInt(metresW * ppm), h = Mathf.RoundToInt(metresH * ppm);
            var px = new Color32[w * h];
            var worn = new Color(0.36f, 0.33f, 0.28f);
            var scuffCol = new Color(0.08f, 0.08f, 0.07f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float mx = x / ppm, my = y / ppm;
                    var p = new Vector2(mx, my);
                    float lum = 1f + (Fbm(mx, my, 0.5f, seed) - 0.5f) * 0.2f + (Fbm(mx, my, 0.04f, seed + 1, 3) - 0.5f) * 0.12f;
                    Color c;
                    // oxblood tiles from the forge picture, each a shade different; otherwise plain sheet lino
                    if (_tiles != null)
                        c = Sample(_tiles, _tilesW, _tilesH, (mx + 0.15f) / TilesSize, my / TilesSize) * lum
                            * (0.9f + Hash(Mathf.FloorToInt((mx + 0.15f) / 0.3f), Mathf.FloorToInt(my / 0.3f), seed) * 0.2f);
                    else
                    {
                        c = lino * lum;
                        // faint seams every 2 m where the sheets meet
                        if (Mathf.Abs(Mathf.Repeat(mx + 0.3f, 2f) - 1f) < 0.004f) c *= 0.8f;
                    }
                    float wear = 0;
                    for (int i = 0; i + 1 < path.Length; i += 2)
                        wear = Mathf.Max(wear, 1 - SegDist(p, path[i], path[i + 1]) / 0.45f);
                    foreach (var s in standing)
                        wear = Mathf.Max(wear, 1 - Vector2.Distance(p, s) / 0.55f);
                    wear = Mathf.Clamp01(wear);
                    wear = wear * wear * (0.5f + Fbm(mx, my, 0.12f, seed + 7, 3) * 0.8f);
                    c = Color.Lerp(c, worn, wear * 0.55f);
                    // black heel marks here and there
                    float mark = Fbm(mx, my, 0.02f, seed + 11, 2);
                    if (mark > 0.68f && Fbm(mx, my, 0.4f, seed + 12, 2) > 0.55f) c = Color.Lerp(c, scuffCol, (mark - 0.68f) * 3f);
                    px[y * w + x] = c;
                }
            return Make(px, w, h, false);
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>A page from the deck notebook: cream, ruled, three holes, a coffee ring.</summary>
        public static Texture2D Paper(int seed)
        {
            const int w = 768, h = 1024;
            var px = new Color32[w * h];
            var cream = new Color(0.93f, 0.9f, 0.8f);
            var rule = new Color(0.55f, 0.65f, 0.8f);
            var margin = new Color(0.85f, 0.45f, 0.45f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fibre = Fbm(x / 90f, y / 90f, 1f, seed, 4) - 0.5f;
                    float grain = Fbm(x / 6f, y / 6f, 1f, seed + 1, 2) - 0.5f;
                    Color c = cream * (1f + fibre * 0.08f + grain * 0.05f);
                    // yellowed at the edges, thumbed at the bottom corner
                    float edge = Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y)) / 60f;
                    c = Color.Lerp(c * 0.86f, c, Mathf.Clamp01(edge));
                    px[y * w + x] = c;
                }
            // rules every 34 px, a red margin, holes down the left
            for (int y = 96; y < h - 40; y += 34)
                for (int x = 28; x < w - 28; x++) Blend(px, w, h, x, h - 1 - y, rule, 0.45f);
            for (int y = 20; y < h - 20; y++) { Blend(px, w, h, 108, y, margin, 0.6f); Blend(px, w, h, 109, y, margin, 0.6f); }
            foreach (var hy in new[] { 0.16f, 0.5f, 0.84f })
            {
                float cy = hy * h, cx = 48;
                for (int y = (int)cy - 16; y <= cy + 16; y++)
                    for (int x = (int)cx - 16; x <= cx + 16; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d < 13) Blend(px, w, h, x, y, new Color(0.08f, 0.07f, 0.06f), 0.95f);
                        else if (d < 15) Blend(px, w, h, x, y, new Color(0.5f, 0.45f, 0.35f), 0.5f);
                    }
            }
            // somebody put a mug down on it
            {
                float cx = w * 0.72f, cy = h * 0.3f, r = 62;
                var coffee = new Color(0.45f, 0.3f, 0.12f);
                for (int y = (int)(cy - r - 4); y <= cy + r + 4; y++)
                    for (int x = (int)(cx - r - 4); x <= cx + r + 4; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        float ring = Mathf.Clamp01(1 - Mathf.Abs(d - r) / 4f);
                        float broken = Fbm(x / 40f, y / 40f, 1f, seed + 5, 3);
                        if (broken > 0.42f) Blend(px, w, h, x, y, coffee, ring * 0.35f * (broken - 0.4f) * 3f);
                    }
            }
            return Make(px, w, h, false);
        }

        /// <summary>A tiling tarnish for brass: mostly bright, dull in the hollows, a few dark spots.</summary>
        public static Texture2D Tarnish(Color brass, int seed)
        {
            const int n = 256;
            var px = new Color32[n * n];
            var dull = new Color(0.3f, 0.22f, 0.1f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    // sample noise on a torus so the tile wraps
                    float a = Mathf.Repeat(u, 1f), b = Mathf.Repeat(v, 1f);
                    float f = (Fbm(a * 4, b * 4, 1f, seed, 4) + Fbm((a + 1) * 4, (b + 1) * 4, 1f, seed, 4)) * 0.5f;
                    float spot = Fbm(a * 4, b * 4, 0.25f, seed + 2, 2);
                    Color c = brass * (0.88f + (f - 0.5f) * 0.35f);
                    if (spot > 0.72f) c = Color.Lerp(c, dull, (spot - 0.72f) * 3f);
                    px[y * n + x] = c;
                }
            return Make(px, n, n, true);
        }

        /// <summary>Hammertone grey for the radar cabinets, with a few bright scratches.</summary>
        public static Texture2D Hammertone(Color grey, int seed)
        {
            const int n = 256;
            var px = new Color32[n * n];
            var rng = new System.Random(seed);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float f = Fbm(x / 256f * 6, y / 256f * 6, 0.5f, seed, 4);
                    float dimple = Fbm(x / 256f * 6, y / 256f * 6, 0.08f, seed + 1, 2);
                    px[y * n + x] = grey * (0.85f + (f - 0.5f) * 0.25f + (dimple - 0.5f) * 0.2f);
                }
            for (int i = 0; i < 6; i++)
            {
                float x0 = (float)rng.NextDouble() * n, y0 = (float)rng.NextDouble() * n, ang = (float)rng.NextDouble() * Mathf.PI, len = 10 + (float)rng.NextDouble() * 40;
                for (float t = 0; t < len; t += 0.5f)
                    Blend(px, n, n, Mathf.RoundToInt(x0 + Mathf.Cos(ang) * t), Mathf.RoundToInt(y0 + Mathf.Sin(ang) * t), grey * 1.6f, 0.5f);
            }
            return Make(px, n, n, true);
        }

        /// <summary>Varnished wood grain running along u.</summary>
        public static Texture2D Wood(Color wood, int seed)
        {
            const int n = 256;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / 256f, v = y / 256f;
                    float grain = Fbm(u * 1.5f, v * 30f, 1f, seed, 4);
                    float ring = Mathf.Sin((v * 7 + Fbm(u * 2, v * 2, 1f, seed + 1, 3) * 3) * Mathf.PI * 2) * 0.5f + 0.5f;
                    px[y * n + x] = wood * (0.8f + (grain - 0.5f) * 0.45f + ring * 0.15f);
                }
            return Make(px, n, n, true);
        }
    }
}
