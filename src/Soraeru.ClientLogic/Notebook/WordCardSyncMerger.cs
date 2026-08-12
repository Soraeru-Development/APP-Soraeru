namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Whole-card LWW merge for optional cloud sync (ADR-0007 / ticket 14).
/// Union by stable card Id; newer <see cref="LocalWordCard.UpdatedAtUtc"/> wins;
/// equal timestamps keep the local side (client-first).
/// </summary>
public static class WordCardSyncMerger
{
    public static IReadOnlyList<LocalWordCard> Merge(
        IReadOnlyList<LocalWordCard> local,
        IReadOnlyList<LocalWordCard> remote)
    {
        var byId = new Dictionary<Guid, LocalWordCard>();

        foreach (var card in local)
            byId[card.Id] = card;

        foreach (var remoteCard in remote)
        {
            if (!byId.TryGetValue(remoteCard.Id, out var localCard))
            {
                byId[remoteCard.Id] = remoteCard;
                continue;
            }

            byId[remoteCard.Id] = PickWinner(localCard, remoteCard);
        }

        return byId.Values.ToList();
    }

    private static LocalWordCard PickWinner(LocalWordCard local, LocalWordCard remote)
    {
        if (remote.UpdatedAtUtc > local.UpdatedAtUtc)
            return remote;

        return local;
    }
}
