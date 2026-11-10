using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Builds the outside of the ship from simple shapes: a 729-foot laker with the
    /// pilothouse forward, 21 hatches down a long spar deck, and the engine house aft.
    /// Ship space: +Z forward, waterline at y = 0, centreline at x = 0.
    /// </summary>
    public static class ShipModel
    {
        public const float BowZ = 118f, SternZ = -104f, HalfBeam = 11.45f;
        public const float SparDeckY = 4.2f, ForecastleY = 6.6f, Keel = -8f;
        public const float PilothouseFloorY = 9.8f;

        public static readonly List<Light> DeckLights = new();
        public static readonly List<Renderer> NightWindows = new();
        public static Light Searchlight;
        public static Transform BowSpray;

        static float HalfWidth(float z)
        {
            if (z > 96f)
            {
                float t = (z - 96f) / (BowZ - 96f);
                return HalfBeam * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) + 0.15f;
            }
            if (z < -94f)
            {
                float t = (-94f - z) / (-94f - SternZ);
                return HalfBeam * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * 0.85f)) + 0.15f;
            }
            return HalfBeam;
        }

        static float DeckY(float z)
        {
            if (z > 88f) return Mathf.Lerp(SparDeckY, ForecastleY, Mathf.SmoothStep(0, 1, (z - 88f) / 4f)) + Mathf.Max(0, z - 105f) * 0.06f;
            if (z < -88f) return Mathf.Lerp(SparDeckY, 5.6f, Mathf.SmoothStep(0, 1, (-88f - z) / 4f));
            return SparDeckY;
        }

        public static void Build(Transform root)
        {
            DeckLights.Clear();
            NightWindows.Clear();

            var hullBlack = Mats.Lit(new Color(0.045f, 0.045f, 0.05f), 0.35f);
            var hullRed = Mats.Lit(new Color(0.32f, 0.07f, 0.05f), 0.2f);
            var deckMat = Mats.Lit(new Color(0.27f, 0.11f, 0.08f), 0.45f);
            var hatchMat = Mats.Lit(new Color(0.2f, 0.21f, 0.2f), 0.5f, 0.2f);
            var white = Mats.Lit(new Color(0.8f, 0.8f, 0.77f), 0.35f);
            var rail = Mats.Lit(new Color(0.75f, 0.75f, 0.72f), 0.3f, 0.3f);
            var stackRed = Mats.Lit(new Color(0.55f, 0.12f, 0.06f), 0.3f);
            var steel = Mats.Lit(new Color(0.18f, 0.19f, 0.2f), 0.5f, 0.6f);

            var ext = Geo.Node(root, "Exterior", Vector3.zero);
            BuildHull(ext, hullBlack, hullRed, deckMat);

            // ---- 21 hatches with their covers ----
            for (int i = 0; i < 21; i++)
            {
                float z = 81f - i * 7.32f;
                Geo.Box(ext, "Hatch" + (i + 1), new Vector3(0, SparDeckY + 0.35f, z), new Vector3(14.6f, 0.7f, 3.4f), hatchMat);
                Geo.Box(ext, "Clamps", new Vector3(0, SparDeckY + 0.72f, z), new Vector3(14.2f, 0.06f, 3.0f), steel);
            }
            // the cargo hold trunk running down the middle
            Geo.Box(ext, "DeckWalkPort", new Vector3(-9.6f, SparDeckY + 0.02f, -2), new Vector3(1.4f, 0.05f, 176f), Mats.Lit(new Color(0.22f, 0.1f, 0.08f), 0.3f));
            Geo.Box(ext, "DeckWalkStbd", new Vector3(9.6f, SparDeckY + 0.02f, -2), new Vector3(1.4f, 0.05f, 176f), Mats.Lit(new Color(0.22f, 0.1f, 0.08f), 0.3f));

            // ---- fence rail along both sides ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = SternZ + 8; z < BowZ - 4; z += 3f)
                {
                    float hw = HalfWidth(z) - 0.25f;
                    float y = DeckY(z);
                    Geo.Box(ext, "Stanchion", new Vector3(side * hw, y + 0.55f, z), new Vector3(0.06f, 1.1f, 0.06f), rail);
                }
                for (float z = SternZ + 8; z < BowZ - 7; z += 6f)
                {
                    float hw = HalfWidth(z + 3) - 0.25f;
                    float y = DeckY(z + 3);
                    Geo.Box(ext, "Rail", new Vector3(side * hw, y + 1.1f, z + 3), new Vector3(0.05f, 0.05f, 6f), rail);
                    Geo.Box(ext, "Rail", new Vector3(side * hw, y + 0.6f, z + 3), new Vector3(0.04f, 0.04f, 6f), rail);
                }
            }

            // ---- forecastle: windlass, foremast, jackstaff ----
            Geo.Box(ext, "Windlass", new Vector3(0, ForecastleY + 0.6f, 108f), new Vector3(4.2f, 1.2f, 1.8f), Mats.Lit(new Color(0.15f, 0.25f, 0.18f), 0.4f));
            Geo.Cyl(ext, "WildcatP", new Vector3(-2.6f, ForecastleY + 0.8f, 108f), 0.7f, 0.5f, steel, new Vector3(0, 0, 90));
            Geo.Cyl(ext, "WildcatS", new Vector3(2.6f, ForecastleY + 0.8f, 108f), 0.7f, 0.5f, steel, new Vector3(0, 0, 90));
            // buff rather than white: the floods at night would otherwise burn it out dead ahead
            Geo.Cyl(ext, "Foremast", new Vector3(0, ForecastleY + 8f, 112f), 0.22f, 16f, Mats.Lit(new Color(0.45f, 0.4f, 0.32f), 0.3f));
            Geo.Box(ext, "Crosstree", new Vector3(0, ForecastleY + 12f, 112f), new Vector3(3.2f, 0.15f, 0.2f), white);
            Geo.Cyl(ext, "Jackstaff", new Vector3(0, ForecastleY + 2.5f, BowZ - 1.2f), 0.06f, 5f, Mats.Lit(new Color(0.35f, 0.36f, 0.36f), 0.3f));
            AddLamp(ext, new Vector3(0, ForecastleY + 16.2f, 112f), new Color(1f, 0.95f, 0.8f), 0f, "Masthead");

            // ---- forward house: three tiers, pilothouse on top ----
            Geo.Box(ext, "TexasHouse", new Vector3(0, (ForecastleY + PilothouseFloorY) / 2, 97f), new Vector3(18f, PilothouseFloorY - ForecastleY, 13.2f), white);
            Geo.Box(ext, "Bridgewings", new Vector3(0, PilothouseFloorY - 0.1f, 101.5f), new Vector3(22f, 0.2f, 3f), white);
            Geo.Box(ext, "WingRailP", new Vector3(-10.9f, PilothouseFloorY + 0.55f, 101.5f), new Vector3(0.08f, 1.1f, 3f), white);
            Geo.Box(ext, "WingRailS", new Vector3(10.9f, PilothouseFloorY + 0.55f, 101.5f), new Vector3(0.08f, 1.1f, 3f), white);
            Geo.Box(ext, "PilothouseRoof", new Vector3(0, Wheelhouse.Ceiling + 0.3f, (Wheelhouse.Front + Wheelhouse.Back) / 2), new Vector3(Wheelhouse.Side * 2 + 0.6f, 0.3f, Wheelhouse.Front - Wheelhouse.Back + 0.6f), white);
            Geo.Cyl(ext, "RadarMast", new Vector3(0, PilothouseFloorY + 5f, 97f), 0.12f, 4f, white);
            Geo.Box(ext, "RadarScanner1", new Vector3(0, PilothouseFloorY + 7.1f, 97f), new Vector3(3.6f, 0.25f, 0.35f), Mats.Lit(new Color(0.85f, 0.85f, 0.85f), 0.4f));
            Geo.Box(ext, "RadarScanner2", new Vector3(0, PilothouseFloorY + 5.4f, 97f), new Vector3(2.6f, 0.2f, 0.3f), Mats.Lit(new Color(0.85f, 0.85f, 0.85f), 0.4f));
            AddLamp(ext, new Vector3(-10.9f, PilothouseFloorY + 1.4f, 101.5f), new Color(1f, 0.1f, 0.08f), 0f, "PortSidelight");
            AddLamp(ext, new Vector3(10.9f, PilothouseFloorY + 1.4f, 101.5f), new Color(0.1f, 1f, 0.3f), 0f, "StbdSidelight");

            // Deck floodlights on the after face of the forward house, shining down the deck.
            AddDeckLight(ext, new Vector3(-6f, PilothouseFloorY - 1.2f, 90.2f), new Vector3(28, 182, 0));
            AddDeckLight(ext, new Vector3(6f, PilothouseFloorY - 1.2f, 90.2f), new Vector3(28, 178, 0));
            AddDeckLight(ext, new Vector3(0, 9f, -86f), new Vector3(30, 0, 0));
            // and a pair on the face of the pilothouse lighting the foredeck and the seas ahead
            AddDeckLight(ext, new Vector3(-3.6f, PilothouseFloorY + 2.5f, 103.7f), new Vector3(42, 10, 0), 140, 95);
            AddDeckLight(ext, new Vector3(3.6f, PilothouseFloorY + 2.5f, 103.7f), new Vector3(42, -10, 0), 140, 95);

            // Searchlight on the pilothouse roof, aimed over the bow.
            var sl = new GameObject("Searchlight").AddComponent<Light>();
            sl.transform.SetParent(ext, false);
            sl.transform.localPosition = new Vector3(0, PilothouseFloorY + 3.5f, 103.5f);
            sl.transform.localRotation = Quaternion.Euler(17, 0, 0);
            sl.type = LightType.Spot;
            sl.spotAngle = 34;
            sl.innerSpotAngle = 10;
            sl.range = 400;
            sl.intensity = 1400;
            sl.color = new Color(1f, 0.96f, 0.88f);
            sl.enabled = false;
            Searchlight = sl;
            Geo.Cyl(ext, "SearchlightBody", new Vector3(0, PilothouseFloorY + 3.5f, 103.2f), 0.3f, 0.6f, steel, new Vector3(90, 0, 0));

            // ---- engine house aft ----
            Geo.Box(ext, "AftHouse1", new Vector3(0, 5.6f + 1.6f, -96f), new Vector3(20.5f, 3.2f, 15f), white);
            Geo.Box(ext, "AftHouse2", new Vector3(0, 5.6f + 4.6f, -97f), new Vector3(16f, 2.8f, 11f), white);
            Geo.Cyl(ext, "Stack", new Vector3(0, 16.5f, -99f), 2.3f, 9f, stackRed);
            Geo.Cyl(ext, "StackTop", new Vector3(0, 21.3f, -99f), 2.35f, 1.0f, hullBlack);
            Geo.Cyl(ext, "Mainmast", new Vector3(0, 17f, -91f), 0.2f, 12f, white);
            AddLamp(ext, new Vector3(0, 23f, -91f), new Color(1f, 0.95f, 0.8f), 0f, "RangeLight");
            Geo.Box(ext, "LifeboatP", new Vector3(-9.2f, 11.3f, -98f), new Vector3(1.8f, 1.2f, 7f), Mats.Lit(new Color(0.9f, 0.35f, 0.08f), 0.4f));
            Geo.Box(ext, "LifeboatS", new Vector3(9.2f, 11.3f, -98f), new Vector3(1.8f, 1.2f, 7f), Mats.Lit(new Color(0.9f, 0.35f, 0.08f), 0.4f));

            // lit portholes on the houses
            var winLit = Mats.Glow(new Color(0.1f, 0.09f, 0.06f), new Color(1.2f, 0.85f, 0.45f));
            for (int i = -3; i <= 3; i++)
            {
                NightWindows.Add(Geo.Box(ext, "AftWin", new Vector3(i * 2.4f, 7.6f, -88.45f), new Vector3(0.8f, 0.6f, 0.05f), winLit).GetComponent<Renderer>());
                NightWindows.Add(Geo.Box(ext, "FwdWin", new Vector3(i * 2.2f, ForecastleY + 1.8f, 90.35f), new Vector3(0.7f, 0.6f, 0.05f), winLit).GetComponent<Renderer>());
            }

            BowSpray = Geo.Node(root, "BowSpray", new Vector3(0, ForecastleY + 1f, 112f));

            StaticBatchingUtility.Combine(ext.gameObject);
        }

        static void AddLamp(Transform parent, Vector3 pos, Color c, float range, string name)
        {
            var m = Mats.Glow(c * 0.3f, c * 4f);
            Geo.Sphere(parent, name, pos, 0.18f, m);
        }

        static void AddDeckLight(Transform parent, Vector3 pos, Vector3 euler, float intensity = 500, float angle = 70)
        {
            var go = new GameObject("DeckLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.4f;
            l.range = 140;
            l.intensity = intensity;   // inverse-square: about one at twenty metres
            l.color = new Color(1f, 0.82f, 0.55f);
            l.shadows = LightShadows.None;
            DeckLights.Add(l);
            Geo.Box(parent, "Floodlight", pos, new Vector3(0.5f, 0.35f, 0.3f), Mats.Glow(new Color(0.2f, 0.2f, 0.2f), new Color(3f, 2.4f, 1.6f)), euler);
        }

        static void BuildHull(Transform parent, Material black, Material red, Material deck)
        {
            // Loft cross-sections from stern to bow.
            var zs = new List<float>();
            for (float z = SternZ; z < -88f; z += 1f) zs.Add(z);
            for (float z = -88f; z < 88f; z += 8f) zs.Add(z);
            for (float z = 88f; z < BowZ; z += 0.75f) zs.Add(z);
            zs.Add(BowZ);

            var hv = new List<Vector3>();
            var ht = new List<int>();
            var bv = new List<Vector3>();
            var bt = new List<int>();
            var dv = new List<Vector3>();
            var dt = new List<int>();

            for (int i = 0; i < zs.Count; i++)
            {
                float z = zs[i];
                float w = HalfWidth(z);
                float top = DeckY(z);
                // Sides above the boot top, black.
                hv.Add(new Vector3(w, top, z));
                hv.Add(new Vector3(w, -0.6f, z));
                hv.Add(new Vector3(-w, top, z));
                hv.Add(new Vector3(-w, -0.6f, z));
                // Below the waterline, red, with a rounded bilge.
                float bottom = z > 100f ? Mathf.Lerp(Keel, -1f, (z - 100f) / (BowZ - 100f)) : Keel;
                float bw = Mathf.Max(0.1f, w - 1.6f);
                bv.Add(new Vector3(w, -0.6f, z));
                bv.Add(new Vector3(bw, bottom, z));
                bv.Add(new Vector3(-bw, bottom, z));
                bv.Add(new Vector3(-w, -0.6f, z));
                dv.Add(new Vector3(w, top, z));
                dv.Add(new Vector3(-w, top, z));

                if (i == 0) continue;
                int a = (i - 1) * 4, b = i * 4;
                // starboard side (outward normal +x)
                ht.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                // port side
                ht.AddRange(new[] { a + 2, a + 3, b + 2, b + 2, a + 3, b + 3 });
                // bottom strip: 0-1-2-3 around
                for (int k = 0; k < 3; k++)
                    bt.AddRange(new[] { a + k, b + k, a + k + 1, b + k, b + k + 1, a + k + 1 });
                int c = (i - 1) * 2, d = i * 2;
                dt.AddRange(new[] { c, c + 1, d, d, c + 1, d + 1 });
            }
            // transom at the stern
            dt.AddRange(new int[0]);

            Geo.MeshObject(parent, "HullSides", MakeMesh(hv, ht, true), black);
            Geo.MeshObject(parent, "HullBottom", MakeMesh(bv, bt, true), red);
            Geo.MeshObject(parent, "SparDeck", MakeMesh(dv, dt, false), deck);
            // stern plate
            float sw = HalfWidth(SternZ + 0.01f);
            Geo.Box(parent, "Transom", new Vector3(0, (DeckY(SternZ) + Keel) / 2, SternZ), new Vector3(sw * 2, DeckY(SternZ) - Keel, 0.2f), black);
            // white bulwark band at the bow
            for (float z = 92f; z < BowZ - 1; z += 1.5f)
            {
                float w = HalfWidth(z);
                Geo.Box(parent, "BowBulwark", new Vector3(w - 0.1f, DeckY(z) + 0.6f, z), new Vector3(0.15f, 1.2f, 1.6f), black);
                Geo.Box(parent, "BowBulwark", new Vector3(-w + 0.1f, DeckY(z) + 0.6f, z), new Vector3(0.15f, 1.2f, 1.6f), black);
            }
        }

        static Mesh MakeMesh(List<Vector3> v, List<int> t, bool doubleSided)
        {
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            if (doubleSided)
            {
                int n = v.Count;
                var v2 = new List<Vector3>(v);
                v2.AddRange(v);
                var t2 = new List<int>(t);
                for (int i = 0; i < t.Count; i += 3)
                    t2.AddRange(new[] { t[i] + n, t[i + 2] + n, t[i + 1] + n });
                v = v2;
                t = t2;
            }
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
