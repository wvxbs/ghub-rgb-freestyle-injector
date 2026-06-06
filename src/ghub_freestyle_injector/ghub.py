"""Read and update Logitech G HUB settings.db."""

from __future__ import annotations

from collections.abc import Iterable
from pathlib import Path
import json
import os
import shutil
import sqlite3
import subprocess
import tempfile
from datetime import datetime
from uuid import NAMESPACE_URL, uuid5

from .keyboard import ALL_CODES, apply_codes, codes_for_key, codes_for_zone
from .palettes import Palette


DEFAULT_PREFIX = "RGB - "
DEVICE_SUPPORT = ["KEYBOARD_RGB_PER_KEY"]


def default_lghub_dir() -> Path:
    override = os.environ.get("LGHUB_DIR")
    if override:
        return Path(override).expanduser()

    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        return Path(local_app_data) / "LGHUB"

    win_user = os.environ.get("WINUSER") or os.environ.get("USERPROFILE", "").split("\\")[-1]
    candidates = []
    if win_user:
        candidates.append(Path(f"/mnt/c/Users/{win_user}/AppData/Local/LGHUB"))
    candidates += sorted(Path("/mnt/c/Users").glob("*/AppData/Local/LGHUB")) if Path("/mnt/c/Users").exists() else []
    for candidate in candidates:
        if (candidate / "settings.db").exists():
            return candidate

    return Path("/mnt/c/Users/gabri/AppData/Local/LGHUB")


def default_db_path() -> Path:
    return default_lghub_dir() / "settings.db"


def deterministic_effect_id(palette_id: str) -> str:
    return str(uuid5(NAMESPACE_URL, f"ghub-rgb-freestyle-injector:{palette_id}"))


def make_effect(palette: Palette) -> dict:
    color_map = {str(code): {"hex": palette.base_color.lower()} for code in ALL_CODES}
    for zone, color in palette.zones.items():
        try:
            apply_codes(color_map, codes_for_zone(zone), color)
        except KeyError:
            continue
    for key, color in palette.exact_key_overrides.items():
        try:
            apply_codes(color_map, codes_for_key(key), color)
        except KeyError:
            continue

    return {
        "deviceSupport": DEVICE_SUPPORT,
        "fixedInfo": {
            "lightingSlots": {
                "infoMap": {
                    "PERKEY_KEYBOARD": {
                        "perKeyMap": {
                            "colorCodeMap": color_map,
                        }
                    }
                }
            }
        },
    }


def backup_lghub_db(db_path: Path, backup_root: Path | None = None) -> Path:
    db_path = db_path.resolve()
    backup_root = backup_root or db_path.parent / "ghub-freestyle-backups"
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    backup_dir = backup_root / stamp
    backup_dir.mkdir(parents=True, exist_ok=True)
    for path in db_path.parent.glob(f"{db_path.name}*"):
        if path.is_file():
            shutil.copy2(path, backup_dir / path.name)
    return backup_dir


def load_settings(db_path: Path) -> tuple[sqlite3.Connection, dict]:
    con = sqlite3.connect(db_path)
    row = con.execute("select file from data where _id=1").fetchone()
    if not row:
        raise RuntimeError("settings.db não contém data._id=1")
    blob = row[0]
    if isinstance(blob, str):
        raw = blob.encode("utf-8")
    else:
        raw = bytes(blob)
    return con, json.loads(raw.decode("utf-8"))


def save_settings(con: sqlite3.Connection, settings: dict) -> None:
    payload = json.dumps(settings, ensure_ascii=False, indent=2).encode("utf-8")
    con.execute("update data set file=?, _date_created=current_timestamp where _id=1", (payload,))
    con.commit()
    con.close()


def managed_prefabs(settings: dict, prefix: str = DEFAULT_PREFIX) -> list[dict]:
    prefabs = settings.get("lighting_prefabs", {}).get("list", [])
    return [p for p in prefabs if p.get("name", "").startswith(prefix)]


def list_db_presets(db_path: Path, prefix: str | None = None) -> list[dict]:
    try:
        con, settings = load_settings(db_path)
    except sqlite3.OperationalError:
        with tempfile.TemporaryDirectory(prefix="ghub-freestyle-db-") as tmp:
            tmp_db = Path(tmp) / db_path.name
            for path in db_path.parent.glob(f"{db_path.name}*"):
                if path.is_file():
                    shutil.copy2(path, Path(tmp) / path.name)
            con, settings = load_settings(tmp_db)
    con.close()
    prefabs = settings.get("lighting_prefabs", {}).get("list", [])
    if prefix is not None:
        prefabs = [p for p in prefabs if p.get("name", "").startswith(prefix)]
    return sorted(prefabs, key=lambda p: p.get("name", "").lower())


def sync_palettes(
    db_path: Path,
    palettes: Iterable[Palette],
    changed_ids: set[str],
    *,
    prefix: str = DEFAULT_PREFIX,
    prune: bool = False,
) -> dict:
    con, settings = load_settings(db_path)
    prefabs = settings.setdefault("lighting_prefabs", {}).setdefault("list", [])
    palette_by_id = {palette.id: palette for palette in palettes}
    existing_by_id = {p.get("id"): p for p in prefabs}
    updated: list[str] = []
    skipped: list[str] = []

    for palette in palette_by_id.values():
        effect_id = deterministic_effect_id(palette.id)
        effect_key = f"lighting_effects/{effect_id}"
        exists = effect_key in settings and effect_id in existing_by_id
        if palette.id not in changed_ids and exists:
            skipped.append(palette.preset_name)
            continue

        settings[effect_key] = make_effect(palette)
        prefab = {
            "deviceSupport": DEVICE_SUPPORT,
            "id": effect_id,
            "name": palette.preset_name,
            "type": "FIXED",
        }
        if effect_id in existing_by_id:
            existing_by_id[effect_id].update(prefab)
        else:
            prefabs.append(prefab)
        updated.append(palette.preset_name)

    removed: list[str] = []
    if prune:
        keep_effect_ids = {deterministic_effect_id(palette.id) for palette in palette_by_id.values()}
        new_prefabs = []
        for prefab in prefabs:
            name = prefab.get("name", "")
            effect_id = prefab.get("id")
            if name.startswith(prefix) and effect_id not in keep_effect_ids:
                settings.pop(f"lighting_effects/{effect_id}", None)
                removed.append(name)
            else:
                new_prefabs.append(prefab)
        settings["lighting_prefabs"]["list"] = new_prefabs

    save_settings(con, settings)
    return {"updated": updated, "skipped": skipped, "removed": removed}


def integrity_check(db_path: Path) -> str:
    con = sqlite3.connect(db_path)
    try:
        return str(con.execute("pragma integrity_check").fetchone()[0])
    finally:
        con.close()


def kill_ghub() -> int:
    names = ["lghub.exe", "lghub_agent.exe", "lghub_updater.exe"]
    if Path("/mnt/c/Windows/System32/taskkill.exe").exists():
        code = 0
        for name in names:
            result = subprocess.run(
                ["/mnt/c/Windows/System32/taskkill.exe", "/IM", name, "/F"],
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                check=False,
            )
            if result.returncode not in (0, 128):
                code = result.returncode
        return code

    script = "Stop-Process -Name lghub,lghub_agent,lghub_updater -Force -ErrorAction SilentlyContinue"
    result = subprocess.run(["powershell.exe", "-NoProfile", "-Command", script], check=False)
    return result.returncode
