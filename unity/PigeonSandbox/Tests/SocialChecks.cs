using System;
using PigeonSandbox;

public static class SocialChecks
{
    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    static TownSimulation Fixture()
    {
        var town = new TownSimulation(46);
        town.Build(FacilityKind.Park, 2, 0);
        for (int i = 0; i < town.Birds.Count; i++)
        {
            var b = town.Birds[i];
            b.SocialCooldown = 100;
            b.Decision = 100;
        }

        var a = town.Birds[0];
        var b2 = town.Birds[1];
        a.X = 4.4f;
        a.Z = -1.5f;
        b2.X = 5;
        b2.Z = -1.5f;
        a.SocialCooldown = b2.SocialCooldown = 0;
        return town;
    }

    static void Run(TownSimulation t, float seconds)
    {
        for (int i = 0; i < (int)(seconds * 10); i++)
            t.Tick(.1f);
    }

    static void Together(TownSimulation t)
    {
        var a = t.Birds[0];
        var b = t.Birds[1];
        a.X = 4.4f;
        a.Z = -1.5f;
        b.X = 5;
        b.Z = -1.5f;
        a.Y = b.Y = 0;
        a.SocialCooldown = b.SocialCooldown = 0;
        a.CheerRemaining = b.CheerRemaining = 0;
        a.Action = b.Action = "休憩";
        Check(t.TryStartCompanionship(a, b, t.At(2, 0)), "repeat visit can start");
        Run(t, 22);
    }

    static void FriendshipChecks()
    {
        var t = Fixture();
        var a = t.Birds[0];
        var b = t.Birds[1];
        Check(t.BestFriendOf(a) == null, "new town has no invented friendship");
        Together(t);
        Check(t.FriendshipTime(a.Id, b.Id) > 5 && t.BestFriendOf(a) == null, "one shared rest begins a friendship without instant best friends");
        Together(t);
        Check(t.BestFriendOf(a) == b && t.BestFriendOf(b) == a, "repeated shared rests create reciprocal friendship");
        t.RenameBird(b.Id, "しずくちゃん");
        Check(t.BestFriendOf(a).Name == "しずくちゃん", "friend lookup follows renamed bird");
        float shared = t.FriendshipTime(a.Id, b.Id);
        t.Tick(0);
        Check(t.FriendshipTime(a.Id, b.Id) == shared, "pause does not advance friendship");
        a.SocialCooldown = b.SocialCooldown = 1000;
        Run(t, 40);
        Check(t.FriendshipTime(a.Id, b.Id) == shared, "time apart does not reduce or inflate friendship");
        var saved = t.Capture();
        var restored = new TownSimulation();
        Check(restored.Restore(saved) && restored.BestFriendOf(restored.Birds[0]).Name == "しずくちゃん" && restored.Birds[0].Social == SocialActivity.None, "friendship survives load while activities restart");
        saved.Friendships[0].SharedSeconds = 0;
        Check(t.FriendshipTime(a.Id, b.Id) == shared && restored.FriendshipTime(a.Id, b.Id) == shared, "friendship saves are deep copies");
        var old = t.Capture();
        old.Friendships = null;
        Check(restored.Restore(old) && restored.BestFriendOf(restored.Birds[0]) == null, "legacy save without relationships loads");
        foreach (int invalidCase in new[]{0, 1, 2, 3, 4, 5, 6, 7, 8})
        {
            var broken = t.Capture();
            var bond = broken.Friendships[0];
            if (invalidCase == 0)
                bond.SecondId = bond.FirstId;
            if (invalidCase == 1)
                bond.SecondId = 999999;
            if (invalidCase == 2)
                bond.SharedSeconds = float.NaN;
            if (invalidCase == 3)
                bond.SharedSeconds = -1;
            if (invalidCase == 4)
                broken.Friendships.Add(new BirdFriendship{FirstId = bond.SecondId, SecondId = bond.FirstId, SharedSeconds = 3});
            if (invalidCase == 5)
                bond.SharedSeconds = float.PositiveInfinity;
            if (invalidCase == 6)
                bond.SecondId = broken.Facilities[0].Id;
            if (invalidCase == 7)
                bond.SharedSeconds = 121;
            if (invalidCase == 8)
                broken.Friendships[0] = null;
            Check(!restored.Restore(broken) && restored.BestFriendOf(restored.Birds[0]) == null, "invalid relationship rejected atomically " + invalidCase);
        }

        bool greeting = false;
        for (int attempt = 0; attempt < 30 && !greeting; attempt++)
        {
            var r = new TownSimulation(attempt);
            r.Restore(t.Capture());
            var x = r.Birds[0];
            var y = r.Birds[1];
            x.X = 4.4f;
            x.Z = -1.5f;
            y.X = 5;
            y.Z = -1.5f;
            Check(r.TryStartCompanionship(x, y, r.At(2, 0)), "friends can reunite");
            if (x.Social != SocialActivity.Greeting)
                continue;
            greeting = true;
            float px = x.X, pz = x.Z;
            r.Tick(.25f);
            Check(x.CheerTurn > 0 && x.X == px && x.Z == pz, "reunion twirl is visible and stationary");
            float turn = x.CheerTurn;
            r.Tick(0);
            Check(x.CheerTurn == turn, "paused greeting retains turn");
            Run(r, 1.5f);
            Check(x.Social == SocialActivity.Walking && x.CheerTurn == 0, "greeting ends cleanly in paired walk");
        }

        Check(greeting, "friends sometimes greet with a twirl");
        var interrupted = Fixture();
        var ia = interrupted.Birds[0];
        var ib = interrupted.Birds[1];
        interrupted.TryStartCompanionship(ia, ib, interrupted.At(2, 0));
        ia.Social = ib.Social = SocialActivity.Greeting;
        ia.SocialTime = ib.SocialTime = 1.4f;
        interrupted.Tick(.25f);
        interrupted.Remove(interrupted.At(2, 0).Id);
        interrupted.Tick(.1f);
        Check(ia.CheerTurn == 0 && ib.CheerTurn == 0 && ia.Social == SocialActivity.None && ib.Social == SocialActivity.None, "removing greeting destination clears both twirls");
        foreach (var kind in new[]{FacilityKind.Fountain, FacilityKind.Plaza})
        {
            var r = Fixture();
            r.Remove(r.At(2, 0).Id);
            r.Build(kind, 2, 0);
            var x = r.Birds[0];
            var y = r.Birds[1];
            Check(r.TryStartCompanionship(x, y, r.At(2, 0)), "pair accepts shared destination " + kind);
            bool arrived = false;
            for (int i = 0; i < 160; i++)
            {
                r.Tick(.1f);
                if (x.Social == SocialActivity.Resting || x.Social == SocialActivity.Bathing)
                {
                    arrived = true;
                    break;
                }
            }

            Check(arrived, "pair reaches shared destination " + kind);
            Run(r, 1);
            Check(x.Y > 0 && Math.Abs(x.Y - y.Y) < .01f && Math.Abs(x.X - y.X) > .45f, "pair rests side by side above destination " + kind);
            if (kind == FacilityKind.Fountain)
                Check(x.Action == "水浴び" && y.Action == "水浴び" && r.ActivityOf(x).Contains("と水浴び中"), "both birds bathe with companion label");
            if (kind == FacilityKind.Plaza)
                r.Move(r.At(2, 0).Id, 4, 4);
            else
                r.Remove(r.At(2, 0).Id);
            r.Tick(.1f);
            Check(x.Y == 0 && y.Y == 0 && x.Social == SocialActivity.None && y.Social == SocialActivity.None && x.CheerTurn == 0, "destination change releases elevated pair");
        }
    }

    public static void RunAll()
    {
        FriendshipChecks();
        var t = Fixture();
        var a = t.Birds[0];
        var b = t.Birds[1];
        var park = t.At(2, 0);
        Check(t.TryStartCompanionship(a, b, park), "nearby pigeons form a reciprocal pair");
        Check(a.CompanionId == b.Id && b.CompanionId == a.Id, "pair links use stable bird IDs");
        Check(!t.TryStartCompanionship(t.Birds[2], a, park), "paired bird cannot join another pair");
        Check(t.ActivityOf(a) == b.Name + "と散歩中", "social label names companion");
        t.RenameBird(b.Id, "ともだち");
        Check(t.ActivityOf(a) == "ともだちと散歩中", "renaming updates social label immediately");
        float x = a.X;
        t.Tick(0);
        Check(a.X == x && a.CompanionId == b.Id, "pause preserves social activity");
        bool rested = false;
        for (int i = 0; i < 140; i++)
        {
            t.Tick(.1f);
            if (a.Social == SocialActivity.Resting)
            {
                rested = true;
                break;
            }
        }

        Check(rested && b.Social == SocialActivity.Resting, "pair arrives and rests together in park");
        float distance = (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
        Check(distance > .45f && distance < 1.1f, "resting pair keeps visible personal space");
        Check(t.ActivityOf(a) == "ともだちと休憩中", "resting label names companion");
        var restored = new TownSimulation();
        Check(restored.Restore(t.Capture()) && restored.Birds[1].Name == "ともだち" && restored.Birds[0].CompanionId == -1, "save retains names and safely restarts transient activities");
        for (int i = 0; i < 90; i++)
            t.Tick(.1f);
        Check(a.CompanionId == -1 && b.CompanionId == -1 && a.SocialCooldown > 0, "pair separates with a cooldown");
        var removed = Fixture();
        a = removed.Birds[0];
        b = removed.Birds[1];
        park = removed.At(2, 0);
        removed.TryStartCompanionship(a, b, park);
        removed.Remove(park.Id);
        removed.Tick(.1f);
        Check(a.CompanionId == -1 && b.CompanionId == -1, "removing park releases both birds");
        var moved = Fixture();
        a = moved.Birds[0];
        b = moved.Birds[1];
        park = moved.At(2, 0);
        moved.TryStartCompanionship(a, b, park);
        moved.Move(park.Id, 4, 4);
        moved.Tick(.1f);
        Check(a.CompanionId == -1 && b.CompanionId == -1, "moving park releases both birds");
        var missing = Fixture();
        a = missing.Birds[0];
        b = missing.Birds[1];
        missing.TryStartCompanionship(a, b, missing.At(2, 0));
        missing.Birds.Remove(b);
        missing.Tick(.1f);
        Check(a.CompanionId == -1, "missing partner is handled safely");
        foreach (string action in new[]{"食事", "水浴び", "ごきげんクルクル"})
        {
            var busy = Fixture();
            busy.Birds[0].Action = action;
            Check(!busy.TryStartCompanionship(busy.Birds[0], busy.Birds[1], busy.At(2, 0)), "socializing does not interrupt " + action);
        }

        var automatic = new TownSimulation(12);
        automatic.Money = 10000;
        automatic.Build(FacilityKind.Park, 1, 0);
        automatic.Build(FacilityKind.Park, -1, -1);
        bool seen = false;
        for (int i = 0; i < 3000; i++)
        {
            automatic.Tick(.1f);
            seen |= automatic.Birds.Exists(bird => bird.Social != SocialActivity.None);
        }

        Check(seen, "ordinary simulation starts companionship without a player command");
        Check(automatic.Birds.Exists(bird => automatic.BestFriendOf(bird) != null), "friendships develop through ordinary autonomous play");
        foreach (var bird in automatic.Birds)
            Check(!float.IsNaN(bird.X) && Math.Abs(bird.X) < automatic.MapEdge && Math.Abs(bird.Z) < automatic.MapEdge, "social simulation stays within town bounds");
        var flying = Fixture();
        flying.Birds[0].Y = 1;
        Check(!flying.TryStartCompanionship(flying.Birds[0], flying.Birds[1], flying.At(2, 0)), "socializing does not interrupt flight");
    }
}
