"""Reusable operations shared by the CLI, desktop GUI, and web UI."""

from __future__ import annotations

from collections.abc import Callable
from pathlib import Path

from .ghub import backup_lghub_db, integrity_check, kill_ghub, list_db_presets, sync_palettes
from .palettes import discover_palettes
from .state import changed_palette_ids, default_state_path, load_state, save_state, update_state

Log = Callable[[str], None]


def list_summary(input_dir: Path, db_path: Path, *, managed_only: bool = True, prefix: str = "RGB - ") -> dict:
    palettes = discover_palettes(input_dir)
    presets = list_db_presets(db_path, prefix if managed_only else None) if db_path.exists() else []
    return {"palettes": palettes, "presets": presets}


def sync_with_logs(
    input_dir: Path,
    db_path: Path,
    *,
    state_path: Path | None = None,
    backup_dir: Path | None = None,
    prefix: str = "RGB - ",
    dry_run: bool = False,
    force: bool = False,
    prune: bool = False,
    kill_first: bool = False,
    log: Log | None = None,
) -> dict:
    def emit(message: str) -> None:
        if log:
            log(message)

    input_dir = input_dir.expanduser().resolve()
    db_path = db_path.expanduser()
    state_path = state_path or default_state_path(input_dir)

    emit(f"Entrada: {input_dir}")
    emit(f"Banco do G HUB: {db_path}")
    palettes = discover_palettes(input_dir)
    emit(f"Paletas detectadas: {len(palettes)}")
    if not palettes:
        return {"palettes": palettes, "updated": [], "skipped": [], "removed": [], "changed": set()}

    state = load_state(state_path)
    changed = changed_palette_ids(palettes, state, force=force)
    emit(f"Novas/alteradas pelo estado: {len(changed)}")

    for palette in palettes:
        marker = "UPDATE" if palette.id in changed else "SKIP"
        emit(f"{marker}: {palette.preset_name}")

    if dry_run:
        emit("Dry-run concluído; nada foi gravado.")
        return {"palettes": palettes, "updated": [], "skipped": [], "removed": [], "changed": changed}

    if kill_first:
        emit("Encerrando processos do G HUB...")
        kill_ghub()

    if not db_path.exists():
        raise FileNotFoundError(f"settings.db não encontrado: {db_path}")

    backup = backup_lghub_db(db_path, backup_dir)
    emit(f"Backup criado em: {backup}")
    result = sync_palettes(db_path, palettes, changed, prefix=prefix, prune=prune)
    save_state(state_path, update_state(state, palettes))
    check = integrity_check(db_path)
    emit(f"Integridade SQLite: {check}")
    emit(f"Atualizados/criados: {len(result['updated'])}")
    emit(f"Ignorados sem mudança: {len(result['skipped'])}")
    if prune:
        emit(f"Removidos por prune: {len(result['removed'])}")
    emit(f"Estado salvo em: {state_path}")
    return {**result, "palettes": palettes, "changed": changed, "backup": backup, "integrity": check}
