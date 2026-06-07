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
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        ExtendsContentIntoTitleBar = true;
        TryApplyBackdrop();
        InitializeDefaults();
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
        FullLogBox.Text = _log.ToString();
    }

    private void ClearLog()
    {
        _log.Clear();
        OutputBox.Text = string.Empty;
    }

    private void SetBusy(bool busy)
    {
        RootGrid.IsEnabled = !busy;
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
