using System;
using System.IO;
using PigeonSandbox;

public static class SaveStoreChecks
{
    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    public static void RunAll()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pigeon-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "mayor-town.json");
            var store = new TownSaveStore(path);
            Check(store.Read() == null, "missing save reads as nothing");
            store.Write("first");
            Check(File.ReadAllText(path) == "first" && !File.Exists(path + ".tmp"), "write replaces atomically without leftovers");
            store.Write("second");
            Check(File.ReadAllText(path) == "second" && File.ReadAllText(path + ".bak") == "first", "previous save is kept as backup");
            Check(store.Read() == "second", "read returns the latest save");
            // A corrupt main file falls back to the backup, and is set aside instead of being overwritten later.
            File.WriteAllText(path, "{broken");
            string recovered = store.Read(json => json != "{broken");
            Check(recovered == "first", "unreadable save falls back to backup");
            Check(!File.Exists(path) || File.ReadAllText(path) != "{broken", "unreadable save is moved out of the way");
            Check(Directory.GetFiles(folder, "mayor-town.unreadable-*.json").Length == 1, "unreadable save is preserved for recovery");
            File.WriteAllText(path, "{broken");
            File.Delete(path + ".bak");
            Check(store.Read(json => json != "{broken") == null && !File.Exists(path), "no usable save starts fresh without deleting data");
            Check(Directory.GetFiles(folder, "mayor-town.unreadable-*.json").Length == 2, "every unreadable save is preserved");
            // Versioning: newer saves are refused so an older app never overwrites them.
            var town = new TownSimulation();
            var save = town.Capture();
            Check(save.SaveVersion == TownSimulation.CurrentSaveVersion, "captures record the save version");
            save.SaveVersion = TownSimulation.CurrentSaveVersion + 1;
            Check(!new TownSimulation().Restore(save), "save from a newer version is refused");
            save.SaveVersion = 0;
            Check(new TownSimulation().Restore(save), "legacy save without a version still loads");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
