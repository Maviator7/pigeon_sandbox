using System;
using System.Collections.Generic;

namespace PigeonSandbox
{
    public partial class TownSimulation
    {
        public const int WaterfrontCost = 3600;
        static readonly int[] ShoulderWidths = {8, 7, 6, 6, 5, 4, 4};
        public bool WaterfrontUnlocked
        {
            get;
            private set;
        }

        public readonly List<TownRequest> WaterfrontRequests = new List<TownRequest>{new TownRequest{Title = "木陰の散歩道", Description = "水辺地区の街路樹と花壇の公園を2マス以内に", Reward = 180}, new TownRequest{Title = "水浴びの庭", Description = "水辺地区の噴水と花壇の公園を2マス以内に", Reward = 220}, new TownRequest{Title = "水辺のパン屋", Description = "水辺地区のパン屋と広場を2マス以内に。お客さんがパンを買うと達成", Reward = 260}};
        bool waterfrontPurchaseWitnessed;
        public float WaterfrontEastEdge => 15.55f * CellSize;
        internal bool IsWalkableGround(float x, float z)
        {
            if (x >= -MapEdge && x <= MapEdge && z >= -MapEdge && z <= MapEdge)
                return true;
            if (!WaterfrontUnlocked)
                return false;
            float gx = x / CellSize, gz = Math.Abs(z / CellSize);
            if (gx >= 8.5f && gx <= 15.55f && gz <= 3.5f)
                return true;
            for (int col = 0; col < ShoulderWidths.Length; col++)
                if (gx >= 8.5f + col && gx <= 9.5f + col && gz >= 3.5f && gz <= ShoulderWidths[col] + .5f)
                    return true;
            return false;
        }

        void ClampBirdToGround(ref float x, ref float z)
        {
            if (!WaterfrontUnlocked)
                return;
            float bestX = Clamp(x, -MapEdge, MapEdge), bestZ = Clamp(z, -MapEdge, MapEdge);
            float bestDistance = Distance(bestX - x, bestZ - z);
            ConsiderGroundRectangle(ref bestX, ref bestZ, ref bestDistance, x, z, 8.5f, 15.55f, -3.5f, 3.5f);
            for (int col = 0; col < ShoulderWidths.Length; col++)
                foreach (int side in new[]{-1, 1})
                {
                    float low = side < 0 ? -ShoulderWidths[col] - .5f : 3.5f;
                    float high = side < 0 ? -3.5f : ShoulderWidths[col] + .5f;
                    ConsiderGroundRectangle(ref bestX, ref bestZ, ref bestDistance, x, z, 8.5f + col, 9.5f + col, low, high);
                }

            x = bestX;
            z = bestZ;
        }

        static void ConsiderGroundRectangle(ref float bestX, ref float bestZ, ref float bestDistance, float x, float z, float left, float right, float bottom, float top)
        {
            float candidateX = Clamp(x, left * CellSize, right * CellSize);
            float candidateZ = Clamp(z, bottom * CellSize, top * CellSize);
            float distance = Distance(candidateX - x, candidateZ - z);
            if (distance >= bestDistance)
                return;
            bestX = candidateX;
            bestZ = candidateZ;
            bestDistance = distance;
        }

        Facility WaterfrontVenueFor(int visitorId)
        {
            int count = 0;
            foreach (var facility in Facilities)
                if (IsWaterfrontPlot(facility.X, facility.Z) && (facility.Kind == FacilityKind.Bakery || facility.Kind == FacilityKind.Cafe || facility.Kind == FacilityKind.Park || facility.Kind == FacilityKind.Fountain))
                    count++;
            if (count == 0)
                return null;
            int pick = visitorId % count;
            foreach (var facility in Facilities)
                if (IsWaterfrontPlot(facility.X, facility.Z) && (facility.Kind == FacilityKind.Bakery || facility.Kind == FacilityKind.Cafe || facility.Kind == FacilityKind.Park || facility.Kind == FacilityKind.Fountain) && pick-- == 0)
                    return facility;
            return null;
        }

        bool WaterfrontPair(FacilityKind a, FacilityKind b)
        {
            foreach (var first in Facilities)
                if (first.Kind == a && IsWaterfrontPlot(first.X, first.Z))
                    foreach (var second in Facilities)
                        if (second.Kind == b && IsWaterfrontPlot(second.X, second.Z) && Near(first, second))
                            return true;
            return false;
        }

        void EvaluateWaterfrontRequests()
        {
            if (!WaterfrontUnlocked)
                return;
            RewardWaterfront(0, WaterfrontPair(FacilityKind.Tree, FacilityKind.Park));
            RewardWaterfront(1, WaterfrontPair(FacilityKind.Fountain, FacilityKind.Park));
            RewardWaterfront(2, waterfrontPurchaseWitnessed && WaterfrontPair(FacilityKind.Bakery, FacilityKind.Plaza));
        }

        void RewardWaterfront(int index, bool ready)
        {
            var request = WaterfrontRequests[index];
            if (!ready || request.Complete)
                return;
            request.Complete = true;
            Money += request.Reward;
            Notice = "水辺のお願い達成：" + request.Title + " +" + request.Reward;
        }

        void WitnessWaterfrontPurchase(Facility target)
        {
            if (!WaterfrontUnlocked || target.Kind != FacilityKind.Bakery || !IsWaterfrontPlot(target.X, target.Z) || !WaterfrontPair(FacilityKind.Bakery, FacilityKind.Plaza))
                return;
            waterfrontPurchaseWitnessed = true;
            EvaluateWaterfrontRequests();
        }

        public bool IsWaterfrontPlot(int x, int z) => x >= 9 && x <= 15 && z >= -3 && z <= 3;
        public bool IsPlayablePlot(int x, int z) => x >= -MapRadius && x <= MapRadius && z >= -MapRadius && z <= MapRadius || WaterfrontUnlocked && IsWaterfrontPlot(x, z);
        public bool UnlockWaterfront()
        {
            if (WaterfrontUnlocked || ExpansionLevel != 4 || Money < WaterfrontCost)
                return false;
            Money -= WaterfrontCost;
            WaterfrontUnlocked = true;
            Changed("水辺地区が開きました！");
            return true;
        }

        static bool ValidWaterfrontPlotForSave(int x, int z, int radius, bool unlocked)
        {
            return Math.Abs((long)x) <= radius && Math.Abs((long)z) <= radius || unlocked && x >= 9 && x <= 15 && z >= -3 && z <= 3;
        }
    }
}
