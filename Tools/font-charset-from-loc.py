# Writes unique characters from localization tables for TMP Font Asset Creator.
# Usage: python Tools/font-charset-from-loc.py
from pathlib import Path

root = Path(__file__).resolve().parents[1]
loc_dir = root / "Assets/Resources/Localization"
out_dir = root / "Tools/font-charsets"
out_dir.mkdir(parents=True, exist_ok=True)

groups = {
    "latin": ["en", "es", "tl"],
    "zh-Hans": ["zh-Hans"],
    "zh-Hant": ["zh-Hant"],
    "all": ["en", "es", "tl", "zh-Hans", "zh-Hant"],
}

for name, codes in groups.items():
    chars = set()
    for code in codes:
        path = loc_dir / f"{code}.txt"
        if not path.exists():
            continue
        for line in path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            _, value = line.split("=", 1)
            chars.update(value)
    ordered = "".join(sorted(chars, key=ord))
    (out_dir / f"{name}.txt").write_text(ordered + "\n", encoding="utf-8")
    print(f"{name}: {len(ordered)} unique chars -> {out_dir / (name + '.txt')}")
