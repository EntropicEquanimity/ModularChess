import re
from pathlib import Path

root = Path(r"C:\Users\joshu\Desktop\GameDev\Unity\ModularChess")
loc_dir = root / "Assets/Resources/Localization"
scripts = list((root / "Assets/Scripts").rglob("*.cs"))

keys = set()
loc_like = re.compile(
    r'"((?:menu|play|settings|mode|unlocks|options|match|side|result|hud|'
    r'history|replay|martyr|promotion|piece|join|lobby|account|survey|'
    r'lang|credits|campaign|debug|quit)\.[a-zA-Z0-9_.]+)"'
)
for p in scripts:
    text = p.read_text(encoding="utf-8")
    for m in loc_like.finditer(text):
        keys.add(m.group(1))


def parse(path: Path):
    d = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        k, v = line.split("=", 1)
        d[k] = v
    return d


tables = {f.stem: parse(f) for f in loc_dir.glob("*.txt")}
en = tables["en"]

print("=== Keys in code missing from en.txt ===")
missing_en = sorted(k for k in keys if k not in en)
for k in missing_en:
    print(k)
print("count", len(missing_en))
print()

for lang, table in sorted(tables.items()):
    if lang == "en":
        continue
    miss = sorted(k for k in en if k not in table)
    print(f"--- {lang}: {len(miss)} missing ---")
    for k in miss:
        print(" ", k)
