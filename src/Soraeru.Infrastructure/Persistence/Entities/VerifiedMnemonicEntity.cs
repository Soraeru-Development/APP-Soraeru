namespace Soraeru.Infrastructure.Persistence.Entities;

/// <summary>
/// Curated verified empty-ear entry (not a learner WordCard).
/// </summary>
public sealed class VerifiedMnemonicEntity
{
    public Guid Id { get; set; }

    public string Language { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public string NormalizedSource { get; set; } = string.Empty;

    public string DisplayText { get; set; } = string.Empty;

    public string NotationText { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
