using UnityEngine;

namespace Fitz
{
    /// <summary>The one object in the scene. Builds the world in code and runs it.</summary>
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Fitz").AddComponent<Game>();
        }
    }
}
