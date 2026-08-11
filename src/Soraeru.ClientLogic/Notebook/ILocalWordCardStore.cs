namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Raw persistence for local word cards (device file / in-memory).
/// </summary>
public interface ILocalWordCardStore
{
    Task<IReadOnlyList<LocalWordCard>> LoadAllAsync(CancellationToken cancellationToken = default);

    Task SaveAllAsync(IReadOnlyList<LocalWordCard> cards, CancellationToken cancellationToken = default);
}
