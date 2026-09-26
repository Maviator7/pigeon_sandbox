using System;
using System.Reflection;
using UnityEngine;
namespace PigeonSandbox.Editor
{
    public static class DaylightChecks
    {
        public static void Verify()
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var root=new GameObject("Daylight verification");
            // UpdateDaylight writes scene-wide lighting; restore it so the open scene is left untouched.
            var ambientMode=RenderSettings.ambientMode; var ambientLight=RenderSettings.ambientLight;
            try
            {
                var app=root.AddComponent<SandboxApp>();var camera=root.AddComponent<Camera>();var sun=root.AddComponent<Light>();
                var town=new TownSimulation();
                typeof(SandboxApp).GetField("town",flags).SetValue(app,town);
                typeof(SandboxApp).GetField("view",flags).SetValue(app,camera);
                typeof(SandboxApp).GetField("sun",flags).SetValue(app,sun);
                var update=typeof(SandboxApp).GetMethod("UpdateDaylight",flags);
                Color previous=Color.clear;
                for(int i=0;i<=1200;i++)
                {
                    town.Time=i*.1f;update.Invoke(app,null);
                    if(sun.intensity<.75f||RenderSettings.ambientLight.grayscale<.69f||camera.backgroundColor.grayscale<.74f)
                        throw new Exception("Daylight became too dark");
                    if(i>0&&Vector4.Distance(previous,sun.color)>.005f)throw new Exception("Abrupt daylight transition");
                    previous=sun.color;
                }
                town.Time=80;update.Invoke(app,null);Color evening=sun.color;
                town.Time=40;update.Invoke(app,null);
                if(evening.b>=sun.color.b)throw new Exception("Evening should be warmer than noon");
                Debug.Log("PIGEON DAYLIGHT VERIFICATION PASSED: brightness, full-cycle continuity, warm evening");
                // Prevent the player-save quit callback from touching any real town.
                typeof(SandboxApp).GetField("town",flags).SetValue(app,null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                RenderSettings.ambientMode=ambientMode; RenderSettings.ambientLight=ambientLight;
            }
        }
    }
}
