using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Fitz
{
    /// <summary>Command-line switches, mostly for testing and screenshots.</summary>
    public static class Args
    {
        static string[] _args;
        static string[] All => _args ??= Environment.GetCommandLineArgs();

        public static bool Has(string name) => Array.IndexOf(All, name) >= 0;

        public static string Get(string name)
        {
            var a = All;
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == name) return a[i + 1];
            return null;
        }

        public static float Float(string name, float fallback)
        {
            var s = Get(name);
            return s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        /// <summary>Parses "HH:MM" into minutes after midnight.</summary>
        public static float ClockMinutes(string name, float fallback)
        {
            var s = Get(name);
            if (s == null) return fallback;
            var parts = s.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return h * 60 + m;
            return fallback;
        }
    }

    /// <summary>
    /// Game time runs from 2:40 PM to 7:15 PM on November 10, 1975, squeezed into
    /// about thirty real minutes.
    /// </summary>
    public class GameClock
    {
        public const float StartMinutes = 14 * 60 + 40;
        public const float SinkMinutes = 19 * 60 + 15;
        public const float RealSeconds = 24 * 60;
        public static float Rate => (SinkMinutes - StartMinutes) / RealSeconds; // game minutes per real second

        public float Minutes = StartMinutes;
        public float Scale = 1f;
        public bool Running;

        public void Tick(float dt)
        {
            if (Running) Minutes += dt * Rate * Scale;
        }

        public float Progress => Mathf.InverseLerp(StartMinutes, SinkMinutes, Minutes);
        public float Hours24 => Minutes / 60f;

        public static string Format(float minutes)
        {
            int total = Mathf.FloorToInt(minutes);
            int h = total / 60, m = total % 60;
            string ampm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12;
            if (h12 == 0) h12 = 12;
            return $"{h12}:{m:00} {ampm}";
        }

        public static float At(int h, int m) => h * 60 + m;
    }

    /// <summary>Material factory. Clones the templates FitzBuild.Setup saves.</summary>
    public static class Mats
    {
        static Material _opaque, _emissive, _transparent;
        static readonly Dictionary<string, Material> Cache = new();

        static Material Load(ref Material slot, string name)
        {
            if (slot == null)
            {
                slot = Resources.Load<Material>("Materials/" + name);
                if (slot == null) Debug.LogError("[Fitz] Missing material template " + name + ". Run FitzBuild.Setup.");
            }
            return slot;
        }

        public static Material Lit(Color c, float smooth = 0.3f, float metal = 0f)
        {
            string key = $"lit{c}{smooth}{metal}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Load(ref _opaque, "LitOpaque"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            Cache[key] = m;
            return m;
        }

        /// <summary>A fresh (uncached) emissive material, so callers can animate it.</summary>
        public static Material Glow(Color baseColor, Color emission, Texture tex = null, float smooth = 0.4f)
        {
            var m = new Material(Load(ref _emissive, "LitEmissive"));
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_Smoothness", smooth);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_EmissionMap", tex);
            }
            return m;
        }

        public static Material Clear(Color c, float smooth = 0.9f)
        {
            var m = new Material(Load(ref _transparent, "LitTransparent"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        public static Material Custom(string name)
        {
            var t = Resources.Load<Material>("Materials/" + name);
            if (t == null) { Debug.LogError("[Fitz] Missing material " + name); return null; }
            return new Material(t);
        }
    }

    /// <summary>Small geometry helpers for building everything out of boxes and meshes.</summary>
    public static class Geo
    {
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, Vector3? euler = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Cyl(Transform parent, string name, Vector3 center, float radius, float height, Material mat, Vector3? euler = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 center, float radius, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = Vector3.one * radius * 2;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>A quad facing local -Z (towards a viewer standing in front of it), UVs 0..1.</summary>
        public static GameObject Quad(Transform parent, string name, Vector3 center, Vector2 size, Material mat, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            var mesh = new Mesh();
            float hx = size.x / 2, hy = size.y / 2;
            mesh.vertices = new[] { new Vector3(-hx, -hy, 0), new Vector3(hx, -hy, 0), new Vector3(-hx, hy, 0), new Vector3(hx, hy, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.tangents = new[] { new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1) };
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A flat disc facing local -Z with UVs mapped 0..1 across it (for dial faces).</summary>
        public static GameObject Disc(Transform parent, string name, Vector3 center, float diameter, Material mat, int seg = 48)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var v = new Vector3[seg + 1];
            var uv = new Vector2[seg + 1];
            var n = new Vector3[seg + 1];
            var t = new int[seg * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f); n[0] = Vector3.back;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2;
                v[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * diameter * 0.5f;
                uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                n[i + 1] = Vector3.back;
                t[i * 3] = 0; t[i * 3 + 1] = 1 + (i + 1) % seg; t[i * 3 + 2] = 1 + i;
            }
            var m = new Mesh { vertices = v, uv = uv, normals = n, triangles = t };
            m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A pane of glass whose UVs are in metres (the rain shader needs real scale).</summary>
        public static GameObject Pane(Transform parent, string name, Vector3 center, Vector2 size, Material mat, Vector3 euler)
        {
            var go = Quad(parent, name, center, size, mat, euler);
            if (Args.Has("-fitzNoGlass")) go.GetComponent<Renderer>().enabled = false;
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(size.x, 0), new Vector2(0, size.y), new Vector2(size.x, size.y) };
            return go;
        }

        public static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static Transform Node(Transform parent, string name, Vector3 pos, Vector3? euler = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return t;
        }

        /// <summary>A torus lying in the local XY plane (for the ship's wheel and life rings).</summary>
        public static Mesh Torus(float radius, float tube, int seg = 48, int side = 10)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2;
                var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2;
                    var n = c * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    verts.Add(c * radius + n * tube);
                    norms.Add(n);
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    tris.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Draws instrument faces into textures with a 5x7 pixel font.</summary>
    public class Painter
    {
        public readonly int Size;
        public readonly Color32[] Px;

        public Painter(int size, Color bg)
        {
            Size = size;
            Px = new Color32[size * size];
            Color32 c = bg;
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public Texture2D ToTexture(bool mips = true)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, mips) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            t.SetPixels32(Px);
            t.Apply(mips);
            return t;
        }

        void Blend(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size || a <= 0) return;
            int i = y * Size + x;
            Color o = Px[i];
            Px[i] = Color.Lerp(o, c, Mathf.Clamp01(a * c.a));
        }

        /// <summary>Anti-aliased disc or ring, coordinates in 0..1 texture space.</summary>
        public void Ring(Vector2 c, float r0, float r1, Color col)
        {
            int minX = Mathf.FloorToInt((c.x - r1) * Size) - 1, maxX = Mathf.CeilToInt((c.x + r1) * Size) + 1;
            int minY = Mathf.FloorToInt((c.y - r1) * Size) - 1, maxY = Mathf.CeilToInt((c.y + r1) * Size) + 1;
            float px = 1f / Size;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Vector2.Distance(new Vector2((x + 0.5f) * px, (y + 0.5f) * px), c);
                    float a = Mathf.Clamp01((r1 - d) / px + 0.5f) * Mathf.Clamp01((d - r0) / px + 0.5f);
                    Blend(x, y, col, a);
                }
        }

        public void Disc(Vector2 c, float r, Color col) => Ring(c, -1, r, col);

        public void Line(Vector2 a, Vector2 b, float width, Color col)
        {
            float px = 1f / Size;
            int minX = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - width) * Size) - 1, maxX = Mathf.CeilToInt((Mathf.Max(a.x, b.x) + width) * Size) + 1;
            int minY = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - width) * Size) - 1, maxY = Mathf.CeilToInt((Mathf.Max(a.y, b.y) + width) * Size) + 1;
            Vector2 ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 1e-8f);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2((x + 0.5f) * px, (y + 0.5f) * px);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float d = Vector2.Distance(p, a + ab * t);
                    Blend(x, y, col, Mathf.Clamp01((width * 0.5f - d) / px + 0.5f));
                }
        }

        public void Rect(Vector2 min, Vector2 max, Color col)
        {
            for (int y = Mathf.FloorToInt(min.y * Size); y < Mathf.CeilToInt(max.y * Size); y++)
                for (int x = Mathf.FloorToInt(min.x * Size); x < Mathf.CeilToInt(max.x * Size); x++)
                    Blend(x, y, col, 1);
        }

        /// <summary>Tick marks around an arc. Angles in degrees, 0 = up, clockwise.</summary>
        public void Ticks(Vector2 c, float rOuter, float fromDeg, float toDeg, int count, float len, float width, Color col)
        {
            for (int i = 0; i <= count; i++)
            {
                float deg = Mathf.Lerp(fromDeg, toDeg, i / (float)count);
                var dir = Dir(deg);
                Line(c + dir * (rOuter - len), c + dir * rOuter, width, col);
            }
        }

        public static Vector2 Dir(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        /// <summary>Text centred at c; height is the cap height in texture units.</summary>
        public void Text(string s, Vector2 c, float height, Color col, float rotDeg = 0)
        {
            s = s.ToUpperInvariant();
            float cell = height / 7f;
            float width = s.Length * 6 * cell - cell;
            var right = Dir(rotDeg + 90);
            var up = Dir(rotDeg);
            Vector2 origin = c - right * (width / 2) - up * (height / 2);
            for (int k = 0; k < s.Length; k++)
            {
                if (!Font.TryGetValue(s[k], out var rows)) continue;
                for (int ry = 0; ry < 7; ry++)
                    for (int rx = 0; rx < 5; rx++)
                    {
                        if (rows[ry][rx] != '1') continue;
                        Vector2 p = origin + right * ((k * 6 + rx + 0.5f) * cell) + up * ((6 - ry + 0.5f) * cell);
                        Disc(p, cell * 0.62f, col);
                    }
            }
        }

        static readonly Dictionary<char, string[]> Font = BuildFont();

        static Dictionary<char, string[]> BuildFont()
        {
            var d = new Dictionary<char, string>
            {
                ['0'] = "01110,10001,10011,10101,11001,10001,01110", ['1'] = "00100,01100,00100,00100,00100,00100,01110",
                ['2'] = "01110,10001,00001,00010,00100,01000,11111", ['3'] = "11111,00010,00100,00010,00001,10001,01110",
                ['4'] = "00010,00110,01010,10010,11111,00010,00010", ['5'] = "11111,10000,11110,00001,00001,10001,01110",
                ['6'] = "00110,01000,10000,11110,10001,10001,01110", ['7'] = "11111,00001,00010,00100,01000,01000,01000",
                ['8'] = "01110,10001,10001,01110,10001,10001,01110", ['9'] = "01110,10001,10001,01111,00001,00010,01100",
                ['A'] = "01110,10001,10001,11111,10001,10001,10001", ['B'] = "11110,10001,10001,11110,10001,10001,11110",
                ['C'] = "01110,10001,10000,10000,10000,10001,01110", ['D'] = "11100,10010,10001,10001,10001,10010,11100",
                ['E'] = "11111,10000,10000,11110,10000,10000,11111", ['F'] = "11111,10000,10000,11110,10000,10000,10000",
                ['G'] = "01110,10001,10000,10111,10001,10001,01111", ['H'] = "10001,10001,10001,11111,10001,10001,10001",
                ['I'] = "01110,00100,00100,00100,00100,00100,01110", ['J'] = "00111,00010,00010,00010,00010,10010,01100",
                ['K'] = "10001,10010,10100,11000,10100,10010,10001", ['L'] = "10000,10000,10000,10000,10000,10000,11111",
                ['M'] = "10001,11011,10101,10101,10001,10001,10001", ['N'] = "10001,10001,11001,10101,10011,10001,10001",
                ['O'] = "01110,10001,10001,10001,10001,10001,01110", ['P'] = "11110,10001,10001,11110,10000,10000,10000",
                ['Q'] = "01110,10001,10001,10001,10101,10010,01101", ['R'] = "11110,10001,10001,11110,10100,10010,10001",
                ['S'] = "01111,10000,10000,01110,00001,00001,11110", ['T'] = "11111,00100,00100,00100,00100,00100,00100",
                ['U'] = "10001,10001,10001,10001,10001,10001,01110", ['V'] = "10001,10001,10001,10001,10001,01010,00100",
                ['W'] = "10001,10001,10001,10101,10101,10101,01010", ['X'] = "10001,10001,01010,00100,01010,10001,10001",
                ['Y'] = "10001,10001,01010,00100,00100,00100,00100", ['Z'] = "11111,00001,00010,00100,01000,10000,11111",
                ['.'] = "00000,00000,00000,00000,00000,01100,01100", ['-'] = "00000,00000,00000,11111,00000,00000,00000",
                ['/'] = "00001,00010,00010,00100,01000,01000,10000", [':'] = "00000,01100,01100,00000,01100,01100,00000",
                [' '] = "00000,00000,00000,00000,00000,00000,00000",
            };
            var r = new Dictionary<char, string[]>();
            foreach (var kv in d) r[kv.Key] = kv.Value.Split(',');
            return r;
        }
    }
}
