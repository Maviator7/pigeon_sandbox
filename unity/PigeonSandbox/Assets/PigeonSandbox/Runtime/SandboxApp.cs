using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    public sealed class SandboxApp : MonoBehaviour
    {
        PigeonSimulation simulation;
        PigeonWorld world;
        Camera view;
        readonly Dictionary<int, GameObject> seeds = new Dictionary<int, GameObject>();
        float yaw=25, pitch=32, distance=11;
        bool paused, feedMode=true;
        Vector2 pointerStart;
        GUIStyle title, body, small, button;
        float UiScale => Mathf.Max(.6f, Mathf.Min(Screen.width/1100f,Screen.height/700f));
        void Start()
        {
            Application.targetFrameRate=60;
            simulation=new PigeonSimulation(new System.Random().NextDouble);
            world=gameObject.AddComponent<PigeonWorld>(); world.Initialize();
            RenderSettings.ambientLight=new Color(.68f,.73f,.7f);
            var sun=new GameObject("Afternoon sunlight").AddComponent<Light>();
            sun.type=LightType.Directional; sun.intensity=1.15f;
            sun.color=new Color(1,.94f,.82f); sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-32,0);
            QualitySettings.shadowDistance=40;
            view=new GameObject("Observation camera").AddComponent<Camera>();
            view.backgroundColor=new Color(.84f,.87f,.82f);
            view.clearFlags=CameraClearFlags.SolidColor;
            view.fieldOfView=42; view.nearClipPlane=.1f; view.farClipPlane=100;
            view.gameObject.AddComponent<AudioListener>();
            UpdateCamera();
        }
        void UpdateCamera()
        {
            var focus=new Vector3(0,.7f,0);
            view.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);
            view.transform.LookAt(focus);
        }
        bool OverUi(Vector2 p) => p.y>Screen.height-130*UiScale || p.y<112*UiScale;
        void PlaceFood(Vector2 screen)
        {
            if(paused || OverUi(screen)) return;
            var ray=view.ScreenPointToRay(screen);
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float hit))
            {
                var pos=ray.GetPoint(hit);
                if(new Vector2(pos.x,pos.z).magnitude>5.8f) return;
                if(feedMode) simulation.AddFood(pos.x,pos.z);
                else simulation.Call(pos.x,pos.z);
            }
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space)) paused=!paused;
            if(Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
            if(Input.touchCount>0)
            {
                var touch=Input.GetTouch(0);
                if(touch.phase==TouchPhase.Began) pointerStart=touch.position;
                if(Input.touchCount==1 && touch.phase==TouchPhase.Ended && Vector2.Distance(pointerStart,touch.position)<12) PlaceFood(touch.position);
                if(Input.touchCount==2)
                {
                    var other=Input.GetTouch(1);
                    float old=Vector2.Distance(touch.position-touch.deltaPosition,other.position-other.deltaPosition);
                    distance=Mathf.Clamp(distance+(old-Vector2.Distance(touch.position,other.position))*.015f,5,19);
                    yaw+=touch.deltaPosition.x*.15f;
                }
            }
            else
            {
                if(Input.GetMouseButtonDown(0)) pointerStart=Input.mousePosition;
                if(Input.GetMouseButtonUp(0) && Vector2.Distance(pointerStart,Input.mousePosition)<8) PlaceFood(Input.mousePosition);
                if(Input.GetMouseButton(1)) { yaw+=Input.GetAxis("Mouse X")*3; pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,15,70); }
                distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.5f,5,19);
            }
            UpdateCamera();
            if(!paused) simulation.Tick(Time.deltaTime);
            world.Bird.position=new Vector3(simulation.X,simulation.Y,simulation.Z);
            world.Bird.rotation=Quaternion.Euler(0,simulation.Heading*Mathf.Rad2Deg,0);
            world.Animate(simulation.Age,paused?0:simulation.Speed,simulation.State==Activity.Fly || simulation.State==Activity.Land,simulation.State==Activity.Eat);
            foreach(var seed in simulation.Seeds)
                if(!seeds.ContainsKey(seed.Id)) seeds.Add(seed.Id,world.CreateSeed(new Vector3(seed.X,.035f,seed.Z)));
            var removed=new List<int>();
            foreach(var pair in seeds)
                if(!simulation.Seeds.Exists(s=>s.Id==pair.Key)) { Destroy(pair.Value); removed.Add(pair.Key); }
            foreach(int id in removed) seeds.Remove(id);
        }
        void Styles()
        {
            if(title!=null) return;
            title=new GUIStyle(GUI.skin.label) { fontSize=30, fontStyle=FontStyle.Bold };
            body=new GUIStyle(GUI.skin.label) { fontSize=17 };
            small=new GUIStyle(GUI.skin.label) { fontSize=13 };
            foreach(var style in new[]{title,body,small}) style.normal.textColor=new Color(.18f,.25f,.22f);
            button=new GUIStyle(GUI.skin.button) { fontSize=16, padding=new RectOffset(15,15,10,10) };
        }
        void OnGUI()
        {
            if(simulation==null) return;
            Styles(); float scale=UiScale,w=Screen.width/scale,h=Screen.height/scale;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            GUI.color=new Color(.96f,.96f,.91f,.96f); GUI.DrawTexture(new Rect(0,0,w,122),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,h-106,w,106),Texture2D.whiteTexture); GUI.color=Color.white;
            GUI.Label(new Rect(28,15,550,43),"Pigeon Sandbox",title);
            GUI.Label(new Rect(30,61,650,30),"A small park. A curious pigeon. Time to slow down.",body);
            GUI.Label(new Rect(30,94,750,25),"Click: seeds / call     Right-drag: orbit     Scroll: zoom     Space: pause",small);
            GUI.Label(new Rect(w-265,24,240,30),"01 / BLUE BAR PIGEON",body);
            GUI.Label(new Rect(w-265,56,240,25),"Hunger "+simulation.Hunger.ToString("0")+"   Energy "+simulation.Energy.ToString("0"),small);
            GUI.Label(new Rect(w-265,79,240,25),"Trust "+simulation.Trust.ToString("0")+"   Meals "+simulation.Eaten,small);
            GUI.Label(new Rect(30,h-96,w-60,26),simulation.Goal+(paused?"  ·  Paused":""),body);
            float x=28,y=h-58;
            if(GUI.Button(new Rect(x,y,145,40),feedMode?"Seeds  ●":"Seeds",button)) feedMode=true;
            if(GUI.Button(new Rect(x+153,y,130,40),!feedMode?"Call  ●":"Call",button)) feedMode=false;
            GUI.enabled=!paused;
            if(GUI.Button(new Rect(x+291,y,110,40),"Fly",button)) simulation.Fly();
            if(GUI.Button(new Rect(x+409,y,110,40),"Rest",button)) simulation.Rest();
            GUI.enabled=true;
            if(GUI.Button(new Rect(x+527,y,110,40),paused?"Resume":"Pause",button)) paused=!paused;
            if(GUI.Button(new Rect(w-145,y,115,40),"Reset",button))
            {
                simulation=new PigeonSimulation(new System.Random().NextDouble); paused=false;
                foreach(var seed in seeds.Values) Destroy(seed); seeds.Clear();
            }
        }
    }
}
