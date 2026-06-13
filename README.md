# G HUB RGB Freestyle Injector

Aplicativo Windows para criar e manter presets **Freestyle** do Logitech G HUB sem depender do editor visual do G HUB tecla por tecla.

O projeto continua tendo uma CLI, mas o caminho principal é o app nativo **WinUI 3 / Windows App SDK**. Ele foi pensado para Windows 11, Logitech G HUB e teclados Logitech com RGB por tecla, especialmente o **Logitech G515 TKL em layout US internacional**.

## O que ele faz

- Lê `palettes_codex_ready.json` quando existir.
- Se não houver JSON, tenta ler arquivos `.md` individuais com seções `Aplicação`, `Zonas` e `Teclas exatas`.
- Aplica cores na ordem correta: `base_color`, depois `zones`, depois `exact_key_overrides`.
- Cria presets Freestyle com prefixo `RGB - `.
- Só regrava paletas novas ou alteradas desde a última execução.
- Cria backup automático do `settings.db` antes de sincronizar.
- Pode encerrar o G HUB antes de gravar.
- Pode remover presets gerenciados que não existem mais na entrada com `--prune`.

## Baixar e usar

O workflow **Build Windows app** gera o artefato:

```text
GHubFreestyleInjector-WinUI3-windows-x64
```

Depois de baixar o artefato no GitHub Actions ou em uma Release:

1. extraia a pasta;
2. execute `GHubFreestyleInjector.WinUI.exe`;
3. escolha a pasta das paletas;
4. confirme o `settings.db` do G HUB;
5. use `Simular` antes de `Aplicar`.

O app publicado inclui a CLI `ghub-freestyle.exe` na mesma pasta.

## Modo portátil ou instalado

O app não precisa ser instalado.

No **modo portátil**, basta manter estes arquivos na mesma pasta:

```text
GHubFreestyleInjector.WinUI.exe
ghub-freestyle.exe
```

Esse modo não cria atalhos, não registra app no Windows e pode ser apagado removendo a pasta.

No **modo instalado**, use o botão `Instalar` dentro do app. A instalação é por usuário e cria:

- atalho `.lnk` no Menu Iniciar;
- atalho `.lnk` na Área de Trabalho;
- entrada em **Configurações > Aplicativos > Aplicativos instalados**;
- chave `App Paths` em `HKCU`, para o Windows localizar o app pelo nome.

As ações `Atualizar`, `Reparar` e `Desinstalar` ficam no próprio app.

## Interface WinUI 3

A interface usa:

- WinUI 3 / Windows App SDK;
- Acrylic por padrão, com fallback para Mica;
- cor de destaque do Windows via `UISettings.GetColorValue(UIColorType.Accent)`;
- tema claro/escuro seguindo o Windows;
- titlebar estendida;
- layout inspirado em Configurações/PowerToys;
- assistente de instalação por usuário.

Detalhes técnicos ficam em [winui/README.md](winui/README.md).

## CLI

A CLI é mantida para automação, diagnóstico e uso avançado.

Instalação local para desenvolvimento:

```bash
python -m pip install -e .
```

Listar paletas detectadas e presets existentes:

```bash
ghub-freestyle list \
  --input /mnt/c/Users/<usuario>/RGB-palettes \
  --managed-only
```

Simular sincronização:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/<usuario>/RGB-palettes \
  --dry-run
```

Aplicar:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/<usuario>/RGB-palettes \
  --kill-ghub
```

Remover presets gerenciados que não existem mais na entrada:

```bash
ghub-freestyle sync \
  --input /mnt/c/Users/<usuario>/RGB-palettes \
  --prune \
  --kill-ghub
```

## Docker

Docker não é o foco do projeto, porque o G HUB é um aplicativo Windows e a experiência principal deve acontecer no Windows. Ainda assim, a imagem CLI continua disponível para simulações e automações.

Build local:

```bash
docker build -t ghub-rgb-freestyle-injector .
```

Uso:

```bash
docker run --rm \
  -v /mnt/c/Users/<usuario>/RGB-palettes:/input \
  -v /mnt/c/Users/<usuario>/AppData/Local/LGHUB:/lghub \
  -v "$PWD/.state:/state" \
  ghub-rgb-freestyle-injector sync \
  --input /input \
  --db /lghub/settings.db \
  --state /state/state.json \
  --dry-run
```

Observação: dentro do Docker, `--kill-ghub` normalmente não consegue encerrar o G HUB do Windows. Feche o G HUB manualmente ou use a versão WinUI/CLI no Windows para aplicar mudanças reais.

O workflow Docker fica manual por enquanto (`workflow_dispatch`).

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

## Desenvolvimento

Build da CLI:

```bash
python -m pip install -e .
python -m pytest
```

Build do app Windows:

```powershell
dotnet restore .\winui\GHubFreestyleInjector.WinUI\GHubFreestyleInjector.WinUI.csproj -r win-x64
dotnet publish .\winui\GHubFreestyleInjector.WinUI\GHubFreestyleInjector.WinUI.csproj `
  -c Release `
  -r win-x64 `
  -p:SelfContained=true `
  -p:WindowsPackageType=None `
  -o .\artifacts\winui
```

## Licença

MIT.
