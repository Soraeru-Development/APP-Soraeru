namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// In-memory cloud mirror for protocol tests (stand-in until ticket 15 API).
/// </summary>
public sealed class InMemoryCloudWordCardMirror : ICloudWordCardMirror
{
    private readonly List<LocalWordCard> _cards = [];
    private readonly object _gate = new();

    public bool IsOnline { get; set; } = true;

    public IReadOnlyList<LocalWordCard> Cards
    {
        get
        {
            lock (_gate)
                return _cards.ToList();
        }
    }

    public void Seed(IEnumerable<LocalWordCard> cards)
    {
        lock (_gate)
        {
            _cards.Clear();
            _cards.AddRange(cards);
        }
    }

    public Task<CloudMirrorPullResult> PullAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
            return Task.FromResult(CloudMirrorPullResult.Failure("OFFLINE"));

        lock (_gate)
            return Task.FromResult(CloudMirrorPullResult.Success(_cards.ToList()));
    }

    public Task<CloudMirrorPushResult> PushAsync(
        IReadOnlyList<LocalWordCard> cards,
        CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
            return Task.FromResult(CloudMirrorPushResult.Failure("OFFLINE"));

        lock (_gate)
        {
            _cards.Clear();
            _cards.AddRange(cards);
        }

        return Task.FromResult(CloudMirrorPushResult.Success());
    }
}
