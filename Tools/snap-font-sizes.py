# Snap TMP font sizes in prefabs (direct fields + PrefabInstance overrides) to multiples of 12.
from pathlib import Path
import re

root = Path(r"C:\Users\joshu\Desktop\GameDev\Unity\ModularChess\Assets")
paths = list(root.rglob("*.prefab")) + list(root.rglob("*.unity"))

direct_keys = ("m_fontSize:", "m_fontSizeBase:", "m_fontSizeMin:", "m_fontSizeMax:", "m_FontSize:")


def snap(n: float) -> float:
    if n <= 0:
        return n
    lo = (int(n) // 12) * 12
    hi = lo + 12
    if lo == 0:
        lo = 12
    if abs(n - lo) < abs(n - hi):
        return float(lo)
    if abs(n - hi) < abs(n - lo):
        return float(hi)
    return float(hi)


def fmt(raw: str, nxt: float) -> str:
    return str(int(nxt)) if "." not in raw else f"{nxt:g}"


changed = []
report = []

for path in paths:
    text = path.read_text(encoding="utf-8")
    original = text

    def repl_direct(m):
        key, raw = m.group(1), m.group(2)
        val = float(raw)
        if val <= 0 or abs(val % 12) < 0.001:
            return m.group(0)
        nxt = snap(val)
        report.append(f"{path.relative_to(root)}: {key} {raw} -> {fmt(raw, nxt)}")
        return f"{key} {fmt(raw, nxt)}"

    text = re.sub(
        r"(" + "|".join(re.escape(k) for k in direct_keys) + r")\s*([0-9]+(?:\.[0-9]+)?)",
        repl_direct,
        text,
    )

    # PrefabInstance override blocks:
    # - target: ...
    #   propertyPath: m_fontSize
    #   value: 32
    override_pat = re.compile(
        r"(propertyPath: m_fontSize(?:Base|Min|Max)?\n\s+value: )([0-9]+(?:\.[0-9]+)?)",
        re.MULTILINE,
    )

    def repl_override(m):
        prefix, raw = m.group(1), m.group(2)
        val = float(raw)
        if val <= 0 or abs(val % 12) < 0.001:
            return m.group(0)
        nxt = snap(val)
        report.append(f"{path.relative_to(root)}: override {raw} -> {fmt(raw, nxt)}")
        return f"{prefix}{fmt(raw, nxt)}"

    text = override_pat.sub(repl_override, text)

    if text != original:
        path.write_text(text, encoding="utf-8")
        changed.append(path)

print(f"Updated {len(changed)} files, {len(report)} sizes")
for line in report:
    print(line)
