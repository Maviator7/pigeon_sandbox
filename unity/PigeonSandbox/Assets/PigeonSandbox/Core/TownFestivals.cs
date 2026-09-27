using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public enum FestivalKind
    {
        BakeryMarket,
        WatersideDay,
        ClockEvening
    }

    [Serializable]
    public class TownFestivalState
    {
        public FestivalKind Kind;
        public bool Active, HumanPurchase;
        public List<int> MainBirdIds = new List<int>();
        public List<int> PartnerBirdIds = new List<int>();
    }

    [Serializable]
    public class TownPostcard
    {
        public FestivalKind Kind;
        public int Day;
        public List<string> BirdNames = new List<string>();
    }

    public partial class TownSimulation
    {
        public FestivalKind SelectedFestival
        {
            get;
            private set;
        }

        public int NextFestivalDay
        {
            get;
            private set;
        }

        = 3;
        public readonly List<TownPostcard> Postcards = new List<TownPostcard>();
        TownFestivalState festival = new TownFestivalState();
        public bool FestivalActive => festival.Active;
        public bool FestivalHumanPurchase => festival.HumanPurchase;
        public IReadOnlyList<int> FestivalMainBirdIds => festival.MainBirdIds;
        public IReadOnlyList<int> FestivalPartnerBirdIds => festival.PartnerBirdIds;
        public bool FestivalDue => Day >= NextFestivalDay;
        public bool FestivalCurrentVenueReady => festival.Active && FestivalVenueReady(festival.Kind);
        public static string FestivalName(FestivalKind kind)
        {
            switch (kind)
            {
                case FestivalKind.BakeryMarket:
                    return "パン屋の朝市";
                case FestivalKind.WatersideDay:
                    return "水辺の午後";
                case FestivalKind.ClockEvening:
                    return "時計台の夕暮れ";
                default:
                    return "街の催し";
            }
        }

        public static string FestivalVenueHint(FestivalKind kind)
        {
            switch (kind)
            {
                case FestivalKind.BakeryMarket:
                    return "パン屋と広場を2マス以内に置く";
                case FestivalKind.WatersideDay:
                    return "噴水と花壇の公園を2マス以内に置く";
                case FestivalKind.ClockEvening:
                    return "時計台と広場を2マス以内に置く";
                default:
                    return "";
            }
        }

        Facility FestivalVenue(FestivalKind kind)
        {
            FacilityKind main = kind == FestivalKind.BakeryMarket ? FacilityKind.Bakery : kind == FestivalKind.WatersideDay ? FacilityKind.Fountain : FacilityKind.ClockTower;
            FacilityKind neighbor = kind == FestivalKind.WatersideDay ? FacilityKind.Park : FacilityKind.Plaza;
            return FirstNear(main, neighbor);
        }

        public bool FestivalVenueReady(FestivalKind kind)
        {
            return Enum.IsDefined(typeof(FestivalKind), kind) && FestivalVenue(kind) != null;
        }

        public bool ChooseFestival(FestivalKind kind)
        {
            if (festival.Active || !Enum.IsDefined(typeof(FestivalKind), kind))
                return false;
            SelectedFestival = kind;
            return true;
        }

        public bool StartFestival()
        {
            if (!FestivalDue || festival.Active)
                return false;
            if (!FestivalVenueReady(SelectedFestival))
                return false;
            festival = new TownFestivalState{Kind = SelectedFestival, Active = true};
            Changed(FestivalName(SelectedFestival) + "が始まりました。街の様子を眺めよう。");
            return true;
        }

        public bool CancelFestival()
        {
            if (!festival.Active)
                return false;
            festival = new TownFestivalState();
            Changed("催しの準備に戻りました。いつでも開き直せます。");
            return true;
        }

        void WitnessFestivalPurchase(Facility shop)
        {
            if (festival.Active && festival.Kind == FestivalKind.BakeryMarket && FestivalVenueReadyAt(shop, festival.Kind))
                festival.HumanPurchase = true;
        }

        void TickFestival()
        {
            if (!festival.Active)
                return;
            if (!FestivalCurrentVenueReady)
                return;
            foreach (var bird in Birds)
            {
                var target = Find(bird.TargetId);
                if (target == null || Distance(bird.X - target.X * CellSize, bird.Z - target.Z * CellSize) > 2.1f)
                    continue;
                bool atMain = FestivalVenueReadyAt(target, festival.Kind);
                if (atMain && (festival.Kind != FestivalKind.ClockEvening || TimeOfDay == TownTimeOfDay.Evening) && MainAction(bird, festival.Kind) && !festival.MainBirdIds.Contains(bird.Id))
                    festival.MainBirdIds.Add(bird.Id);
                if (festival.Kind == FestivalKind.WatersideDay && target.Kind == FacilityKind.Park && NearKind(target, FacilityKind.Fountain) && (bird.Activity == BirdActivity.Preen || bird.Activity == BirdActivity.Sunbathe || bird.Activity == BirdActivity.Rest) && !festival.PartnerBirdIds.Contains(bird.Id))
                    festival.PartnerBirdIds.Add(bird.Id);
            }

            bool finished = festival.Kind == FestivalKind.BakeryMarket ? festival.HumanPurchase && festival.MainBirdIds.Count > 0 : festival.Kind == FestivalKind.WatersideDay ? festival.MainBirdIds.Count > 0 && festival.PartnerBirdIds.Count > 0 : festival.MainBirdIds.Count > 0;
            if (finished)
                FinishFestival();
        }

        bool FestivalVenueReadyAt(Facility venue, FestivalKind kind)
        {
            if (kind == FestivalKind.BakeryMarket)
                return venue.Kind == FacilityKind.Bakery && NearKind(venue, FacilityKind.Plaza);
            if (kind == FestivalKind.WatersideDay)
                return venue.Kind == FacilityKind.Fountain && NearKind(venue, FacilityKind.Park);
            return venue.Kind == FacilityKind.ClockTower && NearKind(venue, FacilityKind.Plaza);
        }

        static bool MainAction(TownBird bird, FestivalKind kind)
        {
            return kind == FestivalKind.BakeryMarket ? bird.Activity == BirdActivity.Eat : kind == FestivalKind.WatersideDay ? bird.Activity == BirdActivity.Bathe : bird.Activity == BirdActivity.Watch;
        }

        void FinishFestival()
        {
            var card = new TownPostcard{Kind = festival.Kind, Day = Day};
            var ids = new HashSet<int>(festival.MainBirdIds);
            foreach (int id in festival.PartnerBirdIds)
                ids.Add(id);
            foreach (var bird in Birds)
                if (ids.Contains(bird.Id))
                    card.BirdNames.Add(bird.Name);
            Postcards.Add(card);
            string name = FestivalName(festival.Kind);
            festival = new TownFestivalState();
            NextFestivalDay = Day + 3;
            Changed(name + "の絵はがきが届きました！");
        }

        TownFestivalState CopyFestival()
        {
            return new TownFestivalState{Kind = festival.Kind, Active = festival.Active, HumanPurchase = festival.HumanPurchase, MainBirdIds = new List<int>(festival.MainBirdIds), PartnerBirdIds = new List<int>(festival.PartnerBirdIds)};
        }

        static List<TownPostcard> CopyPostcards(List<TownPostcard> source)
        {
            var copy = new List<TownPostcard>();
            if (source != null)
                foreach (var card in source)
                    copy.Add(new TownPostcard{Kind = card.Kind, Day = card.Day, BirdNames = new List<string>(card.BirdNames)});
            return copy;
        }

        bool ValidFestivalSave(TownSave save)
        {
            if (save.NextFestivalDay < 0 || !Enum.IsDefined(typeof(FestivalKind), save.SelectedFestival))
                return false;
            if (save.Postcards != null)
                foreach (var card in save.Postcards)
                {
                    if (card == null || !Enum.IsDefined(typeof(FestivalKind), card.Kind) || card.Day < 1 || card.BirdNames == null || card.BirdNames.Count < 1 || card.BirdNames.Count > 15)
                        return false;
                    foreach (var name in card.BirdNames)
                        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                            return false;
                }

            var state = save.Festival;
            if (state == null)
                return true;
            if (!Enum.IsDefined(typeof(FestivalKind), state.Kind) || state.MainBirdIds == null || state.PartnerBirdIds == null)
                return false;
            if (!state.Active)
                return true;
            if (save.NextFestivalDay < 1)
                return false;
            var birdIds = new HashSet<int>();
            foreach (var bird in save.Birds)
                birdIds.Add(bird.Id);
            foreach (int id in state.MainBirdIds)
                if (!birdIds.Contains(id))
                    return false;
            foreach (int id in state.PartnerBirdIds)
                if (!birdIds.Contains(id))
                    return false;
            return true;
        }

        void RestoreFestival(TownSave save)
        {
            Postcards.Clear();
            Postcards.AddRange(CopyPostcards(save.Postcards));
            festival = save.Festival == null ? new TownFestivalState() : new TownFestivalState{Kind = save.Festival.Kind, Active = save.Festival.Active, HumanPurchase = save.Festival.HumanPurchase, MainBirdIds = new List<int>(save.Festival.MainBirdIds), PartnerBirdIds = new List<int>(save.Festival.PartnerBirdIds)};
            SelectedFestival = festival.Active ? festival.Kind : save.SelectedFestival;
            NextFestivalDay = save.NextFestivalDay > 0 ? save.NextFestivalDay : Math.Max(3, ((Day + 2) / 3) * 3);
        }
    }
}
