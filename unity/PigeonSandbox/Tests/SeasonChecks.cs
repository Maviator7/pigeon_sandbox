using System;
using PigeonSandbox;

public static class SeasonChecks
{
    static void Check(bool condition, string label)
    {
        if (!condition)
            throw new Exception(label);
        Console.WriteLine("PASS: " + label);
    }

    static TownSimulation OnDay(int day)
    {
        var town = new TownSimulation(42);
        var save = town.Capture();
        save.Time = (day - 1) * TownSimulation.DayLength;
        Check(town.Restore(save), "restore day " + day);
        return town;
    }

    static void Arrive(TownSimulation town, TownBird bird, Facility place)
    {
        float angle = (bird.Id % 4) * (float)Math.PI * .5f;
        bird.X = place.X * TownSimulation.CellSize + (float)Math.Cos(angle) * .96f;
        bird.Z = place.Z * TownSimulation.CellSize + (float)Math.Sin(angle) * .96f;
        bird.TargetId = place.Id;
        bird.Decision = 100;
        bird.Wait = 0;
        bird.Activity = BirdActivity.Stroll;
        town.Tick(.01f);
    }

    public static void RunAll()
    {
        var boundaries = new[]{new
        {
        Day = 1, Season = TownSeason.Spring, SeasonDay = 1, Year = 1
        }

        , new
        {
        Day = 15, Season = TownSeason.Spring, SeasonDay = 15, Year = 1
        }

        , new
        {
        Day = 16, Season = TownSeason.Summer, SeasonDay = 1, Year = 1
        }

        , new
        {
        Day = 30, Season = TownSeason.Summer, SeasonDay = 15, Year = 1
        }

        , new
        {
        Day = 31, Season = TownSeason.Autumn, SeasonDay = 1, Year = 1
        }

        , new
        {
        Day = 45, Season = TownSeason.Autumn, SeasonDay = 15, Year = 1
        }

        , new
        {
        Day = 46, Season = TownSeason.Winter, SeasonDay = 1, Year = 1
        }

        , new
        {
        Day = 60, Season = TownSeason.Winter, SeasonDay = 15, Year = 1
        }

        , new
        {
        Day = 61, Season = TownSeason.Spring, SeasonDay = 1, Year = 2
        }
        };
        foreach (var expected in boundaries)
        {
            var town = OnDay(expected.Day);
            Check(town.Season == expected.Season && town.SeasonDay == expected.SeasonDay && town.Year == expected.Year, "season calendar at day " + expected.Day);
        }

        var spring = OnDay(1);
        var winter = OnDay(46);
        foreach (var town in new[]{spring, winter})
        {
            town.Money = 5000;
            Check(town.Build(FacilityKind.Park, -2, 1), "seasonal park can be built");
            foreach (var wish in town.Wishes)
                wish.Complete = true;
            town.Tick(.1f);
        }

        Check(spring.Birds[2].TargetId == spring.At(-2, 1).Id, "spring shy bird visits flowering park");
        Check(winter.Birds[2].TargetId == winter.At(-2, 0).Id, "winter shy bird rests at tree");
        var flowers = OnDay(1);
        flowers.Money = 5000;
        flowers.Build(FacilityKind.Park, -2, 1);
        Check(flowers.SeasonalVenueReady && flowers.SeasonalPostcards.Count == 0, "spring venue is ready");
        Arrive(flowers, flowers.Birds[0], flowers.At(-2, 1));
        Check(flowers.SeasonalEventCompleted && flowers.SeasonalPostcards.Count == 1, "spring postcard follows real park activity");
        Check(flowers.SeasonalPostcards[0].BirdNames.Contains(flowers.Birds[0].Name), "spring postcard names its pigeon");
        var copied = flowers.Capture();
        var loaded = new TownSimulation();
        Check(loaded.Restore(copied) && loaded.SeasonalPostcards.Count == 1, "seasonal postcard survives save");
        copied.SeasonalPostcards[0].BirdNames[0] = "別の名前";
        Check(loaded.SeasonalPostcards[0].BirdNames[0] != "別の名前", "seasonal postcard is deep copied");
        var nextSpring = flowers.Capture();
        nextSpring.Time = 60 * TownSimulation.DayLength;
        Check(flowers.Restore(nextSpring) && flowers.Year == 2 && !flowers.SeasonalEventCompleted && flowers.SeasonalPostcards.Count == 1, "spring can be celebrated again next year");
        Arrive(flowers, flowers.Birds[0], flowers.At(-2, 1));
        Check(flowers.SeasonalPostcards.Count == 2 && flowers.SeasonalPostcards[1].Year == 2, "next year adds a new postcard");
        var summer = OnDay(16);
        summer.Money = 5000;
        summer.Build(FacilityKind.Fountain, 1, 0);
        Check(!summer.SeasonalVenueReady, "summer needs a park beside the fountain");
        summer.Build(FacilityKind.Park, 1, 1);
        Arrive(summer, summer.Birds[1], summer.At(1, 0));
        Check(summer.SeasonalEventCompleted && summer.SeasonalPostcards[0].Season == TownSeason.Summer, "summer postcard follows bathing");
        Check(summer.ChooseFestival(FestivalKind.WatersideDay) && summer.StartFestival() && summer.FestivalActive, "regular festival remains available in summer");
        var autumn = OnDay(31);
        autumn.Money = 5000;
        autumn.Build(FacilityKind.Bakery, 1, 0);
        Arrive(autumn, autumn.Birds[0], autumn.At(1, 0));
        Check(autumn.SeasonalBirdObserved && !autumn.SeasonalEventCompleted, "autumn needs human purchase after pigeon meal");
        var autumnProgress = new TownSimulation();
        Check(autumnProgress.Restore(autumn.Capture()) && autumnProgress.SeasonalBirdObserved, "partial autumn observation survives save");
        Check(autumn.Move(autumn.At(1, 0).Id, 4, 4) && autumn.SeasonalBirdObserved && !autumn.SeasonalVenueReady, "moving autumn venue preserves progress but pauses observation");
        Check(autumn.Move(autumn.At(4, 4).Id, 1, 0), "autumn venue can be restored");
        for (int i = 0; i < 3000 && !autumn.SeasonalEventCompleted; i++)
            autumn.Tick(.1f);
        Check(autumn.SeasonalEventCompleted && autumn.SeasonalPostcards[0].Season == TownSeason.Autumn, "autumn postcard follows actual purchase");
        var winterEvent = OnDay(46);
        winterEvent.Money = 5000;
        winterEvent.Build(FacilityKind.Tree, 1, 0);
        Arrive(winterEvent, winterEvent.Birds[2], winterEvent.At(1, 0));
        Check(winterEvent.SeasonalEventCompleted && winterEvent.SeasonalPostcards[0].Season == TownSeason.Winter, "winter postcard follows tree rest");
        var missing = autumnProgress.Capture();
        missing.Time = 45 * TownSimulation.DayLength;
        Check(autumnProgress.Restore(missing) && autumnProgress.Season == TownSeason.Winter && !autumnProgress.SeasonalBirdObserved, "unfinished seasonal progress resets at the boundary");
        var legacy = summer.Capture();
        legacy.SaveVersion = 1;
        legacy.SeasonalFestival = null;
        legacy.SeasonalPostcards = null;
        var oldTown = new TownSimulation();
        Check(oldTown.Restore(legacy) && oldTown.Season == TownSeason.Summer && oldTown.SeasonalPostcards.Count == 0, "old save gains season without losing its time");
        var invalid = winterEvent.Capture();
        invalid.SeasonalFestival.BirdId = 99999;
        int cardCount = winterEvent.SeasonalPostcards.Count;
        Check(!winterEvent.Restore(invalid) && winterEvent.SeasonalPostcards.Count == cardCount, "invalid seasonal save is rejected atomically");
    }
}
