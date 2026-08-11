namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Local word-card document (App SoT). UpdatedAt / DeletedAt reserved for sync (ticket 14).
/// </summary>
public sealed record LocalWordCard(
    Guid Id,
    Guid OwnerUserId,
    string SourceText,
    string NormalizedText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc);
