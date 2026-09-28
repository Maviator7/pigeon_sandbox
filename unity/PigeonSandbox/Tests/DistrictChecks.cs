using System;
using PigeonSandbox;

public static class DistrictChecks
{
    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    static TownSimulation ExpandedTown()
    {
        var town = new TownSimulation(98);
        town.Money = 20000;
        while (town.CanExpand)
            Check(town.ExpandTown(), "station expansion succeeds");
        return town;
    }

    public static void RunAll()
    {
        var town = new TownSimulation(98);
        town.Money = 10000;
        Check(!town.UnlockWaterfront() && !town.WaterfrontUnlocked, "waterfront requires full station expansion");
        Check(!town.CanPlace(9, 0), "locked waterfront cannot be built");
        town = ExpandedTown();
        float before = town.Money;
        Check(TownSimulation.WaterfrontCost == 3600 && town.UnlockWaterfront() && town.WaterfrontUnlocked && town.Money == before - 3600, "waterfront unlocks for exact cost");
        Check(!town.UnlockWaterfront() && town.Money == before - 3600, "waterfront cannot charge twice");
        Check(town.IsPlayablePlot(8, 8) && town.IsWaterfrontPlot(9, -3) && town.IsWaterfrontPlot(15, 3), "both districts have playable corners");
        Check(!town.IsPlayablePlot(9, 4) && !town.IsPlayablePlot(15, 4) && !town.IsPlayablePlot(16, 0) && !town.IsWaterfrontPlot(8, 0), "waterfront rejects shoulder and outside plots");
        Check(town.IsWalkableGround(15 * TownSimulation.CellSize, 4 * TownSimulation.CellSize), "far shoulder is walkable without becoming buildable");
        Check(town.Build(FacilityKind.Fountain, 15, 3) && !town.Build(FacilityKind.Tree, 9, 4), "annex accepts only its 49 plots");
        int id = town.At(15, 3).Id;
        Check(town.Move(id, 8, 3) && town.Move(id, 15, -3), "building moves across seam");
        town.Birds[0].X = 15 * TownSimulation.CellSize;
        town.Birds[0].Z = -3 * TownSimulation.CellSize;
        var save = town.Capture();
        var restored = new TownSimulation();
        Check(save.SaveVersion == 3 && restored.Restore(save) && restored.WaterfrontUnlocked && restored.At(15, -3) != null && restored.Birds[0].X == town.Birds[0].X, "waterfront layout and bird position survive save");
        var outsideBird = town.Capture();
        outsideBird.Birds[0].X = 15 * TownSimulation.CellSize;
        outsideBird.Birds[0].Z = 8 * TownSimulation.CellSize;
        Check(restored.Restore(outsideBird) && restored.IsWalkableGround(restored.Birds[0].X, restored.Birds[0].Z), "bird outside asymmetric ground is moved onto landscaped land");
        var legacy = new TownSimulation().Capture();
        legacy.SaveVersion = 2;
        Check(restored.Restore(legacy) && !restored.WaterfrontUnlocked, "version-2 save migrates as locked");
        var invalid = town.Capture();
        invalid.ExpansionLevel = 3;
        Check(!restored.Restore(invalid) && !restored.WaterfrontUnlocked, "invalid unlock state cannot mutate live town");
        invalid = town.Capture();
        invalid.Facilities[0].X = 9;
        invalid.Facilities[0].Z = 4;
        Check(!restored.Restore(invalid) && !restored.WaterfrontUnlocked, "shoulder plot in save is rejected before mutation");
        var requestTown = ExpandedTown();
        Check(requestTown.UnlockWaterfront(), "request town unlocks waterfront");
        requestTown.Build(FacilityKind.Tree, 9, 0);
        requestTown.Build(FacilityKind.Park, 9, 1);
        Check(requestTown.WaterfrontRequests[0].Complete && !requestTown.WaterfrontRequests[1].Complete, "waterfront tree and park complete first request");
        float earned = requestTown.Money;
        requestTown.Recalculate();
        Check(requestTown.Money == earned, "waterfront request reward is once only");
        requestTown.Build(FacilityKind.Fountain, 10, 1);
        Check(requestTown.WaterfrontRequests[1].Complete, "nearby waterfront fountain and park complete second request");
        requestTown.Build(FacilityKind.Bakery, 11, 0);
        requestTown.Build(FacilityKind.Plaza, 11, 1);
        Check(!requestTown.WaterfrontRequests[2].Complete, "bakery request waits for a real purchase");
        for (int i = 0; i < 8000 && !requestTown.WaterfrontRequests[2].Complete; i++)
            requestTown.Tick(.1f);
        Check(requestTown.WaterfrontRequests[2].Complete, "waterfront visitor purchase completes third request");
        float finalReward = requestTown.Money;
        requestTown.Recalculate();
        Check(requestTown.Money == finalReward, "purchase request reward is once only");
        var requestRestore = new TownSimulation();
        Check(requestRestore.Restore(requestTown.Capture()) && requestRestore.WaterfrontRequests[0].Complete && requestRestore.WaterfrontRequests[1].Complete && requestRestore.WaterfrontRequests[2].Complete, "waterfront request completion survives save");
        var malformed = requestTown.Capture();
        malformed.CompletedWaterfrontRequests = new bool[2];
        float unchanged = requestRestore.Money;
        Check(!requestRestore.Restore(malformed) && requestRestore.Money == unchanged, "malformed request flags cannot mutate live town");
        var crossSeam = ExpandedTown();
        crossSeam.UnlockWaterfront();
        crossSeam.Build(FacilityKind.Tree, 8, 0);
        crossSeam.Build(FacilityKind.Park, 9, 0);
        Check(!crossSeam.WaterfrontRequests[0].Complete, "station tree does not complete waterfront request");
        var birds = ExpandedTown();
        birds.UnlockWaterfront();
        birds.Build(FacilityKind.Fountain, 1, 1);
        birds.Build(FacilityKind.Fountain, 10, 0);
        birds.Tick(.1f);
        var bather = birds.Birds[1];
        Check(bather.TargetId == birds.At(10, 0).Id, "water-loving bird prefers waterfront fountain");
        bool arrived = false;
        for (int i = 0; i < 1200 && !arrived; i++)
        {
            birds.Tick(.1f);
            arrived = bather.X > 9 * TownSimulation.CellSize && bather.Activity == BirdActivity.Bathe;
        }

        Check(arrived, "bird can reach and bathe in the far waterfront district");
        var longTrip = ExpandedTown();
        longTrip.UnlockWaterfront();
        longTrip.Build(FacilityKind.Fountain, 15, 3);
        var distantBird = longTrip.Birds[1];
        distantBird.X = -8 * TownSimulation.CellSize;
        distantBird.Z = -8 * TownSimulation.CellSize;
        longTrip.Tick(.1f);
        int farFountain = longTrip.At(15, 3).Id;
        Check(distantBird.TargetId == farFountain, "distant bird chooses waterfront bath");
        bool distantArrival = false, stayedOnLand = true;
        for (int i = 0; i < 1600 && !distantArrival; i++)
        {
            longTrip.Tick(.1f);
            stayedOnLand &= longTrip.IsWalkableGround(distantBird.X, distantBird.Z);
            distantArrival = distantBird.Activity == BirdActivity.Bathe && distantBird.X > 14 * TownSimulation.CellSize;
        }

        Check(distantArrival, "distant bird reaches opposite waterfront corner before reconsidering");
        Check(stayedOnLand, "diagonal waterfront walk remains on land");
        foreach (int side in new[]{-1, 1})
        {
            var shoulderTown = ExpandedTown();
            shoulderTown.UnlockWaterfront();
            shoulderTown.Build(FacilityKind.Fountain, 15, side * 3);
            var walker = shoulderTown.Birds[1];
            walker.X = 8 * TownSimulation.CellSize;
            walker.Z = side * 8 * TownSimulation.CellSize;
            bool land = true, reached = false;
            for (int i = 0; i < 1200 && !reached; i++)
            {
                shoulderTown.Tick(.1f);
                land &= shoulderTown.IsWalkableGround(walker.X, walker.Z);
                reached = walker.Activity == BirdActivity.Bathe && walker.X > 14 * TownSimulation.CellSize;
            }

            Check(land && reached, "bird uses landscaped shoulder from station corner " + side);
        }

        bool prefersLocal = true;
        for (int seed = 1; seed <= 12; seed++)
        {
            var localTown = new TownSimulation(seed);
            localTown.Money = 20000;
            while (localTown.CanExpand)
                localTown.ExpandTown();
            localTown.UnlockWaterfront();
            localTown.Build(FacilityKind.Fountain, 9, 0);
            localTown.Build(FacilityKind.Fountain, 15, 0);
            localTown.Birds[1].X = 9 * TownSimulation.CellSize;
            localTown.Birds[1].Z = 0;
            localTown.Tick(.1f);
            prefersLocal &= localTown.Birds[1].TargetId == localTown.At(9, 0).Id;
        }

        Check(prefersLocal, "waterfront resident prefers a nearby bath across random seeds");
        var noVenue = ExpandedTown();
        noVenue.UnlockWaterfront();
        noVenue.Build(FacilityKind.Housing, 10, 0);
        noVenue.Build(FacilityKind.Tree, 11, 0);
        bool invalidEastVisit = false;
        for (int i = 0; i < 250; i++)
        {
            noVenue.Tick(.1f);
            foreach (var visitor in noVenue.Visitors)
                invalidEastVisit |= visitor.FromWaterfront;
        }

        Check(!invalidEastVisit, "non-visitor venues never open waterfront entrance");
        var visitors = ExpandedTown();
        visitors.UnlockWaterfront();
        visitors.Build(FacilityKind.Cafe, 12, 0);
        Visitor local = null;
        for (int i = 0; i < 300 && local == null; i++)
        {
            visitors.Tick(.1f);
            local = visitors.Visitors.Find(v => v.FromWaterfront);
        }

        Check(local != null && local.X > 15 * TownSimulation.CellSize && local.TargetId == visitors.At(12, 0).Id, "east visitor enters for local venue only");
        int cafeId = visitors.At(12, 0).Id;
        Check(visitors.Remove(cafeId), "remove waterfront destination during visit");
        float oldX = local.X;
        visitors.Tick(.1f);
        Check(local.X >= oldX && local.TargetId == -1, "east visitor returns east when venue disappears");
    }
}
