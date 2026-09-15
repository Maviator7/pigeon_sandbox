using System;
using System.IO;
using UnityEngine;
namespace PigeonSandbox
{
 public sealed class SandboxApp : MonoBehaviour
 {
  TownSimulation town; TownWorld world; Camera view; Font font;
  GUIStyle title,heading,text,small,button;
  float yaw=35,pitch=48,zoom=14,saveClock;
  int speed=1,selected=-1,tab;
  FacilityKind building=FacilityKind.Bakery;
  enum Tool { Inspect,Build,Move }
  Tool tool=Tool.Build;
  Vector2 pointerStart,scroll;
  string message="広場のそばにベーカリーを建ててみましょう。";
  float Scale=>Mathf.Min(Screen.width/1440f,Screen.height/900f);
  float Width=>Screen.width/Scale; float Height=>Screen.height/Scale;
  string SavePath=>Path.Combine(Application.persistentDataPath,"mayor-town.json");
  readonly Color ink=new Color(.19f,.27f,.23f),green=new Color(.27f,.41f,.33f),paper=new Color(.96f,.95f,.9f);
  static readonly string[] Tips={"広場から2マス以内で混雑を軽減。噴水を添えると鳩の生活圏に。","パン屋の2マス以内で食事と水浴びを楽しめる場所に。","木や噴水が近く、店舗から離れた場所なら静かな寝床に。","住宅のそばで静かな緑地に。清掃の負担もやわらげます。","パン屋に近づけると混雑をやわらげ、人と鳩の居場所を確保。","観光と目立ちたがりの鳩のための名所。静かな住宅からは距離を。"};
  void Start()
  {
   Application.targetFrameRate=60; font=Resources.Load<Font>("NotoSansJP"); town=new TownSimulation(); Load();
   world=new GameObject("Mayor town").AddComponent<TownWorld>(); world.Initialize();
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.72f,.75f,.7f);
   var sun=new GameObject("Afternoon").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=.85f;
   sun.color=new Color(1,.95f,.84f); sun.shadows=LightShadows.Soft; sun.shadowStrength=.55f; sun.transform.rotation=Quaternion.Euler(52,-35,0);
   QualitySettings.shadowDistance=70;
   view=new GameObject("Town camera").AddComponent<Camera>(); view.clearFlags=CameraClearFlags.SolidColor;
   view.backgroundColor=new Color(.76f,.8f,.71f); view.orthographic=true; view.nearClipPlane=.1f; view.farClipPlane=100;
   view.gameObject.AddComponent<AudioListener>(); CameraPosition(); world.Sync(town,false);
  }
  void CameraPosition()
  {
   view.rect=new Rect(242f/Width,142f/Height,(Width-532f)/Width,(Height-254f)/Height);
   var focus=new Vector3(0,.3f,0); view.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-35);
   view.transform.LookAt(focus); view.orthographicSize=zoom;
  }
  bool InTown(Vector2 point) { var p=point/Scale; p.y=Height-p.y; return p.x>242&&p.x<Width-290&&p.y>142&&p.y<Height-112; }
  bool GridPoint(Vector2 point,out int x,out int z)
  {
   x=z=0; if(!InTown(point)) return false; var ray=view.ScreenPointToRay(point);
   if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float d)) return false;
   var p=ray.GetPoint(d); x=Mathf.RoundToInt(p.x/2.2f); z=Mathf.RoundToInt(p.z/2.2f); return x>=-4&&x<=4&&z>=-4&&z<=4;
  }
  void ClickTown(Vector2 point)
  {
   if(!GridPoint(point,out int x,out int z)) return; var existing=town.At(x,z);
   if(tool==Tool.Move)
   {
    if(town.Move(selected,x,z)) { message="移設しました。鳩の行き先も新しい配置に変わります。"; tool=Tool.Inspect; }
    else message="空いているマスを選んでください。";
   }
   else if(existing!=null) { selected=existing.Id; tool=Tool.Inspect; message=Tips[(int)existing.Kind]; }
   else if(tool==Tool.Build) { if(town.Build(building,x,z)) { selected=town.At(x,z).Id; message=Tips[(int)building]; } else message=town.Notice; }
   else { selected=-1; message="左の施設を選んで、空き地をクリックすると建設できます。"; }
  }
  void Update()
  {
   if(Input.GetKeyDown(KeyCode.Space)) speed=speed==0?1:0;
   if(Input.GetKeyDown(KeyCode.Escape)) { tool=Tool.Inspect; selected=-1; }
   if(Input.touchCount>0)
   {
    var t=Input.GetTouch(0); if(t.phase==TouchPhase.Began) pointerStart=t.position;
    if(Input.touchCount==1&&t.phase==TouchPhase.Ended&&Vector2.Distance(pointerStart,t.position)<12) ClickTown(t.position);
    if(Input.touchCount==2) { var b=Input.GetTouch(1); zoom=Mathf.Clamp(zoom+(Vector2.Distance(t.position-t.deltaPosition,b.position-b.deltaPosition)-Vector2.Distance(t.position,b.position))*.025f,7,20); yaw+=t.deltaPosition.x*.18f; }
   }
   else
   {
    if(Input.GetMouseButtonDown(0)) pointerStart=Input.mousePosition;
    if(Input.GetMouseButtonUp(0)&&Vector2.Distance(pointerStart,Input.mousePosition)<7) ClickTown(Input.mousePosition);
    if(InTown(Input.mousePosition)) { if(Input.GetMouseButton(1)) { yaw+=Input.GetAxis("Mouse X")*3; pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,30,75); } zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*.55f,7,20); }
   }
   CameraPosition(); bool hover=GridPoint(Input.mousePosition,out int hx,out int hz);
   world.Preview(hx,hz,hover&&tool!=Tool.Inspect,town.CanPlace(hx,hz,tool==Tool.Move?selected:-1)&&(tool!=Tool.Build||town.Money>=TownSimulation.Cost(building)));
   for(int i=0;i<speed;i++) town.Tick(Mathf.Min(UnityEngine.Time.deltaTime,.1f)); world.Sync(town,speed==0);
   saveClock+=UnityEngine.Time.unscaledDeltaTime; if(saveClock>=30) { Save(false); saveClock=0; }
  }
  void Save(bool notify)
  {
   try { File.WriteAllText(SavePath,JsonUtility.ToJson(town.Capture(),true)); if(notify) message="街を保存しました。次回もここから再開します。"; }
   catch(Exception e) { message="保存できませんでした："+e.Message; }
  }
  void Load()
  {
   if(!File.Exists(SavePath)) return;
   try { if(town.Restore(JsonUtility.FromJson<TownSave>(File.ReadAllText(SavePath)))) message="おかえりなさい、市長。保存した街を開きました。"; }
   catch(Exception e) { message="保存を読み込めなかったため、新しい広場から開始します。"; Debug.LogWarning(e.Message); }
  }
  void OnApplicationQuit() { if(town!=null) Save(false); }
  void Styles()
  {
   if(text!=null) return;
   text=new GUIStyle(GUI.skin.label){font=font,fontSize=16,wordWrap=true}; text.normal.textColor=ink;
   small=new GUIStyle(text){fontSize=13}; heading=new GUIStyle(text){fontSize=21,fontStyle=FontStyle.Bold}; title=new GUIStyle(heading){fontSize=30};
   button=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontSize=15,wordWrap=true};
  }
  void Panel(Rect r,Color c) { GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white; }
  void Label(float x,float y,float w,float h,string value,GUIStyle style=null) { GUI.Label(new Rect(x,y,w,h),value,style??text); }
  bool Button(Rect r,string value,bool active=false,bool enabled=true)
  {
   Panel(r,active?green:enabled?new Color(.89f,.89f,.82f):new Color(.92f,.92f,.87f));
   button.normal.textColor=active?Color.white:enabled?ink:new Color(.6f,.62f,.56f); bool hit=GUI.Button(r,value,button); button.normal.textColor=ink; return hit&&enabled;
  }
  void Meter(float x,float y,float w,string name,float value)
  {
   Label(x,y,w,24,name+"  "+Mathf.RoundToInt(value),small); Panel(new Rect(x,y+27,w,5),new Color(.84f,.85f,.77f)); Panel(new Rect(x,y+27,w*Mathf.Clamp01(value/100),5),green);
  }
  void OnGUI()
  {
   if(town==null) return; Styles(); float w=Width,h=Height; GUI.matrix=Matrix4x4.Scale(new Vector3(Scale,Scale,1));
   Panel(new Rect(0,0,w,112),paper); Panel(new Rect(0,112,242,h-112),paper); Panel(new Rect(w-290,112,290,h-112),paper); Panel(new Rect(242,h-142,w-532,142),paper);
   Label(24,14,310,43,"鳩が市長のまち",title); Label(26,65,310,25,"PIGEON MAYOR  /  駅前広場",small);
   Label(355,17,235,30,"街の予算  ¥"+town.Money.ToString("0"),heading);
   Label(355,57,245,30,"今期収入 ¥"+town.Income.ToString("0")+"  維持費 ¥"+town.Upkeep.ToString("0")+" / 20秒",small);
   Meter(615,23,140,"鳩の幸福",town.PigeonHappiness); Meter(785,23,140,"人の満足",town.HumanSatisfaction);
   Label(965,20,210,25,"住民 "+town.Birds.Count+"羽 / 来訪 "+town.Visitors.Count+"人",text); Label(965,58,190,24,"DAY "+town.Day+" · 購買 "+town.Purchases+"回",small);
   if(Button(new Rect(w-210,25,78,45),speed==0?"再開":"一時停止",speed==0)) speed=speed==0?1:0;
   if(Button(new Rect(w-122,25,94,45),"速度 ×"+(speed==0?1:speed))) speed=speed>=3?1:3;
   LeftPanel(h); RightPanel(w,h); BottomPanel(w,h);
  }
  void LeftPanel(float h)
  {
   Label(22,130,200,30,"街をつくる",heading); Label(22,168,205,42,"ひとつの施設に、\n人と鳩ふたつの価値。",small);
   string[] uses={"食べ物・買い物","水浴び・景観","寝床・税収","休息・清潔さ","社交・混雑対策","特等席・観光"};
   for(int i=0;i<6;i++)
   {
    var kind=(FacilityKind)i;
    if(Button(new Rect(18,222+i*70,204,61),TownSimulation.NameOf(kind)+"   ¥"+TownSimulation.Cost(kind)+"\n"+uses[i],tool==Tool.Build&&building==kind)) { building=kind; tool=Tool.Build; selected=-1; message=Tips[i]; }
   }
   if(Button(new Rect(18,656,204,39),"観察 / 施設を選ぶ",tool==Tool.Inspect)) tool=Tool.Inspect;
   Label(22,710,200,70,"クリック：建設 / 選択\n右ドラッグ：視点回転\nスクロール：ズーム",small);
   if(Button(new Rect(18,h-64,204,40),"街を保存")) Save(true);
  }
  void RightPanel(float w,float h)
  {
   float x=w-272; Meter(x,130,116,"食料供給",town.FoodSupply); Meter(x+132,130,116,"清潔さ",town.Cleanliness);
   Label(x,176,245,32,"混雑 "+town.Crowding.ToString("0")+" · 木と広場でゆとりを",small);
   string[] tabs={"お願い","鳩図鑑","条例"}; for(int i=0;i<3;i++) if(Button(new Rect(x+i*84,217,78,38),tabs[i],tab==i)) { tab=i; scroll=Vector2.zero; }
   float content=tab==1?Mathf.Max(550,town.Birds.Count*83+170):610;
   scroll=GUI.BeginScrollView(new Rect(x,272,254,h-300),scroll,new Rect(0,0,233,content));
   if(tab==0)
   {
    Label(0,0,230,32,"住民からのおたより",heading);
    for(int i=0;i<town.Requests.Count;i++) { var r=town.Requests[i]; float y=52+i*145; Label(0,y,228,32,(r.Complete?"✓ ":"0"+(i+1)+"  ")+r.Title,heading); Label(0,y+39,226,64,r.Description); Label(0,y+108,228,25,r.Complete?"達成済み · お礼を受け取りました":"お礼 ¥"+r.Reward,small); }
    Label(0,510,230,85,"鳩を追い払う必要はありません。居場所と人の通路を、配置で整えましょう。",small);
   }
   else if(tab==1)
   {
    Label(0,0,230,32,"この街の住民たち",heading); int i=0;
    foreach(var b in town.Birds) { float y=48+i++*83; Label(0,y,229,29,(b.Mayor?"市長 ":b.Rare?"白い羽 ":"")+b.Name,heading); Label(0,y+33,230,40,TownSimulation.PersonalityName(b.Personality)+" · "+b.Action,small); }
    Label(0,55+i*83,230,100,"白い鳩のうわさ\n"+town.RareHint,small);
   }
   else
   {
    Label(0,0,230,32,"市長のひと声",heading);
    string[] names={"公共施設に巣箱","水浴び優先区域","オープンカフェ支援"};
    string[] notes={"寝床が増え、鳩が安心。維持費が増えます。","水浴びが充実。人間のイベント空間は少し減少。","食料供給と商業を支援。清掃需要と維持費が増加。"};
    bool[] on={town.NestBoxes,town.BathPriority,town.CafeSupport};
    for(int i=0;i<3;i++) { float y=52+i*155; if(Button(new Rect(0,y,230,48),names[i]+(on[i]?" ON":" OFF"),on[i])) town.TogglePolicy((TownPolicy)i); Label(0,y+59,228,75,notes[i],small); }
   }
   GUI.EndScrollView();
  }
  void BottomPanel(float w,float h)
  {
   float x=262,width=w-574; var f=town.Facilities.Find(a=>a.Id==selected);
   if(f!=null&&tool!=Tool.Build)
   {
    Label(x,h-132,width,30,TownSimulation.NameOf(f.Kind)+" Lv."+f.Level+" / "+f.X+", "+f.Z,heading); Label(x,h-98,width,40,tool==Tool.Move?"空き地をクリックして移設。費用はかかりません。":Tips[(int)f.Kind],small);
    if(Button(new Rect(x,h-50,118,34),"移設 ¥0",tool==Tool.Move)) tool=Tool.Move;
    int cost=TownSimulation.Cost(f.Kind)*f.Level/2;
    if(Button(new Rect(x+127,h-50,143,34),f.Level>=3?"改良済み":"改良 ¥"+cost,false,f.Level<3&&town.Money>=cost)) town.Upgrade(f.Id);
    if(Button(new Rect(x+279,h-50,160,34),"撤去 / 70%返金")) { town.Remove(f.Id); selected=-1; tool=Tool.Inspect; }
   }
   else { Label(x,h-132,width,30,tool==Tool.Build?TownSimulation.NameOf(building)+"を建てる":"市長の観察ノート",heading); Label(x,h-96,width,42,message,small); Label(x,h-43,width,30,town.Notice,small); }
  }
 }
}
