using System;
using PigeonSandbox;

public static class BenchmarkChecks
{
    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    public static void RunAll()
    {
        var town = TownSimulation.CreateBenchmark();
        Check(town.MapSize == 17 && !town.CanExpand, "benchmark town uses the largest map");
        Check(town.Facilities.Count == town.MapSize * town.MapSize, "benchmark town fills every plot");
        bool free = false;
        for (int x = -town.MapRadius; x <= town.MapRadius; x++)
            for (int z = -town.MapRadius; z <= town.MapRadius; z++)
                free |= town.CanPlace(x, z);
        Check(!free, "benchmark town leaves no free plot");
        foreach (FacilityKind kind in Enum.GetValues(typeof(FacilityKind)))
            Check(town.Facilities.Exists(f => f.Kind == kind), "benchmark town includes " + kind);
        Check(town.Facilities.Exists(f => f.Level == 1) && town.Facilities.Exists(f => f.Level == 3), "benchmark town mixes facility levels");
        Check(town.Birds.Count == 15, "benchmark town has the maximum pigeon count");
        foreach (Plumage feather in Enum.GetValues(typeof(Plumage)))
            Check(town.Discovered(feather), "benchmark town shows plumage " + feather);
        var other = TownSimulation.CreateBenchmark();
        bool same = other.Facilities.Count == town.Facilities.Count;
        for (int i = 0; same && i < town.Facilities.Count; i++)
            same = town.Facilities[i].Kind == other.Facilities[i].Kind && town.Facilities[i].X == other.Facilities[i].X && town.Facilities[i].Z == other.Facilities[i].Z && town.Facilities[i].Level == other.Facilities[i].Level;
        Check(same, "benchmark town layout is deterministic");
        for (int i = 0; i < 600; i++)
            town.Tick(.1f);
        Check(town.Birds.Count == 15 && town.Visitors.Count > 0, "benchmark town stays populated while running");
        // Steady-state ticks should not churn the GC: hot lookups must avoid closures and temporary lists.
        for (int i = 0; i < 600; i++)
            town.Tick(1f / 60);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 3000; i++)
            town.Tick(1f / 60);
        double perTick = (GC.GetAllocatedBytesForCurrentThread() - before) / 3000.0;
        Console.WriteLine("benchmark town allocates " + perTick.ToString("0") + " bytes per tick");
        Check(perTick < 512, "benchmark town tick allocates under 512 bytes");
        var restored = new TownSimulation();
        Check(restored.Restore(town.Capture()) && restored.Facilities.Count == town.Facilities.Count, "benchmark town survives save and restore");
    }
}
