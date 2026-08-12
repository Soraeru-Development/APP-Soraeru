namespace Soraeru.Infrastructure.Persistence.Entities;

public sealed class WordRegenerationEntity
{
    public Guid UserId { get; set; }

    public string SourceLanguage { get; set; } = string.Empty;

    public string NormalizedText { get; set; } = string.Empty;

    public int RegenerationCount { get; set; }
}
