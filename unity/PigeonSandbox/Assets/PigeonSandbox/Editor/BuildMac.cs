using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    public static class BuildMac
    {
        public const string ReleaseVersion = "0.3.0";
        const string ParkScene = "Assets/Scenes/Park.unity";
        const string BenchmarkScene = "Assets/Scenes/Benchmark.unity";
        const string VertexColorShader = "Assets/PigeonSandbox/Shaders/VertexColorLit.shader";
        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if (Application.isBatchMode || File.Exists(ParkScene))
                return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ParkScene))
                    Scene();
            }

            ;
        }

        static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        // Quick in-Editor smoke test. The full suite lives in Tests/ and runs through Tests/verify.py.
        [MenuItem("Pigeon Sandbox/Verify simulation")]
        public static void Verify()
        {
            var town = new TownSimulation(9);
            Assert(town.Build(FacilityKind.Bakery, 1, 0), "Town build");
            for (int i = 0; i < 1500; i++)
                town.Tick(1f / 60);
            Assert(town.Purchases > 0 && town.Income > 0, "Town visitor economy");
            Assert(town.Move(town.At(1, 0).Id, 3, 3), "Town free relocation");
            town.TogglePolicy(TownPolicy.NestBoxes);
            Assert(town.NestBoxes, "Town policy");
            var a = new TownSimulation(7);
            var b = new TownSimulation(7);
            for (int i = 0; i < 3000; i++)
            {
                a.Tick(.1f);
                b.Tick(.1f);
            }

            Assert(a.Money == b.Money && a.Birds[0].X == b.Birds[0].X && a.Birds[0].Z == b.Birds[0].Z, "Seeded town determinism");
            var benchmark = TownSimulation.CreateBenchmark();
            Assert(benchmark.Facilities.Count == benchmark.MapSize * benchmark.MapSize && benchmark.Birds.Count == 15, "Benchmark town is full");
            FriendshipChecks.Verify();
            DaylightChecks.Verify();
            WorldSyncChecks.Verify();
            FontChecks.Verify();
            Debug.Log("PIGEON VERIFICATION PASSED: build, economy, relocation, policy, determinism, benchmark town, save JSON, daylight, world sync, font");
        }

        [MenuItem("Pigeon Sandbox/Create observation scene")]
        public static void Scene() => CreateScene(ParkScene, false);
        [MenuItem("Pigeon Sandbox/Create benchmark scene")]
        public static void CreateBenchmarkScene() => CreateScene(BenchmarkScene, true);
        static void CreateScene(string path, bool benchmark)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Pigeon Sandbox").AddComponent<SandboxApp>();
            var serialized = new SerializedObject(app);
            serialized.FindProperty("benchmark").boolValue = benchmark;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            ConfigurePlayer();
        }

        // Scenes are only generated when missing, so repeated builds leave the checked-in scenes untouched.
        static void EnsureScene(string path, bool benchmark)
        {
            if (!File.Exists(path))
                CreateScene(path, benchmark);
        }

        // Writes settings only when they differ, so builds do not produce ProjectSettings churn.
        static void ConfigurePlayer()
        {
            bool changed = false;
            void Set<T>(Func<T> get, Action<T> set, T value)
            {
                if (Equals(get(), value))
                    return;
                set(value);
                changed = true;
            }

            Set(() => PlayerSettings.productName, v => PlayerSettings.productName = v, "Pigeon Sandbox");
            Set(() => PlayerSettings.bundleVersion, v => PlayerSettings.bundleVersion = v, ReleaseVersion);
            Set(() => PlayerSettings.companyName, v => PlayerSettings.companyName = v, "PigeonSandbox");
            Set(() => PlayerSettings.defaultScreenWidth, v => PlayerSettings.defaultScreenWidth = v, 1280);
            Set(() => PlayerSettings.defaultScreenHeight, v => PlayerSettings.defaultScreenHeight = v, 800);
            Set(() => PlayerSettings.fullScreenMode, v => PlayerSettings.fullScreenMode = v, FullScreenMode.Windowed);
            Set(() => PlayerSettings.resizableWindow, v => PlayerSettings.resizableWindow = v, true);
            Set(() => PlayerSettings.runInBackground, v => PlayerSettings.runInBackground = v, false);
            // FrameTimingManager needs this for the benchmark's CPU/GPU frame times.
            Set(() => PlayerSettings.enableFrameTimingStats, v => PlayerSettings.enableFrameTimingStats = v, true);
            var scenes = new[]{ParkScene, BenchmarkScene}.Where(File.Exists).ToArray();
            if (!EditorBuildSettings.scenes.Select(s => s.path).SequenceEqual(scenes))
                EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s, s == ParkScene)).ToArray();
            // iOS: landscape only (the town UI is laid out for wide screens) and automatic signing.
            // PIGEON_IOS_BUNDLE_ID / PIGEON_APPLE_TEAM override the identifier and team for a local device build.
            string bundleId = Environment.GetEnvironmentVariable("PIGEON_IOS_BUNDLE_ID");
            if (string.IsNullOrEmpty(bundleId))
                bundleId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            if (string.IsNullOrEmpty(bundleId) || bundleId == "com.Company.ProductName")
                bundleId = "com.pigeonsandbox.mayortown";
            Set(() => PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS), v => PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, v), bundleId);
            Set(() => PlayerSettings.defaultInterfaceOrientation, v => PlayerSettings.defaultInterfaceOrientation = v, UIOrientation.AutoRotation);
            Set(() => PlayerSettings.allowedAutorotateToPortrait, v => PlayerSettings.allowedAutorotateToPortrait = v, false);
            Set(() => PlayerSettings.allowedAutorotateToPortraitUpsideDown, v => PlayerSettings.allowedAutorotateToPortraitUpsideDown = v, false);
            Set(() => PlayerSettings.allowedAutorotateToLandscapeLeft, v => PlayerSettings.allowedAutorotateToLandscapeLeft = v, true);
            Set(() => PlayerSettings.allowedAutorotateToLandscapeRight, v => PlayerSettings.allowedAutorotateToLandscapeRight = v, true);
            Set(() => PlayerSettings.iOS.appleEnableAutomaticSigning, v => PlayerSettings.iOS.appleEnableAutomaticSigning = v, true);
            // Runtime-created materials need their shaders in the player. Keep exactly one entry for each.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            foreach (var shader in new[]{Shader.Find("Standard"), AssetDatabase.LoadAssetAtPath<Shader>(VertexColorShader)})
            {
                bool included = false;
                for (int i = shaders.arraySize - 1; i >= 0; i--)
                {
                    if (shaders.GetArrayElementAtIndex(i).objectReferenceValue != shader)
                        continue;
                    if (!included)
                    {
                        included = true;
                        continue;
                    }

                    shaders.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    shaders.DeleteArrayElementAtIndex(i);
                }

                if (!included)
                {
                    int size = shaders.arraySize;
                    shaders.InsertArrayElementAtIndex(size);
                    shaders.GetArrayElementAtIndex(size).objectReferenceValue = shader;
                }
            }

            changed |= settings.ApplyModifiedPropertiesWithoutUndo();
            if (changed)
                AssetDatabase.SaveAssets();
        }

        [MenuItem("Pigeon Sandbox/Build Mac app")]
        public static void Build()
        {
            Verify();
            EnsureScene(ParkScene, false);
            ConfigurePlayer();
            BuildPlayer(new[]{ParkScene}, "Builds/Mac/Pigeon Sandbox.app", BuildOptions.None);
            Debug.Log("PIGEON MAC BUILD PASSED");
        }

        // Development build of the full-map benchmark. Launch with -benchmark-quit to log results and exit.
        [MenuItem("Pigeon Sandbox/Build Mac benchmark")]
        public static void BuildBenchmark()
        {
            Verify();
            EnsureScene(BenchmarkScene, true);
            ConfigurePlayer();
            BuildPlayer(new[]{BenchmarkScene}, "Builds/MacBenchmark/Pigeon Sandbox Benchmark.app", BuildOptions.Development);
            Debug.Log("PIGEON MAC BENCHMARK BUILD PASSED");
        }

        // Development build of the benchmark as an Xcode project. Open Builds/iOSBenchmark/Unity-iPhone.xcodeproj,
        // pick your team if PIGEON_APPLE_TEAM was not set, and run on a connected device. Results show on screen
        // and in the Xcode console as the PIGEON BENCHMARK line.
        [MenuItem("Pigeon Sandbox/Build iOS benchmark (Xcode project)")]
        public static void BuildIOSBenchmark()
        {
            Verify();
            EnsureScene(BenchmarkScene, true);
            ConfigurePlayer();
            // The signing team is personal: apply PIGEON_APPLE_TEAM for this build only and never save it to ProjectSettings.
            string previousTeam = PlayerSettings.iOS.appleDeveloperTeamID;
            string team = Environment.GetEnvironmentVariable("PIGEON_APPLE_TEAM");
            try
            {
                if (!string.IsNullOrEmpty(team))
                    PlayerSettings.iOS.appleDeveloperTeamID = team;
                BuildPlayer(new[]{BenchmarkScene}, "Builds/iOSBenchmark", BuildOptions.Development, BuildTarget.iOS);
                // Unity writes CADisableMinimumFrameDurationOnPhone=false, capping ProMotion iPhones at 60Hz.
                // The benchmark lifts it so results show headroom; the game itself stays at 60fps.
                var plistBuddy = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/usr/libexec/PlistBuddy", "-c \"Set :CADisableMinimumFrameDurationOnPhone true\" Builds/iOSBenchmark/Info.plist")
                {UseShellExecute = false});
                plistBuddy.WaitForExit();
                Assert(plistBuddy.ExitCode == 0, "Could not lift the 60Hz cap in Info.plist");
            }
            finally
            {
                PlayerSettings.iOS.appleDeveloperTeamID = previousTeam;
                // The build saves ProjectSettings with the temporary team; write the original back.
                AssetDatabase.SaveAssets();
            }

            Debug.Log("PIGEON IOS BENCHMARK PROJECT PASSED");
        }

        static void BuildPlayer(string[] scenes, string location, BuildOptions options, BuildTarget target = BuildTarget.StandaloneOSX)
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes = scenes, locationPathName = location, target = target, options = options});
            Assert(result.summary.result == BuildResult.Succeeded, target + " build failed: " + result.summary.result);
        }
    }
}
