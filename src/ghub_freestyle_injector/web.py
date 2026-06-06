"""Tiny standard-library web UI for container/dev usage."""

from __future__ import annotations

from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import html
import json
import os
import urllib.parse

from .ghub import default_db_path, kill_ghub
from .service import list_summary, sync_with_logs


PAGE = """<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>G HUB RGB Freestyle Injector</title>
  <style>
    :root { color-scheme: light dark; font-family: "Segoe UI", system-ui, sans-serif; }
    body { margin: 0; background: #f5f6f8; color: #17191f; }
    main { max-width: 1040px; margin: 0 auto; padding: 28px; }
    h1 { margin: 0 0 18px; font-size: 28px; }
    .panel { background: white; border: 1px solid #dde1e8; border-radius: 10px; padding: 18px; box-shadow: 0 8px 28px rgba(20, 30, 50, .08); }
    label { display: block; font-weight: 600; margin: 12px 0 6px; }
    input[type=text] { width: 100%; box-sizing: border-box; padding: 10px 12px; border-radius: 8px; border: 1px solid #bcc4d2; font: inherit; }
    .row { display: flex; gap: 14px; flex-wrap: wrap; align-items: center; margin: 14px 0; }
    button { border: 0; border-radius: 8px; padding: 10px 14px; font: inherit; cursor: pointer; background: #1f6feb; color: white; }
    button.secondary { background: #3b4252; }
    button.danger { background: #c62828; }
    .checks label { display: inline-flex; gap: 6px; align-items: center; margin-right: 18px; font-weight: 500; }
    pre { white-space: pre-wrap; min-height: 260px; background: #10131a; color: #dbe7ff; border-radius: 10px; padding: 14px; overflow: auto; }
    @media (prefers-color-scheme: dark) {
      body { background: #111318; color: #eef1f6; }
      .panel { background: #1a1d24; border-color: #303641; box-shadow: none; }
      input[type=text] { background: #111318; color: #eef1f6; border-color: #3a4352; }
    }
  </style>
</head>
<body>
<main>
  <h1>G HUB RGB Freestyle Injector</h1>
  <section class="panel">
    <label>Pasta das paletas</label>
    <input id="input" type="text" value="__INPUT_DIR__">
    <label>settings.db do G HUB</label>
    <input id="db" type="text" value="__DB_PATH__">
    <label>Arquivo de estado opcional</label>
    <input id="state" type="text" value="__STATE_PATH__">
    <div class="checks">
      <label><input id="force" type="checkbox"> Forçar regravação</label>
      <label><input id="prune" type="checkbox"> Remover órfãos</label>
      <label><input id="killFirst" type="checkbox"> Encerrar G HUB antes</label>
    </div>
    <div class="row">
      <button onclick="run('list')">Listar</button>
      <button class="secondary" onclick="run('dry-run')">Simular</button>
      <button onclick="run('sync')">Aplicar</button>
      <button class="danger" onclick="run('kill')">Encerrar G HUB</button>
    </div>
    <pre id="log">Pronto.</pre>
  </section>
</main>
<script>
function payload() {
  return {
    input: document.getElementById('input').value,
    db: document.getElementById('db').value,
    state: document.getElementById('state').value,
    force: document.getElementById('force').checked,
    prune: document.getElementById('prune').checked,
    kill_first: document.getElementById('killFirst').checked
  };
}
async function run(action) {
  const log = document.getElementById('log');
  log.textContent = 'Rodando...';
  const res = await fetch('/api/' + action, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify(payload())
  });
  const data = await res.json();
  log.textContent = data.log || JSON.stringify(data, null, 2);
}
</script>
</body>
</html>
"""


class Handler(BaseHTTPRequestHandler):
    server_version = "GHubFreestyleWeb/0.1"

    def _json(self, payload: dict, status: int = 200) -> None:
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def _read_payload(self) -> dict:
        length = int(self.headers.get("Content-Length", "0"))
        if not length:
            return {}
        return json.loads(self.rfile.read(length).decode("utf-8"))

    def do_GET(self) -> None:  # noqa: N802
        if urllib.parse.urlparse(self.path).path != "/":
            self.send_error(404)
            return
        input_dir = os.environ.get("INPUT_DIR", "/input")
        db_path = os.environ.get("LGHUB_DB", str(default_db_path()))
        state_path = os.environ.get("STATE_PATH", "")
        data = (
            PAGE.replace("__INPUT_DIR__", html.escape(input_dir))
            .replace("__DB_PATH__", html.escape(db_path))
            .replace("__STATE_PATH__", html.escape(state_path))
        ).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_POST(self) -> None:  # noqa: N802
        path = urllib.parse.urlparse(self.path).path
        payload = self._read_payload()
        logs: list[str] = []
        try:
            input_dir = Path(payload.get("input") or os.environ.get("INPUT_DIR", "/input"))
            db = Path(payload.get("db") or os.environ.get("LGHUB_DB", str(default_db_path())))
            state_raw = payload.get("state") or os.environ.get("STATE_PATH", "")
            state = Path(state_raw) if state_raw else None

            if path == "/api/list":
                summary = list_summary(input_dir, db)
                logs.append(f"Paletas detectadas: {len(summary['palettes'])}")
                logs.extend(f"- {palette.id}: {palette.preset_name}" for palette in summary["palettes"])
                logs.append("")
                logs.append(f"Presets RGB no G HUB: {len(summary['presets'])}")
                logs.extend(f"- {preset.get('name')}" for preset in summary["presets"])
            elif path in {"/api/sync", "/api/dry-run"}:
                sync_with_logs(
                    input_dir,
                    db,
                    state_path=state,
                    dry_run=path == "/api/dry-run",
                    force=bool(payload.get("force")),
                    prune=bool(payload.get("prune")),
                    kill_first=bool(payload.get("kill_first")),
                    log=logs.append,
                )
                logs.append("Concluído.")
            elif path == "/api/kill":
                code = kill_ghub()
                logs.append(f"Comando de encerramento finalizado com código {code}.")
            else:
                self.send_error(404)
                return
            self._json({"ok": True, "log": "\n".join(logs)})
        except Exception as exc:
            logs.append(f"ERRO: {exc}")
            self._json({"ok": False, "log": "\n".join(logs)}, status=500)


def main() -> int:
    host = os.environ.get("HOST", "0.0.0.0")
    port = int(os.environ.get("PORT", "8080"))
    server = ThreadingHTTPServer((host, port), Handler)
    print(f"G HUB RGB Freestyle web UI em http://{host}:{port}", flush=True)
    server.serve_forever()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
