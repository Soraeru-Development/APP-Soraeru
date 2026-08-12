using Microsoft.Extensions.DependencyInjection;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru;

public partial class App : Application
{
    private int _syncGate;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Created += (_, _) => _ = TrySyncAsync();
        window.Resumed += (_, _) => _ = TrySyncAsync();
        return window;
    }

    async Task TrySyncAsync()
    {
        // Prevent overlapping push/pull from Created + Resumed or rapid resume.
        if (Interlocked.CompareExchange(ref _syncGate, 1, 0) != 0)
            return;

        try
        {
            var services = Handler?.MauiContext?.Services
                ?? Current?.Handler?.MauiContext?.Services;
            var sync = services?.GetService<NotebookSyncCoordinator>();
            if (sync is null)
                return;

            await sync.SyncAsync();
        }
        catch
        {
            // Sync is best-effort; never block App lifecycle.
        }
        finally
        {
            Interlocked.Exchange(ref _syncGate, 0);
        }
    }
}
