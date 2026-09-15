using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public enum Activity { Rest, Look, Walk, Eat, Fly, Land }
    public sealed class Seed
    {
        public int Id;
        public float X, Z;
    }

    // Pure C#: rendering and input never enter the simulation.
    public sealed class PigeonSimulation
    {
        readonly Func<double> random;
        public readonly List<Seed> Seeds = new List<Seed>();
        public Activity State { get; private set; } = Activity.Look;
        public float X, Y, Z, Heading;
        public float Hunger = 67, Energy = 88, Trust = 32;
        public float Speed { get; private set; }
        public int Eaten { get; private set; }
        public float Age { get; private set; }
        public string Goal { get; private set; } = "A quiet moment in the park";
        float elapsed, targetX, targetZ, startX, startY, startZ;
        int nextId;
        Seed selected;
        bool called;
        public PigeonSimulation(Func<double> random) { this.random = random; }
        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));
        static float Distance(float x, float z) => (float)Math.Sqrt(x*x+z*z);
        static void Safe(ref float x, ref float z)
        {
            float d = Distance(x,z);
            if (d > 5.8f) { x *= 5.8f/d; z *= 5.8f/d; }
        }
        public Seed AddFood(float x, float z)
        {
            if (Seeds.Count >= 60) return null;
            Safe(ref x, ref z);
            var seed = new Seed { Id = ++nextId, X=x, Z=z };
            Seeds.Add(seed); return seed;
        }
        void Enter(Activity state, string goal)
        {
            State=state; Goal=goal; elapsed=0; Speed=0;
        }
        void Target()
        {
            double a=random()*Math.PI*2, r=1+random()*4;
            targetX=(float)(Math.Sin(a)*r); targetZ=(float)(Math.Cos(a)*r);
        }
        public void Call(float x, float z)
        {
            if (State==Activity.Fly || State==Activity.Land) return;
            Safe(ref x,ref z); targetX=x; targetZ=z; selected=null; called=true;
            Enter(Activity.Walk,"Coming over to say hello");
        }
        public void Fly()
        {
            if (State==Activity.Fly || State==Activity.Land || Energy<20) return;
            selected=null; called=false; Target(); startX=X; startY=Y; startZ=Z;
            Enter(Activity.Fly,"A little flight around the park");
        }
        public void Rest()
        {
            if (Y>0) return;
            selected=null; called=false; Enter(Activity.Rest,"Taking a peaceful break");
        }
        public void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt<=0) return;
            dt=Math.Min(dt,.1f); elapsed+=dt; Age+=dt;
            Hunger=Clamp(Hunger+dt*.16f,0,100);
            Energy=Clamp(Energy+dt*(State==Activity.Fly?-3.7f:State==Activity.Walk?-.55f:1.5f),0,100);
            if (State==Activity.Fly)
            {
                float t=Clamp(elapsed/3.6f,0,1), e=t*t*(3-2*t);
                X=startX+(targetX-startX)*e; Z=startZ+(targetZ-startZ)*e;
                Y=startY*(1-e)+(float)Math.Sin(Math.PI*t)*2.7f+e*.65f;
                Heading=(float)Math.Atan2(targetX-startX,targetZ-startZ); Speed=2;
                if(t>=1) { startY=Y; Enter(Activity.Land,"Landing softly"); }
                return;
            }
            if(State==Activity.Land)
            {
                float t=Clamp(elapsed/1.2f,0,1); Y=startY*(1-t)*(1-t);
                if(t>=1) { Y=0; Enter(Activity.Rest,"Back on the grass"); }
                return;
            }
            if(selected==null && !called && Hunger>12 && State!=Activity.Eat && !(State==Activity.Rest && elapsed<3))
            {
                float best=8;
                foreach(var seed in Seeds)
                {
                    float d=Distance(seed.X-X,seed.Z-Z);
                    if(d<best) { best=d; selected=seed; }
                }
                if(selected!=null) { targetX=selected.X; targetZ=selected.Z; Enter(Activity.Walk,"Some seeds to share"); }
            }
            if(State==Activity.Walk)
            {
                float dx=targetX-X,dz=targetZ-Z,d=Distance(dx,dz);
                float step=Math.Min(d,.65f*dt);
                if(d>.001f) { X+=dx/d*step; Z+=dz/d*step; Heading=(float)Math.Atan2(dx,dz); Speed=.65f; }
                if(d<.2f)
                {
                    if(selected!=null) Enter(Activity.Eat,"Enjoying the seeds");
                    else { called=false; Enter(Activity.Look,"What a lovely spot"); }
                }
            }
            else if(State==Activity.Eat && elapsed>=1.8f)
            {
                if(selected!=null && Seeds.Remove(selected)) { Hunger=Clamp(Hunger-24,0,100); Trust=Clamp(Trust+7,0,100); Eaten++; }
                selected=null; Enter(Activity.Look,"Thank you for the snack");
            }
            else if((State==Activity.Rest || State==Activity.Look) && elapsed>=3)
            {
                if(Energy<15) Enter(Activity.Rest,"Resting in the sunshine");
                else if(random()<.12) Fly();
                else if(random()<.75) { Target(); Enter(Activity.Walk,"Exploring at my own pace"); }
                else Enter(Activity.Look,"Watching the world go by");
            }
        }
    }
}
