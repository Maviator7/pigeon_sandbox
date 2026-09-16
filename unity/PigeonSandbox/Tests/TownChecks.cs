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
 var recovery=new TownSimulation();recovery.Money=0;Run(recovery,50);Check(recovery.Money>0,"zero money recovery");
 }
 public static void Main(){RunAll();}
}
