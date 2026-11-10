using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Caribou Island: the only land the Fitzgerald came near that afternoon. A low,
    /// wooded hump a few miles long, placed in the world from the navigation position
    /// so it agrees with the radar and the chart. In the murk it is a dark band on
    /// the horizon off the starboard bow around three o'clock.
    /// </summary>
    public class Land : MonoBehaviour
    {
        public const float CaribouRadiusNm = 1.3f;     // for the helmsman's collision check
        Transform _caribou;
        Material _lamp;
        /// <summary>Caribou Island Light: on a five-acre islet a mile west of the island's south tip. 1886, rebuilt 1911.</summary>
        public static readonly Vector2 LightNm = Charts.ToNm(47.316, -85.832);

        public void Build()
        {
            var rock = Mats.Lit(new Color(0.5f, 0.47f, 0.42f), 0.2f);
            var forest = Mats.Lit(new Color(0.1f, 0.14f, 0.09f), 0.1f);
            _caribou = Geo.Node(transform, "Caribou", Vector3.zero);
            // outline in metres about the island's centre
            var ring = new List<Vector2>();
            var poly = Charts.Caribou;
            for (int i = 1; i < poly.Length; i++)
            {
                var a = (Charts.ToNm(poly[i - 1][0], poly[i - 1][1]) - Charts.CaribouIsland) * Ship.NmToM;
                var b = (Charts.ToNm(poly[i][0], poly[i][1]) - Charts.CaribouIsland) * Ship.NmToM;
                for (int k = 0; k < 8; k++)
                {
                    var p = Vector2.Lerp(a, b, k / 8f);
                    // a little raggedness so it isn't a polygon
                    p *= 1f + (Surfaces.Fbm(p.x * 0.001f, p.y * 0.001f, 0.6f, 4, 3) - 0.5f) * 0.25f;
                    ring.Add(p);
                }
            }
            // taller than life so it still reads through the murk at two miles
            Band(_caribou, "Shore", ring, 1f, -2f, 0.86f, 8f, rock);
            Band(_caribou, "Woods", ring, 0.86f, 8f, 0.45f, 62f, forest);
            Cap(_caribou, "Crown", ring, 0.45f, 62f, 80f, forest);

            // the lighthouse: a hundred-foot white concrete tower with a red lantern, on its own low islet
            var at = (LightNm - Charts.CaribouIsland) * Ship.NmToM;
            var lh = Geo.Node(_caribou, "Lighthouse", new Vector3(at.x, 0, at.y));
            var islet = new List<Vector2>();
            for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2; islet.Add(new Vector2(Mathf.Cos(a) * 90f * (1f + Mathf.Sin(a * 3) * 0.15f), Mathf.Sin(a) * 70f)); }
            Band(lh, "Islet", islet, 1f, -2f, 0.4f, 4f, rock);
            Cap(lh, "IsletTop", islet, 0.4f, 4f, 5f, rock);
            var concrete = Mats.Lit(new Color(0.9f, 0.9f, 0.86f), 0.3f);
            Geo.Cyl(lh, "Tower", new Vector3(0, 5f + 13.5f, 0), 3.2f, 27f, concrete);
            Geo.Cyl(lh, "Gallery", new Vector3(0, 32.2f, 0), 4.2f, 0.6f, Mats.Lit(new Color(0.6f, 0.12f, 0.1f), 0.4f));
            Geo.Cyl(lh, "Lantern", new Vector3(0, 33.8f, 0), 2.2f, 2.6f, Mats.Lit(new Color(0.65f, 0.14f, 0.1f), 0.5f));
            _lamp = Mats.Glow(new Color(1f, 0.97f, 0.85f), Color.black);
            Geo.Sphere(lh, "Lamp", new Vector3(0, 33.8f, 0), 2.6f, _lamp);
            Geo.Cyl(lh, "Roof", new Vector3(0, 35.6f, 0), 2.4f, 1.0f, Mats.Lit(new Color(0.6f, 0.12f, 0.1f), 0.4f));
            Geo.Box(lh, "Keepers", new Vector3(40f, 7.5f, 10f), new Vector3(14f, 5f, 9f), concrete);
        }

        static void Band(Transform parent, string name, List<Vector2> ring, float s0, float h0, float s1, float h1, Material m)
        {
            int n = ring.Count;
            var v = new Vector3[n * 2];
            var t = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                v[i] = new Vector3(ring[i].x * s0, h0, ring[i].y * s0);
                v[n + i] = new Vector3(ring[i].x * s1, h1, ring[i].y * s1);
                int j = (i + 1) % n;
                t[i * 6] = i; t[i * 6 + 1] = n + i; t[i * 6 + 2] = j;
                t[i * 6 + 3] = j; t[i * 6 + 4] = n + i; t[i * 6 + 5] = n + j;
            }
            var mesh = new Mesh { vertices = v, triangles = t };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Geo.MeshObject(parent, name, mesh, m);
        }

        static void Cap(Transform parent, string name, List<Vector2> ring, float s, float h, float top, Material m)
        {
            int n = ring.Count;
            var v = new Vector3[n + 1];
            var t = new int[n * 3];
            v[n] = new Vector3(0, top, 0);
            for (int i = 0; i < n; i++)
            {
                v[i] = new Vector3(ring[i].x * s, h, ring[i].y * s);
                t[i * 3] = i; t[i * 3 + 1] = n; t[i * 3 + 2] = (i + 1) % n;
            }
            var mesh = new Mesh { vertices = v, triangles = t };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Geo.MeshObject(parent, name, mesh, m);
        }

        /// <summary>Puts the island where it is relative to the ship, which sits at the origin.</summary>
        public void Place(Vector2 shipNm)
        {
            var d = (Charts.CaribouIsland - shipNm) * Ship.NmToM;
            _caribou.localPosition = new Vector3(d.x, 0, d.y);
            // nothing to draw once it's well beyond the fog
            _caribou.gameObject.SetActive(d.magnitude < 12000f);
            // the light: a long flash every ten seconds, bright enough to show as a spark through the murk
            bool on = Mathf.Repeat(Time.time, 10f) < 1.3f;
            _lamp.SetColor("_EmissionColor", on ? new Color(90f, 85f, 70f) : Color.black);
        }
    }
}
