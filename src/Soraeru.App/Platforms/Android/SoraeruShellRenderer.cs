using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace Soraeru.Platforms.Android;

/// <summary>
/// Android Shell does not pop the tab stack when the selected tab is tapped again.
/// Force 首頁 reselect → absolute L05 Home root.
/// </summary>
public sealed class SoraeruShellRenderer : ShellRenderer
{
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem) =>
        new SoraeruShellItemRenderer(this);
}

sealed class SoraeruShellItemRenderer : ShellItemRenderer
{
    public SoraeruShellItemRenderer(IShellContext shellContext) : base(shellContext)
    {
    }

    protected override void OnTabReselected(ShellSection shellSection)
    {
        base.OnTabReselected(shellSection);

        if (!IsHomeSection(shellSection))
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await AppShell.OnHomeTabReselectedAsync();
        });
    }

    static bool IsHomeSection(ShellSection? section)
    {
        if (section is null)
            return false;

        if (string.Equals(section.Route, Routes.Home, StringComparison.Ordinal))
            return true;

        return section.Items.Any(c =>
            string.Equals(c.Route, Routes.Home, StringComparison.Ordinal));
    }
}
