using System;
using PigeonSandbox;

public static class WishChecks
{
    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    static void Run(TownSimulation t, float seconds)
    {
        for (int i = 0; i < (int)(seconds * 10); i++)
            t.Tick(.1f);
    }

    public static void RunAll()
    {
        var t = new TownSimulation();
        Check(t.Wishes.Count == 3, "three personal wishes are assigned to residents");
        var wish = t.Wishes.Find(w => w.Kind == BirdWishKind.HomeBath);
        var bird = t.Birds.Find(b => b.Id == wish.BirdId);
        t.RenameBird(bird.Id, "みずもち");
        Check(t.WishOwner(wish).Name == "みずもち", "wish owner tracks renamed bird");
        t.Money = 5000;
        t.Build(FacilityKind.Fountain, 2, 2);
        Check(!t.WishHabitatReady(wish), "bath wish needs nearby housing");
        t.Build(FacilityKind.Housing, 2, 3);
        Check(t.WishHabitatReady(wish), "nearby home makes bath habitat ready");
        t.Tick(0);
        Check(!wish.Complete, "building habitat alone does not fulfill wish");
        var bath = t.At(2, 2);
        bird.TargetId = bath.Id;
        bird.X = 4.4f;
        bird.Z = 5.36f;
        bird.Y = 0;
        bird.Action = "水浴び";
        bird.Wait = 10;
        bird.Decision = 20;
        bird.SocialCooldown = 100;
        Run(t, 1);
        bird.Action = "休憩";
        Run(t, .1f);
        bird.Action = "水浴び";
        Run(t, 1.2f);
        Check(!wish.Complete, "interrupted visit resets fulfillment timer");
        Run(t, 1.1f);
        Check(wish.Complete, "owner actually bathing fulfills wish");
        Check(bird.Action == "水浴び", "thanks does not interrupt current bath");
        bool danced = false;
        for (int i = 0; i < 150; i++)
        {
            t.Tick(.1f);
            danced |= bird.Action == "ありがとうのクルクル";
        }

        Check(danced, "fulfilled owner celebrates when activity finishes");
        var restored = new TownSimulation();
        Check(restored.Restore(t.Capture()) && restored.Wishes.Find(w => w.Kind == BirdWishKind.HomeBath).Complete, "completed wishes survive save");
        Check(!restored.Birds.Exists(b => b.WishThanksPending), "load does not replay gratitude");
        var snapshot = t.Capture();
        snapshot.Wishes[0].Complete = !snapshot.Wishes[0].Complete;
        Check(snapshot.Wishes[0].Complete != t.Wishes[0].Complete, "wish save is deep copied");
        var legacy = t.Capture();
        legacy.Wishes = null;
        Check(restored.Restore(legacy) && restored.Wishes.Count == 3, "legacy town gains wishes without losing residents");
        var invalid = t.Capture();
        invalid.Wishes[0].BirdId = invalid.Facilities[0].Id;
        Check(!restored.Restore(invalid), "nonbird wish owner is rejected");
        invalid = t.Capture();
        invalid.Wishes.Add(invalid.Wishes[0]);
        Check(!restored.Restore(invalid), "duplicate wish kind is rejected");
        var quiet = new TownSimulation();
        quiet.Money = 5000;
        quiet.Build(FacilityKind.Park, -2, 1);
        var quietWish = quiet.Wishes.Find(w => w.Kind == BirdWishKind.QuietShade);
        Check(quiet.WishHabitatReady(quietWish), "park near tree is quiet habitat");
        quiet.Build(FacilityKind.Cafe, -1, 1);
        Check(!quiet.WishHabitatReady(quietWish), "busy cafe prevents quiet habitat");
        var friendly = new TownSimulation();
        friendly.Money = 5000;
        friendly.Build(FacilityKind.Bakery, 1, 0);
        var friendsWish = friendly.Wishes.Find(w => w.Kind == BirdWishKind.FriendlyLunch);
        Check(friendly.WishHabitatReady(friendsWish), "bakery near plaza prepares friendly lunch place");
        Run(friendly, 1);
        Check(!friendsWish.Complete, "foodie must rest there with a friend to fulfill lunch wish");
        quiet.Remove(quiet.At(-1, 1).Id);
        var shy = quiet.WishOwner(quietWish);
        shy.TargetId = quiet.At(-2, 1).Id;
        shy.X = -3.4f;
        shy.Z = 2.2f;
        shy.Action = "休憩";
        shy.Wait = 10;
        shy.Decision = 20;
        shy.SocialCooldown = 100;
        Run(quiet, 2.2f);
        Check(quietWish.Complete, "shy owner resting in quiet park fulfills wish");
        var fs = friendly.Capture();
        var fa = fs.Birds[0];
        var fb = fs.Birds[1];
        fs.Friendships.Add(new BirdFriendship{FirstId = fa.Id, SecondId = fb.Id, SharedSeconds = 14});
        friendly.Restore(fs);
        var first = friendly.Birds[0];
        var second = friendly.Birds[1];
        first.X = -.32f;
        first.Z = .76f;
        second.X = .32f;
        second.Z = .76f;
        Check(friendly.TryStartCompanionship(first, second, friendly.At(0, 0)), "friends start at bakery plaza");
        Run(friendly, 4);
        Check(friendly.Wishes.Find(w => w.Kind == BirdWishKind.FriendlyLunch).Complete, "actual rest with friend fulfills lunch wish");
        int gratitude = 0;
        bool thanking = false;
        for (int i = 0; i < 800; i++)
        {
            t.Tick(.1f);
            bool now = bird.Action == "ありがとうのクルクル";
            if (now && !thanking)
                gratitude++;
            thanking = now;
        }

        Check(gratitude == 0, "completed wish does not repeatedly trigger gratitude");
    }
}
