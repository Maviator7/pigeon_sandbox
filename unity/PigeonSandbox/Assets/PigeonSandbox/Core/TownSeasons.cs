using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public enum TownSeason
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    [Serializable]
    public class SeasonalFestivalState
    {
        public TownSeason Season;
        public int Year = 1, BirdId = -1;
        public bool HumanPurchase, Completed;
    }

    [Serializable]
    public class SeasonalPostcard
    {
        public TownSeason Season;
        public int Year, Day;
        public List<string> BirdNames = new List<string>();
    }

    public partial class TownSimulation
    {
        public const int DaysPerSeason = 15;
        public const int DaysPerYear = DaysPerSeason * 4;
        public TownSeason Season => (TownSeason)(((Day - 1) / DaysPerSeason) % 4);
        public int SeasonDay => (Day - 1) % DaysPerSeason + 1;
        public int SeasonDaysRemaining => DaysPerSeason - SeasonDay;
        public int Year => (Day - 1) / DaysPerYear + 1;
        public string SeasonName => new[]{"春", "夏", "秋", "冬"}[(int)Season];
        public readonly List<SeasonalPostcard> SeasonalPostcards = new List<SeasonalPostcard>();
        SeasonalFestivalState seasonalFestival = new SeasonalFestivalState();
        public bool SeasonalBirdObserved => seasonalFestival.BirdId >= 0;
        public bool SeasonalHumanPurchase => seasonalFestival.HumanPurchase;
        public bool SeasonalEventCompleted => seasonalFestival.Completed;
        public static string SeasonalFestivalName(TownSeason season) => new[]{"花見", "水辺の集い", "秋の市", "冬の観察会"}[(int)season];
        public static string SeasonalVenueHint(TownSeason season) => new[]{"花壇の公園と街路樹を2マス以内に", "噴水と花壇の公園を2マス以内に", "パン屋と広場を2マス以内に", "街路樹と広場を2マス以内に"}[(int)season];
        FacilityKind SeasonalMainKind => Season == TownSeason.Spring ? FacilityKind.Park : Season == TownSeason.Summer ? FacilityKind.Fountain : Season == TownSeason.Autumn ? FacilityKind.Bakery : FacilityKind.Tree;
        FacilityKind SeasonalNeighborKind => Season == TownSeason.Spring ? FacilityKind.Tree : Season == TownSeason.Summer ? FacilityKind.Park : FacilityKind.Plaza;
        readonly HashSet<int> seasonalVenues = new HashSet<int>();
        int seasonalVenueRevision = -1;
        TownSeason cachedVenueSeason;
        void RefreshSeasonalVenues()
        {
            if (seasonalVenueRevision == Revision && cachedVenueSeason == Season)
                return;
            seasonalVenues.Clear();
            foreach (var facility in Facilities)
                if (facility.Kind == SeasonalMainKind && NearKind(facility, SeasonalNeighborKind))
                    seasonalVenues.Add(facility.Id);
            seasonalVenueRevision = Revision;
            cachedVenueSeason = Season;
        }

        public bool SeasonalVenueReady
        {
            get
            {
                RefreshSeasonalVenues();
                return seasonalVenues.Count > 0;
            }
        }

        void RefreshSeasonalFestival()
        {
            if (seasonalFestival.Year != Year || seasonalFestival.Season != Season)
                seasonalFestival = new SeasonalFestivalState{Year = Year, Season = Season};
        }

        void WitnessSeasonalPurchase(Facility shop)
        {
            RefreshSeasonalVenues();
            if (Season == TownSeason.Autumn && !seasonalFestival.Completed && seasonalVenues.Contains(shop.Id))
                seasonalFestival.HumanPurchase = true;
        }

        void TickSeasonalFestival()
        {
            if (seasonalFestival.Completed || !SeasonalVenueReady)
                return;
            foreach (var bird in Birds)
            {
                var target = Find(bird.TargetId);
                if (target == null || !seasonalVenues.Contains(target.Id) || Distance(bird.X - target.X * CellSize, bird.Z - target.Z * CellSize) > 2.1f)
                    continue;
                bool observed = Season == TownSeason.Spring ? bird.Activity == BirdActivity.Preen || bird.Activity == BirdActivity.Sunbathe : Season == TownSeason.Summer ? bird.Activity == BirdActivity.Bathe : Season == TownSeason.Autumn ? bird.Activity == BirdActivity.Eat : bird.Activity == BirdActivity.Rest;
                if (observed)
                {
                    seasonalFestival.BirdId = bird.Id;
                    break;
                }
            }

            if (seasonalFestival.BirdId < 0 || (Season == TownSeason.Autumn && !seasonalFestival.HumanPurchase))
                return;
            var participant = Birds.Find(b => b.Id == seasonalFestival.BirdId);
            if (participant == null)
                return;
            seasonalFestival.Completed = true;
            var card = new SeasonalPostcard{Season = Season, Year = Year, Day = Day};
            card.BirdNames.Add(participant.Name);
            SeasonalPostcards.Add(card);
            Notice = SeasonalFestivalName(Season) + "の絵はがきが届きました！";
        }

        static SeasonalFestivalState CopySeasonalFestival(SeasonalFestivalState state) => new SeasonalFestivalState{Season = state.Season, Year = state.Year, BirdId = state.BirdId, HumanPurchase = state.HumanPurchase, Completed = state.Completed};
        static List<SeasonalPostcard> CopySeasonalPostcards(List<SeasonalPostcard> cards)
        {
            var copy = new List<SeasonalPostcard>();
            if (cards != null)
                foreach (var card in cards)
                    copy.Add(new SeasonalPostcard{Season = card.Season, Year = card.Year, Day = card.Day, BirdNames = new List<string>(card.BirdNames)});
            return copy;
        }

        bool ValidSeasonSave(TownSave save)
        {
            if (save.SaveVersion < 2)
                return true;
            if (save.SeasonalFestival == null || save.SeasonalPostcards == null)
                return false;
            var progress = save.SeasonalFestival;
            if (!Enum.IsDefined(typeof(TownSeason), progress.Season) || progress.Year < 1 || progress.BirdId < -1)
                return false;
            if (progress.BirdId >= 0 && !save.Birds.Exists(b => b.Id == progress.BirdId))
                return false;
            foreach (var card in save.SeasonalPostcards)
            {
                if (card == null || !Enum.IsDefined(typeof(TownSeason), card.Season) || card.Year < 1 || card.Day < 1 || card.BirdNames == null || card.BirdNames.Count < 1 || card.BirdNames.Count > 15)
                    return false;
                foreach (var name in card.BirdNames)
                    if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                        return false;
            }

            return true;
        }

        void RestoreSeasonalFestival(TownSave save)
        {
            SeasonalPostcards.Clear();
            SeasonalPostcards.AddRange(CopySeasonalPostcards(save.SeasonalPostcards));
            seasonalFestival = save.SaveVersion < 2 || save.SeasonalFestival == null ? new SeasonalFestivalState{Year = Year, Season = Season} : CopySeasonalFestival(save.SeasonalFestival);
            RefreshSeasonalFestival();
        }

        float SeasonPreference(FacilityKind kind)
        {
            switch (Season)
            {
                case TownSeason.Spring:
                    return kind == FacilityKind.Park ? 5 : kind == FacilityKind.Tree ? 3 : 0;
                case TownSeason.Summer:
                    return kind == FacilityKind.Fountain ? 6 : kind == FacilityKind.Park ? 2 : 0;
                case TownSeason.Autumn:
                    return kind == FacilityKind.Bakery || kind == FacilityKind.Plaza ? 5 : 0;
                default:
                    return kind == FacilityKind.Tree ? 8 : kind == FacilityKind.Housing ? 5 : kind == FacilityKind.Park ? 2 : 0;
            }
        }
    }
}
