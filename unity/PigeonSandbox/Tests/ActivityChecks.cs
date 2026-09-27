using System;
using System.Collections.Generic;
using PigeonSandbox;

public static class ActivityChecks
{
    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    public static void RunAll()
    {
        var names = new HashSet<string>();
        foreach (BirdActivity activity in Enum.GetValues(typeof(BirdActivity)))
            Check(!string.IsNullOrEmpty(TownSimulation.ActivityName(activity)) && names.Add(TownSimulation.ActivityName(activity)), "activity " + activity + " has its own display name");
        var bird = new TownBird{Activity = BirdActivity.Preen};
        Check(bird.Action == "羽繕い", "display text follows the activity");
        var town = new TownSimulation();
        town.Birds[0].Activity = BirdActivity.Bathe;
        var restored = new TownSimulation();
        Check(restored.Restore(town.Capture()) && restored.Birds[0].Activity == BirdActivity.Stroll, "restored birds start strolling");
    }
}
