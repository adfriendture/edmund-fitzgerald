using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// Drop-in models. Put an FBX or OBJ at Assets/Resources/Props/&lt;name&gt;.fbx and the
    /// matching primitive prop is hidden and the model placed on its node instead.
    /// See docs/ASSETS.md for the list of names, sizes and pivots.
    /// </summary>
    public static class Props
    {
        /// <summary>
        /// The game's own weathered materials by name. A model's materials are swapped for
        /// these by name (brass, wood, paint, steel, iron, chrome, black, concrete, cream...),
        /// so a model needs no textures and matches the room. Filled by the wheelhouse.
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, Material> Library = new();

        public enum Pivot { Base, Center, Hook, Back, Front }

        /// <summary>
        /// What each slot expects: the largest dimension in metres and where the pivot
        /// sits. A model that comes in at the wrong size (generated models are normalised)
        /// is scaled to fit and its bounds moved onto the pivot. Hand-built models that
        /// already match are left alone.
        /// </summary>
        /// <summary>frontAt: for Front pivots, where the front face sits on the node's z (the game's dial quads are just proud of it).</summary>
        public static readonly System.Collections.Generic.Dictionary<string, (float size, Pivot pivot, float frontAt)> Slots = new()
        {
            ["oilskins"] = (1.3f, Pivot.Hook, 0), ["wheel"] = (0.95f, Pivot.Center, 0), ["telegraph"] = (1.45f, Pivot.Base, 0),
            ["binnacle"] = (1.45f, Pivot.Base, 0), ["radar"] = (1.4f, Pivot.Base, 0), ["phone"] = (0.36f, Pivot.Back, 0),
            ["vhf"] = (0.65f, Pivot.Front, -0.11f), ["amradio"] = (0.56f, Pivot.Front, -0.09f), ["stool"] = (0.86f, Pivot.Base, 0),
            ["lifering"] = (0.7f, Pivot.Center, 0), ["binoculars"] = (0.2f, Pivot.Base, 0), ["mug"] = (0.14f, Pivot.Base, 0),
            ["console"] = (1.25f, Pivot.Base, 0), ["gyro"] = (1.2f, Pivot.Base, 0), ["whistle"] = (0.18f, Pivot.Hook, 0),
            ["door"] = (2.0f, Pivot.Base, 0), ["charttable"] = (1.7f, Pivot.Base, 0),
        };

        /// <summary>
        /// Square-up for a generated model, in degrees (yaw about the vertical, pitch about
        /// the side axis): one built from a three-quarter picture seen a little from above
        /// comes in turned and leaning by that view. Measured with tools/measure_prop.py --square.
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, Vector2> Turns = new()
        {
            ["console"] = new Vector2(33, 21), ["gyro"] = new Vector2(21, 0), ["door"] = new Vector2(23, -1), ["radar"] = new Vector2(0, 8),
        };

        /// <summary>Slots always scaled to exactly their size, because the game places its own parts on them by measurement.</summary>
        static readonly System.Collections.Generic.HashSet<string> Exact = new() { "console", "gyro", "whistle", "door", "charttable", "radar" };

        /// <summary>
        /// Swaps the primitives under <paramref name="node"/> for the model named
        /// <paramref name="name"/> if one exists. Renderers under children whose names are
        /// in <paramref name="keep"/> (needles, levers, screens) stay visible.
        /// </summary>
        public static bool Swap(Transform node, string name, params string[] keep)
        {
            var prefab = Resources.Load<GameObject>("Props/" + name);
            if (prefab == null) return false;
            foreach (var r in node.GetComponentsInChildren<Renderer>())
                if (!Kept(r.transform, node, keep)) r.enabled = false;
            var go = Object.Instantiate(prefab, node);
            go.name = name;
            // keep the prefab's own rotation and scale: the FBX importer stores the
            // Z-up to Y-up conversion there, and resetting it lays the model on its side
            go.transform.localPosition = Vector3.zero;
            // Unity's FBX import mirrors handedness, which puts a Blender model's front (-Y) on the
            // far side; a half turn brings the front round to face the player and the left back to the left
            var turn = Turns.TryGetValue(name, out var tv) ? tv : Vector2.zero;
            go.transform.localRotation = Quaternion.Euler(turn.y, 0, 0) * Quaternion.Euler(0, 180 + turn.x, 0) * go.transform.localRotation;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string key = (mats[i] != null ? mats[i].name : "").Replace(" (Instance)", "").ToLowerInvariant();
                    if (Library.TryGetValue(key, out var m)) mats[i] = m;
                    else if (mats[i] != null && mats[i].mainTexture != null)
                        // a textured model (from the picture-to-3D route) keeps its own colour map on the game's lit shader
                        mats[i] = Mats.Lit(Color.white, 0.25f).CloneWith(mats[i].mainTexture);
                    else if (Library.TryGetValue("default", out var d)) mats[i] = d;
                }
                r.sharedMaterials = mats;
            }
            Fit(go, name);
            Debug.Log($"[Fitz] Prop '{name}' loaded");
            return true;
        }

        /// <summary>Scales and shifts a model onto its slot when its size is off by more than a tenth.</summary>
        static void Fit(GameObject go, string name)
        {
            if (!Slots.TryGetValue(name, out var slot)) return;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var node = go.transform.parent;
            bool exact = Exact.Contains(name);
            var b = exact ? NodeBounds(go, node) : WorldBounds(rs);
            float largest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (largest < 1e-4f) return;
            float k = slot.size / largest;
            bool rescale = Mathf.Abs(k - 1f) > 0.1f || (exact && Mathf.Abs(k - 1f) > 0.002f);
            if (rescale) go.transform.localScale *= k;
            // measure again in the node's frame and put the pivot where the slot wants it
            Vector3 min, max;
            if (exact) { b = NodeBounds(go, node); min = b.min; max = b.max; }
            else { b = WorldBounds(rs); min = node.InverseTransformPoint(b.min); max = node.InverseTransformPoint(b.max); }
            var c = (min + max) * 0.5f;
            Vector3 want = slot.pivot switch
            {
                Pivot.Base => new Vector3(c.x, Mathf.Min(min.y, max.y), c.z),
                Pivot.Hook => new Vector3(c.x, Mathf.Max(min.y, max.y), c.z),
                Pivot.Back => new Vector3(c.x, c.y, Mathf.Max(min.z, max.z)),
                Pivot.Front => new Vector3(c.x, c.y, Mathf.Min(min.z, max.z) - slot.frontAt),
                _ => c,
            };
            if (rescale || want.magnitude > 0.08f)
                go.transform.localPosition -= want;
            Debug.Log($"[Fitz] Prop '{name}' size {largest:0.00} m{(rescale ? $" scaled x{k:0.00}" : "")}, pivot moved {want.magnitude:0.00} m");
        }

        /// <summary>
        /// Cuts away the triangles of a placed model whose centres fall inside <paramref name="box"/>
        /// (in the node's frame): a generated radar's viewing hood, say, that hides the scope
        /// from a standing player.
        /// </summary>
        public static void Trim(Transform node, string name, Bounds box)
        {
            var go = node.Find(name);
            if (go == null) return;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh;
                if (src == null || !src.isReadable) continue;
                var m = node.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var v = src.vertices;
                var mesh = Object.Instantiate(src);
                int cut = 0;
                for (int sm = 0; sm < src.subMeshCount; sm++)
                {
                    var tris = src.GetTriangles(sm);
                    var kept = new System.Collections.Generic.List<int>(tris.Length);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        var c = m.MultiplyPoint3x4((v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f);
                        if (box.Contains(c)) { cut++; continue; }
                        kept.Add(tris[i]); kept.Add(tris[i + 1]); kept.Add(tris[i + 2]);
                    }
                    mesh.SetTriangles(kept, sm);
                }
                mf.sharedMesh = mesh;
                Debug.Log($"[Fitz] Prop '{name}' trimmed {cut} triangles");
            }
        }

        static Bounds WorldBounds(Renderer[] rs)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>
        /// Bounds of the model's vertices in the node's own frame. A world box grows when the
        /// node is turned, which would size a model on an angled stand too small; this doesn't.
        /// </summary>
        static Bounds NodeBounds(GameObject go, Transform node)
        {
            bool first = true;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = node.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static bool Kept(Transform t, Transform node, string[] keep)
        {
            for (var p = t; p != null && p != node; p = p.parent)
                foreach (var k in keep)
                    if (p.name == k) return true;
            return false;
        }
    }
}
