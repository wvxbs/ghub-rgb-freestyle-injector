# G HUB RGB Freestyle Injector

Sincronizador de paletas RGB para criar presets **Freestyle** no Logitech G HUB sem configurar tecla por tecla manualmente.

O fluxo esperado é simples: você aponta para uma pasta com paletas, roda um comando e o projeto cria ou atualiza os presets no `settings.db` do G HUB. Ele foi pensado para teclado Logitech com RGB por tecla, especialmente o **Logitech G515 TKL em layout US internacional**.

## O que ele faz

- Lê `palettes_codex_ready.json` quando existir. Essa é a fonte de verdade prioritária.
- Se não houver JSON, tenta ler arquivos `.md` individuais com seções `Aplicação`, `Zonas` e `Teclas exatas`.
- Aplica as cores na ordem correta: `base_color` no teclado inteiro, depois `zones`, depois `exact_key_overrides`.
- Usa `exact_key_overrides` como prioridade máxima.
- Cria presets Freestyle com prefixo `RGB - `.
- Só regrava paletas novas ou alteradas desde a última execução, usando hash de conteúdo.
- Cria backup automático do banco do G HUB antes de sincronizar.
- Pode encerrar o G HUB antes de gravar, se você pedir.
- Pode remover presets gerenciados que não existem mais na pasta de entrada com `--prune`.

## Instalação no Arch/WSL

```bash
cd ~/Documentos/repos/ghub-rgb-freestyle-injector
python3 -m pip install --user -e .
```

Se você não quiser instalar nem em modo editável, pode rodar pelo módulo:

```bash
PYTHONPATH=src python3 -m ghub_freestyle_injector.cli --help
```

## Uso rápido

Com a pasta gerada na Área de Trabalho:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/gabri/OneDrive/Desktop \
  --kill-ghub
```

Para ver o que aconteceria sem alterar nada:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/gabri/OneDrive/Desktop \
  --dry-run
```

Para listar paletas detectadas e presets já existentes no G HUB:

```bash
ghub-freestyle list \
  --input /mnt/c/Users/gabri/OneDrive/Desktop \
  --managed-only
```

Para forçar a regravação de tudo:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/gabri/OneDrive/Desktop \
  --force \
  --kill-ghub
```

Para remover presets `RGB - ...` que foram criados por este projeto, mas não existem mais na entrada:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/gabri/OneDrive/Desktop \
  --prune \
  --kill-ghub
```

## Docker

O projeto tem `Dockerfile`. A ideia é montar:

- a pasta de entrada em `/input`;
- a pasta do G HUB em `/lghub`;
- opcionalmente uma pasta de estado em `/state`.

Exemplo no WSL com Docker Desktop integrado:

```bash
docker build -t ghub-rgb-freestyle-injector .

docker run --rm \
  -v /mnt/c/Users/gabri/OneDrive/Desktop:/input \
  -v /mnt/c/Users/gabri/AppData/Local/LGHUB:/lghub \
  -v "$PWD/.state:/state" \
  ghub-rgb-freestyle-injector sync \
  --input /input \
  --db /lghub/settings.db \
  --state /state/state.json
```

Observação: no WSL, o comando `--kill-ghub` depende de acesso ao `taskkill.exe` do Windows. Dentro do Docker isso normalmente não existe, então o jeito mais previsível é fechar o G HUB antes ou rodar `ghub-freestyle kill-ghub` fora do contêiner.

## Interface visual

Esta branch adiciona duas interfaces visuais sobre o mesmo motor da CLI.

### Executável nativo do Windows

A interface nativa usa Tkinter/ttk, abre como uma janela simples do Windows e permite:

- escolher a pasta das paletas;
- escolher o `settings.db`;
- listar paletas e presets existentes;
- simular a sincronização;
- aplicar a sincronização;
- encerrar o G HUB antes de aplicar;
- ver o log do que está acontecendo.

O workflow `Build GUI artifacts` gera o artifact:

```text
GHubFreestyleInjector-windows-x64
```

Por enquanto o formato é `.exe`, não `.msi`. Para esta ferramenta, o `.exe` faz mais sentido: é portátil, não precisa instalar nada e reduz a chance de dor de cabeça em máquina recém-formatada. Um `.msi` só passaria a valer a pena se o projeto precisasse de instalador, atalhos no menu iniciar, atualização automática ou associação de arquivos.

Localmente, em um Windows com Python instalado, a GUI pode ser aberta com:

```powershell
ghub-freestyle-gui
```

Ou, no checkout:

```powershell
python -m ghub_freestyle_injector.gui_tk
```

### Interface web em Docker

A interface web é pensada para desenvolvimento e para quem quer usar Docker/Compose. Ela não substitui o `.exe` para uso cotidiano no Windows, porque um contêiner não é um bom lugar para encerrar processos do G HUB.

Subir a interface:

```bash
docker compose up gui
```

Abrir:

```text
http://localhost:8080
```

Rodar a CLI via Compose em modo simulação:

```bash
docker compose run --rm cli
```

Você pode trocar os volumes com variáveis:

```bash
PALETTES_DIR=/mnt/c/Users/gabri/OneDrive/Desktop \
LGHUB_DIR=/mnt/c/Users/gabri/AppData/Local/LGHUB \
docker compose up gui
```

### Organização de branches e imagens

A organização recomendada é:

- `main`: CLI estável, biblioteca principal e imagem Docker inegociável `wvxbs/ghub-rgb-freestyle-injector`.
- `feature/windows-gui`: evolução da interface visual, do `.exe` e da imagem web `wvxbs/ghub-rgb-freestyle-injector-gui`.

Manter duas imagens faz sentido se a interface web continuar existindo: a imagem da CLI deve permanecer pequena e previsível; a imagem da interface pode ter servidor, assets e comportamento de app. Para o executável nativo, Docker não agrega muito no uso final. O melhor caminho prático é ter os dois: Compose para desenvolvimento e `.exe` para rodar e esquecer no Windows.

## Publicação no Docker Hub

O workflow `.github/workflows/docker-publish.yml` publica a imagem no Docker Hub quando há push na branch `main`, seguindo o mesmo padrão usado em `telemetry-lab`.

Imagem publicada:

```text
wvxbs/ghub-rgb-freestyle-injector
```

Tags geradas:

- `latest`
- nome da branch
- `sha-<commit>`

O repositório no GitHub precisa ter o token do Docker Hub disponível como secret ou variable:

- `DOCKERHUB_TOKEN`

O usuário é fixo no workflow como `wvxbs`, para evitar falha por variable ausente.

## Formato prioritário

O formato recomendado é `palettes_codex_ready.json`:

```json
{
  "palettes": [
    {
      "id": "valorant_omen_jett_astra",
      "title": "Valorant — Omen/Jett/Astra",
      "base_mode": "keyboard_wasd",
      "base_color": "#6B3CFF",
      "esc_color": "#FF8A1F",
      "zones": {
        "modifiers": "#3B2A7A",
        "number_row": "#3D7CFF"
      },
      "exact_key_overrides": {
        "ESC": "#FF8A1F",
        "W": "#48D7FF",
        "A": "#48D7FF",
        "S": "#48D7FF",
        "D": "#48D7FF"
      }
    }
  ]
}
```

Ordem de aplicação:

1. `base_color` no teclado inteiro.
2. `zones`, se existirem.
3. `exact_key_overrides`, sempre por último.

## Zonas aceitas

- `letters`
- `function_row`
- `modifiers`
- `number_row`
- `arrows`
- `navigation`
- `wasd`

## Teclas aceitas

Letras e números usam o próprio caractere: `A`, `B`, `C`, `1`, `2`, `3`.

Também são aceitas:

`ESC`, `TAB`, `CAPS`, `SHIFT`, `CTRL`, `ALT`, `SPACE`, `ENTER`, `BACKSPACE`, `INS`, `HOME`, `PGUP`, `DEL`, `END`, `PGDN`, `UP`, `DOWN`, `LEFT`, `RIGHT`, `F1` a `F12`.

## Estado de sincronização

Por padrão, o arquivo de estado fica dentro da pasta de entrada:

```text
.ghub-freestyle-injector-state.json
```

Ele guarda o hash das paletas aplicadas. Se o conteúdo não mudou e o preset ainda existe no G HUB, a paleta é ignorada. Se o conteúdo mudou, o preset é substituído. Se o preset não existe, ele é criado mesmo que o hash esteja igual.

Você pode escolher outro caminho:

```bash
ghub-freestyle sync \
  --input /input \
  --state /state/state.json
```

## Backup

Antes de gravar, o projeto copia `settings.db`, `settings.db-wal` e `settings.db-shm`, quando existirem, para uma pasta de backup.

Por padrão, o backup fica ao lado do banco:

```text
AppData/Local/LGHUB/ghub-freestyle-backups/
```

Você pode mudar:

```bash
ghub-freestyle sync \
  --input /input \
  --backup-dir /algum/lugar/seguro
```

## Recuperação manual

Se algo ficar estranho:

1. Feche o G HUB.
2. Copie os arquivos do backup de volta para `AppData/Local/LGHUB`.
3. Abra o G HUB novamente.

## Notas importantes

- O projeto mexe diretamente no banco local do G HUB. Por isso o backup automático é obrigatório no fluxo normal.
- O G HUB fechado reduz bastante a chance de conflito com `settings.db-wal`.
- O projeto não associa presets a jogos. Ele cria presets Freestyle globais para você escolher no app.
- O atalho/prompt para gerar novas paletas descrito no pacote de entrada é documentação útil, mas não faz parte do escopo deste injetor.
