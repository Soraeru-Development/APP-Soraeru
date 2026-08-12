namespace Soraeru.Infrastructure.Persistence.Entities;

public sealed class WordCardEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string SourceText { get; set; } = string.Empty;

    public string NormalizedText { get; set; } = string.Empty;

    public string DetectedLanguage { get; set; } = string.Empty;

    public string MeaningZh { get; set; } = string.Empty;

    public string Pronunciation { get; set; } = string.Empty;

    public string SelectedMnemonic { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
