from pathlib import Path
import json
import sqlite3
import unittest

from ghub_freestyle_injector.ghub import list_db_presets, sync_palettes
from ghub_freestyle_injector.palettes import discover_palettes


def make_db(path: Path) -> None:
    con = sqlite3.connect(path)
    con.execute("create table data(_id integer primary key, _date_created datetime default current_timestamp, file blob not null)")
    con.execute("create table snapshots(_id integer primary key, _date_created datetime default current_timestamp, uuid text not null, label text not null, file blob not null)")
    payload = {
        "lighting_prefabs": {"list": []},
    }
    con.execute("insert into data(_id, file) values(1, ?)", (json.dumps(payload).encode("utf-8"),))
    con.commit()
    con.close()


class PaletteSyncTest(unittest.TestCase):
    def test_markdown_palette_sync_is_idempotent(self) -> None:
        import tempfile

        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            input_dir = tmp_path / "input"
            input_dir.mkdir()
            (input_dir / "valorant.md").write_text(
                """# Valorant

**Modo:** `keyboard_wasd`

## Aplicação

```text
SET_ALL_KEYS #6B3CFF
SET_KEY ESC #FF8A1F
```

## Zonas

| Zona | Cor |
| --- | --- |
| modifiers | #3B2A7A |

## Teclas exatas

| Tecla | Cor |
| --- | --- |
| W | #48D7FF |
| ESC | #FF8A1F |
""",
                encoding="utf-8",
            )
            db = tmp_path / "settings.db"
            make_db(db)

            palettes = discover_palettes(input_dir)
            self.assertEqual(len(palettes), 1)
            result = sync_palettes(db, palettes, {"valorant"})
            self.assertEqual(result["updated"], ["RGB - Valorant"])
            self.assertEqual(len(list_db_presets(db, "RGB - ")), 1)

            result = sync_palettes(db, palettes, set())
            self.assertEqual(result["updated"], [])
            self.assertEqual(result["skipped"], ["RGB - Valorant"])
            self.assertEqual(len(list_db_presets(db, "RGB - ")), 1)


if __name__ == "__main__":
    unittest.main()
