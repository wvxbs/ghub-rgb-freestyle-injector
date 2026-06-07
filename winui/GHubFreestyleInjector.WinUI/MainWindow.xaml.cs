using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace GHubFreestyleInjector.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private Grid RootGrid = null!;
    private Border StatusPanel = null!;
    private TextBlock StatusText = null!;
    private TextBox InputPathBox = null!;
    private TextBox DbPathBox = null!;
    private TextBox OutputBox = null!;
    private TextBlock InstallStateText = null!;
    private ToggleSwitch KillGHubBox = null!;
    private ToggleSwitch ForceBox = null!;
    private ToggleSwitch PruneBox = null!;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        App.LogInfo("MainWindow manual content start");
        Closed += (_, _) =>
        {
            App.LogInfo("MainWindow closed");
            App.KeepAlive();
        };

        Title = "G HUB RGB Freestyle Injector";
        RootGrid = BuildRootGrid();
        Content = RootGrid;
        BuildUi();
        App.LogInfo("MainWindow BuildUi done");

        _hwnd = WindowNative.GetWindowHandle(this);
        App.LogInfo($"HWND acquired: {_hwnd}");
        InitializeWindowChrome();
        TryApplyBackdrop();
        App.LogInfo("Backdrop step done");
        InitializeDefaults();
        RefreshInstallState();
        App.LogInfo("Defaults initialized");
    }

    private static Grid BuildRootGrid()
    {
        var grid = new Grid
        {
            MinWidth = 900,
            MinHeight = 640,
            ColumnSpacing = 0,
            Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00))
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(292) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    private static Brush ResolvePageBackground()
    {
        return new SolidColorBrush(IsLightTheme
            ? Color.FromArgb(0xE8, 0xF8, 0xF4, 0xEF)
            : Color.FromArgb(0xE8, 0x20, 0x20, 0x20));
    }

    private void InitializeWindowChrome()
    {
        ExtendsContentIntoTitleBar = false;
        try
        {
            AppWindow.Title = "G HUB RGB Freestyle Injector";
            AppWindow.Resize(new SizeInt32(1160, 820));
            App.LogInfo($"Window chrome initialized. AppWindowId={AppWindow.Id.Value}");
        }
        catch (Exception ex)
        {
            App.LogInfo("Window chrome fallback: " + ex.Message);
        }
    }

    private void BuildUi()
    {
        var shellBackground = ResolvePageBackground();
        RootGrid.Children.Add(BuildSidebar());

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = shellBackground,
            Content = BuildMainSurface()
        };
        Grid.SetColumn(scroll, 1);
        RootGrid.Children.Add(scroll);
    }

    private UIElement BuildSidebar()
    {
        var side = new Grid
        {
            Padding = new Thickness(14, 18, 12, 18),
            Background = new SolidColorBrush(IsLightTheme
                ? Color.FromArgb(0xA8, 0xF1, 0xEA, 0xE2)
                : Color.FromArgb(0x9C, 0x25, 0x25, 0x25))
        };
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new StackPanel { Spacing = 2, Margin = new Thickness(6, 0, 0, 24) };
        title.Children.Add(new TextBlock
        {
            Text = "G HUB RGB",
            FontSize = 14,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        title.Children.Add(new TextBlock
        {
            Text = "Freestyle Injector",
            FontSize = 12,
            Opacity = 0.68
        });
        side.Children.Add(title);

        var search = new TextBox
        {
            PlaceholderText = "Localizar ação",
            Margin = new Thickness(0, 0, 0, 14),
            IsTabStop = false
        };
        Grid.SetRow(search, 1);
        side.Children.Add(search);

        var nav = new StackPanel { Spacing = 4 };
        nav.Children.Add(BuildNavItem("\uE895", "Sincronização", selected: true));
        nav.Children.Add(BuildNavItem("\uE7B8", "Instalação"));
        nav.Children.Add(BuildNavItem("\uE8A7", "Logs"));
        nav.Children.Add(BuildNavItem("\uE713", "Configurações"));
        Grid.SetRow(nav, 2);
        side.Children.Add(nav);

        var footer = new StackPanel { Spacing = 4, Margin = new Thickness(4, 0, 0, 0) };
        footer.Children.Add(BuildSmallFooter("\uE946", "CLI local"));
        footer.Children.Add(BuildSmallFooter("\uE930", "G HUB settings.db"));
        Grid.SetRow(footer, 3);
        side.Children.Add(footer);

        return side;
    }

    private UIElement BuildMainSurface()
    {
        var main = new StackPanel
        {
            Spacing = 18,
            Padding = new Thickness(28, 24, 32, 32)
        };

        main.Children.Add(BuildHeader());
        main.Children.Add(BuildInputCard());
        main.Children.Add(BuildOperationCard());
        main.Children.Add(BuildInstallCard());
        main.Children.Add(BuildOutputCard());
        return main;
    }

    private UIElement BuildHeader()
    {
        var header = new Grid { ColumnSpacing = 18 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Spacing = 5 };
        copy.Children.Add(new TextBlock
        {
            Text = "Sincronização RGB",
            FontSize = 30,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        copy.Children.Add(new TextBlock
        {
            Text = "Crie, atualize e reaplique presets Freestyle do G HUB a partir dos seus arquivos Markdown.",
            FontSize = 13,
            Opacity = 0.76,
            TextWrapping = TextWrapping.Wrap
        });
        header.Children.Add(copy);

        StatusText = new TextBlock
        {
            Text = "Pronto para simular.",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        StatusPanel = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            CornerRadius = new CornerRadius(999),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x78, 0xD4)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x78, 0xD4)),
            Child = StatusText,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(StatusPanel, 1);
        header.Children.Add(StatusPanel);
        return header;
    }

    private UIElement BuildInputCard()
    {
        var panel = BuildCardStack("Entradas", "Escolha a pasta das paletas e o banco de dados do G HUB.");

        InputPathBox = new TextBox
        {
            Header = "Pasta das paletas",
            PlaceholderText = @"C:\Caminho\Para\paletas"
        };
        panel.Children.Add(BuildPickerRow(InputPathBox, "\uE8B7", ChooseInput_Click));

        DbPathBox = new TextBox
        {
            Header = "settings.db do G HUB",
            PlaceholderText = @"%LOCALAPPDATA%\LGHUB\settings.db"
        };
        panel.Children.Add(BuildPickerRow(DbPathBox, "\uE8A5", ChooseDb_Click));

        return BuildCard(panel);
    }

    private UIElement BuildOperationCard()
    {
        var panel = BuildCardStack("Operação", "Controle o que será feito quando a CLI rodar.");

        var options = new StackPanel { Spacing = 1 };
        KillGHubBox = BuildSettingToggle(
            options,
            "Encerrar G HUB",
            "Fecha o G HUB antes de gravar presets para evitar conflito com o banco de dados.",
            isOn: true);
        ForceBox = BuildSettingToggle(
            options,
            "Forçar regravação",
            "Substitui presets gerenciados já detectados, mesmo sem mudança nos arquivos Markdown.");
        PruneBox = BuildSettingToggle(
            options,
            "Limpar órfãos",
            "Remove presets gerenciados que não têm mais um arquivo Markdown correspondente.");
        panel.Children.Add(options);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0)
        };
        actions.Children.Add(BuildActionButton("\uE721", "Listar", List_Click));
        actions.Children.Add(BuildActionButton("\uE9D9", "Simular", DryRun_Click));
        actions.Children.Add(BuildActionButton("\uE73E", "Aplicar", Apply_Click, primary: true));
        actions.Children.Add(BuildActionButton("\uE711", "Encerrar G HUB", KillGHub_Click));
        panel.Children.Add(actions);

        return BuildCard(panel);
    }

    private UIElement BuildInstallCard()
    {
        var panel = BuildCardStack("Instalação", "Gerencie a cópia instalada no perfil do usuário.");

        InstallStateText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.78,
            FontSize = 13
        };
        panel.Children.Add(InstallStateText);

        var wizardButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 2, 0, 0)
        };
        wizardButtons.Children.Add(BuildActionButton("\uE896", "Instalar", Install_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE895", "Atualizar", Update_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE90F", "Reparar", Repair_Click));
        wizardButtons.Children.Add(BuildActionButton("\uE74D", "Desinstalar", Uninstall_Click));
        panel.Children.Add(wizardButtons);

        return BuildCard(panel);
    }

    private UIElement BuildOutputCard()
    {
        var panel = BuildCardStack("Saída", "Acompanhe exatamente o que a CLI retornou.");
        OutputBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            MinHeight = 210,
            PlaceholderText = "Os logs da execução aparecem aqui."
        };
        ScrollViewer.SetVerticalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        panel.Children.Add(OutputBox);
        return BuildCard(panel);
    }

    private static StackPanel BuildCardStack(string title, string subtitle)
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        stack.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            Opacity = 0.66,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, -8, 0, 0)
        });
        return stack;
    }

    private static Border BuildCard(UIElement content)
    {
        return new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(18),
            Background = new SolidColorBrush(IsLightTheme
                ? Color.FromArgb(0xD8, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0xCC, 0x2D, 0x2D, 0x2D)),
            BorderBrush = new SolidColorBrush(IsLightTheme
                ? Color.FromArgb(0x40, 0xA8, 0xA0, 0x96)
                : Color.FromArgb(0x44, 0x75, 0x75, 0x75)),
            Child = content
        };
    }

    private static Grid BuildPickerRow(TextBox textBox, string glyph, RoutedEventHandler clickHandler)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(textBox);

        var button = new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 16 },
            Width = 42,
            Height = 32,
            Margin = new Thickness(0, 27, 0, 0)
        };
        ToolTipService.SetToolTip(button, "Escolher");
        button.Click += clickHandler;
        Grid.SetColumn(button, 1);
        row.Children.Add(button);
        return row;
    }

    private static ToggleSwitch BuildSettingToggle(StackPanel host, string title, string description, bool isOn = false)
    {
        var row = new Grid
        {
            MinHeight = 58,
            Padding = new Thickness(0, 8, 0, 8),
            ColumnSpacing = 18
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var copy = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        copy.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 500 }
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Opacity = 0.68,
            TextWrapping = TextWrapping.Wrap
        });
        row.Children.Add(copy);

        var toggle = new ToggleSwitch
        {
            IsOn = isOn,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(toggle, 1);
        row.Children.Add(toggle);
        host.Children.Add(row);
        return toggle;
    }

    private static Button BuildActionButton(string glyph, string text, RoutedEventHandler clickHandler, bool primary = false)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new FontIcon { Glyph = glyph, FontSize = 15 });
        content.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });

        var button = new Button
        {
            Content = content,
            MinHeight = 34,
            Padding = new Thickness(12, 6, 12, 6)
        };
        if (primary)
        {
            button.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }

        button.Click += clickHandler;
        return button;
    }

    private static UIElement BuildNavItem(string glyph, string text, bool selected = false)
    {
        var row = new Grid
        {
            Height = 38,
            Padding = new Thickness(12, 0, 10, 0),
            ColumnSpacing = 12,
            Background = selected
                ? new SolidColorBrush(Color.FromArgb(0x70, 0xE7, 0xDF, 0xD7))
                : new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00)),
            CornerRadius = new CornerRadius(6)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(new FontIcon
        {
            Glyph = glyph,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = selected
                ? new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4))
                : null
        });
        var label = new TextBlock
        {
            Text = text,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        return row;
    }

    private static bool IsLightTheme
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return !Equals(key?.GetValue("AppsUseLightTheme"), 0);
            }
            catch
            {
                return true;
            }
        }
    }

    private static UIElement BuildSmallFooter(string glyph, string text)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Padding = new Thickness(8, 8, 8, 8)
        };
        row.Children.Add(new FontIcon { Glyph = glyph, FontSize = 14, Opacity = 0.76 });
        row.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            Opacity = 0.78,
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }

    private void TryApplyBackdrop()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("GHUB_WINUI_BACKDROP"), "0", StringComparison.Ordinal))
        {
            App.LogInfo("Backdrop disabled by GHUB_WINUI_BACKDROP=0");
            return;
        }

        try
        {
            SystemBackdrop = new MicaBackdrop();
            App.LogInfo("Mica backdrop enabled");
        }
        catch (Exception ex)
        {
            App.LogInfo("Mica backdrop fallback: " + ex.Message);
            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
                App.LogInfo("Acrylic backdrop enabled");
            }
            catch (Exception acrylicEx)
            {
                App.LogInfo("Acrylic backdrop unavailable: " + acrylicEx.Message);
            }
        }
    }

    private void InitializeDefaults()
    {
        InputPathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DbPathBox.Text = Path.Combine(localAppData, "LGHUB", "settings.db");
    }

    private async void ChooseInput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, _hwnd);
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            InputPathBox.Text = folder.Path;
        }
    }

    private async void ChooseDb_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, _hwnd);
        picker.FileTypeFilter.Add(".db");
        picker.FileTypeFilter.Add("*");
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            DbPathBox.Text = file.Path;
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Instalar", () => InstallOrUpdateAsync(allowSameSource: false));
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Atualizar", () => InstallOrUpdateAsync(allowSameSource: false));
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Reparar", async () =>
        {
            if (!Directory.Exists(InstallDir))
            {
                await InstallOrUpdateAsync(allowSameSource: false);
                return;
            }

            CreateLaunchers();
            RegisterUninstallEntry();
            await Task.CompletedTask;
        });
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Desinstalar", async () =>
        {
            if (!Directory.Exists(InstallDir))
            {
                RemoveLaunchers();
                RemoveUninstallEntry();
                return;
            }

            if (IsRunningFromInstallDir)
            {
                ScheduleSelfRemoval();
                Close();
                return;
            }

            Directory.Delete(InstallDir, recursive: true);
            RemoveLaunchers();
            RemoveUninstallEntry();
            await Task.CompletedTask;
        });
    }

    private async Task RunWizardActionAsync(string action, Func<Task> work)
    {
        SetBusy(true);
        try
        {
            AppendLog($"{action}: iniciando.");
            await Task.Run(work);
            RefreshInstallState();
            SetStatus("Instalação", $"{action} concluído.", StatusKind.Success);
            AppendLog($"{action}: concluído.");
        }
        catch (Exception ex)
        {
            SetStatus("Instalação", ex.Message, StatusKind.Error);
            AppendLog($"{action}: erro: {ex}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InstallOrUpdateAsync(bool allowSameSource)
    {
        var sourceDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetDir = Path.GetFullPath(InstallDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!allowSameSource && string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Para atualizar, execute uma versão baixada fora da pasta instalada.");
        }

        var stagingDir = Path.Combine(Path.GetTempPath(), "GHubFreestyleInjector-install-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyDirectory(sourceDir, stagingDir);
            if (Directory.Exists(InstallDir))
            {
                Directory.Delete(InstallDir, recursive: true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(InstallDir)!);
            Directory.Move(stagingDir, InstallDir);
            CreateLaunchers();
            RegisterUninstallEntry();
        }
        finally
        {
            if (Directory.Exists(stagingDir))
            {
                Directory.Delete(stagingDir, recursive: true);
            }
        }

        await Task.CompletedTask;
    }

    private void RefreshInstallState()
    {
        if (InstallStateText is null) return;

        var installed = File.Exists(InstalledExe);
        var origin = IsRunningFromInstallDir ? "instalada" : "artefato baixado";
        InstallStateText.Text = installed
            ? $"Instalado em {InstallDir}. Esta execução veio de {origin}."
            : $"Ainda não instalado. A instalação por usuário será criada em {InstallDir}.";
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(targetDir, Path.GetRelativePath(sourceDir, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var targetFile = Path.Combine(targetDir, Path.GetRelativePath(sourceDir, file));
            File.Copy(file, targetFile, overwrite: true);
        }
    }

    private void CreateLaunchers()
    {
        Directory.CreateDirectory(StartMenuProgramsDir);
        File.WriteAllText(StartMenuLauncher, $"@echo off\r\nstart \"\" \"{InstalledExe}\" %*\r\n", Encoding.UTF8);
        File.WriteAllText(UninstallScript, BuildUninstallScript(), Encoding.UTF8);
    }

    private static void RemoveLaunchers()
    {
        TryDelete(StartMenuLauncher);
    }

    private void RegisterUninstallEntry()
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegistryKey);
        key?.SetValue("DisplayName", "G HUB RGB Freestyle Injector");
        key?.SetValue("DisplayVersion", "0.1.0");
        key?.SetValue("Publisher", "wvxbs");
        key?.SetValue("InstallLocation", InstallDir);
        key?.SetValue("DisplayIcon", InstalledExe);
        key?.SetValue("UninstallString", $"\"{UninstallScript}\"");
        key?.SetValue("QuietUninstallString", $"\"{UninstallScript}\"");
        key?.SetValue("NoModify", 1, RegistryValueKind.DWord);
    }

    private static void RemoveUninstallEntry()
    {
        Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryKey, throwOnMissingSubKey: false);
    }

    private void ScheduleSelfRemoval()
    {
        var script = Path.Combine(Path.GetTempPath(), "GHubFreestyleInjector-uninstall-" + Guid.NewGuid().ToString("N") + ".cmd");
        File.WriteAllText(script, BuildUninstallScript(deleteSelf: true), Encoding.UTF8);
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            ArgumentList = { "/c", script },
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static string BuildUninstallScript(bool deleteSelf = false)
    {
        var selfDelete = deleteSelf ? "\r\ndel \"%~f0\" >nul 2>nul" : string.Empty;
        return $"""
@echo off
taskkill /IM GHubFreestyleInjector.WinUI.exe /F >nul 2>nul
timeout /t 2 /nobreak >nul 2>nul
rmdir /s /q "{InstallDir}" >nul 2>nul
del "{StartMenuLauncher}" >nul 2>nul
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\GHubFreestyleInjector /f >nul 2>nul
{selfDelete}
""";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private static string InstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "GHubFreestyleInjector");

    private static string InstalledExe => Path.Combine(InstallDir, "GHubFreestyleInjector.WinUI.exe");

    private static string StartMenuProgramsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
        "Programs");

    private static string StartMenuLauncher => Path.Combine(StartMenuProgramsDir, "G HUB RGB Freestyle Injector.cmd");

    private static string UninstallScript => Path.Combine(InstallDir, "uninstall.cmd");

    private static string UninstallRegistryKey => @"Software\Microsoft\Windows\CurrentVersion\Uninstall\GHubFreestyleInjector";

    private static bool IsRunningFromInstallDir
    {
        get
        {
            var sourceDir = Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var targetDir = Path.GetFullPath(InstallDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async void List_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync("list", "--managed-only");
    }

    private async void DryRun_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync("sync", "--dry-run");
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        var args = new List<string> { "sync" };
        if (KillGHubBox.IsOn) args.Add("--kill-ghub");
        if (ForceBox.IsOn) args.Add("--force");
        if (PruneBox.IsOn) args.Add("--prune");
        await RunCliAsync(args.ToArray());
    }

    private async void KillGHub_Click(object sender, RoutedEventArgs e)
    {
        await RunCliAsync(new[] { "kill-ghub" }, includePaths: false);
    }

    private async Task RunCliAsync(params string[] args)
    {
        await RunCliAsync(args, includePaths: true);
    }

    private async Task RunCliAsync(string[] args, bool includePaths)
    {
        SetBusy(true);
        ClearLog();

        try
        {
            var fullArgs = new List<string>(args);
            if (includePaths)
            {
                fullArgs.AddRange(["--input", InputPathBox.Text, "--db", DbPathBox.Text]);
            }

            var result = await Task.Run(() => ExecuteCli(fullArgs));
            AppendLog(result);
            SetStatus(
                result.ExitCode == 0 ? "Concluído" : "Falhou",
                $"ghub-freestyle terminou com código {result.ExitCode}.",
                result.ExitCode == 0 ? StatusKind.Success : StatusKind.Error);
        }
        catch (Exception ex)
        {
            AppendLog("ERRO: " + ex);
            SetStatus("Erro", ex.Message, StatusKind.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private CliResult ExecuteCli(IEnumerable<string> args)
    {
        var cli = ResolveCliPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = cli,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Não foi possível iniciar a CLI.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new CliResult(process.ExitCode, output, error);
    }

    private static string ResolveCliPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var localCli = Path.Combine(baseDir, "ghub-freestyle.exe");
        if (File.Exists(localCli))
        {
            return localCli;
        }

        return "ghub-freestyle";
    }

    private void AppendLog(CliResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output)) AppendLog(result.Output.TrimEnd());
        if (!string.IsNullOrWhiteSpace(result.Error)) AppendLog(result.Error.TrimEnd());
    }

    private void AppendLog(string text)
    {
        _log.AppendLine(text);
        OutputBox.Text = _log.ToString();
    }

    private void ClearLog()
    {
        _log.Clear();
        OutputBox.Text = string.Empty;
    }

    private void SetBusy(bool busy)
    {
        RootGrid.Opacity = busy ? 0.72 : 1.0;
        if (busy)
        {
            SetStatus("Rodando", "Executando ghub-freestyle...", StatusKind.Info);
        }
    }

    private void SetStatus(string title, string message, StatusKind kind)
    {
        StatusText.Text = $"{title}: {message}";

        var (background, border) = kind switch
        {
            StatusKind.Success => (
                Color.FromArgb(0x24, 0x10, 0x7C, 0x10),
                Color.FromArgb(0x72, 0x10, 0x7C, 0x10)),
            StatusKind.Error => (
                Color.FromArgb(0x24, 0xC4, 0x2B, 0x1C),
                Color.FromArgb(0x72, 0xC4, 0x2B, 0x1C)),
            _ => (
                Color.FromArgb(0x22, 0x00, 0x78, 0xD4),
                Color.FromArgb(0x66, 0x00, 0x78, 0xD4))
        };

        StatusPanel.Background = new SolidColorBrush(background);
        StatusPanel.BorderBrush = new SolidColorBrush(border);
    }

    private enum StatusKind
    {
        Info,
        Success,
        Error
    }

    private sealed record CliResult(int ExitCode, string Output, string Error)
    {
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] { Output, Error }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
