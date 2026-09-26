using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    // Builds a real TownWorld in edit mode and checks that syncing only rebuilds what changed.
    public static class WorldSyncChecks
    {
        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception("World sync: " + message);
        }

        [MenuItem("Pigeon Sandbox/Verify world sync")]
        public static void Verify()
        {
            var root = new GameObject("World sync verification");
            try
            {
                var world = root.AddComponent<TownWorld>();
                world.Initialize();
                var town = new TownSimulation(9);
                town.Money = 10000;
                Check(town.Build(FacilityKind.Bakery, 1, 0) && town.Build(FacilityKind.Fountain, 2, 0), "setup builds");
                world.Sync(town, true);
                int built = world.FacilityModelsBuilt;
                Check(built == town.Facilities.Count, "first sync builds every facility");
                var bakery = town.At(1, 0);
                var fountain = town.At(2, 0);
                var bakeryObject = world.FacilityObject(bakery.Id);
                var fountainObject = world.FacilityObject(fountain.Id);
                town.TogglePolicy(TownPolicy.NestBoxes);
                world.Sync(town, true);
                Check(world.FacilityModelsBuilt == built, "policy change rebuilds nothing");
                Check(town.Move(bakery.Id, 3, 3), "move bakery");
                world.Sync(town, true);
                Check(world.FacilityModelsBuilt == built && world.FacilityObject(bakery.Id) == bakeryObject, "moved facility keeps its object");
                Check(bakeryObject.transform.localPosition == new Vector3(3 * 2.2f, 0, 3 * 2.2f), "moved facility follows its plot");
                Check(town.Upgrade(fountain.Id), "upgrade fountain");
                world.Sync(town, true);
                Check(world.FacilityModelsBuilt == built + 1 && world.FacilityObject(fountain.Id) != fountainObject, "upgrade rebuilds only that facility");
                Check(world.FacilityObject(bakery.Id) == bakeryObject, "upgrade leaves other facilities alone");
                Check(town.Remove(bakery.Id), "remove bakery");
                world.Sync(town, true);
                Check(world.FacilityObject(bakery.Id) == null && bakeryObject == null, "removed facility object is destroyed");
                // Many identical pigeons and facilities must still share a small set of materials.
                var full = TownSimulation.CreateBenchmark();
                world.Sync(full, true);
                var shared = new HashSet<Material>();
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    shared.Add(renderer.sharedMaterial);
                Check(shared.Count <= 80, "benchmark town shares materials (" + shared.Count + ")");
                Debug.Log("PIGEON WORLD SYNC VERIFICATION PASSED: incremental rebuild, move in place, removal, shared materials (" + shared.Count + ")");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
