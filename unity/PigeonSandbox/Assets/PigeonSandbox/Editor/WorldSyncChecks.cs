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
                Color springGround = world.transform.Find("Town ground").GetComponent<MeshFilter>().sharedMesh.colors[0];
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
                var summer = town.Capture();
                summer.Time = 15 * TownSimulation.DayLength;
                Check(town.Restore(summer), "advance town to summer");
                int beforeSeason = world.FacilityModelsBuilt;
                world.Sync(town, true);
                Check(world.FacilityModelsBuilt == beforeSeason + town.Facilities.Count, "season change rebuilds each facility once");
                Color summerGround = world.transform.Find("Town ground").GetComponent<MeshFilter>().sharedMesh.colors[0];
                Check(summerGround != springGround, "summer ground differs from spring ground");
                world.Sync(town, true);
                Check(world.FacilityModelsBuilt == beforeSeason + town.Facilities.Count, "same season does not rebuild again");
                Check(town.Remove(bakery.Id), "remove bakery");
                world.Sync(town, true);
                Check(world.FacilityObject(bakery.Id) == null && bakeryObject == null, "removed facility object is destroyed");
                Check(town.Build(FacilityKind.Tree, 1, 0), "build winter perch tree");
                world.Sync(town, true);
                var winter = town.Capture();
                winter.Time = 45 * TownSimulation.DayLength;
                Check(town.Restore(winter), "advance town to winter");
                world.Sync(town, true);
                var tree = town.At(1, 0);
                Check(world.FacilityObject(tree.Id).GetComponent<MeshFilter>().sharedMesh.bounds.max.y >= 2.5f, "winter tree supports a perched pigeon");
                // Many identical pigeons and facilities must still share a small set of materials.
                var full = TownSimulation.CreateBenchmark();
                world.Sync(full, true);
                int beforeFullSeason = world.FacilityModelsBuilt;
                var fullSummer = full.Capture();
                fullSummer.Time = 15 * TownSimulation.DayLength;
                Check(full.Restore(fullSummer), "advance full town to summer");
                world.Sync(full, true);
                Check(world.FacilityModelsBuilt - beforeFullSeason <= 8, "season transition spreads large rebuild across frames");
                for (int i = 0; i < 40; i++)
                    world.Sync(full, true);
                Check(world.FacilityModelsBuilt == beforeFullSeason + full.Facilities.Count, "full town finishes seasonal rebuild");
                var shared = new HashSet<Material>();
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    shared.Add(renderer.sharedMaterial);
                Check(shared.Count <= 3, "baked world uses the shared vertex-colour material (" + shared.Count + ")");
                // Baking: one renderer per facility, one for the ground, at most seven per pigeon.
                foreach (var f in full.Facilities)
                {
                    var facility = world.FacilityObject(f.Id);
                    Check(facility.GetComponentsInChildren<Renderer>(true).Length == 1 && facility.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0, "facility " + f.Kind + " is one baked mesh");
                }

                foreach (var pigeon in root.GetComponentsInChildren<PigeonWorld>(true))
                    Check(pigeon.GetComponentsInChildren<Renderer>(true).Length <= 7, "pigeon parts are baked per pivot");
                int renderers = root.GetComponentsInChildren<Renderer>(true).Length;
                Check(renderers < full.Facilities.Count + full.Birds.Count * 7 + 40, "baked benchmark town stays under the renderer budget (" + renderers + ")");
                Debug.Log("PIGEON WORLD SYNC VERIFICATION PASSED: incremental rebuild, move in place, removal, baked meshes (" + renderers + " renderers, " + shared.Count + " materials)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
