using System;
using PigeonSandbox;

public static class FestivalChecks
{
    static void Check(bool value,string name)
    {
        if(!value)throw new Exception(name);
        Console.WriteLine("PASS: "+name);
    }

    static void Arrive(TownSimulation town,TownBird bird,Facility place)
    {
        float angle=(bird.Id%4)*(float)Math.PI*.5f;
        bird.X=place.X*TownSimulation.CellSize+(float)Math.Cos(angle)*.96f;
        bird.Z=place.Z*TownSimulation.CellSize+(float)Math.Sin(angle)*.96f;
        bird.TargetId=place.Id;
        bird.Decision=100;
        bird.Wait=0;
        bird.Action="散歩";
        town.Tick(.01f);
    }

    public static void RunAll()
    {
        var town=new TownSimulation(42);
        Check(town.NextFestivalDay==3&&!town.FestivalActive,"first festival opens on day three");
        Check(!town.StartFestival(),"festival cannot start early or without a venue");
        Check(town.ChooseFestival(FestivalKind.BakeryMarket)&&town.Build(FacilityKind.Bakery,1,0),"bakery market can be prepared");
        Check(town.FestivalVenueReady(FestivalKind.BakeryMarket),"bakery near plaza is a valid venue");
        var day3=town.Capture();day3.Time=TownSimulation.DayLength*2;
        Check(town.Restore(day3)&&town.Day==3&&town.StartFestival(),"prepared festival starts when its day arrives");
        var bakery=town.At(1,0);
        Arrive(town,town.Birds[0],bakery);
        Check(town.FestivalActive&&town.FestivalMainBirdIds.Count==1&&town.Postcards.Count==0,"pigeon visit alone does not finish market");
        var progress=town.Capture();var progressRestored=new TownSimulation();
        Check(progressRestored.Restore(progress)&&progressRestored.FestivalActive&&progressRestored.FestivalMainBirdIds.Count==1,"ongoing participation survives save and load");
        Check(town.Move(bakery.Id,4,4)&&!town.FestivalCurrentVenueReady,"moving venue away pauses event observation");
        Check(town.Move(bakery.Id,1,0)&&town.FestivalCurrentVenueReady,"restoring venue resumes event without losing progress");
        for(int i=0;i<3000&&town.Postcards.Count==0;i++)town.Tick(.1f);
        Check(town.Postcards.Count==1&&town.Postcards[0].Kind==FestivalKind.BakeryMarket,"actual bakery purchase completes market");
        Check(town.Postcards[0].BirdNames.Contains(town.Birds[0].Name)&&town.NextFestivalDay>=town.Day+3,"postcard records participant and next event is scheduled");
        var copy=town.Capture();var restored=new TownSimulation();
        Check(restored.Restore(copy)&&restored.Postcards.Count==1&&restored.Postcards[0].BirdNames[0]==town.Postcards[0].BirdNames[0],"postcard persists in save");
        copy.Postcards[0].BirdNames[0]="変更";
        Check(restored.Postcards[0].BirdNames[0]!="変更","postcard save is a deep copy");
        var legacy=town.Capture();legacy.Postcards=null;legacy.Festival=null;legacy.NextFestivalDay=0;
        Check(restored.Restore(legacy)&&restored.Postcards.Count==0&&restored.NextFestivalDay>=restored.Day,"old save gains a future festival");

        var water=new TownSimulation(11);water.Money=5000;
        Check(water.Build(FacilityKind.Fountain,1,0)&&water.Build(FacilityKind.Park,1,1),"water venue can be built");
        var chosen=new TownSimulation();
        Check(water.ChooseFestival(FestivalKind.WatersideDay)&&chosen.Restore(water.Capture())&&chosen.SelectedFestival==FestivalKind.WatersideDay,"chosen event saves before it begins");
        var due=water.Capture();due.Time=240;
        Check(water.Restore(due)&&water.ChooseFestival(FestivalKind.WatersideDay)&&water.StartFestival(),"waterside event starts");
        Arrive(water,water.Birds[1],water.At(1,0));
        Check(water.Postcards.Count==0,"bathing alone does not finish waterside day");
        Arrive(water,water.Birds[2],water.At(1,1));
        Check(water.Postcards.Count==1&&water.Postcards[0].BirdNames.Count==2,"bathing and park visit make a two-bird postcard");

        var clock=new TownSimulation(9);clock.Money=5000;
        Check(clock.Build(FacilityKind.ClockTower,1,0),"clock venue can be built");
        var evening=clock.Capture();evening.Time=240;
        Check(clock.Restore(evening)&&clock.ChooseFestival(FestivalKind.ClockEvening)&&clock.StartFestival(),"evening event starts");
        Arrive(clock,clock.Birds[3],clock.At(1,0));
        Check(clock.Postcards.Count==0,"clock visit before evening does not count");
        clock.Time=320;clock.Tick(.01f);
        Check(clock.Postcards.Count==1&&clock.Postcards[0].BirdNames.Contains(clock.Birds[3].Name),"evening clock visit makes postcard");
        Check(!clock.StartFestival()&&clock.NextFestivalDay>clock.Day,"event cannot be farmed before next date");
        var next=clock.Capture();next.Time=(clock.NextFestivalDay-1)*TownSimulation.DayLength;
        Check(clock.Restore(next)&&clock.StartFestival(),"festival can be held again after three days");
        var active=clock.Capture();var resumed=new TownSimulation();
        Check(resumed.Restore(active)&&resumed.FestivalActive,"ongoing festival survives save and load");
        Check(resumed.CancelFestival()&&resumed.StartFestival(),"festival can restart without a penalty");
        var invalid=clock.Capture();invalid.Postcards[0].Kind=(FestivalKind)99;
        Check(!clock.Restore(invalid)&&clock.Postcards.Count==1,"invalid event save rejected without mutation");
    }
}
