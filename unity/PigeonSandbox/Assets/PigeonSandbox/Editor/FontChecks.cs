using System;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    // The shipped font is a subset (Tools/subset_font.py). Font.HasCharacter also reports OS fallback glyphs,
    // so renaming checks names against FontCharacters.txt, which the subset script writes alongside the font.
    public static class FontChecks
    {
        public static void Verify()
        {
            if (Resources.Load<Font>("NotoSansJP") == null)
                throw new Exception("UI font missing");
            var list = Resources.Load<TextAsset>("FontCharacters");
            if (list == null)
                throw new Exception("FontCharacters.txt missing; run Tools/subset_font.py");
            foreach (char ch in "鳩市長の街づくりあア漢字✓○¥Aa1 ")
                if (list.text.IndexOf(ch) < 0)
                    throw new Exception("font subset lacks " + ch);
            foreach (char ch in "鰯鱈")
                if (list.text.IndexOf(ch) >= 0)
                    throw new Exception("font subset unexpectedly has " + ch);
            Debug.Log("PIGEON FONT VERIFICATION PASSED: subset list covers game text and Jōyō kanji only (" + list.text.Length + " characters)");
        }
    }
}
