using System;

namespace PigeonSandbox
{
    public partial class TownSimulation
    {
        // Worst-case town for performance measurement: largest map, every plot built, maximum pigeons.
        // Deterministic so before/after numbers compare the same scene.
        public static TownSimulation CreateBenchmark(int seed = 5)
        {
            var town = new TownSimulation(seed);
            town.ExpansionLevel = 4;
            var kinds = (FacilityKind[])Enum.GetValues(typeof(FacilityKind));
            int radius = town.MapRadius;
            for (int x = -radius; x <= radius; x++)
                for (int z = -radius; z <= radius; z++)
                {
                    if (town.At(x, z) != null)
                        continue;
                    int mix = ((x * 3 + z * 5) % kinds.Length + kinds.Length) % kinds.Length;
                    town.AddFacility(kinds[mix], x, z);
                    town.Facilities[town.Facilities.Count - 1].Level = 1 + ((x + z) % 3 + 3) % 3;
                }

            var feathers = new[]{Plumage.Checker, Plumage.Brown, Plumage.Pied};
            for (int i = 0; town.Birds.Count < 14; i++)
            {
                town.AddBird((Personality)(i % 4), false, false);
                if (i < feathers.Length)
                    town.Birds[town.Birds.Count - 1].Plumage = feathers[i];
            }

            town.AddBird(Personality.Shy, false, true);
            town.rareArrived = true;
            town.Money = 100000;
            town.Changed("ベンチマーク用の街です。保存されません。");
            return town;
        }
    }
}
