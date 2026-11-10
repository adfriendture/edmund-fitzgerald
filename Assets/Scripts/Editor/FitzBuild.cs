using System;
using System.IO;
using System.Linq;
using Fitz;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fitz.EditorTools
{
    /// <summary>
    /// Headless entry points. Every method exits the editor with 0 on success.
    ///
    ///   Unity -batchmode -quit -projectPath . -executeMethod Fitz.EditorTools.FitzBuild.Setup
    ///   Unity -batchmode -quit -projectPath . -executeMethod Fitz.EditorTools.FitzBuild.BuildMac -buildOutput Builds/mac
    ///   Unity -batchmode -quit -projectPath . -executeMethod Fitz.EditorTools.FitzBuild.BuildWindows -buildOutput Builds/win
    /// </summary>
    public static class FitzBuild
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string MatDir = "Assets/Resources/Materials";
        public const string Product = "Edmund Fitzgerald";

        [MenuItem("Fitz/Setup Project")]
        public static void SetupMenu() => DoSetup();

        public static void Setup() => Run(DoSetup);
        public static void BuildMac() => Run(() => DoBuild(BuildTarget.StandaloneOSX));
        public static void BuildWindows() => Run(() => DoBuild(BuildTarget.StandaloneWindows64));

        static void Run(Action a)
        {
            try
            {
                a();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[FitzBuild] FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void DoSetup()
        {
            Directory.CreateDirectory(MatDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new Exception("URP Lit shader not found");

            // Templates the game clones at runtime. Each one pins the keyword set
            // so URP keeps those shader variants in the player build.
            var opaque = new Material(lit) { name = "LitOpaque" };
            Save(opaque);

            var emissive = new Material(lit) { name = "LitEmissive" };
            emissive.EnableKeyword("_EMISSION");
            emissive.SetColor("_EmissionColor", Color.white);
            emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            emissive.SetTexture("_EmissionMap", Texture2D.whiteTexture);
            emissive.SetTexture("_BaseMap", Texture2D.whiteTexture);
            Save(emissive);

            var transparent = new Material(lit) { name = "LitTransparent" };
            transparent.SetFloat("_Surface", 1);
            transparent.SetFloat("_Blend", 0);
            transparent.SetFloat("_ZWrite", 0);
            transparent.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            transparent.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            transparent.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            transparent.renderQueue = (int)RenderQueue.Transparent;
            transparent.SetOverrideTag("RenderType", "Transparent");
            Save(transparent);

            foreach (var s in new[] { "Ocean", "Sky", "Glass", "Precip" })
            {
                var sh = Shader.Find("Fitz/" + s);
                if (sh == null) throw new Exception("Shader Fitz/" + s + " failed to compile; check the log");
                Save(new Material(sh) { name = s });
            }

            // Boot scene: a single object that builds the whole game in code.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            go.AddComponent<Bootstrap>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Framework Foundry LLC";
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.frameworkfoundry.edmundfitzgerald");
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FitzBuild] Setup complete");
        }

        static void Save(Material m)
        {
            string path = $"{MatDir}/{m.name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = m.shader;
                existing.CopyPropertiesFromMaterial(m);
                existing.shaderKeywords = m.shaderKeywords;
                existing.renderQueue = m.renderQueue;
                EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(m, path);
        }

        static void DoBuild(BuildTarget target)
        {
            DoSetup();
            string outDir = Arg("-buildOutput") ?? Path.Combine("Builds", target == BuildTarget.StandaloneOSX ? "mac" : "windows");
            Directory.CreateDirectory(outDir);
            string loc = target == BuildTarget.StandaloneOSX
                ? Path.Combine(outDir, Product + ".app")
                : Path.Combine(outDir, Product + ".exe");

            // Universal (x64ARM64) needs Burst's llvm-lipo to be executable in the editor
            // install; pass -universal once it is. Apple Silicon only by default.
            if (target == BuildTarget.StandaloneOSX)
                UnityEditor.OSXStandalone.UserBuildSettings.architecture =
                    Environment.GetCommandLineArgs().Contains("-universal") ? OSArchitecture.x64ARM64 : OSArchitecture.ARM64;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = loc,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[FitzBuild] {target}: {s.result}, {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB -> {loc}");
            if (s.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var msg in step.messages.Where(m => m.type == LogType.Error))
                        Debug.LogError("[FitzBuild] " + msg.content);
                throw new Exception("Build failed");
            }
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
