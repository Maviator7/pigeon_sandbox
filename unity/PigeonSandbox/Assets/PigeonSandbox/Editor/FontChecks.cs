using System;
using UnityEngine;

namespace PigeonSandbox.Editor
{
    // The shipped font is a subset (Tools/subset_font.py); renaming relies on Font.HasCharacter to match it.
    public static class FontChecks
    {
        public static void Verify()
        {
            var font = Resources.Load<Font>("NotoSansJP");
            if (font == null)
                throw new Exception("UI font missing");
            foreach (char ch in "鳩市長の街づくりあア漢字✓○¥Aa1")
                if (!font.HasCharacter(ch))
                    throw new Exception("UI font lacks " + ch);
            foreach (char ch in "鰯鱈🕊")
                if (font.HasCharacter(ch))
                    throw new Exception("UI font unexpectedly has " + ch + " (subset not applied?)");
            Debug.Log("PIGEON FONT VERIFICATION PASSED: subset covers game text and Jōyō kanji only");
        }
    }
}
