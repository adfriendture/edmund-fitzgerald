using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Gerstner-wave lake. The same sums run here and in Ocean.shader, so the ship
    /// can ask for the height under any point. The world uses a floating origin:
    /// the ship stays near (0,0) and NavOffset carries how far it has sailed.
    /// </summary>
    public class Ocean : MonoBehaviour
    {
        public const int MaxWaves = 10;

        struct Wave
        {
            public Vector2 Dir;
            public float Length, Height, Steep, Phase, Share;
        }

        readonly List<Wave> _waves = new();
        readonly Vector4[] _a = new Vector4[MaxWaves];
        readonly Vector4[] _b = new Vector4[MaxWaves];

        public Vector2 NavOffset;     // metres east, north
        public float Storm = 0.5f;    // 0 calm .. 1 worst of it
        public float SignificantHeight = 5f; // metres
        public float WaveTime;
        public float WindFromDeg = 315f; // northwest gale

        static readonly int IdA = Shader.PropertyToID("_FitzWaveA");
        static readonly int IdB = Shader.PropertyToID("_FitzWaveB");
        static readonly int IdCount = Shader.PropertyToID("_FitzWaveCount");
        static readonly int IdTime = Shader.PropertyToID("_FitzTime");
        static readonly int IdOffset = Shader.PropertyToID("_FitzOceanOffset");
        static readonly int IdWind = Shader.PropertyToID("_FitzWind");

        public Vector2 WaveTravelDir
        {
            get
            {
                float to = (WindFromDeg + 180f) * Mathf.Deg2Rad;
                return new Vector2(Mathf.Sin(to), Mathf.Cos(to));
            }
        }

        public void Build()
        {
            var rng = new System.Random(1975);
            // Long swell, the main sea, and shorter cross seas. Share = fraction of the total height.
            AddWave(0, 145f, 0.30f, 0.55f, rng);
            AddWave(-18, 110f, 0.26f, 0.7f, rng);
            AddWave(22, 92f, 0.20f, 0.75f, rng);
            AddWave(-38, 70f, 0.12f, 0.7f, rng);
            AddWave(45, 55f, 0.08f, 0.65f, rng);
            AddWave(10, 38f, 0.06f, 0.6f, rng);
            AddWave(-60, 30f, 0.04f, 0.5f, rng);
            AddWave(70, 22f, 0.03f, 0.5f, rng);

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildMesh();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Custom("Ocean");
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Push();
        }

        void AddWave(float angleOffset, float length, float share, float steep, System.Random rng)
        {
            _waves.Add(new Wave { Dir = new Vector2(angleOffset, 0), Length = length, Share = share, Steep = steep, Phase = (float)rng.NextDouble() * 6.283f });
        }

        static Mesh BuildMesh()
        {
            // Polar grid: dense near the ship, coarse at the fogged-out horizon.
            const int rings = 150, segs = 220;
            var verts = new List<Vector3> { Vector3.zero };
            for (int r = 1; r <= rings; r++)
            {
                float t = r / (float)rings;
                float radius = 0.6f * r + Mathf.Pow(t, 3.2f) * 4000f;
                for (int s = 0; s < segs; s++)
                {
                    float a = s / (float)segs * Mathf.PI * 2;
                    verts.Add(new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
                }
            }
            var tris = new List<int>();
            for (int s = 0; s < segs; s++)
                tris.AddRange(new[] { 0, 1 + (s + 1) % segs, 1 + s });
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < segs; s++)
                {
                    int a = 1 + r * segs + s, b = 1 + r * segs + (s + 1) % segs;
                    int c = a + segs, d = b + segs;
                    tris.AddRange(new[] { a, b, c, b, d, c });
                }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.bounds = new Bounds(Vector3.zero, new Vector3(9000, 80, 9000));
            return m;
        }

        void Update()
        {
            WaveTime += Time.deltaTime;
            Push();
        }

        /// <summary>Recomputes per-wave parameters for the current storm level and uploads them.</summary>
        public void Push()
        {
            var travel = WaveTravelDir;
            float baseAngle = Mathf.Atan2(travel.x, travel.y) * Mathf.Rad2Deg;
            // Significant wave height is roughly 4x the RMS of the surface; sum of shares ~ 1.
            float hs = SignificantHeight;
            int n = Mathf.Min(_waves.Count, MaxWaves);
            float steepSum = 0;
            for (int i = 0; i < n; i++)
            {
                var w = _waves[i];
                float ang = (baseAngle + w.Dir.x) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang));
                float k = 2 * Mathf.PI / w.Length;
                float amp = hs * 0.5f * w.Share * 1.15f;
                float omega = Mathf.Sqrt(9.81f * k);
                float q = w.Steep * (0.5f + 0.5f * Storm) / Mathf.Max(0.001f, k * amp * n);
                q = Mathf.Min(q, 1.5f);
                steepSum += q * k * amp;
                _a[i] = new Vector4(d.x, d.y, k, amp);
                _b[i] = new Vector4(omega, w.Phase, q, 0);
            }
            Shader.SetGlobalVectorArray(IdA, _a);
            Shader.SetGlobalVectorArray(IdB, _b);
            Shader.SetGlobalFloat(IdCount, n);
            Shader.SetGlobalFloat(IdTime, WaveTime);
            Shader.SetGlobalVector(IdOffset, new Vector4(NavOffset.x, NavOffset.y, 0, 0));
            Shader.SetGlobalVector(IdWind, new Vector4(travel.x, travel.y, Storm, Mathf.Lerp(0.25f, 1f, Storm)));
        }

        Vector3 Displace(Vector2 p)
        {
            Vector3 disp = Vector3.zero;
            int n = Mathf.Min(_waves.Count, MaxWaves);
            for (int i = 0; i < n; i++)
            {
                var a = _a[i];
                var b = _b[i];
                float f = a.z * (a.x * p.x + a.y * p.y) - b.x * WaveTime + b.y;
                float s = Mathf.Sin(f), c = Mathf.Cos(f);
                disp.x += b.z * a.w * a.x * c;
                disp.z += b.z * a.w * a.y * c;
                disp.y += a.w * s;
            }
            return disp;
        }

        /// <summary>Water height at a world-space XZ position (scene coordinates).</summary>
        public float SampleHeight(Vector3 world)
        {
            var target = new Vector2(world.x + NavOffset.x, world.z + NavOffset.y);
            var p = target;
            Vector3 d = Vector3.zero;
            // Gerstner moves points sideways, so step back to find which grid point lands here.
            for (int i = 0; i < 4; i++)
            {
                d = Displace(p);
                p = target - new Vector2(d.x, d.z);
            }
            return d.y;
        }
    }
}
