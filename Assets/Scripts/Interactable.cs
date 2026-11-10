using System;
using UnityEngine;

namespace Fitz
{
    /// <summary>Something in the wheelhouse you can point at and click.</summary>
    public class Interactable : MonoBehaviour
    {
        public string Label;
        public Func<string> DynamicLabel;
        public Action<int> OnUse; // 0 = left click, 1 = right click

        public string Text => DynamicLabel != null ? DynamicLabel() : Label;

        public static Interactable Add(GameObject go, string label, Action<int> use, Func<string> dyn = null)
        {
            if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
            var i = go.AddComponent<Interactable>();
            i.Label = label;
            i.OnUse = use;
            i.DynamicLabel = dyn;
            return i;
        }
    }
}
