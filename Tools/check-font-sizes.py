# Report any prefab/scene TMP font sizes that are not multiples of 12.
from pathlib import Path
import re

root = Path(r"C:\Users\joshu\Desktop\GameDev\Unity\ModularChess\Assets")
paths = list(root.rglob("*.prefab")) + list(root.rglob("*.unity"))
off = []

direct = re.compile(
    r"(m_fontSize(?:Base|Min|Max)?|m_FontSize):\s*([0-9]+(?:\.[0-9]+)?)"
)
override = re.compile(
    r"propertyPath: (m_fontSize(?:Base|Min|Max)?)\n\s+value: ([0-9]+(?:\.[0-9]+)?)",
    re.MULTILINE,
)

for path in paths:
    text = path.read_text(encoding="utf-8")
    for m in direct.finditer(text):
        val = float(m.group(2))
        if val > 0 and abs(val % 12) > 0.001:
            off.append(f"{path.relative_to(root)}: {m.group(1)}={m.group(2)}")
    for m in override.finditer(text):
        val = float(m.group(2))
        if val > 0 and abs(val % 12) > 0.001:
            off.append(f"{path.relative_to(root)}: override {m.group(1)}={m.group(2)}")

print(f"Off-grid count: {len(off)}")
for line in off:
    print(line)
