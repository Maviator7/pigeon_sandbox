using System;
using PigeonSandbox;

static class SimulationChecks
{
    static void Check(bool ok, string message) { if(!ok) throw new Exception(message); }
    static void Main()
    {
        var p = new PigeonSimulation(new Random(42).NextDouble);
        p.AddFood(2,1);
        for(int i=0;i<600 && p.Eaten==0;i++) p.Tick(1f/60);
        Check(p.Eaten==1 && p.Seeds.Count==0 && p.Trust>32,"Approach and eat food");
        p.Call(-3,-2);
        for(int i=0;i<1000 && p.State==Activity.Walk;i++) p.Tick(1f/60);
        Check(Math.Abs(p.X+3)<.3 && Math.Abs(p.Z+2)<.3,"Call reaches destination");
        p.Fly(); bool airborne=false, landed=false;
        for(int i=0;i<310;i++) { p.Tick(1f/60); airborne |= p.Y>1; landed |= airborne && p.Y==0; }
        Check(airborne && landed,"Flight lands safely");
        var a=new PigeonSimulation(new Random(7).NextDouble);
        var b=new PigeonSimulation(new Random(7).NextDouble);
        for(int i=0;i<10000;i++)
        {
            a.Tick(1f/60); b.Tick(1f/60);
            Check(a.X*a.X+a.Z*a.Z<=36 && a.Y>=0,"Position bounds");
            Check(a.Hunger>=0 && a.Hunger<=100 && a.Energy>=0 && a.Energy<=100,"Need bounds");
        }
        Check(a.X==b.X && a.Z==b.Z && a.State==b.State,"Determinism");
        for(int i=0;i<100;i++) a.AddFood(100,100);
        Check(a.Seeds.Count==60,"Seed capacity");
        foreach(var seed in a.Seeds) Check(seed.X*seed.X+seed.Z*seed.Z<34,"Seed bounds");
        float age=a.Age; a.Tick(float.NaN); a.Tick(-1);
        Check(a.Age==age,"Invalid dt ignored");
        Console.WriteLine("PASS: food, call, flight, position/need bounds, determinism, food capacity, invalid time");
    }
}
