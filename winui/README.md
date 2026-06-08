# Interface WinUI 3

Esta branch mantém a CLI como núcleo do projeto e adiciona uma interface WinUI 3 para Windows 11. A GUI existe para executar a CLI sem Docker, WSL ou terminal, preservando o fluxo principal: escolher a pasta de paletas, apontar o `settings.db` do G HUB, simular, aplicar e gerenciar a instalação local.

## Stack

- .NET 8 com `net8.0-windows10.0.19041.0`.
- Windows App SDK / WinUI 3 via `Microsoft.WindowsAppSDK`.
- Publicação unpackaged e self-contained para `win-x64`.
- Assinatura Authenticode opcional via `scripts/sign-windows-artifact.ps1`.

## Experiência visual

A interface segue o paradigma visual do Windows 11:

- titlebar estendida com Acrylic habilitado por padrão;
- fallback para Mica quando Acrylic não estiver disponível;
- uso da cor de destaque do Windows em ícones, seleção, cartões e ações principais;
- troca claro/escuro baseada no tema de aplicativos do Windows;
- navegação lateral inspirada em Configurações/PowerToys;
- cartões com linhas de configuração em vez de formulário cru;
- caminhos exibidos como valores de configuração, com botão `Alterar`;
- toggles nativos para opções booleanas;
- ações principais em botões com ícones Fluent;
- status compacto no topo, sem caixa modal ou alerta pesado.

A superfície de fundo usa `DesktopAcrylicBackdrop` por padrão para se aproximar do efeito acrílico do Windows 11, com influência visual do wallpaper, transparência e cor de destaque. O app também lê a cor de destaque do Windows no registro e aplica esse tom nos principais acentos da tela.

O backdrop pode ser alterado para diagnóstico:

```powershell
# Desliga Acrylic/Mica
$env:GHUB_WINUI_BACKDROP = "0"

# Força Mica em vez de Acrylic
$env:GHUB_WINUI_BACKDROP = "mica"
```

## Build local

Na raiz do repositório:

```powershell
dotnet publish .\winui\GHubFreestyleInjector.WinUI\GHubFreestyleInjector.WinUI.csproj `
  -c Release `
  -r win-x64 `
  -p:SelfContained=true `
  -p:WindowsPackageType=None `
  -o .\artifacts\winui
```

Para a interface executar a CLI embutida, copie `ghub-freestyle.exe` para a mesma pasta do executável WinUI publicado.

## Assinatura local

O script de assinatura espera variáveis de ambiente, sem senha hardcoded no repositório:

```powershell
$env:CODESIGN_PFX_PATH = "C:\caminho\para\certificado.pfx"
$env:CODESIGN_PFX_PASSWORD = "senha-do-pfx"
powershell -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\sign-windows-artifact.ps1 `
  -ArtifactDir .\artifacts\winui
```

O script assina os executáveis Windows encontrados no artefato e valida a assinatura com `signtool`.

## Instalação pela GUI

A própria janela tem ações de instalação:

- `Instalar`: copia o artefato atual para a pasta de programas do usuário.
- `Atualizar`: substitui a instalação existente a partir de um artefato baixado.
- `Reparar`: recria atalhos e entrada de desinstalação.
- `Desinstalar`: remove a instalação local e os atalhos criados.

Essas ações não exigem instalação global da CLI e não escrevem em diretórios de máquina.

## Nota sobre NavigationView

Durante os testes locais, o `NavigationView` nativo do WinUI 3 causou janela preta/travamento em publicação unpackaged/self-contained nesta máquina. Por isso, a branch usa uma navegação lateral própria, mais simples, mas mantém os controles WinUI estáveis no restante da tela.

Se a stack for atualizada no futuro, vale reavaliar `NavigationView`, `SettingsCard` ou controles equivalentes do Windows Community Toolkit. A prioridade atual é manter a GUI abrindo de forma confiável no Windows local antes de depender do GitHub Actions.

## Validação esperada

Antes de publicar um novo artefato:

1. publicar o projeto WinUI em `Release`;
2. copiar a CLI para a pasta publicada;
3. abrir o executável local;
4. confirmar que a janela não abre preta;
5. testar por pelo menos 60 segundos com Acrylic ligado;
6. assinar o artefato;
7. abrir o executável assinado;
8. verificar logs em `%LOCALAPPDATA%\GHubFreestyleInjector`.
