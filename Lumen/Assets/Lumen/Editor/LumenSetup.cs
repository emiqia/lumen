using System.IO;
using System.Linq;
using Lumen.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lumen.EditorTools
{
    /// <summary>
    /// One-click project configuration and builds. Also callable from the command line:
    ///   Unity -batchmode -quit -projectPath . -executeMethod Lumen.EditorTools.LumenSetup.SetupAndBuildLinux
    /// </summary>
    public static class LumenSetup
    {
        const string ScenePath = "Assets/Lumen/Scenes/Main.unity";
        const string ResourcesDir = "Assets/Lumen/Resources";
        static readonly string[] RequiredLayers = { "Walls", "Player", "Enemies", "Pickups" };

        [MenuItem("Lumen/Setup Project")]
        public static void Setup()
        {
            EnsureLayers();
            EnsureAssets();
            EnsureScene();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Lumen] Project setup complete.");
        }

        [MenuItem("Lumen/Build/Linux")]
        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Builds/Linux/Lumen.x86_64");

        [MenuItem("Lumen/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Lumen.exe");

        public static void SetupAndBuildLinux()
        {
            Setup();
            BuildLinux();
        }

        public static void SetupAndBuildAll()
        {
            Setup();
            BuildLinux();
            BuildWindows();
        }

        static void Build(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Lumen] Build failed for {target}: {report.summary.result}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[Lumen] Built {target} -> {path} ({report.summary.totalSize / (1024 * 1024)} MB)");
        }

        static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < RequiredLayers.Length; i++)
            {
                var prop = layers.GetArrayElementAtIndex(8 + i);
                if (string.IsNullOrEmpty(prop.stringValue) || prop.stringValue == RequiredLayers[i])
                    prop.stringValue = RequiredLayers[i];
                else
                    Debug.LogWarning($"[Lumen] Layer {8 + i} is already used by '{prop.stringValue}'.");
            }
            tagManager.ApplyModifiedProperties();
        }

        static void EnsureAssets()
        {
            Directory.CreateDirectory(ResourcesDir);

            var assets = LoadOrCreate<LumenAssets>(ResourcesDir + "/LumenAssets.asset");
            assets.spriteLit = FindMaterial("Sprite-Lit-Default");
            assets.spriteUnlit = FindMaterial("Sprite-Unlit-Default");
            EditorUtility.SetDirty(assets);

            LoadOrCreate<GameTuning>(ResourcesDir + "/GameTuning.asset");
        }

        static Material FindMaterial(string name)
        {
            string[] candidates =
            {
                $"Packages/com.unity.render-pipelines.universal/Runtime/Materials/{name}.mat",
                $"Packages/com.unity.render-pipelines.universal/Runtime/Materials/2D/{name}.mat",
            };
            foreach (var path in candidates)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null) return m;
            }
            var guid = AssetDatabase.FindAssets(name + " t:Material").FirstOrDefault();
            if (guid != null) return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            Debug.LogWarning("[Lumen] Material not found: " + name);
            return null;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Lumen").AddComponent<GameManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "emiqia";
            PlayerSettings.productName = "LUMEN";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }
    }
}
