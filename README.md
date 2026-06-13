# G HUB RGB Freestyle Injector

Aplicativo Windows para criar e manter presets **Freestyle** do Logitech G HUB sem depender do editor visual do G HUB tecla por tecla.

O projeto continua tendo uma CLI, mas o caminho principal é o app nativo **WinUI 3 / Windows App SDK**. Ele foi pensado para Windows 11, Logitech G HUB e teclados Logitech com RGB por tecla, especialmente o **Logitech G515 TKL em layout US internacional**.

## O que ele faz

- Lê arquivos `.md` individuais com seções `Aplicação`, `Zonas` e `Teclas exatas`.
- Aplica cores na ordem correta: teclado inteiro, depois zonas, depois teclas exatas.
- Cria presets Freestyle com prefixo `RGB - `.
- Só regrava paletas novas ou alteradas desde a última execução.
- Cria backup automático do `settings.db` antes de sincronizar.
- Pode encerrar o G HUB antes de gravar.
- Pode remover presets gerenciados que não existem mais na entrada com `--prune`.

## Baixar e usar

A forma recomendada para usuários finais é baixar o instalador na página de Releases:

```text
GHubFreestyleInjector-Setup-windows-x64.exe
```

O instalador coloca o app e a CLI no perfil do usuário, cria atalhos e registra a desinstalação em **Configurações > Aplicativos > Aplicativos instalados**.

Depois de instalar:

1. abra **G HUB RGB Freestyle Injector** pelo Menu Iniciar;
2. escolha a pasta das paletas;
3. confirme o `settings.db` do G HUB;
4. use `Simular` antes de `Aplicar`.

O app instalado inclui a CLI `ghub-freestyle.exe` na mesma pasta.

## Portátil ou instalado

WinUI 3 não é uma boa plataforma para um executável único realmente portátil: o app precisa carregar dependências nativas do Windows App SDK. Por isso, o projeto publica dois formatos:

- `GHubFreestyleInjector-Setup-windows-x64.exe`: recomendado, é um instalador único e amigável.
- `GHubFreestyleInjector-WinUI3-windows-x64.zip`: portátil/técnico, útil para testes, automação e diagnóstico.

No **modo portátil**, extraia o ZIP e execute `GHubFreestyleInjector.WinUI.exe`. Esse modo não cria atalhos, não registra app no Windows e pode ser apagado removendo a pasta extraída.

No **modo instalado**, use o instalador da Release. A instalação é por usuário e cria:

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

## Formato das paletas

O formato suportado é um arquivo Markdown (`.md`) por paleta. O nome do preset vem do primeiro título `#` do arquivo.

Exemplo:

````markdown
# Valorant Omen Jett Astra

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
| number_row | #3D7CFF |

## Teclas exatas

| Tecla | Cor |
| --- | --- |
| ESC | #FF8A1F |
| W | #48D7FF |
| A | #48D7FF |
| S | #48D7FF |
| D | #48D7FF |
````

Campos reconhecidos:

- `# Título`: obrigatório; vira o nome do preset com prefixo `RGB - `.
- `SET_ALL_KEYS #RRGGBB`: obrigatório; define a cor base do teclado inteiro.
- `SET_KEY ESC #RRGGBB`: opcional; atalho para definir a tecla `ESC`.
- `## Zonas`: opcional; tabela com zona e cor.
- `## Teclas exatas`: opcional; tabela com tecla e cor.
- `**Modo:**`: opcional; guardado como metadado, sem mudar a aplicação das cores.

Ordem de aplicação:

1. cor base no teclado inteiro.
2. zonas, se existirem.
3. teclas exatas, sempre por último.

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

Build do instalador local, com Inno Setup 6 instalado:

```powershell
$env:GHUB_FREESTYLE_SOURCE_DIR = "$PWD\artifacts\winui"
$env:GHUB_FREESTYLE_INSTALLER_OUT = "$PWD\artifacts\installer"
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer\GHubFreestyleInjector.iss
```

## Licença

MIT.
