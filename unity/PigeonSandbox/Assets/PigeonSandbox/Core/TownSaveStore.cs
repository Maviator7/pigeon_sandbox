using System;
using System.IO;

namespace PigeonSandbox
{
    // Save file handling that never loses a town: atomic replace, one backup, and unreadable files set aside
    // (never overwritten) so they can be recovered by hand.
    public sealed class TownSaveStore
    {
        readonly string path;
        public TownSaveStore(string path)
        {
            this.path = path;
        }

        string Backup => path + ".bak";
        string Temporary => path + ".tmp";
        public void Write(string json)
        {
            File.WriteAllText(Temporary, json);
            if (File.Exists(path))
                File.Replace(Temporary, path, Backup);
            else
                File.Move(Temporary, path);
        }

        // Returns the newest save that passes the check, or null for a fresh start.
        public string Read(Func<string, bool> usable = null)
        {
            foreach (string candidate in new[]{path, Backup})
            {
                if (!File.Exists(candidate))
                    continue;
                string text = null;
                try
                {
                    text = File.ReadAllText(candidate);
                }
                catch (IOException)
                {
                }

                if (text != null && (usable == null || usable(text)))
                    return text;
                if (candidate == path)
                    SetAside(candidate);
            }

            return null;
        }

        void SetAside(string file)
        {
            string folder = Path.GetDirectoryName(file);
            string name = Path.GetFileNameWithoutExtension(file) + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            string target = Path.Combine(folder, name + ".json");
            for (int i = 1; File.Exists(target); i++)
                target = Path.Combine(folder, name + "-" + i + ".json");
            File.Move(file, target);
        }
    }
}
