"""Palette discovery and parsing."""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
import hashlib
import re


HEX_RE = re.compile(r"#(?:[0-9a-fA-F]{6})")


@dataclass(slots=True)
class Palette:
    id: str
    title: str
    base_color: str
    esc_color: str | None = None
    base_mode: str | None = None
    zones: dict[str, str] = field(default_factory=dict)
    exact_key_overrides: dict[str, str] = field(default_factory=dict)
    source: str = ""
    source_hash: str = ""

    @property
    def preset_name(self) -> str:
        return f"RGB - {self.title}"


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def normalize_hex(value: str | None) -> str | None:
    if not value:
        return None
    match = HEX_RE.search(value)
    return match.group(0).upper() if match else None


def slugify(text: str) -> str:
    text = text.lower()
    replacements = {
        "—": "-",
        "–": "-",
        "ã": "a",
        "á": "a",
        "à": "a",
        "â": "a",
        "é": "e",
        "ê": "e",
        "í": "i",
        "ó": "o",
        "ô": "o",
        "ú": "u",
        "ç": "c",
    }
    for old, new in replacements.items():
        text = text.replace(old, new)
    text = re.sub(r"[^a-z0-9]+", "_", text)
    return text.strip("_")


def parse_markdown_table(lines: list[str], heading_index: int) -> list[tuple[str, str]]:
    rows: list[tuple[str, str]] = []
    i = heading_index + 1
    while i < len(lines) and not lines[i].lstrip().startswith("|"):
        i += 1
    while i < len(lines) and lines[i].lstrip().startswith("|"):
        line = lines[i].strip()
        i += 1
        if re.match(r"^\|\s*-", line):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip("|").split("|")]
        if len(cells) < 2:
            continue
        if cells[0].lower() in {"tecla", "zona", "região", "regiao"}:
            continue
        color = normalize_hex(cells[1])
        if color:
            rows.append((cells[0], color))
    return rows


def load_markdown_palette(path: Path) -> Palette | None:
    raw = path.read_bytes()
    text = raw.decode("utf-8-sig")
    title_match = re.search(r"(?m)^#\s+(.+?)\s*$", text)
    if not title_match:
        return None

    title = title_match.group(1).strip()
    mode_match = re.search(r"\*\*Modo:\*\*\s*`?([^`\n]+)`?", text)
    base_match = re.search(r"SET_ALL_KEYS\s+(#[0-9a-fA-F]{6})", text)
    esc_match = re.search(r"SET_KEY\s+ESC\s+(#[0-9a-fA-F]{6})", text)
    if not base_match:
        plan_match = re.search(r"Aplicar no teclado inteiro:\s*`?(#[0-9a-fA-F]{6})`?", text)
        base_match = plan_match
    if not base_match:
        return None

    lines = text.splitlines()
    zones: dict[str, str] = {}
    exact: dict[str, str] = {}
    for index, line in enumerate(lines):
        if re.match(r"^##\s+Zonas\b", line):
            zones.update(parse_markdown_table(lines, index))
        if re.match(r"^##\s+Teclas exatas\b", line) or re.match(r"^###\s+Teclas exatas\b", line):
            exact.update(parse_markdown_table(lines, index))

    esc_color = normalize_hex(esc_match.group(1) if esc_match else exact.get("ESC"))
    if esc_color and "ESC" not in exact:
        exact["ESC"] = esc_color

    return Palette(
        id=slugify(path.stem),
        title=title,
        base_color=normalize_hex(base_match.group(1)) or "#00FFFF",
        esc_color=esc_color,
        base_mode=mode_match.group(1).strip() if mode_match else None,
        zones=zones,
        exact_key_overrides=exact,
        source=str(path),
        source_hash=sha256_bytes(raw),
    )


def discover_palettes(input_dir: Path) -> list[Palette]:
    input_dir = input_dir.resolve()
    candidates = sorted(input_dir.rglob("*.md"))
    palettes: list[Palette] = []
    for path in candidates:
        if path.name.lower() in {"readme.md", "codex_apply_rules.md"}:
            continue
        palette = load_markdown_palette(path)
        if palette:
            palettes.append(palette)
    return palettes
