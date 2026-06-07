# WinUI 3 shell

Esta branch adiciona uma interface experimental em **WinUI 3 / Windows App SDK** sem remover a interface Tkinter existente.

## Objetivo

A interface WinUI 3 existe para aproximar o projeto do paradigma visual do Windows 11:

- tema claro/escuro automático;
- controles Fluent nativos;
- NavigationView no estilo Configurações/PowerToys;
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

## Build

O build local exige .NET SDK, Windows SDK e Windows App SDK compatíveis. Em máquinas sem SDK instalado, use o GitHub Actions:

```text
Build WinUI 3 shell
```

## Observações

- A branch `feature/windows-gui` continua sendo a versão Tkinter.
- A branch `feature/winui3-shell` é a tentativa nativa Windows 11.
- O app WinUI 3 chama a CLI como processo e captura stdout/stderr.
- Mica/Acrylic são usados como material do Windows App SDK quando disponíveis; em builds antigos do Windows, a janela cai para a superfície normal do WinUI.
