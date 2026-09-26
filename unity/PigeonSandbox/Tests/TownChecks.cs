using System;
using System.Collections.Generic;
using PigeonSandbox;
public static class TownChecks {
 static void Check(bool value,string name){if(!value)throw new Exception(name); Console.WriteLine("PASS: "+name);}
 static void Run(TownSimulation t,int seconds){for(int i=0;i<seconds*10;i++)t.Tick(.1f);}
 public static void RunAll(){
 var t=new TownSimulation(7);float money=t.Money;
 Check(t.Build(FacilityKind.Bakery,1,0),"build bakery");Check(t.Money<=money-TownSimulation.Cost(FacilityKind.Bakery)+100,"build cost and request reward");
 Check(!t.Build(FacilityKind.Tree,1,0)&&!t.Build(FacilityKind.Tree,5,0),"overlap and bounds rejected");
 var b=t.At(1,0);Run(t,10);float happiness=t.PigeonHappiness;Check(t.Move(b.Id,4,4)&&t.At(4,4)==b,"free move retains ID");t.Recalculate();Check(t.PigeonHappiness!=happiness,"spatial metric changes");
 Check(t.Move(b.Id,1,0),"restore bakery");float reward=t.Money;t.Recalculate();t.Recalculate();Check(t.Money==reward,"request cannot reward twice");
 Run(t,140);Check(t.Purchases>0&&t.Income>0,"real visitor economy");
 var a=new TownSimulation(17);var c=new TownSimulation(17);Run(a,80);Run(c,80);Check(a.Money==c.Money&&a.Birds[0].X==c.Birds[0].X,"seed deterministic");
 var rare=new TownSimulation(2);rare.Money=5000;rare.Build(FacilityKind.Housing,-3,2);rare.Build(FacilityKind.Tree,-3,3);rare.Build(FacilityKind.Fountain,-2,2);Run(rare,90);Check(rare.Birds.Exists(x=>x.Rare),"quiet green housing summons white pigeon");
 var noRare=new TownSimulation(2);Run(noRare,90);Check(!noRare.Birds.Exists(x=>x.Rare),"rare needs habitat");
 var pref=new TownSimulation(33);pref.Money=5000;pref.Build(FacilityKind.Bakery,1,0);pref.Build(FacilityKind.Fountain,1,1);pref.Build(FacilityKind.ClockTower,3,3);pref.Tick(.1f);
 Check(pref.Birds[0].TargetId==pref.At(1,0).Id,"foodie seeks bakery");Check(pref.Birds[1].TargetId==pref.At(1,1).Id,"bather seeks fountain");Check(pref.Birds[2].TargetId==pref.At(-2,0).Id,"shy bird seeks tree");Check(pref.Birds[3].TargetId==pref.At(3,3).Id,"showoff seeks clock");
 var shopper=new TownSimulation(31);shopper.Build(FacilityKind.Bakery,-3,0);for(int i=0;i<3000&&shopper.Purchases==0;i++){float cash=shopper.Money;shopper.Tick(.1f);if(shopper.Purchases>0)Check(shopper.Money>cash,"visitor purchase transfers money");}Check(shopper.Purchases>0,"visitor reaches shop");
 var save=t.Capture();var restored=new TownSimulation();Check(restored.Restore(save)&&restored.Facilities.Count==t.Facilities.Count&&restored.Money==t.Money,"save restore layout and money");save.Facilities[0].X=4;Check(restored.Facilities[0].X!=4,"save clones mutable state");
 var named=new TownSimulation(4);int birdId=named.Birds[0].Id;string original=named.Birds[0].Name;
 Check(!named.RenameBird(birdId,"  ")&&named.Birds[0].Name==original,"empty rename preserves name");
 Check(!named.RenameBird(birdId,"a\nb")&&!named.RenameBird(birdId,new string('あ',25)),"invalid and overlong names rejected");
 Check(!named.RenameBird(-1,"ぽっぽ"),"unknown bird rename rejected");
 Check(named.RenameBird(birdId,"  もち 🕊  ")&&named.Birds[0].Name=="もち 🕊","Japanese and emoji rename trims whitespace");
 Check(named.RenameBird(named.Birds[1].Id,"もち 🕊"),"duplicate names allowed for distinct birds");
 var namedRestore=new TownSimulation();Check(namedRestore.Restore(named.Capture())&&namedRestore.Birds[0].Id==birdId&&namedRestore.Birds[0].Name=="もち 🕊","renamed bird survives save and restore");
 var cheerful=new TownSimulation(42);cheerful.Build(FacilityKind.Bakery,1,0);
 var dancer=cheerful.Birds[0];bool danced=false;
 for(int i=0;i<100&&!danced;i++){dancer.TargetId=cheerful.At(1,0).Id;dancer.Decision=100;dancer.Wait=0;dancer.Action="食事";cheerful.PigeonHappiness=100;cheerful.Tick(.01f);danced=dancer.Action=="ごきげんクルクル";}
 Check(danced,"happy pigeon sometimes dances after eating");
 float danceX=dancer.X,danceZ=dancer.Z,previousTurn=0;
 for(int i=0;i<10;i++){cheerful.Tick(.1f);Check(dancer.CheerTurn>previousTurn&&dancer.CheerTurn<360,"cheer turn progresses smoothly");previousTurn=dancer.CheerTurn;}
 cheerful.Tick(0);Check(dancer.CheerTurn==previousTurn,"paused dance stays still");
 Check(dancer.X==danceX&&dancer.Z==danceZ,"dance stays in place");
 for(int i=0;i<5;i++)cheerful.Tick(.1f);
 Check(dancer.CheerTurn==0&&dancer.Action!="ごきげんクルクル","dance finishes and returns to normal behavior");
 var quiet=new TownSimulation(42);quiet.Build(FacilityKind.Fountain,1,0);var quietBird=quiet.Birds[0];
 for(int i=0;i<10;i++){quietBird.TargetId=quiet.At(1,0).Id;quietBird.Decision=100;quietBird.Wait=0;quietBird.Action="水浴び";quiet.PigeonHappiness=0;quiet.Tick(.01f);Check(quietBird.Action!="ごきげんクルクル","low happiness does not trigger cheerful dance");}
 var land=new TownSimulation(12);land.Money=499;
 Check(!land.ExpandTown()&&land.Money==499&&land.MapSize==9,"insufficient funds leave land unchanged");
 land.Money=500;int originalId=land.Facilities[0].Id;
 Check(!land.CanPlace(5,5)&&land.ExpandTown()&&land.Money==0&&land.MapSize==11,"first expansion charges exact cost and grows map");
 Check(land.Facilities[0].Id==originalId&&land.CanPlace(-5,5)&&!land.CanPlace(6,0),"expansion preserves buildings and enforces new bounds");
 land.Money=10000;Check(land.Build(FacilityKind.Tree,5,5),"new land accepts buildings");
 int[] prices={1000,1800,3000};foreach(int price in prices){float balance=land.Money;Check(land.ExpansionCost==price&&land.ExpandTown()&&land.Money==balance-price,"tier expansion charges correct price");}
 float fullBalance=land.Money;Check(land.MapSize==17&&!land.CanExpand&&!land.ExpandTown()&&land.Money==fullBalance,"maximum expansion cannot charge money");
 Check(land.Move(land.At(5,5).Id,8,-8),"move building into outermost land");
 land.Birds[0].X=17;land.Birds[0].Z=-17;
 var restoredLand=new TownSimulation();Check(restoredLand.Restore(land.Capture())&&restoredLand.MapSize==17&&restoredLand.At(8,-8)!=null&&restoredLand.Birds[0].X==17,"expanded land buildings and bird positions survive restore");
 var legacyLand=new TownSimulation().Capture();Check(restoredLand.Restore(legacyLand)&&restoredLand.MapSize==9,"old save defaults to original land size");
 legacyLand.ExpansionLevel=5;Check(!restoredLand.Restore(legacyLand)&&restoredLand.MapSize==9,"invalid expansion level rejected without mutation");
 legacyLand.ExpansionLevel=0;legacyLand.Facilities[0].X=8;Check(!restoredLand.Restore(legacyLand),"save cannot place buildings on unpurchased land");
 var amenities=new TownSimulation(81);amenities.Money=10000;
 Check(amenities.Build(FacilityKind.Cafe,-3,-3)&&amenities.Build(FacilityKind.Park,3,-3),"cafe and park can be built");
 Check(amenities.Build(FacilityKind.Bakery,3,3),"bakery for terrace synergy");float isolatedFood=amenities.FoodSupply;
 Check(amenities.Move(amenities.At(-3,-3).Id,3,2)&&amenities.FoodSupply>isolatedFood,"cafe near bakery improves food supply");
 amenities.Build(FacilityKind.Housing,-3,1);float isolatedCrowding=amenities.Crowding;
 Check(amenities.Move(amenities.At(3,-3).Id,-3,2)&&amenities.Crowding<isolatedCrowding,"park near housing adds crowd relief");
 int parkId=amenities.At(-3,2).Id;Check(amenities.Upgrade(parkId)&&amenities.At(-3,2).Level==2,"park can be upgraded");
 var amenitiesRestore=new TownSimulation();Check(amenitiesRestore.Restore(amenities.Capture())&&amenitiesRestore.At(3,2).Kind==FacilityKind.Cafe&&amenitiesRestore.At(-3,2).Kind==FacilityKind.Park,"new facility types survive restore");
 var cafeVisitors=new TownSimulation(32);cafeVisitors.Build(FacilityKind.Cafe,-3,0);Run(cafeVisitors,180);Check(cafeVisitors.Purchases>0,"visitors purchase at cafe without bakery");
 var parkBirds=new TownSimulation(16);parkBirds.Build(FacilityKind.Park,1,0);var parkFacility=parkBirds.At(1,0);
 for(int i=0;i<2;i++){
 var pb=parkBirds.Birds[i];float angle=(pb.Id%4)*(float)Math.PI*.5f;pb.X=parkFacility.X*TownSimulation.CellSize+(float)Math.Cos(angle)*.96f;pb.Z=parkFacility.Z*TownSimulation.CellSize+(float)Math.Sin(angle)*.96f;pb.TargetId=parkFacility.Id;pb.Decision=100;pb.Wait=0;parkBirds.Tick(.01f);
 Check(pb.Action==(pb.Id%2==0?"羽繕い":"日向ぼっこ"),"park arrival gives individual resting behavior");}
 var interaction=new TownSimulation(8);interaction.Build(FacilityKind.Cafe,1,0);var terrace=interaction.At(1,0);var cb=interaction.Birds[0];
 float ca=(cb.Id%4)*(float)Math.PI*.5f;cb.X=2.2f+(float)Math.Cos(ca)*.96f;cb.Z=(float)Math.Sin(ca)*.96f;cb.TargetId=terrace.Id;cb.Decision=100;cb.Wait=0;
 interaction.Tick(.01f);Check(cb.Action=="テラスで休憩","cafe bird rests when no person is nearby");
 interaction.Visitors.Add(new Visitor{Id=999,X=cb.X,Z=cb.Z,Wait=5});interaction.Tick(.01f);Check(cb.Action=="人と交流","cafe bird interacts with nearby visitor");
 interaction.Visitors.Clear();interaction.Tick(.01f);Check(cb.Action=="テラスで休憩","interaction ends when visitor leaves");
 var recovery=new TownSimulation();recovery.Money=0;Run(recovery,50);Check(recovery.Money>0,"zero money recovery");
 }
 static void FeatherChecks(){
 var t=new TownSimulation(88);Run(t,60);Check(!t.Discovered(Plumage.Checker)&&!t.Discovered(Plumage.Brown)&&!t.Discovered(Plumage.Pied),"feathers require their habitats");
 t.Money=10000;t.Build(FacilityKind.Bakery,1,0);Run(t,20);t.Move(t.At(1,0).Id,4,4);Run(t,1);t.Move(t.At(4,4).Id,1,0);Run(t,30);Check(!t.Discovered(Plumage.Checker),"interrupted habitat resets arrival timer");
 t.Build(FacilityKind.Cafe,1,1);t.Build(FacilityKind.Park,-1,0);Run(t,46);
 Check(t.Discovered(Plumage.Checker)&&t.Discovered(Plumage.Brown)&&t.Discovered(Plumage.Pied),"three habitats summon three feather variants");
 var brown=t.Birds.Find(b=>b.Plumage==Plumage.Brown);t.RenameBird(brown.Id,"ココア");var saved=t.Capture();var restored=new TownSimulation();Check(restored.Restore(saved)&&restored.Birds.Find(b=>b.Id==brown.Id).Name=="ココア"&&restored.Discovered(Plumage.Brown),"feathers and custom names persist");
 Run(restored,180);foreach(var f in new[]{Plumage.Checker,Plumage.Brown,Plumage.Pied})Check(restored.Birds.FindAll(b=>b.Plumage==f).Count==1,"discovered variants do not duplicate after load");
 var legacy=new TownSimulation().Capture();legacy.Birds[1].Rare=true;Check(restored.Restore(legacy)&&restored.Discovered(Plumage.White)&&restored.Discovered(Plumage.Blue),"legacy gray and rare white saves migrate");
 var invalid=restored.Capture();invalid.Birds[0].Plumage=(Plumage)999;Check(!restored.Restore(invalid)&&restored.Discovered(Plumage.White),"invalid feather rejected without changing town");
 var full=new TownSimulation();var fullSave=full.Capture();for(int i=0;i<8;i++)fullSave.Birds.Add(new TownBird{Id=100+i,Name="旧住民"+i});fullSave.Birds[1].Rare=true;Check(full.Restore(fullSave),"legacy full town loads");full.Money=10000;full.Build(FacilityKind.Bakery,1,0);full.Build(FacilityKind.Cafe,1,1);full.Build(FacilityKind.Park,-1,0);Run(full,90);Check(full.Birds.Count==15&&full.Discovered(Plumage.Pied)&&full.Birds.Exists(b=>b.Name=="旧住民7"),"full legacy town welcomes new feathers without replacing residents");
 Check(new TownSimulation().Restore(full.Capture()),"expanded bird limit save roundtrip");
 }
 static void DaylightChecks(){
 var t=new TownSimulation();Check(t.TimeOfDay==TownTimeOfDay.Morning,"new town starts in morning");
 foreach(float time in new[]{0f,39.9f,40f,79.9f,80f,119.9f,120f,160f})
 {
  var save=t.Capture();save.Time=time;Check(t.Restore(save),"daylight save loads");
  int expected=(int)((time%120)/40);Check((int)t.TimeOfDay==expected,"time phase boundary "+time);
  float progress=t.DayProgress;t.Tick(0);Check(t.DayProgress==progress,"pause freezes daylight");
 }
 var saved=t.Capture();var restored=new TownSimulation();restored.Restore(saved);Check(restored.TimeOfDay==t.TimeOfDay&&restored.DayProgress==t.DayProgress,"saved daylight restores exactly");
 int morningMeals=0,noonBaths=0,eveningRests=0,morningRests=0;
 for(int seed=0;seed<60;seed++)
 {
  foreach(int phase in new[]{0,1,2})
  {
   var day=new TownSimulation(seed);day.Money=5000;day.Build(FacilityKind.Bakery,1,0);day.Build(FacilityKind.Fountain,1,1);day.Build(FacilityKind.Park,-1,1);day.Build(FacilityKind.Housing,-3,1);day.Time=phase*40;day.Tick(.1f);
   foreach(var bird in day.Birds){var place=day.Facilities.Find(f=>f.Id==bird.TargetId);if(phase==0&&place.Kind==FacilityKind.Bakery)morningMeals++;if(phase==0&&(place.Kind==FacilityKind.Park||place.Kind==FacilityKind.Tree||place.Kind==FacilityKind.Housing))morningRests++;if(phase==1&&place.Kind==FacilityKind.Fountain)noonBaths++;if(phase==2&&(place.Kind==FacilityKind.Park||place.Kind==FacilityKind.Tree||place.Kind==FacilityKind.Housing))eveningRests++;}
  }
 }
 Check(morningMeals>0&&noonBaths>0&&eveningRests>morningRests,"daily routines retain food bathing and evening rest choices");
 }
 static void PerchChecks(){
 var t=new TownSimulation(3);t.Money=5000;t.Build(FacilityKind.ClockTower,2,2);t.Build(FacilityKind.Tree,-3,3);t.Build(FacilityKind.Housing,3,-3);
 var last=new Dictionary<int,float[]>();int perchedSamples=0;bool hovering=false,offStructure=false,airborneJump=false;
 for(int i=0;i<6000;i++)
 {
  t.Tick(.1f);
  foreach(var b in t.Birds)
  {
   if(last.TryGetValue(b.Id,out var p))
   {
    float horizontal=(float)Math.Sqrt((b.X-p[0])*(b.X-p[0])+(b.Z-p[2])*(b.Z-p[2])),moved=horizontal+Math.Abs(b.Y-p[1]);
    if(moved<1e-4f&&b.Y>.15f&&b.Social==SocialActivity.None&&!b.Perched)hovering=true;
    if(p[1]>.15f&&b.Y>.15f&&horizontal>.2f)airborneJump=true;
   }
   if(b.Perched)
   {
    perchedSamples++;var f=t.Facilities.Find(x=>x.Id==b.TargetId);
    if(f==null||(f.Kind!=FacilityKind.Tree&&f.Kind!=FacilityKind.ClockTower)||b.Y<2||Math.Sqrt(Math.Pow(b.X-f.X*TownSimulation.CellSize,2)+Math.Pow(b.Z-f.Z*TownSimulation.CellSize,2))>.4)offStructure=true;
   }
   last[b.Id]=new[]{b.X,b.Y,b.Z};
  }
 }
 Check(perchedSamples>0,"birds perch on trees and clock towers");
 Check(!hovering,"resting birds never hover in mid-air beside structures");
 Check(!offStructure,"perched birds sit on top of their structure");
 Check(!airborneJump,"airborne birds glide without teleporting");
 var alone=new TownSimulation(3);alone.Money=5000;alone.Build(FacilityKind.ClockTower,2,2);var bird=alone.Birds[0];
 for(int i=0;i<3000&&!bird.Perched;i++){bird.TargetId=alone.At(2,2).Id;bird.Decision=100;alone.Tick(.1f);}
 Check(bird.Perched,"bird reaches clock tower perch");
 foreach(var f in alone.Facilities.ToArray())alone.Remove(f.Id);
 for(int i=0;i<30;i++)alone.Tick(.1f);
 Check(!bird.Perched&&bird.Y<.15f,"removing every facility brings perched birds down");
 }
 public static void Main(){RunAll();SocialChecks.RunAll();FeatherChecks();DaylightChecks();WishChecks.RunAll();PerchChecks();}
}
