"""Build the shipped UI font: Noto Sans JP limited to the game's own text plus kana, ASCII and the Jōyō kanji.

Run after adding UI text. Requires fontTools (`pip install fonttools`).
Every non-ASCII character in a C# string literal under Assets (except Editor) is included automatically.
"""
from pathlib import Path
import re
from fontTools import subset
from fontTools.ttLib import TTFont

project = Path(__file__).resolve().parents[1]
source = project / 'Tools/FontSource/NotoSansJP-Medium.ttf'
target = project / 'Assets/Resources/NotoSansJP.ttf'
# Font.HasCharacter also reports OS fallback glyphs, so the game checks names against this list instead.
characters = project / 'Assets/Resources/FontCharacters.txt'
joyo = ''.join(line for line in (project / 'Tools/FontSource/joyo-kanji.txt').read_text(encoding='utf-8').splitlines() if not line.startswith('#'))

ranges = [
    (0x20, 0x7E),      # ASCII
    (0xA0, 0xFF),      # Latin-1 punctuation and signs (¥, ×, ·)
    (0x2010, 0x206F),  # general punctuation (—, …, ‥, ※)
    (0x2190, 0x21FF),  # arrows
    (0x2460, 0x24FF),  # circled numbers
    (0x25A0, 0x25FF),  # geometric shapes (○ ● □ ■ △)
    (0x2600, 0x26FF),  # miscellaneous symbols (☀ ★)
    (0x2700, 0x27BF),  # dingbats (✓)
    (0x3000, 0x303F),  # CJK symbols and punctuation
    (0x3041, 0x309F),  # hiragana
    (0x30A0, 0x30FF),  # katakana
    (0x31F0, 0x31FF),  # katakana phonetic extensions
    (0xFF01, 0xFF9F),  # full-width ASCII and half-width katakana
    (0xFFE0, 0xFFEE),  # full-width signs (￥)
]
text = {chr(c) for low, high in ranges for c in range(low, high + 1)}
text.update(joyo)
literal = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
for file in (project / 'Assets').rglob('*.cs'):
    if 'Editor' in file.parts:
        continue  # Editor-only text never reaches the player (and FontChecks names excluded kanji).
    for match in literal.finditer(file.read_text(encoding='utf-8')):
        text.update(ch for ch in match.group(1) if ord(ch) > 0x7E)

font = TTFont(source)
available = set(chr(c) for c in font.getBestCmap())
wanted = sorted(ch for ch in text if ch in available)
missing = sorted(ch for ch in text if ch not in available and ord(ch) > 0x7E and not (0x2010 <= ord(ch) <= 0x27BF) and not (0xA0 <= ord(ch) <= 0xFF) and not (0x3000 <= ord(ch) <= 0x31FF) and not (0xFF01 <= ord(ch) <= 0xFFEE))
options = subset.Options()
options.layout_features = ['*']
options.name_IDs = ['*']
options.notdef_outline = True
subsetter = subset.Subsetter(options)
subsetter.populate(unicodes=[ord(ch) for ch in wanted])
subsetter.subset(font)
font.save(target)
characters.write_text(''.join(wanted), encoding='utf-8')
print('%d characters, %d KB -> %s' % (len(wanted), target.stat().st_size // 1024, target.relative_to(project)))
if missing:
    print('not in the source font:', ''.join(missing))
