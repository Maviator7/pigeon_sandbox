using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public enum FacilityKind
    {
        Bakery,
        Fountain,
        Housing,
        Tree,
        Plaza,
        ClockTower,
        Cafe,
        Park
    }

    public enum TownTimeOfDay
    {
        Morning,
        Noon,
        Evening
    }

    public enum Plumage
    {
        Blue,
        Checker,
        Brown,
        Pied,
        White
    }

    public enum Personality
    {
        Foodie,
        Bather,
        Showoff,
        Shy
    }

    public enum TownPolicy
    {
        NestBoxes,
        BathPriority,
        CafeSupport
    }

    [Serializable]
    public class Facility
    {
        public int Id, X, Z, Level = 1;
        public FacilityKind Kind;
    }

    [Serializable]
    public class TownBird
    {
        public int Id;
        public string Name;
        public Personality Personality;
        public bool Rare, Mayor;
        public Plumage Plumage;
        // Heading and CheerTurn are both degrees. The cheerful turn is visual only.
        public float X, Y, Z, Heading;
        [NonSerialized]
        public float CheerTurn;
        // True only while settled on a tree or clock tower; airborne travel stays false.
        [NonSerialized]
        public bool Perched;
        internal float CheerRemaining;
        [NonSerialized]
        internal bool WishThanksPending;
        [NonSerialized]
        public int CompanionId = -1;
        [NonSerialized]
        public SocialActivity Social;
        internal float SocialTime, SocialCooldown;
        internal int SocialPlaceId = -1, SocialPlaceX, SocialPlaceZ;
        // Behaviour state; compare this, never the display text.
        public BirdActivity Activity = BirdActivity.Stroll;
        public string Action => TownSimulation.ActivityName(Activity);
        public int TargetId = -1;
        internal float Wait, Decision;
    }

    public enum BirdActivity
    {
        Stroll,
        Rest,
        Bathe,
        Eat,
        Preen,
        Sunbathe,
        Watch,
        TerraceRest,
        MeetPeople,
        WalkTogether,
        ReunionSpin,
        HappySpin,
        ThanksSpin
    }

    public class Visitor
    {
        public int Id;
        public float X, Z, Heading;
        public string Action = "お買い物へ";
        internal int TargetId = -1;
        internal bool Bought;
        internal float Wait;
    }

    public class TownRequest
    {
        public string Title, Description;
        public bool Complete;
        public int Reward;
    }

    [Serializable]
    public class TownSave
    {
        // 0 = saved before versioning. Bump CurrentSaveVersion when the format changes and migrate in Restore.
        public int SaveVersion;
        public List<Facility> Facilities = new List<Facility>();
        public List<TownBird> Birds = new List<TownBird>();
        public List<BirdFriendship> Friendships = new List<BirdFriendship>();
        public List<BirdWish> Wishes = new List<BirdWish>();
        public bool NestBoxes, BathPriority, CafeSupport;
        public float Money, Time;
        public int Purchases, ExpansionLevel;
        public bool[] CompletedRequests;
        public int NextFestivalDay;
        public FestivalKind SelectedFestival;
        public TownFestivalState Festival;
        public List<TownPostcard> Postcards;
        public SeasonalFestivalState SeasonalFestival;
        public List<SeasonalPostcard> SeasonalPostcards;
    }

    public partial class TownSimulation
    {
        readonly float[] plumageClocks = new float[5];
        public static Plumage FeatherOf(TownBird bird) => bird.Rare ? Plumage.White : bird.Plumage;
        public static string FeatherName(Plumage feather) => new[]{"青灰の鳩", "ごま模様の鳩", "茶色い鳩", "白黒まだらの鳩", "白い鳩"}[(int)feather];
        public bool Discovered(Plumage feather)
        {
            foreach (var bird in Birds)
                if (FeatherOf(bird) == feather)
                    return true;
            return false;
        }

        public bool HabitatReady(Plumage feather)
        {
            switch (feather)
            {
                case Plumage.Checker:
                    return FirstNear(FacilityKind.Plaza, FacilityKind.Bakery) != null;
                case Plumage.Brown:
                    return FirstNear(FacilityKind.Cafe, FacilityKind.Bakery) != null;
                case Plumage.Pied:
                    return FirstNear(FacilityKind.Park, FacilityKind.Tree) != null;
                case Plumage.White:
                    return QuietHabitat();
                default:
                    return true;
            }
        }

        public string FeatherHint(Plumage feather)
        {
            if (feather == Plumage.White)
                return RareHint;
            string hint = new[]{"最初から街に暮らす、青灰色の仲間。", "広場の2マス以内にパン屋を。", "カフェの2マス以内にパン屋を。", "花壇の公園の2マス以内に街路樹を。"}[(int)feather];
            if (feather == Plumage.Blue || Discovered(feather))
                return hint;
            return hint + (HabitatReady(feather) ? " 環境が整いました。45秒ほど待ってみよう。" : "");
        }

        void TickPlumage(float dt)
        {
            for (int i = 1; i <= 3; i++)
            {
                var feather = (Plumage)i;
                if (Discovered(feather))
                    continue;
                plumageClocks[i] = HabitatReady(feather) ? plumageClocks[i] + dt : 0;
                if (plumageClocks[i] < 45 || Birds.Count >= 15)
                    continue;
                AddBird(i == 1 ? Personality.Showoff : i == 2 ? Personality.Foodie : Personality.Shy, false, false);
                var bird = Birds[Birds.Count - 1];
                bird.Plumage = feather;
                bird.Name = new[]{"", "ごま", "シナモン", "オセロ"}[i];
                Changed(FeatherName(feather) + "『" + bird.Name + "』がやってきました！");
            }
        }

        public const float CellSize = 2.2f;
        public const float DayLength = 120;
        public float DayProgress => (Time % DayLength) / DayLength;
        public TownTimeOfDay TimeOfDay => (TownTimeOfDay)Math.Min(2, (int)(DayProgress * 3));
        public string TimeOfDayName => new[]{"朝", "昼", "夕方"}[(int)TimeOfDay];
        public int ExpansionLevel
        {
            get;
            private set;
        }

        public int MapRadius => 4 + ExpansionLevel;
        public int MapSize => MapRadius * 2 + 1;
        public float MapEdge => (MapRadius + .55f) * CellSize;
        public bool CanExpand => ExpansionLevel < 4;
        static readonly int[] ExpansionPrices = {500, 1000, 1800, 3000};
        public int ExpansionCost => CanExpand ? ExpansionPrices[ExpansionLevel] : 0;
        public bool ExpandTown()
        {
            if (!CanExpand || Money < ExpansionCost)
                return false;
            Money -= ExpansionCost;
            ExpansionLevel++;
            Changed("街を" + MapSize + "×" + MapSize + "マスに広げました！");
            return true;
        }

        public readonly List<Facility> Facilities = new List<Facility>();
        public readonly List<TownBird> Birds = new List<TownBird>();
        public readonly List<Visitor> Visitors = new List<Visitor>();
        public readonly List<TownRequest> Requests = new List<TownRequest>();
        public float Money = 700, FoodSupply, PigeonHappiness, HumanSatisfaction, Cleanliness, Crowding, Income, Upkeep, Time;
        public int Day = 1, Purchases, Revision;
        public string Notice = "ようこそ、鳩市長。広場のそばにパン屋を開こう。";
        public bool NestBoxes, BathPriority, CafeSupport;
        readonly Random random;
        int nextId = 1;
        float spawnClock, upkeepClock, growthClock, rareClock, metricClock;
        bool rareArrived;
        public TownSimulation(int seed = 42)
        {
            random = new Random(seed);
            AddFacility(FacilityKind.Plaza, 0, 0);
            AddFacility(FacilityKind.Tree, -2, 0);
            AddBird(Personality.Foodie, true, false);
            AddBird(Personality.Bather, false, false);
            AddBird(Personality.Shy, false, false);
            AddBird(Personality.Showoff, false, false);
            Requests.Add(new TownRequest{Title = "焼きたての広場", Description = "広場から2マス以内にパン屋を置く", Reward = 90});
            Requests.Add(new TownRequest{Title = "水辺の暮らし", Description = "パン屋から2マス以内に噴水を置き、集合住宅を建てる", Reward = 110});
            Requests.Add(new TownRequest{Title = "白いお客さま", Description = "静かな家の2マス以内に木と噴水。白い鳩を迎える", Reward = 150});
            EnsureWishes();
            Recalculate();
        }

        public const int MaxBirdNameLength = 24;
        // displayable: glyph check for the UI font. Characters it lacks (emoji, kanji outside the subset) are refused.
        public bool RenameBird(int id, string name, Func<char, bool> displayable = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            name = name.Trim();
            if (new System.Globalization.StringInfo(name).LengthInTextElements > MaxBirdNameLength)
                return false;
            foreach (char ch in name)
                if (char.IsControl(ch) || ch == '\u2028' || ch == '\u2029' || displayable != null && (char.IsSurrogate(ch) || !displayable(ch)))
                    return false;
            var bird = Birds.Find(b => b.Id == id);
            if (bird == null)
                return false;
            bird.Name = name;
            return true;
        }

        public TownSave Capture()
        {
            var save = new TownSave{SaveVersion = CurrentSaveVersion, Wishes = CopyWishes(Wishes), Friendships = CopyFriendships(friendships), Money = Money, Time = Time, Purchases = Purchases, ExpansionLevel = ExpansionLevel, NestBoxes = NestBoxes, BathPriority = BathPriority, CafeSupport = CafeSupport, CompletedRequests = new bool[Requests.Count], NextFestivalDay = NextFestivalDay, SelectedFestival = SelectedFestival, Festival = CopyFestival(), Postcards = CopyPostcards(Postcards), SeasonalFestival = CopySeasonalFestival(seasonalFestival), SeasonalPostcards = CopySeasonalPostcards(SeasonalPostcards)};
            foreach (var f in Facilities)
                save.Facilities.Add(new Facility{Id = f.Id, Kind = f.Kind, X = f.X, Z = f.Z, Level = f.Level});
            foreach (var b in Birds)
                save.Birds.Add(new TownBird{Id = b.Id, Name = b.Name, Personality = b.Personality, Plumage = FeatherOf(b), Rare = b.Rare, Mayor = b.Mayor, X = b.X, Y = b.Y, Z = b.Z, Heading = b.Heading, Activity = b.Activity, TargetId = b.TargetId});
            for (int i = 0; i < Requests.Count; i++)
                save.CompletedRequests[i] = Requests[i].Complete;
            return save;
        }

        public bool Restore(TownSave save)
        {
            if (save == null || save.SaveVersion > CurrentSaveVersion || save.Facilities == null || save.Birds == null || save.Birds.Count == 0 || save.Birds.Count > 15 || float.IsNaN(save.Money) || float.IsInfinity(save.Money) || float.IsNaN(save.Time) || float.IsInfinity(save.Time))
                return false;
            if (save.ExpansionLevel < 0 || save.ExpansionLevel > 4)
                return false;
            int radius = 4 + save.ExpansionLevel;
            var ids = new HashSet<int>();
            var cells = new HashSet<string>();
            foreach (var f in save.Facilities)
            {
                if (f == null || f.Id < 1 || !ids.Add(f.Id) || !cells.Add(f.X + ":" + f.Z) || Math.Abs((long)f.X) > radius || Math.Abs((long)f.Z) > radius || f.Level < 1 || f.Level > 3 || !Enum.IsDefined(typeof(FacilityKind), f.Kind))
                    return false;
            }

            int rares = 0;
            foreach (var b in save.Birds)
            {
                if (b == null || b.Id < 1 || !ids.Add(b.Id) || !Enum.IsDefined(typeof(Personality), b.Personality) || !Enum.IsDefined(typeof(Plumage), b.Plumage) || (!b.Rare && b.Plumage == Plumage.White))
                    return false;
                if (b.Rare)
                    rares++;
            }

            if (rares > 1 || !ValidFriendships(save) || !ValidWishes(save) || !ValidFestivalSave(save) || !ValidSeasonSave(save))
                return false;
            var restoredFriendships = CopyFriendships(save.Friendships);
            friendships.Clear();
            friendships.AddRange(restoredFriendships);
            Array.Clear(plumageClocks, 0, plumageClocks.Length);
            ExpansionLevel = save.ExpansionLevel;
            Facilities.Clear();
            Birds.Clear();
            Visitors.Clear();
            nextId = 1;
            foreach (var f in save.Facilities)
            {
                Facilities.Add(new Facility{Id = f.Id, Kind = f.Kind, X = f.X, Z = f.Z, Level = f.Level});
                nextId = Math.Max(nextId, f.Id + 1);
            }

            foreach (var b in save.Birds)
            {
                Birds.Add(new TownBird{Id = b.Id, Name = b.Name, Personality = b.Personality, Plumage = FeatherOf(b), Rare = b.Rare, Mayor = b.Mayor, X = FinitePosition(b.X), Z = FinitePosition(b.Z), Activity = BirdActivity.Stroll, TargetId = -1});
                nextId = Math.Max(nextId, b.Id + 1);
            }

            Wishes.Clear();
            Wishes.AddRange(CopyWishes(save.Wishes));
            EnsureWishes();
            Money = Math.Max(0, save.Money);
            Time = Math.Max(0, save.Time);
            Day = 1 + (int)(Time / DayLength);
            RestoreFestival(save);
            RestoreSeasonalFestival(save);
            Purchases = Math.Max(0, save.Purchases);
            NestBoxes = save.NestBoxes;
            BathPriority = save.BathPriority;
            CafeSupport = save.CafeSupport;
            rareArrived = rares > 0;
            rareClock = spawnClock = upkeepClock = growthClock = metricClock = socialClock = 0;
            for (int i = 0; i < Requests.Count; i++)
                Requests[i].Complete = save.CompletedRequests != null && i < save.CompletedRequests.Length && save.CompletedRequests[i];
            Changed("保存したまちを再開しました。");
            return true;
        }

        float FinitePosition(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Clamp(value, -MapEdge, MapEdge);
        }

        public static int Cost(FacilityKind k)
        {
            return new[]{140, 110, 120, 55, 80, 230, 180, 95}[(int)k];
        }

        public static string NameOf(FacilityKind k)
        {
            return new[]{"パン屋", "噴水", "集合住宅", "街路樹", "広場", "時計台", "オープンカフェ", "花壇の公園"}[(int)k];
        }

        public const int CurrentSaveVersion = 2;
        public static string ActivityName(BirdActivity activity) => new[]{"散歩", "休憩", "水浴び", "食事", "羽繕い", "日向ぼっこ", "眺める", "テラスで休憩", "人と交流", "仲間と散歩", "再会のクルクル", "ごきげんクルクル", "ありがとうのクルクル"}[(int)activity];
        public static string PersonalityName(Personality p)
        {
            return new[]{"食いしん坊", "水浴び好き", "目立ちたがり", "臆病"}[(int)p];
        }

        static float Clamp(float v, float lo = 0, float hi = 100)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

        static float Distance(float x, float z)
        {
            return (float)Math.Sqrt(x * x + z * z);
        }

        static bool Near(Facility a, Facility b, float radius = 2.1f)
        {
            return a.Id != b.Id && Distance(a.X - b.X, a.Z - b.Z) <= radius;
        }

        // Hot-path lookups below are plain loops: lambdas here allocated closures on every tick.
        bool NearKind(Facility a, FacilityKind kind)
        {
            foreach (var b in Facilities)
                if (b.Kind == kind && Near(a, b))
                    return true;
            return false;
        }

        Facility FirstNear(FacilityKind kind, FacilityKind neighbor)
        {
            foreach (var f in Facilities)
                if (f.Kind == kind && NearKind(f, neighbor))
                    return f;
            return null;
        }

        Facility FirstOfKind(FacilityKind kind, FacilityKind alternative)
        {
            foreach (var f in Facilities)
                if (f.Kind == kind || f.Kind == alternative)
                    return f;
            return null;
        }

        public TownBird BirdById(int id)
        {
            foreach (var bird in Birds)
                if (bird.Id == id)
                    return bird;
            return null;
        }

        bool VisitorNear(TownBird bird, float radius)
        {
            foreach (var v in Visitors)
                if (Distance(v.X - bird.X, v.Z - bird.Z) < radius)
                    return true;
            return false;
        }

        int Levels(FacilityKind kind)
        {
            int n = 0;
            foreach (var f in Facilities)
                if (f.Kind == kind)
                    n += f.Level;
            return n;
        }

        // Spreads visitors over every bakery and cafe in list order, without building a temporary list.
        Facility ShopFor(int visitorId)
        {
            int shops = 0;
            foreach (var f in Facilities)
                if (f.Kind == FacilityKind.Bakery || f.Kind == FacilityKind.Cafe)
                    shops++;
            if (shops == 0)
                return null;
            int pick = visitorId % shops;
            foreach (var f in Facilities)
                if ((f.Kind == FacilityKind.Bakery || f.Kind == FacilityKind.Cafe) && pick-- == 0)
                    return f;
            return null;
        }

        Facility Find(int id)
        {
            foreach (var f in Facilities)
                if (f.Id == id)
                    return f;
            return null;
        }

        public Facility At(int x, int z)
        {
            foreach (var f in Facilities)
                if (f.X == x && f.Z == z)
                    return f;
            return null;
        }

        public bool CanPlace(int x, int z, int ignoreId = -1)
        {
            if (x < -MapRadius || x > MapRadius || z < -MapRadius || z > MapRadius)
                return false;
            foreach (var f in Facilities)
                if (f.Id != ignoreId && f.X == x && f.Z == z)
                    return false;
            return true;
        }

        void AddFacility(FacilityKind kind, int x, int z)
        {
            Facilities.Add(new Facility{Id = nextId++, Kind = kind, X = x, Z = z});
        }

        public bool Build(FacilityKind kind, int x, int z)
        {
            if (!Enum.IsDefined(typeof(FacilityKind), kind) || !CanPlace(x, z) || Money < Cost(kind))
            {
                Notice = "空き地と建設費を確認しよう。";
                return false;
            }

            Money -= Cost(kind);
            AddFacility(kind, x, z);
            Changed(NameOf(kind) + "がオープン！");
            return true;
        }

        public bool Move(int id, int x, int z)
        {
            var f = Find(id);
            if (f == null || !CanPlace(x, z, id))
                return false;
            f.X = x;
            f.Z = z;
            foreach (var b in Birds)
                if (b.TargetId == id)
                {
                    b.Wait = 0;
                    b.Activity = BirdActivity.Stroll;
                }

            Changed("配置を変更しました。移設費は無料です。");
            return true;
        }

        public bool Upgrade(int id)
        {
            var f = Find(id);
            if (f == null || f.Level >= 3)
                return false;
            int cost = Cost(f.Kind) * f.Level / 2;
            if (Money < cost)
                return false;
            Money -= cost;
            f.Level++;
            Changed(NameOf(f.Kind) + "がレベル" + f.Level + "になりました。");
            return true;
        }

        public bool Remove(int id)
        {
            var f = Find(id);
            if (f == null)
                return false;
            float invested = Cost(f.Kind) * (1 + (f.Level - 1) * f.Level * .25f);
            Money += invested * .7f;
            Facilities.Remove(f);
            foreach (var b in Birds)
                if (b.TargetId == id)
                {
                    b.TargetId = -1;
                    b.Wait = 0;
                    b.Decision = 0;
                }

            Changed("撤去費用の70%が戻りました。");
            return true;
        }

        public bool TogglePolicy(TownPolicy p)
        {
            switch (p)
            {
                case TownPolicy.NestBoxes:
                    NestBoxes = !NestBoxes;
                    break;
                case TownPolicy.BathPriority:
                    BathPriority = !BathPriority;
                    break;
                case TownPolicy.CafeSupport:
                    CafeSupport = !CafeSupport;
                    break;
                default:
                    return false;
            }

            Changed("まちの方針を更新しました。");
            return true;
        }

        void Changed(string notice)
        {
            Notice = notice;
            Revision++;
            Recalculate();
        }

        bool QuietHabitat()
        {
            foreach (var h in Facilities)
                if (h.Kind == FacilityKind.Housing && NearKind(h, FacilityKind.Tree) && NearKind(h, FacilityKind.Fountain) && !NearKind(h, FacilityKind.Bakery) && !NearKind(h, FacilityKind.ClockTower))
                    return true;
            return false;
        }

        public string RareHint
        {
            get
            {
                return rareArrived ? "白い鳩が、このまちを気に入りました。" : QuietHabitat() ? "静かな水辺で白い羽の気配…あと" + Math.Max(0, (int)(45 - rareClock)) + "秒ほど。" : "パン屋・時計台から離れた集合住宅に、2マス以内の木と噴水を。";
            }
        }

        public void Recalculate()
        {
            int bakery = Levels(FacilityKind.Bakery), water = Levels(FacilityKind.Fountain), home = Levels(FacilityKind.Housing), tree = Levels(FacilityKind.Tree), plaza = Levels(FacilityKind.Plaza), clock = Levels(FacilityKind.ClockTower);
            int cafe = Levels(FacilityKind.Cafe), park = Levels(FacilityKind.Park);
            int market = 0, baths = 0, terraces = 0, gardens = 0;
            foreach (var f in Facilities)
            {
                if (f.Kind == FacilityKind.Bakery && NearKind(f, FacilityKind.Plaza))
                    market += f.Level;
                if (f.Kind == FacilityKind.Cafe && NearKind(f, FacilityKind.Bakery))
                    terraces += f.Level;
                if (f.Kind == FacilityKind.Park && NearKind(f, FacilityKind.Housing))
                    gardens += f.Level;
                if (f.Kind == FacilityKind.Fountain && NearKind(f, FacilityKind.Bakery))
                    baths += f.Level;
            }

            FoodSupply = Clamp(28 + bakery * 18 + cafe * 10 + terraces * 4 + (CafeSupport ? 15 : 0) - Birds.Count * 2);
            Crowding = Clamp(18 + bakery * 10 + cafe * 6 + clock * 8 - park * 7 - gardens * 3 + Birds.Count * 2 - plaza * 9 - tree * 3 - market * 5 + (CafeSupport ? 8 : 0));
            Cleanliness = Clamp(88 + tree * 5 + water * 4 + park * 4 - cafe * 3 - Birds.Count * 3 - bakery * 4 + (BathPriority ? -8 : 0));
            PigeonHappiness = Clamp(30 + FoodSupply * .28f + park * 5 + gardens * 3 + cafe * 2 + water * 5 + home * 4 + tree * 3 + baths * 5 - Crowding * .16f + (NestBoxes ? 8 : 0) + (BathPriority ? 10 : 0));
            HumanSatisfaction = Clamp(45 + market * 8 + cafe * 4 + park * 5 + terraces * 4 + gardens * 3 + clock * 5 + plaza * 4 + Cleanliness * .2f - Crowding * .32f + (BathPriority ? -7 : 0) + (CafeSupport ? 5 : 0));
            Upkeep = cafe * 3 + park + bakery * 3 + water * 2 + home + tree * .5f + clock * 4 + (NestBoxes ? 3 : 0) + (BathPriority ? 3 : 0) + (CafeSupport ? 4 : 0);
            Reward(0, market > 0);
            Reward(1, baths > 0 && home > 0);
            Reward(2, rareArrived);
        }

        void Reward(int index, bool complete)
        {
            var r = Requests[index];
            if (!complete || r.Complete)
                return;
            r.Complete = true;
            Money += r.Reward;
            Notice = "お願い達成：" + r.Title + " +" + r.Reward;
        }

        void AddBird(Personality p, bool mayor, bool rare)
        {
            int n = Birds.Count;
            Birds.Add(new TownBird{Id = nextId++, Name = rare ? "しらたま" : new[]{"ぽっぽ市長", "しずく", "こむぎ", "きらり", "まめ", "つばさ", "くるみ", "すず", "もち", "あお", "ふわ", "ひなた"}[n % 12], Personality = p, Mayor = mayor, Rare = rare, X = (float)random.NextDouble() * 2 - 1, Z = (float)random.NextDouble() * 2 - 1});
        }

        Facility Select(TownBird b)
        {
            Facility best = null;
            float score = -999;
            foreach (var f in Facilities)
            {
                float s = (float)random.NextDouble() * 3 + WishPreference(b, f) + SeasonPreference(f.Kind);
                // Routine is a preference, not an order: personality and ongoing activities remain intact.
                if (TimeOfDay == TownTimeOfDay.Morning && (f.Kind == FacilityKind.Bakery || f.Kind == FacilityKind.Cafe))
                    s += 3;
                if (TimeOfDay == TownTimeOfDay.Noon && (f.Kind == FacilityKind.Fountain || f.Kind == FacilityKind.Park))
                    s += 3;
                if (TimeOfDay == TownTimeOfDay.Evening && (f.Kind == FacilityKind.Park || f.Kind == FacilityKind.Tree || f.Kind == FacilityKind.Housing))
                    s += 6;
                if (f.Kind == FacilityKind.Bakery && NearKind(f, FacilityKind.Plaza))
                    s += 3;
                if (f.Kind == FacilityKind.Fountain && NearKind(f, FacilityKind.Bakery))
                    s += 3;
                if (f.Kind == FacilityKind.Cafe && NearKind(f, FacilityKind.Bakery))
                    s += 3;
                if (f.Kind == FacilityKind.Park && NearKind(f, FacilityKind.Housing))
                    s += 3;
                switch (b.Personality)
                {
                    case Personality.Foodie:
                        s += f.Kind == FacilityKind.Bakery ? 11 : f.Kind == FacilityKind.Cafe ? 10 : f.Kind == FacilityKind.Plaza ? 3 : 0;
                        break;
                    case Personality.Bather:
                        s += f.Kind == FacilityKind.Fountain ? 12 + (BathPriority ? 4 : 0) : f.Kind == FacilityKind.Park ? 4 : 0;
                        break;
                    case Personality.Showoff:
                        s += f.Kind == FacilityKind.ClockTower ? 13 : f.Kind == FacilityKind.Plaza ? 9 : f.Kind == FacilityKind.Cafe ? 8 : 0;
                        break;
                    case Personality.Shy:
                        s += f.Kind == FacilityKind.Park ? 11 : f.Kind == FacilityKind.Tree ? 10 : f.Kind == FacilityKind.Housing ? 9 : 0;
                        if (NearKind(f, FacilityKind.Bakery) || NearKind(f, FacilityKind.Cafe))
                            s -= 5;
                        break;
                }

                if (f.Id == b.TargetId)
                    s -= 7;
                if (s > score)
                {
                    score = s;
                    best = f;
                }
            }

            return best;
        }

        static void Approach(Facility f, int id, out float x, out float z)
        {
            float angle = (id % 4) * (float)Math.PI * .5f;
            x = f.X * CellSize + (float)Math.Cos(angle) * .96f;
            z = f.Z * CellSize + (float)Math.Sin(angle) * .96f;
        }

        // Perches sit on top of the tree canopy and the clock tower cap, matching TownWorld's models.
        static bool Perch(Facility f, int id, out float x, out float y, out float z)
        {
            float angle = (id % 4) * (float)Math.PI * .5f;
            x = f.X * CellSize + (float)Math.Cos(angle) * .3f;
            z = f.Z * CellSize + (float)Math.Sin(angle) * .3f;
            y = f.Kind == FacilityKind.Tree ? 2.45f : f.Kind == FacilityKind.ClockTower ? 2.98f : 0;
            return y > 0;
        }

        bool Walk(ref float x, ref float z, ref float heading, float tx, float tz, float speed, float dt, bool avoid = true)
        {
            float dx = tx - x, dz = tz - z, d = Distance(dx, dz);
            if (d < .08f)
                return true;
            heading = (float)(Math.Atan2(dx, dz) * 180 / Math.PI);
            float step = Math.Min(d, speed * dt);
            float nx = x + dx / d * step, nz = z + dz / d * step;
            if (avoid)
                foreach (var f in Facilities)
                {
                    float ox = nx - f.X * CellSize, oz = nz - f.Z * CellSize;
                    float dist = Distance(ox, oz);
                    if (dist < .78f)
                    {
                        if (dist < .001f)
                        {
                            ox = 1;
                            oz = 0;
                            dist = 1;
                        }

                        // Advance around the obstacle instead of becoming pinned to its rim.
                        float angle = (float)Math.Atan2(oz, ox);
                        float cross = ox * (tz - f.Z * CellSize) - oz * (tx - f.X * CellSize);
                        angle += (cross < 0 ? -1 : 1) * step / .8f;
                        nx = f.X * CellSize + (float)Math.Cos(angle) * .8f;
                        nz = f.Z * CellSize + (float)Math.Sin(angle) * .8f;
                    }
                }

            x = nx;
            z = nz;
            return d <= step + .08f;
        }

        public void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0)
                return;
            dt = Math.Min(dt, .25f);
            Time += dt;
            Day = 1 + (int)(Time / DayLength);
            RefreshSeasonalFestival();
            spawnClock += dt;
            upkeepClock += dt;
            growthClock += dt;
            metricClock += dt;
            if (metricClock >= 1)
            {
                metricClock = 0;
                Recalculate();
            }

            if (upkeepClock >= 20)
            {
                upkeepClock -= 20;
                float tax = 12 + Levels(FacilityKind.Housing) * 5;
                Money = Math.Max(0, Money + tax - Upkeep);
                Income = tax;
                if (Money < 55)
                {
                    Money += 12;
                    Income += 12;
                }

                Notice = "まちの会計：収入 " + tax.ToString("0") + " / 維持費 " + Upkeep.ToString("0.0");
            }

            if (spawnClock >= Math.Max(2, 6 - Levels(FacilityKind.ClockTower) * .5f - (CafeSupport ? 1 : 0)) && Visitors.Count < 18)
            {
                spawnClock = 0;
                Visitors.Add(new Visitor{Id = nextId++, X = -MapEdge, Z = ((float)random.NextDouble() * 2 - 1) * MapRadius * CellSize});
            }

            TickCompany(dt);
            foreach (var b in Birds)
            {
                if (b.Social != SocialActivity.None)
                    continue;
                if (b.CheerRemaining > 0)
                {
                    b.CheerRemaining = Math.Max(0, b.CheerRemaining - dt);
                    float progress = 1 - b.CheerRemaining / 1.4f;
                    b.CheerTurn = 360 * progress * progress * (3 - 2 * progress);
                    if (b.CheerRemaining == 0)
                    {
                        b.CheerTurn = 0;
                        b.Activity = BirdActivity.Rest;
                        b.Wait = .6f;
                    }

                    continue;
                }

                b.Decision -= dt;
                b.Wait -= dt;
                if (b.WishThanksPending && b.Y < .15f && b.Wait <= 0)
                {
                    b.WishThanksPending = false;
                    b.CheerRemaining = 1.4f;
                    b.CheerTurn = 0;
                    b.Activity = BirdActivity.ThanksSpin;
                    continue;
                }

                var target = Find(b.TargetId);
                // A little celebration after a pleasant meal or bath, never during travel or flight.
                if (target != null && b.Wait <= 0 && b.Y < .15f && PigeonHappiness >= 65 && (b.Activity == BirdActivity.Eat || b.Activity == BirdActivity.Bathe) && random.NextDouble() < .35)
                {
                    b.CheerRemaining = 1.4f;
                    b.CheerTurn = 0;
                    b.Activity = BirdActivity.HappySpin;
                    continue;
                }

                if (target == null || b.Decision <= 0)
                {
                    target = Select(b);
                    b.TargetId = target == null ? -1 : target.Id;
                    b.Decision = 9 + (float)random.NextDouble() * 9;
                    b.Wait = 0;
                }

                if (target == null)
                {
                    b.Perched = false;
                    b.Y *= Math.Max(0, 1 - dt * 4);
                    b.Activity = BirdActivity.Stroll;
                    continue;
                }

                float tx, tz;
                Approach(target, b.Id, out tx, out tz);
                if (b.Wait > 0)
                {
                    if (target.Kind == FacilityKind.Cafe)
                        b.Activity = VisitorNear(b, 2.2f) ? BirdActivity.MeetPeople : BirdActivity.TerraceRest;
                    float px, py, pz;
                    if (Perch(target, b.Id, out px, out py, out pz))
                    {
                        float blend = Math.Min(1, dt * 3);
                        b.X += (px - b.X) * blend;
                        b.Y += (py - b.Y) * blend;
                        b.Z += (pz - b.Z) * blend;
                        if (Distance(px - b.X, pz - b.Z) + Math.Abs(py - b.Y) < .05f)
                        {
                            b.X = px;
                            b.Y = py;
                            b.Z = pz;
                            b.Perched = true;
                        }
                    }
                    else
                        b.Y *= Math.Max(0, 1 - dt * 4);
                    continue;
                }

                b.Perched = false;
                // Birds still in the air glide over buildings; obstacle avoidance applies on the ground.
                bool airborne = b.Y > .15f;
                b.Y *= Math.Max(0, 1 - dt * 4);
                b.Activity = BirdActivity.Stroll;
                if (Walk(ref b.X, ref b.Z, ref b.Heading, tx, tz, .7f + (b.Mayor ? .12f : 0), dt, !airborne))
                {
                    b.Activity = target.Kind == FacilityKind.Fountain ? BirdActivity.Bathe : target.Kind == FacilityKind.Bakery ? BirdActivity.Eat : target.Kind == FacilityKind.ClockTower ? BirdActivity.Watch : target.Kind == FacilityKind.Cafe ? (VisitorNear(b, 2.2f) ? BirdActivity.MeetPeople : BirdActivity.TerraceRest) : target.Kind == FacilityKind.Park ? (TimeOfDay == TownTimeOfDay.Evening ? BirdActivity.Rest : b.Id % 2 == 0 ? BirdActivity.Preen : BirdActivity.Sunbathe) : BirdActivity.Rest;
                    b.Wait = (target.Kind == FacilityKind.Park ? 5 : 2.5f) + (float)random.NextDouble() * 3;
                }
            }

            for (int i = Visitors.Count - 1; i >= 0; i--)
            {
                var v = Visitors[i];
                v.Wait -= dt;
                if (v.Wait > 0)
                    continue;
                var target = Find(v.TargetId);
                if (!v.Bought && target == null)
                {
                    target = v.Id % 4 == 0 ? FirstOfKind(FacilityKind.Park, FacilityKind.Park) : null;
                    if (target == null)
                        target = ShopFor(v.Id);
                    if (target == null)
                        target = FirstOfKind(FacilityKind.Park, FacilityKind.Plaza);
                    v.TargetId = target == null ? -1 : target.Id;
                }

                float tx = MapEdge, tz = v.Id % 5 - 2;
                if (!v.Bought && target != null)
                    Approach(target, v.Id, out tx, out tz);
                if (Walk(ref v.X, ref v.Z, ref v.Heading, tx, tz, 1.5f, dt))
                {
                    if (v.Bought || target == null)
                    {
                        Visitors.RemoveAt(i);
                        continue;
                    }

                    if (target.Kind == FacilityKind.Bakery || target.Kind == FacilityKind.Cafe)
                    {
                        float purchase = 8 + target.Level * 2 + (NearKind(target, FacilityKind.Plaza) ? 4 : 0) + (CafeSupport ? 3 : 0);
                        if (target.Kind == FacilityKind.Cafe)
                            purchase = 10 + target.Level * 2 + (NearKind(target, FacilityKind.Bakery) ? 5 : 0) + (CafeSupport ? 3 : 0);
                        Money += purchase;
                        Income += purchase;
                        Purchases++;
                        v.Action = target.Kind == FacilityKind.Cafe ? "カフェでひと休み" : "パンを購入！";
                        WitnessFestivalPurchase(target);
                        WitnessSeasonalPurchase(target);
                    }
                    else
                        v.Action = "公園でひと休み";
                    v.Bought = true;
                    v.Wait = target.Kind == FacilityKind.Cafe || target.Kind == FacilityKind.Park ? 5 : 2;
                }
                else if (v.Bought)
                    v.Action = "帰り道";
            }

            TickWishes(dt);
            TickFestival();
            TickSeasonalFestival();
            int capacity = 4 + Levels(FacilityKind.Housing) * 2 + (NestBoxes ? 2 : 0);
            if (growthClock >= 32)
            {
                growthClock = 0;
                if (Birds.FindAll(b => b.Plumage == Plumage.Blue && !b.Rare).Count < Math.Min(11, capacity) && Birds.Count < 15 - (rareArrived ? 0 : 1) - (Discovered(Plumage.Checker) ? 0 : 1) - (Discovered(Plumage.Brown) ? 0 : 1) - (Discovered(Plumage.Pied) ? 0 : 1) && FoodSupply >= 35)
                {
                    AddBird((Personality)(Birds.Count % 4), false, false);
                    Notice = "新しい鳩が引っ越してきました！";
                }
            }

            TickPlumage(dt);
            if (!rareArrived)
            {
                rareClock = QuietHabitat() ? rareClock + dt : 0;
                if (rareClock >= 45)
                {
                    if (Birds.Count < 15)
                        AddBird(Personality.Shy, false, true);
                    else
                        return;
                    rareArrived = true;
                    Notice = "白い鳩『しらたま』がやってきました！";
                    Recalculate();
                }
            }
        }
    }
}
