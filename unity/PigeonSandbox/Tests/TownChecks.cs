using System;
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
 public static void Main(){RunAll();}
}
