using System;
using System.Collections.Generic;
namespace PigeonSandbox
{
    public enum BirdWishKind { FriendlyLunch, HomeBath, QuietShade }
    [Serializable] public class BirdWish
    {
        public BirdWishKind Kind;
        public int BirdId;
        public bool Complete;
        [NonSerialized] internal float VisitTime;
    }
    public partial class TownSimulation
    {
        public readonly List<BirdWish> Wishes=new List<BirdWish>();
        public TownBird WishOwner(BirdWish wish)=>Birds.Find(b=>b.Id==wish.BirdId);
        public static string WishTitle(BirdWishKind kind)=>new[]{"仲良しとパン屋のそばで","おうちの近くで水浴び","静かな木陰でひと休み"}[(int)kind];
        public static string WishDescription(BirdWishKind kind)=>new[]{
            "パン屋の2マス以内の広場で、仲良しと一緒に休みたいな。",
            "集合住宅の2マス以内にある噴水で、水浴びしたいな。",
            "木の2マス以内の花壇の公園で休みたいな。パン屋・カフェ・時計台からは離してね。"}[(int)kind];
        void EnsureWishes()
        {
            for(int i=0;i<3;i++)
            {
                var kind=(BirdWishKind)i;if(Wishes.Exists(w=>w.Kind==kind))continue;
                var personality=i==0?Personality.Foodie:i==1?Personality.Bather:Personality.Shy;
                var bird=Birds.Find(b=>b.Personality==personality);
                if(bird!=null)Wishes.Add(new BirdWish{Kind=kind,BirdId=bird.Id});
            }
        }
        bool WishPlace(BirdWishKind kind,Facility f)
        {
            if(f==null)return false;
            switch(kind)
            {
                case BirdWishKind.FriendlyLunch:return f.Kind==FacilityKind.Plaza&&NearKind(f,FacilityKind.Bakery);
                case BirdWishKind.HomeBath:return f.Kind==FacilityKind.Fountain&&NearKind(f,FacilityKind.Housing);
                default:return f.Kind==FacilityKind.Park&&NearKind(f,FacilityKind.Tree)&&!NearKind(f,FacilityKind.Bakery)&&!NearKind(f,FacilityKind.Cafe)&&!NearKind(f,FacilityKind.ClockTower);
            }
        }
        public bool WishHabitatReady(BirdWish wish)=>Facilities.Exists(f=>WishPlace(wish.Kind,f));
        float WishPreference(TownBird bird,Facility place)=>Wishes.Exists(w=>!w.Complete&&w.BirdId==bird.Id&&WishPlace(w.Kind,place))?5:0;
        void TickWishes(float dt)
        {
            EnsureWishes();
            foreach(var wish in Wishes)
            {
                if(wish.Complete)continue;
                var bird=WishOwner(wish);var place=bird==null?null:Find(bird.TargetId);
                bool visiting=bird!=null&&WishPlace(wish.Kind,place)&&Distance(bird.X-place.X*CellSize,bird.Z-place.Z*CellSize)<1.4f;
                if(visiting)
                {
                    if(wish.Kind==BirdWishKind.FriendlyLunch)
                    {
                        var friend=Birds.Find(b=>b.Id==bird.CompanionId);
                        visiting=bird.Social==SocialActivity.Resting&&friend!=null&&friend.CompanionId==bird.Id&&friend.TargetId==place.Id&&FriendshipTime(bird.Id,friend.Id)>=FriendThreshold;
                    }
                    else if(wish.Kind==BirdWishKind.HomeBath)visiting=bird.Action=="水浴び";
                    else visiting=bird.Action=="休憩"||bird.Action=="日向ぼっこ"||bird.Action=="羽繕い";
                }
                wish.VisitTime=visiting?wish.VisitTime+dt:0;
                if(wish.VisitTime<2)continue;
                wish.Complete=true;bird.WishThanksPending=true;
                Notice=bird.Name+"のお願いが叶いました！「ありがとう」";
            }
        }
        static List<BirdWish> CopyWishes(List<BirdWish> source)
        {
            var result=new List<BirdWish>();
            if(source!=null)foreach(var w in source)result.Add(new BirdWish{Kind=w.Kind,BirdId=w.BirdId,Complete=w.Complete});
            return result;
        }
        static bool ValidWishes(TownSave save)
        {
            if(save.Wishes==null)return true;
            if(save.Wishes.Count>3)return false;
            var kinds=new HashSet<BirdWishKind>();
            foreach(var w in save.Wishes)
                if(w==null||!Enum.IsDefined(typeof(BirdWishKind),w.Kind)||!kinds.Add(w.Kind)||!save.Birds.Exists(b=>b.Id==w.BirdId))return false;
            return true;
        }
    }
}
