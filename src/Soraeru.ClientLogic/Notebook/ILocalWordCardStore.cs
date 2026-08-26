namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Raw persistence for local word cards (SQLite / in-memory / legacy JSON).
/// Implementations must retain rows for multiple OwnerUserId values in one store.
/// </summary>
public interface ILocalWordCardStore
{
    Task<IReadOnlyList<LocalWordCard>> LoadAllAsync(CancellationToken cancellationToken = default);

    Task SaveAllAsync(IReadOnlyList<LocalWordCard> cards, CancellationToken cancellationToken = default);
}
