# WinUI 3 shell

Esta branch adiciona uma interface experimental em **WinUI 3 / Windows App SDK** sem remover a interface Tkinter existente.

## Objetivo

A interface WinUI 3 existe para aproximar o projeto do paradigma visual do Windows 11:

- tema claro/escuro automático;
- controles Fluent nativos;
- painel de instalação com ações de instalar, atualizar, reparar e desinstalar;
- superfície com Mica quando o Windows permitir;
- integração com file/folder pickers modernos;
- log visível da CLI em tempo real.

## Arquitetura

O motor continua sendo a CLI Python `ghub-freestyle`.

No artifact gerado pelo GitHub Actions, o workflow:

1. empacota a CLI Python como `ghub-freestyle.exe` com PyInstaller;
2. publica o app WinUI 3 self-contained;
3. copia `ghub-freestyle.exe` para a pasta publicada do WinUI;
4. envia a pasta completa como artifact `GHubFreestyleInjector-WinUI3-windows-x64`.

Isso preserva o comportamento validado da CLI e troca apenas a casca visual.

## Wizard de instalação

A interface WinUI 3 inclui um painel de instalação para uso por usuário, sem exigir administrador. Ele:

- instala a pasta publicada em `%LOCALAPPDATA%\Programs\GHubFreestyleInjector`;
- cria um lançador no Menu Iniciar do usuário;
- registra uma entrada de desinstalação em `HKCU`;
- permite atualizar usando uma versão nova executada fora da pasta instalada;
- permite reparar atalhos/registro;
- permite desinstalar, inclusive quando o app está rodando da pasta instalada.

Esse wizard não substitui assinatura de código. Em máquinas com Smart App Control ou Windows App Control rígido, um executável WinUI não assinado ainda pode ser bloqueado antes de abrir. Para distribuição confortável, o próximo passo é assinar o artefato ou gerar um pacote/instalador assinado.

## Build

O build local exige .NET SDK, Windows SDK e Windows App SDK compatíveis. Em máquinas sem SDK instalado, use o GitHub Actions:

```text
Build WinUI 3 shell
```

## Assinatura

O artifact WinUI 3 deve sair assinado do GitHub Actions. A assinatura acontece depois da publicação do WinUI e depois que a CLI empacotada é copiada para a pasta final.

Arquivos assinados:

- `GHubFreestyleInjector.WinUI.exe`
- `ghub-freestyle.exe`

Secrets necessários no repositório:

```text
WINDOWS_CODESIGN_PFX_BASE64
WINDOWS_CODESIGN_PFX_PASSWORD
```

O script versionado em `scripts/sign-windows-artifact.ps1` aceita tanto `CODESIGN_PFX_BASE64` quanto `CODESIGN_PFX_PATH`. Isso permite usar o mesmo fluxo no GitHub Actions e em testes locais com um `.pfx` já existente.

Como o certificado atual é local/de desenvolvimento, ele resolve a integridade e permite validar o pipeline, mas a confiança da máquina depende do certificado estar instalado nos repositórios confiáveis do usuário. No GitHub Actions, o workflow confia temporariamente no certificado público apenas dentro do runner efêmero para que `signtool verify` consiga validar o artifact. Para distribuição pública ampla, o ideal continua sendo um certificado de assinatura de código emitido por uma autoridade reconhecida.

## Observações

- A branch `feature/windows-gui` continua sendo a versão Tkinter.
- A branch `feature/winui3-shell` é a tentativa nativa Windows 11.
- O app WinUI 3 chama a CLI como processo e captura stdout/stderr.
- A janela atual evita XAML complexo na primeira tela para reduzir falhas de runtime em builds self-contained.
- Mica/Acrylic são usados como material do Windows App SDK quando disponíveis; em builds antigos do Windows, a janela cai para a superfície normal do WinUI.
