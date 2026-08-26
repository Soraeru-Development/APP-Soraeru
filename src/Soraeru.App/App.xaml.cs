using Microsoft.Extensions.DependencyInjection;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru;

public partial class App : Application
{
    private int _syncGate;

    public App()
    {
        InitializeComponent();
        // Product tokens / pages are light-surface only. Follow system dark would flip
        // default Entry TextColor to white while Login still paints SurfaceBright → invisible text.
        UserAppTheme = AppTheme.Light;
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
            services?.GetService<NotebookListRefreshGate>()?.NotifyDataMayHaveChanged();
            var shell = Shell.Current as AppShell
                ?? Current?.Windows.Select(w => w.Page).OfType<AppShell>().FirstOrDefault();
            if (shell is not null)
                await shell.RefreshNotebookListIfSelectedAsync();
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
