"""Sync state stored next to the palette sources by default."""

from __future__ import annotations

from pathlib import Path
import json

from .palettes import Palette


STATE_FILE = ".ghub-freestyle-injector-state.json"


def default_state_path(input_dir: Path) -> Path:
    return input_dir / STATE_FILE


def load_state(path: Path) -> dict:
    if not path.exists():
        return {"version": 1, "palettes": {}}
    return json.loads(path.read_text(encoding="utf-8"))


def save_state(path: Path, state: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def changed_palette_ids(palettes: list[Palette], state: dict, force: bool = False) -> set[str]:
    if force:
        return {palette.id for palette in palettes}
    known = state.get("palettes", {})
    changed: set[str] = set()
    for palette in palettes:
        previous = known.get(palette.id, {})
        if previous.get("source_hash") != palette.source_hash:
            changed.add(palette.id)
    return changed


def update_state(state: dict, palettes: list[Palette]) -> dict:
    state.setdefault("version", 1)
    state["palettes"] = {
        palette.id: {
            "title": palette.title,
            "source": palette.source,
            "source_hash": palette.source_hash,
        }
        for palette in palettes
    }
    return state
