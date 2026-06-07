using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GHubFreestyleInjector.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private InfoBar StatusBar = null!;
    private TextBox InputPathBox = null!;
    private TextBox DbPathBox = null!;
    private TextBox OutputBox = null!;
    private CheckBox KillGHubBox = null!;
    private CheckBox ForceBox = null!;
    private CheckBox PruneBox = null!;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        App.LogInfo("MainWindow InitializeComponent start");
        InitializeComponent();
        App.LogInfo("MainWindow InitializeComponent done");
        BuildUi();
        App.LogInfo("MainWindow BuildUi done");

        _hwnd = WindowNative.GetWindowHandle(this);
        App.LogInfo($"HWND acquired: {_hwnd}");
        ExtendsContentIntoTitleBar = true;
        TryApplyBackdrop();
        App.LogInfo("Backdrop step done");
        InitializeDefaults();
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
        Grid.SetRow(inputBorder, 2);
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
        Grid.SetRow(optionsBorder, 3);
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
        Grid.SetRow(outputBorder, 4);
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
