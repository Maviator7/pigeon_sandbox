# UI font

`Assets/Resources/NotoSansJP.ttf` is a static instance of Noto Sans JP at
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
