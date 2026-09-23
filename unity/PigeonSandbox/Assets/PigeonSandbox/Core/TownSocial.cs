using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public enum SocialActivity { None, Walking, Resting, Greeting, Bathing }

    [Serializable] public class BirdFriendship
    {
        public int FirstId,SecondId;
        public float SharedSeconds;
    }

    public partial class TownSimulation
    {
        float socialClock;
        const float FriendThreshold=12;
        readonly List<BirdFriendship> friendships=new List<BirdFriendship>();

        public float FriendshipTime(int first,int second)
        {
            var bond=friendships.Find(f=>f.FirstId==Math.Min(first,second)&&f.SecondId==Math.Max(first,second));
            return bond==null?0:bond.SharedSeconds;
        }

        public TownBird BestFriendOf(TownBird bird)
        {
            if(bird==null)return null;
            TownBird best=null;float time=FriendThreshold;
            foreach(var candidate in Birds)
            {
                if(candidate.Id==bird.Id)continue;
                float together=FriendshipTime(bird.Id,candidate.Id);
                if(together<time||(together==time&&best!=null&&candidate.Id>best.Id))continue;
                best=candidate;time=together;
            }
            return best;
        }

        void ShareTime(TownBird first,TownBird second,float dt)
        {
            int a=Math.Min(first.Id,second.Id),b=Math.Max(first.Id,second.Id);
            var bond=friendships.Find(f=>f.FirstId==a&&f.SecondId==b);
            if(bond==null){bond=new BirdFriendship{FirstId=a,SecondId=b};friendships.Add(bond);}
            bond.SharedSeconds=Math.Min(120,bond.SharedSeconds+dt);
        }

        static bool ValidFriendships(TownSave save)
        {
            if(save.Friendships==null)return true; // Older saves have no relationship field.
            if(save.Friendships.Count>save.Birds.Count*(save.Birds.Count-1)/2)return false;
            var birdIds=new HashSet<int>();foreach(var bird in save.Birds)birdIds.Add(bird.Id);
            var pairs=new HashSet<string>();
            foreach(var f in save.Friendships)
                if(f==null||f.FirstId==f.SecondId||!birdIds.Contains(f.FirstId)||!birdIds.Contains(f.SecondId)||
                   float.IsNaN(f.SharedSeconds)||float.IsInfinity(f.SharedSeconds)||f.SharedSeconds<0||f.SharedSeconds>120||
                   !pairs.Add(Math.Min(f.FirstId,f.SecondId)+":"+Math.Max(f.FirstId,f.SecondId)))return false;
            return true;
        }

        static List<BirdFriendship> CopyFriendships(List<BirdFriendship> source)
        {
            var copy=new List<BirdFriendship>();
            if(source!=null)foreach(var f in source)copy.Add(new BirdFriendship
                {FirstId=Math.Min(f.FirstId,f.SecondId),SecondId=Math.Max(f.FirstId,f.SecondId),SharedSeconds=f.SharedSeconds});
            return copy;
        }

        public string ActivityOf(TownBird bird)
        {
            var companion=Birds.Find(b=>b.Id==bird.CompanionId);
            if(companion==null||bird.Social==SocialActivity.None)return bird.Action;
            return companion.Name+(bird.Social==SocialActivity.Walking?"と散歩中":
                bird.Social==SocialActivity.Greeting?"と再会のクルクル":bird.Social==SocialActivity.Bathing?"と水浴び中":"と休憩中");
        }

        bool AvailableForCompany(TownBird bird)
        {
            return bird.Social==SocialActivity.None&&bird.SocialCooldown<=0&&bird.Y<.15f&&bird.CheerRemaining<=0&&
                   (bird.Action=="散歩"||bird.Action=="休憩"||bird.Action=="日向ぼっこ"||bird.Action=="羽繕い");
        }

        static bool SocialPlace(Facility place)=>place.Kind==FacilityKind.Park||place.Kind==FacilityKind.Plaza||place.Kind==FacilityKind.Fountain;

        internal bool TryStartCompanionship(TownBird first,TownBird second,Facility place)
        {
            if(first==null||second==null||first==second||place==null||!SocialPlace(place)||
               !AvailableForCompany(first)||!AvailableForCompany(second))return false;
            if(Distance(first.X-second.X,first.Z-second.Z)>(first.Personality==Personality.Shy||second.Personality==Personality.Shy?1.8f:2.8f))return false;
            float x=place.X*CellSize,z=place.Z*CellSize;
            if(Distance(first.X-x,first.Z-z)>4||Distance(second.X-x,second.Z-z)>4)return false;
            // One pair per destination keeps small parks from becoming a pile of pigeons.
            if(Birds.Exists(b=>b.Social!=SocialActivity.None&&b.SocialPlaceId==place.Id))return false;
            bool greet=FriendshipTime(first.Id,second.Id)>=FriendThreshold&&random.NextDouble()<.35;
            BeginCompany(first,second.Id,place,greet);
            BeginCompany(second,first.Id,place,greet);
            return true;
        }

        static void BeginCompany(TownBird bird,int partner,Facility place,bool greet)
        {
            bird.CompanionId=partner;bird.Social=greet?SocialActivity.Greeting:SocialActivity.Walking;bird.SocialTime=greet?1.4f:12;
            bird.SocialPlaceId=place.Id;bird.SocialPlaceX=place.X;bird.SocialPlaceZ=place.Z;
            bird.TargetId=place.Id;bird.Wait=0;bird.CheerTurn=0;bird.Action=greet?"再会のクルクル":"仲間と散歩";
        }

        static void EndCompany(TownBird bird)
        {
            bird.CompanionId=-1;bird.Social=SocialActivity.None;bird.SocialTime=0;
            bird.SocialPlaceId=-1;bird.SocialCooldown=20+bird.Id%7;
            bird.CheerTurn=0;bird.Y=0;bird.Action="休憩";bird.Wait=1;bird.Decision=0;
        }

        void TickCompany(float dt)
        {
            foreach(var bird in Birds)bird.SocialCooldown=Math.Max(0,bird.SocialCooldown-dt);
            foreach(var first in Birds)
            {
                if(first.Social==SocialActivity.None)continue;
                var second=Birds.Find(b=>b.Id==first.CompanionId);
                var place=Find(first.SocialPlaceId);
                bool resting=first.Social==SocialActivity.Resting||first.Social==SocialActivity.Bathing;
                if(second==null||second.CompanionId!=first.Id||second.Social!=first.Social||second.SocialPlaceId!=first.SocialPlaceId||place==null||
                   place.X!=first.SocialPlaceX||place.Z!=first.SocialPlaceZ||(!resting&&(first.Y>=.15f||second.Y>=.15f)))
                {
                    if(second!=null&&second.CompanionId==first.Id)EndCompany(second);
                    EndCompany(first);continue;
                }
                if(first.Id>second.Id)continue; // Advance both birds together, once per tick.
                float elapsed=Math.Min(dt,Math.Max(0,first.SocialTime));
                first.SocialTime-=dt;second.SocialTime=first.SocialTime;
                if(resting)ShareTime(first,second,elapsed);
                if(first.Social==SocialActivity.Greeting)
                {
                    float progress=1-Math.Max(0,first.SocialTime)/1.4f;
                    first.CheerTurn=second.CheerTurn=360*progress*progress*(3-2*progress);
                    if(first.SocialTime<=0)
                    {
                        first.Social=second.Social=SocialActivity.Walking;first.SocialTime=second.SocialTime=12;
                        first.CheerTurn=second.CheerTurn=0;first.Action=second.Action="仲間と散歩";
                    }
                    continue;
                }
                if(first.SocialTime<=0){EndCompany(first);EndCompany(second);continue;}
                if(resting)
                {
                    float height=place.Kind==FacilityKind.Plaza?.3f:place.Kind==FacilityKind.Fountain?.18f:0;
                    first.Y+=(height-first.Y)*Math.Min(1,dt*5);second.Y+=(height-second.Y)*Math.Min(1,dt*5);
                    continue;
                }
                float angle=first.Id%4*(float)Math.PI*.5f;
                float x=place.X*CellSize,z=place.Z*CellSize;
                float ax=x+(float)Math.Cos(angle)*1.05f,az=z+(float)Math.Sin(angle)*1.05f;
                float bx=x+(float)Math.Cos(angle+.7f)*1.05f,bz=z+(float)Math.Sin(angle+.7f)*1.05f;
                // Seat/rim endpoints stay outside Walk's .78m obstacle radius.
                if(place.Kind==FacilityKind.Plaza){ax=x-.32f;bx=x+.32f;az=bz=z+.76f;}
                if(place.Kind==FacilityKind.Fountain){ax=x-.46f;bx=x+.46f;az=bz=z+.7f;}
                bool a=Walk(ref first.X,ref first.Z,ref first.Heading,ax,az,.58f,dt);
                bool b2=Walk(ref second.X,ref second.Z,ref second.Heading,bx,bz,.58f,dt);
                if(a&&b2)
                {
                    first.Social=second.Social=place.Kind==FacilityKind.Fountain?SocialActivity.Bathing:SocialActivity.Resting;
                    first.SocialTime=second.SocialTime=7;
                    first.Heading=second.Heading=place.Kind==FacilityKind.Park?(float)(Math.Atan2(Math.Cos(angle),Math.Sin(angle))*180/Math.PI):180;
                    first.Action=place.Kind==FacilityKind.Fountain?"水浴び":place.Kind==FacilityKind.Park?(first.Id%2==0?"羽繕い":"日向ぼっこ"):"休憩";
                    second.Action=place.Kind==FacilityKind.Fountain?"水浴び":place.Kind==FacilityKind.Park?(second.Id%2==0?"羽繕い":"日向ぼっこ"):"休憩";
                }
            }
            socialClock+=dt;
            if(socialClock<2)return;
            socialClock=0;
            foreach(var first in Birds)
            {
                if(!AvailableForCompany(first)||random.NextDouble()>.35)continue;
                var candidates=Birds.FindAll(b=>b.Id!=first.Id&&AvailableForCompany(b));
                // Familiar neighbours are considered first, but unavailable friends never block other company.
                candidates.Sort((a,b)=>{int order=FriendshipTime(first.Id,b.Id).CompareTo(FriendshipTime(first.Id,a.Id));return order!=0?order:a.Id.CompareTo(b.Id);});
                foreach(var second in candidates)
                {
                    var places=Facilities.FindAll(SocialPlace);
                    places.Sort((a,b)=>Distance(first.X-a.X*CellSize,first.Z-a.Z*CellSize).CompareTo(Distance(first.X-b.X*CellSize,first.Z-b.Z*CellSize)));
                    bool joined=false;
                    foreach(var place in places)if(TryStartCompanionship(first,second,place)){joined=true;break;}
                    if(joined)break;
                }
            }
        }
    }
}
