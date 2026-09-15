using System;
using System.Collections.Generic;
namespace PigeonSandbox
{
    public enum FacilityKind
    {
        Bakery,Fountain,Housing,Tree,Plaza,ClockTower
    }
    public enum Personality
    {
        Foodie,Bather,Showoff,Shy
    }
    public enum TownPolicy
    {
        NestBoxes,BathPriority,CafeSupport
    }
    [Serializable] public class Facility
    {
        public int Id,X,Z,Level=1;
        public FacilityKind Kind;
    }
    [Serializable] public class TownBird
    {
        public int Id;
        public string Name;
        public Personality Personality;
        public bool Rare,Mayor;
        public float X,Y,Z,Heading;
        public string Action="散歩";
        public int TargetId=-1;
        internal float Wait,Decision;
    }
    public class Visitor
    {
        public int Id;
        public float X,Z,Heading;
        public string Action="お買い物へ";
        internal int TargetId=-1;
        internal bool Bought;
        internal float Wait;
    }
    public class TownRequest
    {
        public string Title,Description;
        public bool Complete;
        public int Reward;
    }
    [Serializable] public class TownSave
    {
        public List<Facility> Facilities=new List<Facility>();
        public List<TownBird> Birds=new List<TownBird>();
        public bool NestBoxes,BathPriority,CafeSupport;
        public float Money,Time;
        public int Purchases;
        public bool[] CompletedRequests;
    }
    public class TownSimulation
    {
        public const float CellSize=2.2f;
        public readonly List<Facility> Facilities=new List<Facility>();
        public readonly List<TownBird> Birds=new List<TownBird>();
        public readonly List<Visitor> Visitors=new List<Visitor>();
        public readonly List<TownRequest> Requests=new List<TownRequest>();
        public float Money=700,FoodSupply,PigeonHappiness,HumanSatisfaction,Cleanliness,Crowding,Income,Upkeep,Time;
        public int Day=1,Purchases,Revision;
        public string Notice="ようこそ、鳩市長。広場のそばにパン屋を開こう。";
        public bool NestBoxes,BathPriority,CafeSupport;
        readonly Random random;
        int nextId=1;
        float spawnClock,upkeepClock,growthClock,rareClock,metricClock;
        bool rareArrived;
        public TownSimulation(int seed=42)
        {
            random=new Random(seed);
            AddFacility(FacilityKind.Plaza,0,0);
            AddFacility(FacilityKind.Tree,-2,0);
            AddBird(Personality.Foodie,true,false);
            AddBird(Personality.Bather,false,false);
            AddBird(Personality.Shy,false,false);
            AddBird(Personality.Showoff,false,false);
            Requests.Add(new TownRequest
            {
                Title="焼きたての広場",Description="広場から2マス以内にパン屋を置く",Reward=90
            }
            );
            Requests.Add(new TownRequest
            {
                Title="水辺の暮らし",Description="パン屋から2マス以内に噴水を置き、集合住宅を建てる",Reward=110
            }
            );
            Requests.Add(new TownRequest
            {
                Title="白いお客さま",Description="静かな家の2マス以内に木と噴水。白い鳩を迎える",Reward=150
            }
            );
            Recalculate();
        }
        public TownSave Capture()
        {
            var save=new TownSave
            {
                Money=Money,Time=Time,Purchases=Purchases,NestBoxes=NestBoxes,BathPriority=BathPriority,CafeSupport=CafeSupport,CompletedRequests=new bool[Requests.Count]
            }
            ;
            foreach(var f in Facilities)save.Facilities.Add(new Facility
            {
                Id=f.Id,Kind=f.Kind,X=f.X,Z=f.Z,Level=f.Level
            }
            );
            foreach(var b in Birds)save.Birds.Add(new TownBird
            {
                Id=b.Id,Name=b.Name,Personality=b.Personality,Rare=b.Rare,Mayor=b.Mayor,X=b.X,Y=b.Y,Z=b.Z,Heading=b.Heading,Action=b.Action,TargetId=b.TargetId
            }
            );
            for(int i=0;i<Requests.Count;i++)save.CompletedRequests[i]=Requests[i].Complete;
            return save;
        }
        public bool Restore(TownSave save)
        {
            if(save==null||save.Facilities==null||save.Birds==null||save.Birds.Count==0||save.Birds.Count>12||float.IsNaN(save.Money)||float.IsInfinity(save.Money)||float.IsNaN(save.Time)||float.IsInfinity(save.Time))return false;
            var ids=new HashSet<int>();
            var cells=new HashSet<string>();
            foreach(var f in save.Facilities)
            {
                if(f==null||f.Id<1||!ids.Add(f.Id)||!cells.Add(f.X+":"+f.Z)||Math.Abs((long)f.X)>4||Math.Abs((long)f.Z)>4||f.Level<1||f.Level>3||!Enum.IsDefined(typeof(FacilityKind),f.Kind))return false;
            }
            int rares=0;
            foreach(var b in save.Birds)
            {
                if(b==null||b.Id<1||!ids.Add(b.Id)||!Enum.IsDefined(typeof(Personality),b.Personality))return false;
                if(b.Rare)rares++;
            }
            if(rares>1)return false;
            Facilities.Clear();
            Birds.Clear();
            Visitors.Clear();
            nextId=1;
            foreach(var f in save.Facilities)
            {
                Facilities.Add(new Facility
                {
                    Id=f.Id,Kind=f.Kind,X=f.X,Z=f.Z,Level=f.Level
                }
                );
                nextId=Math.Max(nextId,f.Id+1);
            }
            foreach(var b in save.Birds)
            {
                Birds.Add(new TownBird
                {
                    Id=b.Id,Name=b.Name,Personality=b.Personality,Rare=b.Rare,Mayor=b.Mayor,X=FinitePosition(b.X),Z=FinitePosition(b.Z),Action="散歩",TargetId=-1
                }
                );
                nextId=Math.Max(nextId,b.Id+1);
            }
            Money=Math.Max(0,save.Money);
            Time=Math.Max(0,save.Time);
            Day=1+(int)(Time/120);
            Purchases=Math.Max(0,save.Purchases);
            NestBoxes=save.NestBoxes;
            BathPriority=save.BathPriority;
            CafeSupport=save.CafeSupport;
            rareArrived=rares>0;
            rareClock=spawnClock=upkeepClock=growthClock=metricClock=0;
            for(int i=0;i<Requests.Count;i++)Requests[i].Complete=save.CompletedRequests!=null&&i<save.CompletedRequests.Length&&save.CompletedRequests[i];
            Changed("保存したまちを再開しました。");
            return true;
        }
        static float FinitePosition(float value)
        {
            return float.IsNaN(value)||float.IsInfinity(value)?0:Clamp(value,-10,10);
        }
        public static int Cost(FacilityKind k)
        {
            return new[]
            {
                140,110,120,55,80,230
            }
            [(int)k];
        }
        public static string NameOf(FacilityKind k)
        {
            return new[]
            {
                "パン屋","噴水","集合住宅","街路樹","広場","時計台"
            }
            [(int)k];
        }
        public static string PersonalityName(Personality p)
        {
            return new[]
            {
                "食いしん坊","水浴び好き","目立ちたがり","臆病"
            }
            [(int)p];
        }
        static float Clamp(float v,float lo=0,float hi=100)
        {
            return Math.Max(lo,Math.Min(hi,v));
        }
        static float Distance(float x,float z)
        {
            return (float)Math.Sqrt(x*x+z*z);
        }
        static bool Near(Facility a,Facility b,float radius=2.1f)
        {
            return a.Id!=b.Id&&Distance(a.X-b.X,a.Z-b.Z)<=radius;
        }
        bool NearKind(Facility a,FacilityKind kind)
        {
            return Facilities.Exists(b=>b.Kind==kind&&Near(a,b));
        }
        int Levels(FacilityKind kind)
        {
            int n=0;
            foreach(var f in Facilities)if(f.Kind==kind)n+=f.Level;
            return n;
        }
        Facility Find(int id)
        {
            return Facilities.Find(f=>f.Id==id);
        }
        public Facility At(int x,int z)
        {
            return Facilities.Find(f=>f.X==x&&f.Z==z);
        }
        public bool CanPlace(int x,int z,int ignoreId=-1)
        {
            return x>=-4&&x<=4&&z>=-4&&z<=4&&!Facilities.Exists(f=>f.Id!=ignoreId&&f.X==x&&f.Z==z);
        }
        void AddFacility(FacilityKind kind,int x,int z)
        {
            Facilities.Add(new Facility
            {
                Id=nextId++,Kind=kind,X=x,Z=z
            }
            );
        }
        public bool Build(FacilityKind kind,int x,int z)
        {
            if(!Enum.IsDefined(typeof(FacilityKind),kind)||!CanPlace(x,z)||Money<Cost(kind))
            {
                Notice="空き地と建設費を確認しよう。";
                return false;
            }
            Money-=Cost(kind);
            AddFacility(kind,x,z);
            Changed(NameOf(kind)+"がオープン！");
            return true;
        }
        public bool Move(int id,int x,int z)
        {
            var f=Find(id);
            if(f==null||!CanPlace(x,z,id))return false;
            f.X=x;
            f.Z=z;
            foreach(var b in Birds)if(b.TargetId==id)
            {
                b.Wait=0;
                b.Action="散歩";
            }
            Changed("配置を変更しました。移設費は無料です。");
            return true;
        }
        public bool Upgrade(int id)
        {
            var f=Find(id);
            if(f==null||f.Level>=3)return false;
            int cost=Cost(f.Kind)*f.Level/2;
            if(Money<cost)return false;
            Money-=cost;
            f.Level++;
            Changed(NameOf(f.Kind)+"がレベル"+f.Level+"になりました。");
            return true;
        }
        public bool Remove(int id)
        {
            var f=Find(id);
            if(f==null)return false;
            float invested=Cost(f.Kind)*(1+(f.Level-1)*f.Level*.25f);
            Money+=invested*.7f;
            Facilities.Remove(f);
            foreach(var b in Birds)if(b.TargetId==id)
            {
                b.TargetId=-1;
                b.Wait=0;
                b.Decision=0;
            }
            Changed("撤去費用の70%が戻りました。");
            return true;
        }
        public bool TogglePolicy(TownPolicy p)
        {
            switch(p)
            {
                case TownPolicy.NestBoxes:NestBoxes=!NestBoxes;
                break;
                case TownPolicy.BathPriority:BathPriority=!BathPriority;
                break;
                case TownPolicy.CafeSupport:CafeSupport=!CafeSupport;
                break;
                default:return false;
            }
            Changed("まちの方針を更新しました。");
            return true;
        }
        void Changed(string notice)
        {
            Notice=notice;
            Revision++;
            Recalculate();
        }
        bool QuietHabitat()
        {
            return Facilities.Exists(h=>h.Kind==FacilityKind.Housing&&NearKind(h,FacilityKind.Tree)&&NearKind(h,FacilityKind.Fountain)&&!NearKind(h,FacilityKind.Bakery)&&!NearKind(h,FacilityKind.ClockTower));
        }
        public string RareHint
        {
            get
            {
                return rareArrived?"白い鳩が、このまちを気に入りました。":QuietHabitat()?"静かな水辺で白い羽の気配…あと"+Math.Max(0,(int)(45-rareClock))+"秒ほど。":"パン屋・時計台から離れた集合住宅に、2マス以内の木と噴水を。";
            }
        }
        public void Recalculate()
        {
            int bakery=Levels(FacilityKind.Bakery),water=Levels(FacilityKind.Fountain),home=Levels(FacilityKind.Housing),tree=Levels(FacilityKind.Tree),plaza=Levels(FacilityKind.Plaza),clock=Levels(FacilityKind.ClockTower);
            int market=0,baths=0;
            foreach(var f in Facilities)
            {
                if(f.Kind==FacilityKind.Bakery&&NearKind(f,FacilityKind.Plaza))market+=f.Level;
                if(f.Kind==FacilityKind.Fountain&&NearKind(f,FacilityKind.Bakery))baths+=f.Level;
            }
            FoodSupply=Clamp(28+bakery*18+(CafeSupport?15:0)-Birds.Count*2);
            Crowding=Clamp(18+bakery*10+clock*8+Birds.Count*2-plaza*9-tree*3-market*5+(CafeSupport?8:0));
            Cleanliness=Clamp(88+tree*5+water*4-Birds.Count*3-bakery*4+(BathPriority?-8:0));
            PigeonHappiness=Clamp(30+FoodSupply*.28f+water*5+home*4+tree*3+baths*5-Crowding*.16f+(NestBoxes?8:0)+(BathPriority?10:0));
            HumanSatisfaction=Clamp(45+market*8+clock*5+plaza*4+Cleanliness*.2f-Crowding*.32f+(BathPriority?-7:0)+(CafeSupport?5:0));
            Upkeep=bakery*3+water*2+home+tree*.5f+clock*4+(NestBoxes?3:0)+(BathPriority?3:0)+(CafeSupport?4:0);
            Reward(0,market>0);
            Reward(1,baths>0&&home>0);
            Reward(2,rareArrived);
        }
        void Reward(int index,bool complete)
        {
            var r=Requests[index];
            if(!complete||r.Complete)return;
            r.Complete=true;
            Money+=r.Reward;
            Notice="お願い達成："+r.Title+" +"+r.Reward;
        }
        void AddBird(Personality p,bool mayor,bool rare)
        {
            int n=Birds.Count;
            Birds.Add(new TownBird
            {
                Id=nextId++,Name=rare?"しらたま":new[]
                {
                    "ぽっぽ市長","しずく","こむぎ","きらり","まめ","つばさ","くるみ","すず","もち","あお","ふわ","ひなた"
                }
                [n%12],Personality=p,Mayor=mayor,Rare=rare,X=(float)random.NextDouble()*2-1,Z=(float)random.NextDouble()*2-1
            }
            );
        }
        Facility Select(TownBird b)
        {
            Facility best=null;
            float score=-999;
            foreach(var f in Facilities)
            {
                float s=(float)random.NextDouble()*3;
                if(f.Kind==FacilityKind.Bakery&&NearKind(f,FacilityKind.Plaza))s+=3;
                if(f.Kind==FacilityKind.Fountain&&NearKind(f,FacilityKind.Bakery))s+=3;
                switch(b.Personality)
                {
                    case Personality.Foodie:s+=f.Kind==FacilityKind.Bakery?11:f.Kind==FacilityKind.Plaza?3:0;
                    break;
                    case Personality.Bather:s+=f.Kind==FacilityKind.Fountain?12+(BathPriority?4:0):0;
                    break;
                    case Personality.Showoff:s+=f.Kind==FacilityKind.ClockTower?13:f.Kind==FacilityKind.Plaza?9:0;
                    break;
                    case Personality.Shy:s+=f.Kind==FacilityKind.Tree?10:f.Kind==FacilityKind.Housing?9:0;
                    if(NearKind(f,FacilityKind.Bakery))s-=5;
                    break;
                }
                if(f.Id==b.TargetId)s-=7;
                if(s>score)
                {
                    score=s;
                    best=f;
                }
            }
            return best;
        }
        static void Approach(Facility f,int id,out float x,out float z)
        {
            float angle=(id%4)*(float)Math.PI*.5f;
            x=f.X*CellSize+(float)Math.Cos(angle)*.96f;
            z=f.Z*CellSize+(float)Math.Sin(angle)*.96f;
        }
        bool Walk(ref float x,ref float z,ref float heading,float tx,float tz,float speed,float dt)
        {
            float dx=tx-x,dz=tz-z,d=Distance(dx,dz);
            if(d<.08f)return true;
            heading=(float)(Math.Atan2(dx,dz)*180/Math.PI);
            float step=Math.Min(d,speed*dt);
            float nx=x+dx/d*step,nz=z+dz/d*step;
            foreach(var f in Facilities)
            {
                float ox=nx-f.X*CellSize,oz=nz-f.Z*CellSize;
                float dist=Distance(ox,oz);
                if(dist<.78f)
                {
                    if(dist<.001f)
                    {
                        ox=1;
                        oz=0;
                        dist=1;
                    }
                    // Advance around the obstacle instead of becoming pinned to its rim.
                    float angle=(float)Math.Atan2(oz,ox);
                    float cross=ox*(tz-f.Z*CellSize)-oz*(tx-f.X*CellSize);
                    angle+=(cross<0?-1:1)*step/.8f;
                    nx=f.X*CellSize+(float)Math.Cos(angle)*.8f;
                    nz=f.Z*CellSize+(float)Math.Sin(angle)*.8f;
                }
            }
            x=nx;
            z=nz;
            return d<=step+.08f;
        }
        public void Tick(float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
            dt=Math.Min(dt,.25f);
            Time+=dt;
            Day=1+(int)(Time/120);
            spawnClock+=dt;
            upkeepClock+=dt;
            growthClock+=dt;
            metricClock+=dt;
            if(metricClock>=1)
            {
                metricClock=0;
                Recalculate();
            }
            if(upkeepClock>=20)
            {
                upkeepClock-=20;
                float tax=12+Levels(FacilityKind.Housing)*5;
                Money=Math.Max(0,Money+tax-Upkeep);
                Income=tax;
                if(Money<55){Money+=12;Income+=12;}
                Notice="まちの会計：収入 "+tax.ToString("0")+" / 維持費 "+Upkeep.ToString("0.0");
            }
            if(spawnClock>=Math.Max(2,6-Levels(FacilityKind.ClockTower)*.5f-(CafeSupport?1:0))&&Visitors.Count<18)
            {
                spawnClock=0;
                Visitors.Add(new Visitor
                {
                    Id=nextId++,X=-10,Z=(float)random.NextDouble()*16-8
                }
                );
            }
            foreach(var b in Birds)
            {
                b.Decision-=dt;
                b.Wait-=dt;
                var target=Find(b.TargetId);
                if(target==null||b.Decision<=0)
                {
                    target=Select(b);
                    b.TargetId=target==null?-1:target.Id;
                    b.Decision=9+(float)random.NextDouble()*9;
                    b.Wait=0;
                }
                if(target==null)
                {
                    b.Action="散歩";
                    continue;
                }
                float tx,tz;
                Approach(target,b.Id,out tx,out tz);
                if(b.Wait>0)
                {
                    b.Y+=( ((target.Kind==FacilityKind.Tree||target.Kind==FacilityKind.ClockTower)?1.6f:0)-b.Y)*Math.Min(1,dt*3);
                    continue;
                }
                b.Y*=Math.Max(0,1-dt*4);
                b.Action="散歩";
                if(Walk(ref b.X,ref b.Z,ref b.Heading,tx,tz,.7f+(b.Mayor?.12f:0),dt))
                {
                    b.Action=target.Kind==FacilityKind.Fountain?"水浴び":target.Kind==FacilityKind.Bakery?"食事":target.Kind==FacilityKind.ClockTower?"眺める":"休憩";
                    b.Wait=2.5f+(float)random.NextDouble()*3;
                }
            }
            for(int i=Visitors.Count-1;i>=0;i--)
            {
                var v=Visitors[i];
                v.Wait-=dt;
                if(v.Wait>0)continue;
                var target=Find(v.TargetId);
                if(!v.Bought&&target==null)
                {
                    target=Facilities.Find(f=>f.Kind==FacilityKind.Bakery);
                    if(target==null)target=Facilities.Find(f=>f.Kind==FacilityKind.Plaza);
                    v.TargetId=target==null?-1:target.Id;
                }
                float tx=10,tz=v.Id%5-2;
                if(!v.Bought&&target!=null)Approach(target,v.Id,out tx,out tz);
                if(Walk(ref v.X,ref v.Z,ref v.Heading,tx,tz,1.5f,dt))
                {
                    if(v.Bought||target==null)
                    {
                        Visitors.RemoveAt(i);
                        continue;
                    }
                    if(target.Kind==FacilityKind.Bakery)
                    {
                        float purchase=8+target.Level*2+(NearKind(target,FacilityKind.Plaza)?4:0)+(CafeSupport?3:0);
                        Money+=purchase;
                        Income+=purchase;
                        Purchases++;
                        v.Action="パンを購入！";
                    }
                    else v.Action="公園でひと休み";
                    v.Bought=true;
                    v.Wait=2;
                }
                else if(v.Bought)v.Action="帰り道";
            }
            int capacity=4+Levels(FacilityKind.Housing)*2+(NestBoxes?2:0);
            if(growthClock>=32)
            {
                growthClock=0;
                if(Birds.Count<Math.Min(rareArrived?12:11,capacity)&&FoodSupply>=35)
                {
                    AddBird((Personality)(Birds.Count%4),false,false);
                    Notice="新しい鳩が引っ越してきました！";
                }
            }
            if(!rareArrived)
            {
                rareClock=QuietHabitat()?rareClock+dt:0;
                if(rareClock>=45)
                {
                    if(Birds.Count<12)AddBird(Personality.Shy,false,true);
                    else return;
                    rareArrived=true;
                    Notice="白い鳩『しらたま』がやってきました！";
                    Recalculate();
                }
            }
        }
    }
}
