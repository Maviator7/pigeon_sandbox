using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    // Run inside Unity to exercise the real camera projections without changing a saved town.
    public static class CameraChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static object Call(SandboxApp app,string name,params object[] args) => typeof(SandboxApp).GetMethod(name,Private).Invoke(app,args);
        static T Read<T>(SandboxApp app,string name) => (T)typeof(SandboxApp).GetField(name,Private).GetValue(app);
        static void Set(SandboxApp app,string name,object value) => typeof(SandboxApp).GetField(name,Private).SetValue(app,value);
        static void Check(bool condition,string message) { if(!condition) throw new Exception(message); Debug.Log("CAMERA PASS: "+message); }
        static void Step(SandboxApp app) { for(int i=0;i<180;i++) Call(app,"UpdateCamera",1f/60); }

        [MenuItem("Pigeon Sandbox/Verify camera controls")]
        public static void Verify()
        {
            var root=new GameObject("Camera verification");
            var cameraObject=new GameObject("Verification camera");
            try
            {
                var app=root.AddComponent<SandboxApp>();
                var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true;
                var town=new TownSimulation(9); var bird=town.Birds[0];
                Set(app,"town",town); Set(app,"view",camera);
                float money=town.Money; int count=town.Facilities.Count;
                Call(app,"CameraPosition");
                Call(app,"FocusBird",bird.Id); Step(app);
                var projected=camera.WorldToViewportPoint(new Vector3(bird.X,bird.Y+.4f,bird.Z));
                Check(Mathf.Abs(projected.x-.5f)<.001f&&Mathf.Abs(projected.y-.5f)<.001f,"selected pigeon is centered");
                Check(Mathf.Abs(camera.orthographicSize-4)<.001f,"focus zoom converges");
                bird.X+=2; bird.Z-=1; bird.Y=2; Step(app);
                projected=camera.WorldToViewportPoint(new Vector3(bird.X,bird.Y+.4f,bird.Z));
                Check(Mathf.Abs(projected.x-.5f)<.001f&&Mathf.Abs(projected.y-.5f)<.001f,"moving and flying pigeon stays centered");
                Call(app,"StopFollowing"); Vector3 fixedFocus=Read<Vector3>(app,"cameraFocus"); bird.X+=1; Step(app);
                Check(Read<Vector3>(app,"cameraFocus")==fixedFocus,"release preserves current view");
                Call(app,"FocusBird",bird.Id); Call(app,"SelectBuilding",FacilityKind.Fountain);
                Check(Read<int>(app,"followedBird")==-1,"construction releases follow");
                Check(Read<FacilityKind>(app,"building")==FacilityKind.Fountain,"construction tool still selects facility");
                Call(app,"FocusBird",bird.Id); Step(app);
                Vector2 from=camera.pixelRect.center;
                Call(app,"PanCamera",from,from+new Vector2(30,20));
                Check(Read<int>(app,"followedBird")==-1,"pan releases follow");
                Call(app,"ResetCamera"); Step(app);
                Check(Read<Vector3>(app,"cameraFocus")==new Vector3(0,.3f,0)&&Mathf.Abs(camera.orthographicSize-14)<.001f,"overview restores center and zoom");
                var toolbar=(Rect)Call(app,"CameraToolbar");
                float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
                Vector2 uiPoint=new Vector2(toolbar.center.x*scale,Screen.height-toolbar.center.y*scale);
                Check(!(bool)Call(app,"InTown",uiPoint),"toolbar excluded from world input");
                Call(app,"FocusBird",int.MaxValue); Step(app);
                Check(Read<int>(app,"followedBird")==-1,"missing pigeon safely releases follow");
                Check(town.Money==money&&town.Facilities.Count==count,"camera controls do not change budget or buildings");
                town.Money=10000; while(town.CanExpand) town.ExpandTown();
                Call(app,"ResetCamera"); Step(app);
                Check(Mathf.Abs(camera.orthographicSize-26)<.001f,"expanded overview scales with land size");
                foreach(float x in new[]{-17.6f,17.6f}) foreach(float z in new[]{-17.6f,17.6f})
                {
                    var corner=camera.WorldToViewportPoint(new Vector3(x,0,z));
                    Check(corner.x>=0&&corner.x<=1&&corner.y>=0&&corner.y<=1,"expanded corners fit in overview");
                }
                Debug.Log("PIGEON CAMERA VERIFICATION PASSED");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); }
        }
    }
}
