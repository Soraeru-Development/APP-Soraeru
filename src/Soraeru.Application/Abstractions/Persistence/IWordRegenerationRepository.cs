namespace Soraeru.Application.Abstractions.Persistence;

/// <summary>
/// Counts successful ForceRefresh regenerations per user + language + normalized text.
/// Shared contract with ticket 18「重新分析」— same key when ForceRefresh is used.
/// </summary>
public interface IWordRegenerationRepository
{
    Task<int> GetCountAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default);

    Task IncrementAsync(
        Guid userId,
        string sourceLanguage,
        string normalizedText,
        CancellationToken cancellationToken = default);
}
