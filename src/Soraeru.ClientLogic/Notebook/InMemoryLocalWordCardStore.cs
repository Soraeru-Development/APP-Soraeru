namespace Soraeru.ClientLogic.Notebook;

public sealed class InMemoryLocalWordCardStore : ILocalWordCardStore
{
    private readonly List<LocalWordCard> _cards = [];
    private readonly object _gate = new();

    public Task<IReadOnlyList<LocalWordCard>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<LocalWordCard> copy = _cards.ToList();
            return Task.FromResult(copy);
        }
    }

    public Task SaveAllAsync(IReadOnlyList<LocalWordCard> cards, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _cards.Clear();
            _cards.AddRange(cards);
        }

        return Task.CompletedTask;
    }
}
