using System.Collections.Generic;
using UnityEngine;

namespace PigeonSandbox
{
    public sealed class TownWorld : MonoBehaviour
    {
        readonly Dictionary<int, GameObject> buildings = new Dictionary<int, GameObject>();
        readonly Dictionary<int, PigeonWorld> birds = new Dictionary<int, PigeonWorld>();
        readonly Dictionary<int, Transform> people = new Dictionary<int, Transform>();
        readonly Dictionary<int, Vector3> previous = new Dictionary<int, Vector3>();
        readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        Transform preview;
        int revision=-1;
        public void Initialize()
        {
            Shape("Town island",PrimitiveType.Cube,transform,new Vector3(0,-.46f,0),new Vector3(22,.8f,22),"A6B29A");
            for(int x=-4;x<=4;x++) for(int z=-4;z<=4;z++)
                Shape("Plot "+x+","+z,PrimitiveType.Cube,transform,new Vector3(x*2.2f,-.04f,z*2.2f),new Vector3(2.16f,.12f,2.16f),(x+z)%2==0?"DFDDCB":"D8D8C5");
            for(int i=0;i<5;i++)
            {
                Shape("Station steps",PrimitiveType.Cube,transform,new Vector3(-7+i*1.6f,.1f,10.4f),new Vector3(1.5f,.3f,.6f),"B8BDAA");
            }
            var station=new GameObject("Station gateway").transform; station.SetParent(transform,false);
            Shape("Station left",PrimitiveType.Cube,station,new Vector3(-2,1.1f,10.4f),new Vector3(.4f,2.2f,.4f),"47665D");
            Shape("Station right",PrimitiveType.Cube,station,new Vector3(2,1.1f,10.4f),new Vector3(.4f,2.2f,.4f),"47665D");
            Shape("Station lintel",PrimitiveType.Cube,station,new Vector3(0,2.2f,10.4f),new Vector3(4.5f,.5f,.6f),"47665D");
            preview=new GameObject("Plot cursor").transform; preview.SetParent(transform,false);
            foreach(int sign in new[]{-1,1})
            {
                Shape("Outline",PrimitiveType.Cube,preview,new Vector3(sign*1.04f,.13f,0),new Vector3(.055f,.04f,2.1f),"52765E");
                Shape("Outline",PrimitiveType.Cube,preview,new Vector3(0,.13f,sign*1.04f),new Vector3(2.1f,.04f,.055f),"52765E");
            }
            preview.gameObject.SetActive(false);
        }
        Material Mat(string hex)
        {
            if(palette.TryGetValue(hex,out var value)) return value;
            ColorUtility.TryParseHtmlString("#"+hex,out var color);
            value=new Material(Shader.Find("Standard")) { color=color };
            value.SetFloat("_Glossiness",.12f); palette.Add(hex,value); return value;
        }
        Transform Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,string color)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=name; obj.transform.SetParent(parent,false);
            obj.transform.localPosition=pos; obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=Mat(color); Destroy(obj.GetComponent<Collider>());
            return obj.transform;
        }
        public void Preview(int x,int z,bool show,bool valid)
        {
            preview.gameObject.SetActive(show); preview.position=new Vector3(x*2.2f,0,z*2.2f);
            foreach(var r in preview.GetComponentsInChildren<Renderer>()) r.sharedMaterial=Mat(valid?"487F65":"C26D52");
        }
        void FacilityModel(Facility f)
        {
            var root=new GameObject(TownSimulation.NameOf(f.Kind)); root.transform.SetParent(transform,false);
            root.transform.localPosition=new Vector3(f.X*2.2f,0,f.Z*2.2f); buildings[f.Id]=root;
            var p=root.transform;
            if(f.Kind==FacilityKind.Bakery)
            {
                Shape("Bakery walls",PrimitiveType.Cube,p,new Vector3(0,.65f,.1f),new Vector3(1.55f,1.3f,1.3f),"E1B489");
                Shape("Terracotta roof",PrimitiveType.Cube,p,new Vector3(0,1.37f,.12f),new Vector3(1.75f,.2f,1.55f),"B56B4C");
                Shape("Shop window",PrimitiveType.Cube,p,new Vector3(-.38f,.62f,-.562f),new Vector3(.56f,.62f,.035f),"526F68");
                Shape("Door",PrimitiveType.Cube,p,new Vector3(.38f,.5f,-.562f),new Vector3(.45f,.95f,.04f),"59766D");
                for(int i=0;i<4;i++) Shape("Awning",PrimitiveType.Cube,p,new Vector3(-.63f+i*.42f,1.04f,-.77f),new Vector3(.42f,.12f,.55f),i%2==0?"F2E3C5":"BA7255");
                Shape("Bread sign",PrimitiveType.Sphere,p,new Vector3(0,1.62f,.05f),new Vector3(.65f,.25f,.24f),"DCA955");
            }
            else if(f.Kind==FacilityKind.Fountain)
            {
                Shape("Basin",PrimitiveType.Cylinder,p,new Vector3(0,.12f,0),new Vector3(1.8f,.12f,1.8f),"A0B2AC");
                Shape("Water",PrimitiveType.Cylinder,p,new Vector3(0,.25f,0),new Vector3(1.56f,.025f,1.56f),"70ABA9");
                Shape("Water column",PrimitiveType.Cylinder,p,new Vector3(0,.51f,0),new Vector3(.15f,.27f,.15f),"B3D7CF");
                Shape("Upper bowl",PrimitiveType.Sphere,p,new Vector3(0,.73f,0),new Vector3(.58f,.14f,.58f),"BCD6CE");
            }
            else if(f.Kind==FacilityKind.Housing)
            {
                float h=1.8f+f.Level*.2f;
                Shape("Apartments",PrimitiveType.Cube,p,new Vector3(0,h/2,0),new Vector3(1.5f,h,1.45f),"D2C5AB");
                Shape("Roof garden",PrimitiveType.Cube,p,new Vector3(0,h+.09f,0),new Vector3(1.7f,.18f,1.6f),"7B9582");
                for(int row=0;row<2;row++) for(int col=0;col<2;col++)
                    Shape("Window",PrimitiveType.Cube,p,new Vector3(-.37f+col*.74f,.57f+row*.85f,-.735f),new Vector3(.38f,.45f,.03f),"557A78");
                Shape("Nest box",PrimitiveType.Cube,p,new Vector3(.5f,h+.3f,.2f),new Vector3(.38f,.35f,.4f),"B48A5D");
            }
            else if(f.Kind==FacilityKind.Tree)
            {
                Shape("Garden",PrimitiveType.Cylinder,p,new Vector3(0,.055f,0),new Vector3(1.95f,.055f,1.95f),"94AA80");
                Shape("Trunk",PrimitiveType.Cylinder,p,new Vector3(0,.7f,0),new Vector3(.18f,.7f,.18f),"896D51");
                Shape("Canopy",PrimitiveType.Sphere,p,new Vector3(0,1.75f,0),new Vector3(1.25f,1.65f,1.25f),"77986E");
                Bench(p,new Vector3(0,.12f,-.68f));
            }
            else if(f.Kind==FacilityKind.Plaza)
            {
                Shape("Public square",PrimitiveType.Cube,p,new Vector3(0,.045f,0),new Vector3(2.04f,.06f,2.04f),"EAE5CE");
                Bench(p,new Vector3(0,.1f,.7f));
                Shape("Grain planter",PrimitiveType.Cylinder,p,new Vector3(-.65f,.14f,-.5f),new Vector3(.35f,.1f,.35f),"D3AC6C");
            }
            else
            {
                Shape("Tower base",PrimitiveType.Cube,p,new Vector3(0,.14f,0),new Vector3(1.3f,.28f,1.3f),"A1A993");
                Shape("Clock tower",PrimitiveType.Cube,p,new Vector3(0,1.45f,0),new Vector3(.8f,2.6f,.8f),"C9B892");
                Shape("Tower cap",PrimitiveType.Cube,p,new Vector3(0,2.85f,0),new Vector3(1.1f,.25f,1.1f),"547566");
                var face=Shape("Clock face",PrimitiveType.Cylinder,p,new Vector3(0,2.28f,-.421f),new Vector3(.55f,.02f,.55f),"F1E5C9"); face.localRotation=Quaternion.Euler(90,0,0);
                Shape("Hour hand",PrimitiveType.Cube,p,new Vector3(.06f,2.28f,-.45f),new Vector3(.15f,.04f,.025f),"435F53");
                Shape("Minute hand",PrimitiveType.Cube,p,new Vector3(0,2.37f,-.45f),new Vector3(.035f,.21f,.025f),"435F53");
            }
            if(f.Level>1) Shape("Improvement garden",PrimitiveType.Sphere,p,new Vector3(.78f,.2f,.7f),new Vector3(.35f,.4f,.35f),"80A479");
        }
        void Bench(Transform p,Vector3 at)
        {
            Shape("Bench seat",PrimitiveType.Cube,p,at+new Vector3(0,.23f,0),new Vector3(1.15f,.12f,.35f),"AF8A5D");
            Shape("Bench back",PrimitiveType.Cube,p,at+new Vector3(0,.45f,.16f),new Vector3(1.15f,.36f,.07f),"AF8A5D");
            foreach(int s in new[]{-1,1}) Shape("Bench foot",PrimitiveType.Cube,p,at+new Vector3(s*.43f,.07f,0),new Vector3(.08f,.3f,.3f),"4F6D5B");
        }
        public void Sync(TownSimulation town,bool paused)
        {
            if(revision!=town.Revision)
            {
                foreach(var building in buildings.Values) Destroy(building);
                buildings.Clear(); foreach(var f in town.Facilities) FacilityModel(f); revision=town.Revision;
            }
            foreach(var b in town.Birds)
            {
                if(!birds.TryGetValue(b.Id,out var model))
                {
                    var obj=new GameObject(b.Name); obj.transform.SetParent(transform,false);
                    model=obj.AddComponent<PigeonWorld>(); model.InitializeBirdOnly(b.Rare,b.Mayor);
                    model.transform.localScale=Vector3.one*.46f; birds.Add(b.Id,model);
                }
                var pos=new Vector3(b.X,b.Y+.06f,b.Z);
                float speed=previous.TryGetValue(b.Id,out var old)?Vector3.Distance(old,pos)/Mathf.Max(.001f,UnityEngine.Time.deltaTime):0;
                model.transform.position=pos; model.transform.rotation=Quaternion.Euler(0,b.Heading*Mathf.Rad2Deg,0);
                model.Animate(town.Time,paused?0:speed,b.Y>.15f,b.Action=="食事" || b.Action=="水浴び"); previous[b.Id]=pos;
            }
            var ids=new HashSet<int>();
            foreach(var v in town.Visitors)
            {
                ids.Add(v.Id);
                if(!people.TryGetValue(v.Id,out var human))
                {
                    human=new GameObject("Visitor").transform; human.SetParent(transform,false);
                    Shape("Coat",PrimitiveType.Capsule,human,new Vector3(0,.4f,0),new Vector3(.29f,.32f,.25f),v.Id%2==0?"BE886A":"718E94");
                    Shape("Head",PrimitiveType.Sphere,human,new Vector3(0,.84f,0),Vector3.one*.25f,"D8B995");
                    foreach(int s in new[]{-1,1}) Shape("Leg",PrimitiveType.Cube,human,new Vector3(s*.07f,.1f,0),new Vector3(.08f,.25f,.1f),"53645B");
                    people.Add(v.Id,human);
                }
                human.position=new Vector3(v.X,.065f,v.Z); human.rotation=Quaternion.Euler(0,v.Heading*Mathf.Rad2Deg,0);
            }
            var gone=new List<int>(); foreach(var pair in people) if(!ids.Contains(pair.Key)) { Destroy(pair.Value.gameObject); gone.Add(pair.Key); }
            foreach(int id in gone) people.Remove(id);
        }
    }
}
