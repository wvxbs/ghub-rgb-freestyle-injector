using Microsoft.UI.Xaml;

namespace GHubFreestyleInjector.WinUI;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        UnhandledException += App_UnhandledException;

        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            throw;
        }
    }

    private static void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GHubFreestyleInjector");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "winui-crash.log"),
                $"{DateTimeOffset.Now:O}\n{ex}\n\n");
        }
        catch
        {
            // Last-chance logging must never hide the original startup failure.
        }
    }
}
