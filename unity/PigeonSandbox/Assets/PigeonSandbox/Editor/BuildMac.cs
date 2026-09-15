using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    public static class BuildMac
    {
        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if(Application.isBatchMode || File.Exists("Assets/Scenes/Park.unity")) return;
            EditorApplication.delayCall += () => { if(!File.Exists("Assets/Scenes/Park.unity")) Scene(); };
        }
        static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
        [MenuItem("Pigeon Sandbox/Verify simulation")]
        public static void Verify()
        {
            var p=new PigeonSimulation(new System.Random(42).NextDouble);
            p.AddFood(2,1);
            for(int i=0;i<600 && p.Eaten==0;i++) p.Tick(1f/60);
            Assert(p.Eaten==1 && p.Seeds.Count==0 && p.Trust>32,"Food approach, consumption and trust");
            p.Call(-3,-2);
            for(int i=0;i<1000 && p.State==Activity.Walk;i++) p.Tick(1f/60);
            Assert(Math.Abs(p.X+3)<.3 && Math.Abs(p.Z+2)<.3,"Call destination");
            p.Fly(); bool airborne=false,landed=false;
            for(int i=0;i<310;i++) { p.Tick(1f/60); airborne |= p.Y>1; landed |= airborne && p.Y==0; }
            Assert(airborne && landed,"Flight and soft landing");
            var a=new PigeonSimulation(new System.Random(7).NextDouble);
            var b=new PigeonSimulation(new System.Random(7).NextDouble);
            for(int i=0;i<10000;i++)
            {
                a.Tick(1f/60); b.Tick(1f/60);
                Assert(a.X*a.X+a.Z*a.Z<=36 && a.Y>=0,"Park bounds");
                Assert(a.Energy>=0 && a.Energy<=100 && a.Hunger>=0 && a.Hunger<=100,"Need bounds");
            }
            Assert(a.X==b.X && a.Z==b.Z && a.State==b.State,"Injected randomness determinism");
            for(int i=0;i<100;i++) a.AddFood(100,100);
            Assert(a.Seeds.Count==60,"Food capacity");
            foreach(var seed in a.Seeds) Assert(seed.X*seed.X+seed.Z*seed.Z<34,"Food bounds");
            Debug.Log("PIGEON VERIFICATION PASSED: food, call, flight, bounds, determinism, capacity");
        }
        [MenuItem("Pigeon Sandbox/Create observation scene")]
        public static void Scene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Pigeon Sandbox").AddComponent<SandboxApp>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Park.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Park.unity",true)};
            PlayerSettings.productName="Pigeon Sandbox";
            PlayerSettings.companyName="PigeonSandbox";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=800;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            // Runtime procedural materials need this shader included in the player.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=settings.FindProperty("m_AlwaysIncludedShaders");
            int size=shaders.arraySize; shaders.InsertArrayElementAtIndex(size);
            shaders.GetArrayElementAtIndex(size).objectReferenceValue=Shader.Find("Standard");
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Pigeon Sandbox/Build Mac app")]
        public static void Build()
        {
            Verify(); Scene();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/Park.unity"}, locationPathName="Builds/Mac/Pigeon Sandbox.app",
                target=BuildTarget.StandaloneOSX, options=BuildOptions.None
            });
            Assert(result.summary.result==BuildResult.Succeeded,"Mac build failed: "+result.summary.result);
            Debug.Log("PIGEON MAC BUILD PASSED");
        }
    }
}
