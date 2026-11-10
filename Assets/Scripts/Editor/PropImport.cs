using UnityEditor;

namespace Fitz.EditorTools
{
    /// <summary>
    /// Drop-in props stay readable after import, so the game can measure their vertices
    /// when it fits a generated model to its slot (Props.Fit).
    /// </summary>
    public class PropImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Resources/Props/")) return;
            ((ModelImporter)assetImporter).isReadable = true;
        }
    }
}
