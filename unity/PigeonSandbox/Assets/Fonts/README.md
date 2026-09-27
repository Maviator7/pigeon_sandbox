# UI font

`Tools/FontSource/NotoSansJP-Medium.ttf` is a static instance of Noto Sans JP at
weight 500 (Medium). The original variable font defaults to weight 100;
using that file directly made the Unity IMGUI labels unusually thin.
Keep the static instance so runtime font metrics and appearance agree.

The font remains under the SIL Open Font License in `OFL.txt`.
The instance was produced with fontTools:

```python
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

font = instantiateVariableFont(TTFont("NotoSansJP-variable.ttf"), {"wght": 500})
font.save("NotoSansJP.ttf")
```

## Shipped subset

`Assets/Resources/NotoSansJP.ttf` is a subset of that instance (about 1 MB instead of 5.6 MB):
ASCII and full-width forms, kana, common symbols, the Jōyō kanji with their allowed variants
(`Tools/FontSource/joyo-kanji.txt`, generated from Unicode Unihan `kJoyoKanji`), and every
non-ASCII character used in C# string literals under `Assets`. Regenerate after adding UI text:

```sh
python3 unity/PigeonSandbox/Tools/subset_font.py
```

Bird names are limited to characters the font contains (`Font.HasCharacter`), so they always render.
