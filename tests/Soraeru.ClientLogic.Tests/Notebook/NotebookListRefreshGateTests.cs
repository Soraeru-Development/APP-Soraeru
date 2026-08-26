using Shouldly;
using Soraeru.ClientLogic.Notebook;

namespace Soraeru.ClientLogic.Tests.Notebook;

public sealed class NotebookListRefreshGateTests
{
    [Fact]
    public void NeedsReload_is_false_until_notify()
    {
        var gate = new NotebookListRefreshGate();

        gate.NeedsReload(lastLoadedVersion: gate.Version).ShouldBeFalse();
    }

    [Fact]
    public void NotifyDataMayHaveChanged_requires_list_reload()
    {
        var gate = new NotebookListRefreshGate();
        var loadedAt = gate.Version;

        gate.NotifyDataMayHaveChanged();

        gate.NeedsReload(loadedAt).ShouldBeTrue();
        gate.Version.ShouldBe(loadedAt + 1);
    }
}
