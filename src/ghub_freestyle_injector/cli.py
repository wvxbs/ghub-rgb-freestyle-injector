"""Command-line interface."""

from __future__ import annotations

from pathlib import Path
import argparse
import sys

from .ghub import (
    DEFAULT_PREFIX,
    backup_lghub_db,
    default_db_path,
    integrity_check,
    kill_ghub,
    list_db_presets,
    sync_palettes,
)
from .palettes import discover_palettes
from .state import changed_palette_ids, default_state_path, load_state, save_state, update_state


def positive_path(value: str) -> Path:
    return Path(value).expanduser()


def add_common_paths(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--input",
        "-i",
        type=positive_path,
        default=Path.cwd(),
        help="Diretório com palettes_codex_ready.json ou arquivos .md.",
    )
    parser.add_argument(
        "--db",
        type=positive_path,
        default=default_db_path(),
        help="Caminho do settings.db do Logitech G HUB.",
    )
    parser.add_argument(
        "--prefix",
        default=DEFAULT_PREFIX,
        help="Prefixo dos presets gerenciados.",
    )


def cmd_list(args: argparse.Namespace) -> int:
    palettes = discover_palettes(args.input)
    print(f"Paletas detectadas em {args.input}: {len(palettes)}")
    for palette in palettes:
        print(f"- {palette.id}: {palette.preset_name} ({palette.base_mode or 'sem modo'})")

    if args.db.exists():
        presets = list_db_presets(args.db, args.prefix if args.managed_only else None)
        print(f"\nPresets no G HUB ({args.db}): {len(presets)}")
        for preset in presets:
            print(f"- {preset.get('name')} [{preset.get('id')}]")
    else:
        print(f"\nsettings.db não encontrado em {args.db}")
    return 0


def cmd_sync(args: argparse.Namespace) -> int:
    if args.kill_ghub:
        kill_ghub()

    if not args.db.exists():
        print(f"settings.db não encontrado: {args.db}", file=sys.stderr)
        return 2

    palettes = discover_palettes(args.input)
    if not palettes:
        print(f"Nenhuma paleta encontrada em {args.input}", file=sys.stderr)
        return 3

    state_path = args.state or default_state_path(args.input)
    state = load_state(state_path)
    changed = changed_palette_ids(palettes, state, force=args.force)

    if args.dry_run:
        print(f"Paletas detectadas: {len(palettes)}")
        print(f"Alteradas/novas pelo estado: {len(changed)}")
        for palette in palettes:
            marker = "UPDATE" if palette.id in changed else "SKIP"
            print(f"- {marker}: {palette.preset_name}")
        return 0

    backup_dir = backup_lghub_db(args.db, args.backup_dir)
    result = sync_palettes(args.db, palettes, changed, prefix=args.prefix, prune=args.prune)
    save_state(state_path, update_state(state, palettes))
    check = integrity_check(args.db)

    print(f"Backup criado em: {backup_dir}")
    print(f"Integridade SQLite: {check}")
    print(f"Atualizados/criados: {len(result['updated'])}")
    for name in result["updated"]:
        print(f"- {name}")
    print(f"Ignorados sem mudança: {len(result['skipped'])}")
    if args.prune:
        print(f"Removidos por prune: {len(result['removed'])}")
        for name in result["removed"]:
            print(f"- {name}")
    print(f"Estado salvo em: {state_path}")
    return 0 if check == "ok" else 4


def cmd_kill_ghub(_: argparse.Namespace) -> int:
    code = kill_ghub()
    print("Processos do G HUB encerrados ou já ausentes.")
    return 0 if code in (0, 128) else code


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="ghub-freestyle",
        description="Sincroniza paletas RGB como presets Freestyle do Logitech G HUB.",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    list_parser = sub.add_parser("list", help="Lista paletas de entrada e presets do G HUB.")
    add_common_paths(list_parser)
    list_parser.add_argument("--managed-only", action="store_true", help="Mostra só presets com o prefixo gerenciado.")
    list_parser.set_defaults(func=cmd_list)

    sync_parser = sub.add_parser("sync", help="Sincroniza paletas no G HUB.")
    add_common_paths(sync_parser)
    sync_parser.add_argument("--state", type=positive_path, help="Arquivo de estado; padrão: dentro do diretório de entrada.")
    sync_parser.add_argument("--backup-dir", type=positive_path, help="Diretório onde backups do settings.db serão criados.")
    sync_parser.add_argument("--dry-run", action="store_true", help="Mostra o que seria feito sem alterar o G HUB.")
    sync_parser.add_argument("--force", action="store_true", help="Regrava todos os presets detectados.")
    sync_parser.add_argument("--prune", action="store_true", help="Remove presets gerenciados que não existem mais na entrada.")
    sync_parser.add_argument("--kill-ghub", action="store_true", help="Encerra o G HUB antes de sincronizar.")
    sync_parser.set_defaults(func=cmd_sync)

    kill_parser = sub.add_parser("kill-ghub", help="Encerra processos do Logitech G HUB.")
    kill_parser.set_defaults(func=cmd_kill_ghub)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return int(args.func(args))


if __name__ == "__main__":
    raise SystemExit(main())
