"""Small native Windows GUI for the injector."""

from __future__ import annotations

from pathlib import Path
import queue
import threading
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

from .ghub import default_db_path, kill_ghub
from .service import list_summary, sync_with_logs


class App(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title("G HUB RGB Freestyle Injector")
        self.geometry("900x620")
        self.minsize(760, 520)
        self.log_queue: queue.Queue[str] = queue.Queue()

        self.input_dir = tk.StringVar(value=str(Path.home()))
        self.db_path = tk.StringVar(value=str(default_db_path()))
        self.state_path = tk.StringVar(value="")
        self.force = tk.BooleanVar(value=False)
        self.prune = tk.BooleanVar(value=False)
        self.kill_first = tk.BooleanVar(value=True)

        self._configure_style()
        self._build()
        self.after(100, self._drain_log)

    def _configure_style(self) -> None:
        style = ttk.Style(self)
        if "vista" in style.theme_names():
            style.theme_use("vista")
        elif "clam" in style.theme_names():
            style.theme_use("clam")
        style.configure("Accent.TButton", padding=(14, 8))
        style.configure("TButton", padding=(10, 6))
        style.configure("TEntry", padding=4)

    def _build(self) -> None:
        root = ttk.Frame(self, padding=18)
        root.pack(fill=tk.BOTH, expand=True)
        root.columnconfigure(1, weight=1)
        root.rowconfigure(5, weight=1)

        title = ttk.Label(root, text="G HUB RGB Freestyle Injector", font=("Segoe UI", 17, "bold"))
        title.grid(row=0, column=0, columnspan=3, sticky="w", pady=(0, 14))

        self._path_row(root, 1, "Pasta das paletas", self.input_dir, self._pick_input)
        self._path_row(root, 2, "settings.db do G HUB", self.db_path, self._pick_db)
        self._path_row(root, 3, "Estado opcional", self.state_path, self._pick_state)

        options = ttk.Frame(root)
        options.grid(row=4, column=0, columnspan=3, sticky="ew", pady=(8, 12))
        ttk.Checkbutton(options, text="Encerrar G HUB antes de aplicar", variable=self.kill_first).pack(side=tk.LEFT)
        ttk.Checkbutton(options, text="Forçar regravação", variable=self.force).pack(side=tk.LEFT, padx=(18, 0))
        ttk.Checkbutton(options, text="Remover presets órfãos", variable=self.prune).pack(side=tk.LEFT, padx=(18, 0))

        actions = ttk.Frame(root)
        actions.grid(row=5, column=0, sticky="nsw", padx=(0, 12))
        ttk.Button(actions, text="Listar", command=self.list_palettes).pack(fill=tk.X, pady=(0, 8))
        ttk.Button(actions, text="Simular", command=lambda: self.run_sync(dry_run=True)).pack(fill=tk.X, pady=(0, 8))
        ttk.Button(actions, text="Aplicar", style="Accent.TButton", command=lambda: self.run_sync(dry_run=False)).pack(fill=tk.X, pady=(0, 8))
        ttk.Button(actions, text="Encerrar G HUB", command=self.kill_ghub).pack(fill=tk.X, pady=(0, 8))

        log_frame = ttk.Frame(root)
        log_frame.grid(row=5, column=1, columnspan=2, sticky="nsew")
        log_frame.rowconfigure(0, weight=1)
        log_frame.columnconfigure(0, weight=1)
        self.log = tk.Text(log_frame, wrap=tk.WORD, font=("Consolas", 10), height=18)
        scroll = ttk.Scrollbar(log_frame, orient=tk.VERTICAL, command=self.log.yview)
        self.log.configure(yscrollcommand=scroll.set)
        self.log.grid(row=0, column=0, sticky="nsew")
        scroll.grid(row=0, column=1, sticky="ns")

    def _path_row(self, parent: ttk.Frame, row: int, label: str, variable: tk.StringVar, command) -> None:
        ttk.Label(parent, text=label).grid(row=row, column=0, sticky="w", pady=4)
        ttk.Entry(parent, textvariable=variable).grid(row=row, column=1, sticky="ew", padx=8, pady=4)
        ttk.Button(parent, text="Escolher", command=command).grid(row=row, column=2, sticky="e", pady=4)

    def _pick_input(self) -> None:
        path = filedialog.askdirectory(title="Escolha a pasta com as paletas")
        if path:
            self.input_dir.set(path)

    def _pick_db(self) -> None:
        path = filedialog.askopenfilename(title="Escolha o settings.db", filetypes=[("SQLite", "*.db"), ("Todos", "*.*")])
        if path:
            self.db_path.set(path)

    def _pick_state(self) -> None:
        path = filedialog.asksaveasfilename(title="Escolha o arquivo de estado", defaultextension=".json")
        if path:
            self.state_path.set(path)

    def write_log(self, message: str) -> None:
        self.log_queue.put(message)

    def _drain_log(self) -> None:
        while True:
            try:
                message = self.log_queue.get_nowait()
            except queue.Empty:
                break
            self.log.insert(tk.END, message + "\n")
            self.log.see(tk.END)
        self.after(100, self._drain_log)

    def clear_log(self) -> None:
        self.log.delete("1.0", tk.END)

    def list_palettes(self) -> None:
        self.clear_log()
        try:
            summary = list_summary(Path(self.input_dir.get()), Path(self.db_path.get()))
            self.write_log(f"Paletas detectadas: {len(summary['palettes'])}")
            for palette in summary["palettes"]:
                self.write_log(f"- {palette.id}: {palette.preset_name}")
            self.write_log("")
            self.write_log(f"Presets RGB no G HUB: {len(summary['presets'])}")
            for preset in summary["presets"]:
                self.write_log(f"- {preset.get('name')}")
        except Exception as exc:
            messagebox.showerror("Erro ao listar", str(exc))

    def run_sync(self, *, dry_run: bool) -> None:
        self.clear_log()
        state = Path(self.state_path.get()) if self.state_path.get().strip() else None

        def worker() -> None:
            try:
                sync_with_logs(
                    Path(self.input_dir.get()),
                    Path(self.db_path.get()),
                    state_path=state,
                    dry_run=dry_run,
                    force=self.force.get(),
                    prune=self.prune.get(),
                    kill_first=self.kill_first.get(),
                    log=self.write_log,
                )
                self.write_log("Concluído.")
            except Exception as exc:
                self.write_log(f"ERRO: {exc}")
                self.after(0, lambda: messagebox.showerror("Erro ao sincronizar", str(exc)))

        threading.Thread(target=worker, daemon=True).start()

    def kill_ghub(self) -> None:
        self.clear_log()
        code = kill_ghub()
        self.write_log(f"Comando de encerramento finalizado com código {code}.")


def main() -> int:
    app = App()
    app.mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
