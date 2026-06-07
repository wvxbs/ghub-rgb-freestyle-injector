using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace GHubFreestyleInjector.WinUI;

internal static class Program
{
    private static App? s_app;

    [STAThread]
    private static void Main(string[] args)
    {
        App.LogInfo("Program Main start");
        ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
            SynchronizationContext.SetSynchronizationContext(context);
            s_app = new App();
        });
        GC.KeepAlive(s_app);
        App.LogInfo("Program Main exit");
    }
}
