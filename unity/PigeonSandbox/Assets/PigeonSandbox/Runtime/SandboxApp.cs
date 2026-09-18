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
  Vector2 pointerStart,scroll,lastPointer;
  Vector3 cameraFocus=new Vector3(0,.3f,0);
  int followedBird=-1;
  float targetZoom=14;
  bool pointerInTown,dragged,multiTouch;
  string message="広場のそばにベーカリーを建ててみましょう。";
  float Scale=>Mathf.Min(Screen.width/1440f,Screen.height/900f);
  float Width=>Screen.width/Scale; float Height=>Screen.height/Scale;
  string SavePath=>Path.Combine(Application.persistentDataPath,"mayor-town.json");
  readonly Color ink=new Color(.12f,.2f,.16f),muted=new Color(.22f,.29f,.24f),green=new Color(.27f,.41f,.33f),paper=new Color(.96f,.95f,.9f);
  static readonly string[] Tips={"広場から2マス以内で混雑を軽減。噴水を添えると鳩の生活圏に。","パン屋の2マス以内で食事と水浴びを楽しめる場所に。","木や噴水が近く、店舗から離れた場所なら静かな寝床に。","住宅のそばで静かな緑地に。清掃の負担もやわらげます。","パン屋に近づけると混雑をやわらげ、人と鳩の居場所を確保。","観光と目立ちたがりの鳩のための名所。静かな住宅からは距離を。"};
  struct UiLayout
  {
   public float gap,headerH,bottomH,leftW,rightW;
   public Rect left,right,center,bottom;
  }
  UiLayout Layout(float w,float h)
  {
   float gap=Mathf.Clamp(w*.012f,14,20), headerH=Mathf.Clamp(h*.12f,104,116), bottomH=192;
   float leftW=Mathf.Clamp(w*.18f,248,276), rightW=Mathf.Clamp(w*.205f,286,320);
   float centerX=leftW+gap, centerY=headerH+gap;
   float centerW=Mathf.Max(480,w-leftW-rightW-gap*2), centerH=Mathf.Max(260,h-headerH-bottomH-gap*2);
   return new UiLayout
   {
    gap=gap,headerH=headerH,bottomH=bottomH,leftW=leftW,rightW=rightW,
    left=new Rect(0,headerH,leftW,h-headerH),
    right=new Rect(w-rightW,headerH,rightW,h-headerH),
    center=new Rect(centerX,centerY,centerW,centerH),
    bottom=new Rect(centerX,h-bottomH,centerW,bottomH)
   };
  }
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
   var l=Layout(Width,Height); view.rect=new Rect(l.center.x/Width,(Height-l.center.y-l.center.height)/Height,l.center.width/Width,l.center.height/Height);
   var focus=cameraFocus; view.transform.position=focus+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-35);
   view.transform.LookAt(focus); view.orthographicSize=zoom;
  }
  Rect CameraToolbar() { var r=Layout(Width,Height).center; return new Rect(r.x+10,r.y+10,r.width-20,48); }
  bool InTown(Vector2 point) { var p=point/Scale; p.y=Height-p.y; return Layout(Width,Height).center.Contains(p)&&!CameraToolbar().Contains(p); }
  void FocusBird(int id)
  {
   followedBird=id; targetZoom=4; tool=Tool.Inspect; selected=-1;
  }
  void ResetCamera() { followedBird=-1; cameraFocus=new Vector3(0,.3f,0); targetZoom=14; yaw=35; pitch=48; }
  void PanCamera(Vector2 from,Vector2 to)
  {
   var plane=new Plane(Vector3.up,new Vector3(0,cameraFocus.y,0));
   var a=view.ScreenPointToRay(from); var b=view.ScreenPointToRay(to);
   if(!plane.Raycast(a,out float da)||!plane.Raycast(b,out float db)) return;
   followedBird=-1; cameraFocus+=a.GetPoint(da)-b.GetPoint(db);
   cameraFocus.x=Mathf.Clamp(cameraFocus.x,-14,14); cameraFocus.z=Mathf.Clamp(cameraFocus.z,-14,14);
  }
  void CameraInput()
  {
   if(Input.touchCount>0)
   {
    var t=Input.GetTouch(0);
    if(t.phase==TouchPhase.Began) { pointerStart=lastPointer=t.position; pointerInTown=InTown(t.position); dragged=false; if(Input.touchCount==1) multiTouch=false; }
    if(Input.touchCount>=2)
    {
     multiTouch=true; dragged=true;
     var b=Input.GetTouch(1);
     if(pointerInTown&&InTown(b.position)&&(t.phase==TouchPhase.Moved||b.phase==TouchPhase.Moved)&&b.phase!=TouchPhase.Began)
     {
      var oldA=t.position-t.deltaPosition; var oldB=b.position-b.deltaPosition;
      targetZoom=Mathf.Clamp(targetZoom+(Vector2.Distance(oldA,oldB)-Vector2.Distance(t.position,b.position))*.015f,3,22);
      PanCamera((oldA+oldB)*.5f,(t.position+b.position)*.5f);
     }
    }
    else if(pointerInTown&&!multiTouch)
    {
     if(Vector2.Distance(pointerStart,t.position)>7*Scale) dragged=true;
     if(dragged&&t.phase==TouchPhase.Moved) PanCamera(lastPointer,t.position);
     if(t.phase==TouchPhase.Ended&&!dragged) ClickTown(t.position);
    }
    lastPointer=t.position;
    if(t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended) pointerInTown=false;
    return;
   }
   Vector2 mouse=Input.mousePosition;
   if(Input.GetMouseButtonDown(0)) { pointerStart=lastPointer=mouse; pointerInTown=InTown(mouse); dragged=false; }
   if(Input.GetMouseButton(0)&&pointerInTown)
   {
    if(Vector2.Distance(pointerStart,mouse)>7*Scale) dragged=true;
    if(dragged) PanCamera(lastPointer,mouse);
   }
   if(Input.GetMouseButtonUp(0)) { dragged|=Vector2.Distance(pointerStart,mouse)>7*Scale; if(pointerInTown&&!dragged&&InTown(mouse)) ClickTown(mouse); pointerInTown=false; dragged=false; }
   if(InTown(mouse))
   {
    if(Input.GetMouseButton(1)) { yaw+=Input.GetAxis("Mouse X")*3; pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,30,75); }
    targetZoom=Mathf.Clamp(targetZoom-Input.mouseScrollDelta.y*.55f,3,22);
   }
   lastPointer=mouse;
  }
  void UpdateCamera()
  {
   float blend=1-Mathf.Exp(-8*UnityEngine.Time.unscaledDeltaTime);
   var bird=town.Birds.Find(b=>b.Id==followedBird);
   if(bird!=null) cameraFocus=Vector3.Lerp(cameraFocus,new Vector3(bird.X,bird.Y+.4f,bird.Z),blend);
   else followedBird=-1;
   zoom=Mathf.Lerp(zoom,targetZoom,blend); CameraPosition();
  }
  void CameraControls()
  {
   Rect r=CameraToolbar(); Panel(r,paper); float x=r.x+4,y=r.y+2;
   if(Button(new Rect(x,y,44,44),"＋")) targetZoom=Mathf.Max(3,targetZoom-2);
   if(Button(new Rect(x+50,y,44,44),"−")) targetZoom=Mathf.Min(22,targetZoom+2);
   if(Button(new Rect(x+100,y,96,44),"街全体")) ResetCamera();
   var bird=town.Birds.Find(b=>b.Id==followedBird);
   string caption=bird==null?"ドラッグで移動":bird.Name+"を追従中";
   GUI.Label(new Rect(x+208,y+9,r.width-220,34),caption,small);
  }
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
   CameraInput();
   UpdateCamera(); bool hover=GridPoint(Input.mousePosition,out int hx,out int hz);
   world.Preview(hx,hz,hover&&!(pointerInTown&&dragged)&&tool!=Tool.Inspect,town.CanPlace(hx,hz,tool==Tool.Move?selected:-1)&&(tool!=Tool.Build||town.Money>=TownSimulation.Cost(building)));
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
   text=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true}; text.normal.textColor=ink;
   small=new GUIStyle(text){fontSize=15,fontStyle=FontStyle.Normal}; small.normal.textColor=muted;
   heading=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold}; heading.normal.textColor=ink;
   title=new GUIStyle(heading){fontSize=30};
   button=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontSize=15,fontStyle=FontStyle.Bold,wordWrap=true,padding=new RectOffset(8,8,6,6)};
  }
  void Panel(Rect r,Color c) { GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white; }
  void Label(float x,float y,float w,float h,string value,GUIStyle style=null)
  {
   style=style??text;
   GUI.Label(new Rect(x,y,w,Mathf.Max(h,TextHeight(value,w,style))),value,style);
  }
  float TextHeight(string value,float width,GUIStyle style) => Mathf.Ceil(style.CalcHeight(new GUIContent(value),width))+6;
  float FlowLabel(float x,float y,float width,string value,GUIStyle style,bool draw=true)
  {
   float height=TextHeight(value,width,style);
   if(draw) Label(x,y,width,height,value,style);
   return y+height+8;
  }
  bool Button(Rect r,string value,bool active=false,bool enabled=true)
  {
   Panel(r,active?green:enabled?new Color(.89f,.89f,.82f):new Color(.92f,.92f,.87f));
   button.normal.textColor=active?Color.white:enabled?ink:new Color(.6f,.62f,.56f); bool hit=GUI.Button(r,value,button); button.normal.textColor=ink; return hit&&enabled;
  }
  float Meter(float x,float y,float w,string name,float value)
  {
   float barY=FlowLabel(x,y,w,name+"  "+Mathf.RoundToInt(value),small);
   Panel(new Rect(x,barY,w,5),new Color(.84f,.85f,.77f)); Panel(new Rect(x,barY,w*Mathf.Clamp01(value/100),5),green);
   return barY+13;
  }
  void OnGUI()
  {
   if(town==null) return; Styles(); float w=Width,h=Height; var l=Layout(w,h); GUI.matrix=Matrix4x4.Scale(new Vector3(Scale,Scale,1));
   Panel(new Rect(0,0,w,l.headerH),paper); Panel(l.left,paper); Panel(l.right,paper); Panel(l.bottom,paper);
   Panel(new Rect(l.left.width,l.headerH,l.center.x-l.left.width,h-l.headerH),paper);
   Panel(new Rect(l.center.x+l.center.width,l.headerH,w-(l.center.x+l.center.width),h-l.headerH),paper);
   Panel(new Rect(l.center.x,l.headerH,l.center.width,l.center.y-l.headerH),paper);
   Panel(new Rect(l.center.x,l.center.y+l.center.height,l.center.width,l.bottom.y-(l.center.y+l.center.height)),paper);
   float titleBottom=FlowLabel(24,10,310,"鳩市長の街づくり",title);
   FlowLabel(24,titleBottom,310,"鳩と人が暮らす駅前広場",small);
   float moneyBottom=FlowLabel(355,14,245,"街の予算  ¥"+town.Money.ToString("0"),heading);
   FlowLabel(355,moneyBottom,245,"今期収入 ¥"+town.Income.ToString("0")+"  維持費 ¥"+town.Upkeep.ToString("0")+" / 20秒",small);
   Meter(615,23,140,"鳩の幸福",town.PigeonHappiness); Meter(785,23,140,"人の満足",town.HumanSatisfaction);
   float residentX=Mathf.Min(965,w-470);
   float residentBottom=FlowLabel(residentX,18,210,"住民 "+town.Birds.Count+"羽 / 来訪 "+town.Visitors.Count+"人",text);
   FlowLabel(residentX,residentBottom,210,"DAY "+town.Day+" · 購買 "+town.Purchases+"回",small);
   float speedW=104,pauseW=94,controlGap=10,speedX=w-24-speedW,pauseX=speedX-controlGap-pauseW;
   if(Button(new Rect(pauseX,25,pauseW,45),speed==0?"再開":"一時停止",speed==0)) speed=speed==0?1:0;
   if(Button(new Rect(speedX,25,speedW,45),"速度 ×"+(speed==0?1:speed))) speed=speed>=3?1:3;
   LeftPanel(l); RightPanel(l); BottomPanel(l); CameraControls();
  }
  void LeftPanel(UiLayout l)
  {
   Rect r=l.left; float x=r.x+16,w=r.width-32;
   float introY=FlowLabel(x,r.y+18,w,"街をつくる",heading);
   float introBottom=FlowLabel(x,introY,w,"施設を選んで、中央の街に配置",small);
   string[] uses={"食料","水浴び","寝床","休憩","交流","観光"};
   float gridY=introBottom+16,gap=8,bw=(w-gap)/2,bh=58;
   for(int i=0;i<6;i++)
   {
    string caption=TownSimulation.NameOf((FacilityKind)i)+"\n¥"+TownSimulation.Cost((FacilityKind)i)+"\n"+uses[i];
    bh=Mathf.Max(bh,TextHeight(caption,bw,button));
   }
   for(int i=0;i<6;i++)
   {
    var kind=(FacilityKind)i; int col=i%2,row=i/2;
    Rect buttonRect=new Rect(x+col*(bw+gap),gridY+row*(bh+gap),bw,bh);
    if(Button(buttonRect,TownSimulation.NameOf(kind)+"\n¥"+TownSimulation.Cost(kind)+"\n"+uses[i],tool==Tool.Build&&building==kind)) { building=kind; tool=Tool.Build; selected=-1; message=Tips[i]; }
   }
   float inspectY=gridY+3*(bh+gap)+12;
   if(Button(new Rect(x,inspectY,w,40),"観察 / 施設を選ぶ",tool==Tool.Inspect)) tool=Tool.Inspect;
   FlowLabel(x,inspectY+52,w,"左クリック：建設・選択\n左ドラッグ：マップ移動\n右ドラッグ：視点回転\nホイール / ＋−：ズーム",small);
   if(Button(new Rect(x,r.y+r.height-58,w,40),"街を保存")) Save(true);
  }
  void RightPanel(UiLayout l)
  {
   Rect r=l.right; float x=r.x+16,innerW=r.width-32;
   float meterGap=10,meterW=(innerW-meterGap)/2;
   float foodBottom=Meter(x,r.y+18,meterW,"食料供給",town.FoodSupply);
   float cleanBottom=Meter(x+meterW+meterGap,r.y+18,meterW,"清潔さ",town.Cleanliness);
   float tabsTop=FlowLabel(x,Mathf.Max(foodBottom,cleanBottom)+8,innerW,"混雑 "+town.Crowding.ToString("0")+" · 木と広場でゆとりを",small);
   string[] tabs={"お願い","鳩図鑑","条例"}; float tabGap=6,tabW=(innerW-tabGap*2)/3,tabY=tabsTop+8;
   for(int i=0;i<3;i++) if(Button(new Rect(x+i*(tabW+tabGap),tabY,tabW,38),tabs[i],tab==i)) { tab=i; scroll=Vector2.zero; }
   float scrollY=tabY+50,scrollH=r.y+r.height-scrollY-16;
   float contentW=innerW-GUI.skin.verticalScrollbar.fixedWidth-12;
   float content=RightContent(contentW,false);
   scroll=GUI.BeginScrollView(new Rect(x,scrollY,innerW,scrollH),scroll,new Rect(0,0,contentW,content),false,true);
   RightContent(contentW,true);
   GUI.EndScrollView();
  }
  float RightContent(float width,bool draw)
  {
   float y=0;
   if(tab==0)
   {
    y=FlowLabel(0,y,width,"住民からのおたより",heading,draw)+12;
    for(int i=0;i<town.Requests.Count;i++)
    {
     var request=town.Requests[i];
     y=FlowLabel(0,y,width,(request.Complete?"✓ ":"0"+(i+1)+"  ")+request.Title,heading,draw);
     y=FlowLabel(0,y,width,request.Description,small,draw);
     y=FlowLabel(0,y,width,request.Complete?"達成済み · お礼を受け取りました":"お礼 ¥"+request.Reward,small,draw)+20;
    }
    y=FlowLabel(0,y,width,"鳩を追い払う必要はありません。居場所と人の通路を、配置で整えましょう。",small,draw);
   }
   else if(tab==1)
   {
    y=FlowLabel(0,y,width,"この街の住民たち",heading,draw)+12;
    foreach(var b in town.Birds)
    {
     string name=(b.Mayor?"市長 ":b.Rare?"白い羽 ":"")+b.Name;
     float nameHeight=Mathf.Max(44,TextHeight(name,width,button));
     if(draw&&Button(new Rect(0,y,width,nameHeight),name,followedBird==b.Id)) FocusBird(b.Id);
     y+=nameHeight+8;
     y=FlowLabel(0,y,width,TownSimulation.PersonalityName(b.Personality)+" · "+b.Action,small,draw)+16;
    }
    y=FlowLabel(0,y,width,"白い鳩のうわさ\n"+town.RareHint,small,draw);
   }
   else
   {
    y=FlowLabel(0,y,width,"市長のひと声",heading,draw)+12;
    string[] names={"公共施設に巣箱","水浴び優先区域","オープンカフェ支援"};
    string[] notes={"寝床が増え、鳩が安心。維持費が増えます。","水浴びが充実。人間のイベント空間は少し減少。","食料供給と商業を支援。清掃需要と維持費が増加。"};
    bool[] on={town.NestBoxes,town.BathPriority,town.CafeSupport};
    for(int i=0;i<3;i++)
    {
     string caption=names[i]+(on[i]?" ON":" OFF");
     float height=Mathf.Max(48,TextHeight(caption,width,button));
     if(draw&&Button(new Rect(0,y,width,height),caption,on[i])) town.TogglePolicy((TownPolicy)i);
     y+=height+10;
     y=FlowLabel(0,y,width,notes[i],small,draw)+24;
    }
   }
   return y+16;
  }
  void BottomPanel(UiLayout l)
  {
   Rect r=l.bottom; float x=r.x+18,width=r.width-36; var f=town.Facilities.Find(a=>a.Id==selected);
   if(f!=null&&tool!=Tool.Build)
   {
    float y=FlowLabel(x,r.y+10,width,TownSimulation.NameOf(f.Kind)+" Lv."+f.Level+" / "+f.X+", "+f.Z,heading);
    FlowLabel(x,y,width,tool==Tool.Move?"空き地をクリックして移設。費用はかかりません。":Tips[(int)f.Kind],small);
    float by=r.yMax-58;
    if(Button(new Rect(x,by,110,44),"移設 ¥0",tool==Tool.Move)) tool=Tool.Move;
    int cost=TownSimulation.Cost(f.Kind)*f.Level/2;
    if(Button(new Rect(x+120,by,132,44),f.Level>=3?"改良済み":"改良 ¥"+cost,false,f.Level<3&&town.Money>=cost)) town.Upgrade(f.Id);
    if(Button(new Rect(x+width-150,by,150,44),"撤去 / 70%返金")) { town.Remove(f.Id); selected=-1; tool=Tool.Inspect; }
   }
   else
   {
    float y=FlowLabel(x,r.y+10,width,tool==Tool.Build?TownSimulation.NameOf(building)+"を建てる":"市長の観察ノート",heading);
    y=FlowLabel(x,y,width,message,small);
    FlowLabel(x,y,width,town.Notice,small);
   }
  }
 }
}
