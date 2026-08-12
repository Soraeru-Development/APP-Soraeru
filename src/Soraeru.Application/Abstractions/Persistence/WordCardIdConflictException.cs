namespace Soraeru.Application.Abstractions.Persistence;

/// <summary>
/// Thrown when Upsert would insert a card Id already owned by another user (global PK).
/// </summary>
public sealed class WordCardIdConflictException : Exception
{
    public WordCardIdConflictException(Guid cardId)
        : base($"Word card id {cardId} is already owned by another user.")
    {
        CardId = cardId;
    }

    public Guid CardId { get; }
}
