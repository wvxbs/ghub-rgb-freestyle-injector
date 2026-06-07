using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GHubFreestyleInjector.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private Grid RootGrid = null!;
    private InfoBar StatusBar = null!;
    private TextBox InputPathBox = null!;
    private TextBox DbPathBox = null!;
    private TextBox OutputBox = null!;
    private TextBlock InstallStateText = null!;
    private CheckBox KillGHubBox = null!;
    private CheckBox ForceBox = null!;
    private CheckBox PruneBox = null!;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        App.LogInfo("MainWindow manual content start");
        RootGrid = new Grid();
        Content = RootGrid;
        App.LogInfo("MainWindow manual content done");
        BuildUi();
        App.LogInfo("MainWindow BuildUi done");

        _hwnd = WindowNative.GetWindowHandle(this);
        App.LogInfo($"HWND acquired: {_hwnd}");
        ExtendsContentIntoTitleBar = true;
        TryApplyBackdrop();
        App.LogInfo("Backdrop step done");
        InitializeDefaults();
        RefreshInstallState();
        App.LogInfo("Defaults initialized");
    }


    private void BuildUi()
    {
        RootGrid.Margin = new Thickness(24);
        RootGrid.RowSpacing = 16;
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Spacing = 4 };
        header.Children.Add(new TextBlock
        {
            Text = "G HUB RGB Freestyle Injector",
            FontSize = 24,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        header.Children.Add(new TextBlock
        {
            Text = "Sincronização de presets Freestyle a partir de arquivos Markdown.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.78
        });
        RootGrid.Children.Add(header);

        StatusBar = new InfoBar
        {
            IsOpen = true,
            Severity = InfoBarSeverity.Informational,
            Title = "Pronto",
            Message = "Escolha a pasta de paletas e execute uma simulação antes de aplicar."
        };
        Grid.SetRow(StatusBar, 1);
        RootGrid.Children.Add(StatusBar);

        var installPanel = new StackPanel { Spacing = 12 };
        installPanel.Children.Add(new TextBlock
        {
            Text = "Instalação",
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        InstallStateText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.78
        };
        installPanel.Children.Add(InstallStateText);

        var wizardButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        wizardButtons.Children.Add(BuildWizardButton("Instalar", Install_Click));
        wizardButtons.Children.Add(BuildWizardButton("Atualizar", Update_Click));
        wizardButtons.Children.Add(BuildWizardButton("Reparar", Repair_Click));
        wizardButtons.Children.Add(BuildWizardButton("Desinstalar", Uninstall_Click));
        installPanel.Children.Add(wizardButtons);

        var installBorder = BuildPanel(installPanel);
        Grid.SetRow(installBorder, 2);
        RootGrid.Children.Add(installBorder);

        var inputPanel = new StackPanel { Spacing = 12 };
        inputPanel.Children.Add(new TextBlock
        {
            Text = "Entradas",
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });

        InputPathBox = new TextBox
        {
            Header = "Pasta das paletas",
            PlaceholderText = @"C:\Caminho\Para\RGB-palettes"
        };
        inputPanel.Children.Add(BuildPickerRow(InputPathBox, ChooseInput_Click));

        DbPathBox = new TextBox
        {
            Header = "settings.db do G HUB",
            PlaceholderText = @"C:\Users\...\AppData\Local\LGHUB\settings.db"
        };
        inputPanel.Children.Add(BuildPickerRow(DbPathBox, ChooseDb_Click));

        var inputBorder = BuildPanel(inputPanel);
        Grid.SetRow(inputBorder, 3);
        RootGrid.Children.Add(inputBorder);

        KillGHubBox = new CheckBox { Content = "Encerrar G HUB antes de aplicar", IsChecked = true };
        ForceBox = new CheckBox { Content = "Forçar regravação dos presets detectados" };
        PruneBox = new CheckBox { Content = "Remover presets RGB órfãos" };

        var optionsPanel = new StackPanel { Spacing = 12 };
        optionsPanel.Children.Add(new TextBlock
        {
            Text = "Opções",
            FontSize = 18,
            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }
        });
        optionsPanel.Children.Add(KillGHubBox);
        optionsPanel.Children.Add(ForceBox);
        optionsPanel.Children.Add(PruneBox);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        buttons.Children.Add(new Button { Content = "Listar" });
        ((Button)buttons.Children[^1]).Click += List_Click;
        buttons.Children.Add(new Button { Content = "Simular" });
        ((Button)buttons.Children[^1]).Click += DryRun_Click;
        buttons.Children.Add(new Button { Content = "Aplicar" });
        ((Button)buttons.Children[^1]).Click += Apply_Click;
        buttons.Children.Add(new Button { Content = "Encerrar G HUB" });
        ((Button)buttons.Children[^1]).Click += KillGHub_Click;
        optionsPanel.Children.Add(buttons);

        var optionsBorder = BuildPanel(optionsPanel);
        Grid.SetRow(optionsBorder, 4);
        RootGrid.Children.Add(optionsBorder);

        OutputBox = new TextBox
        {
            Header = "Saída",
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Consolas"),
            MinHeight = 220
        };
        ScrollViewer.SetVerticalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(OutputBox, ScrollBarVisibility.Auto);
        var outputBorder = BuildPanel(OutputBox);
        Grid.SetRow(outputBorder, 5);
        RootGrid.Children.Add(outputBorder);
    }

    private static Border BuildPanel(UIElement content)
    {
        return new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0x33, 0x66, 0x66, 0x66)),
            Child = content
        };
    }

    private static Grid BuildPickerRow(TextBox textBox, RoutedEventHandler clickHandler)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(textBox);

        var button = new Button
        {
            Content = "Escolher",
            Margin = new Thickness(0, 28, 0, 0)
        };
        button.Click += clickHandler;
        Grid.SetColumn(button, 1);
        row.Children.Add(button);
        return row;
    }


    private static Button BuildWizardButton(string text, RoutedEventHandler clickHandler)
    {
        var button = new Button { Content = text };
        button.Click += clickHandler;
        return button;
    }

    private void TryApplyBackdrop()
    {
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            catch
            {
                // Older Windows builds can still render the normal WinUI surface.
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
            StatusBar.Severity = InfoBarSeverity.Success;
            StatusBar.Title = "Instalação";
            StatusBar.Message = $"{action} concluído.";
            AppendLog($"{action}: concluído.");
        }
        catch (Exception ex)
        {
            StatusBar.Severity = InfoBarSeverity.Error;
            StatusBar.Title = "Instalação";
            StatusBar.Message = ex.Message;
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
        if (KillGHubBox.IsChecked == true) args.Add("--kill-ghub");
        if (ForceBox.IsChecked == true) args.Add("--force");
        if (PruneBox.IsChecked == true) args.Add("--prune");
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
            StatusBar.Severity = result.ExitCode == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            StatusBar.Title = result.ExitCode == 0 ? "Concluído" : "Falhou";
            StatusBar.Message = $"ghub-freestyle terminou com código {result.ExitCode}.";
        }
        catch (Exception ex)
        {
            AppendLog("ERRO: " + ex);
            StatusBar.Severity = InfoBarSeverity.Error;
            StatusBar.Title = "Erro";
            StatusBar.Message = ex.Message;
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
        StatusBar.Severity = InfoBarSeverity.Informational;
        StatusBar.Title = busy ? "Rodando" : StatusBar.Title;
        StatusBar.Message = busy ? "Executando ghub-freestyle..." : StatusBar.Message;
    }

    private sealed record CliResult(int ExitCode, string Output, string Error)
    {
        public override string ToString()
        {
            return string.Join(Environment.NewLine, new[] { Output, Error }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
