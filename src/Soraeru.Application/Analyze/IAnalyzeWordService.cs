using Soraeru.Application.Common;

namespace Soraeru.Application.Analyze;

public interface IAnalyzeWordService
{
    Task<ServiceResult<AnalyzeWordResult>> AnalyzeAsync(
        AnalyzeWordCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record AnalyzeWordCommand(
    Guid UserId,
    string Text,
    string? SourceLanguage,
    string? MemoryLanguage,
    string? NotationPreference,
    bool ForceRefresh = false);

public sealed record AnalyzeWordResult(
    string SourceText,
    string NormalizedText,
    string SourceLanguage,
    string LanguageDisplayName,
    string Meaning,
    string ReadingText,
    IReadOnlyList<AnalyzeMnemonicCandidate> Mnemonics,
    string Notice,
    bool Cached,
    int RemainingDailyQuota,
    string MnemonicSource,
    int RemainingRegenerations = AnalyzeWordService.MaxRegenerationsPerWord);

public sealed record AnalyzeMnemonicCandidate(
    string DisplayText,
    string NotationType,
    string NotationText,
    string Explanation);

/// <summary>
/// Distinguishes curated verified mnemonics vs LLM draft (glossary: LLM 草稿標示).
/// </summary>
public static class AnalyzeMnemonicSources
{
    public const string LlmDraft = "llm_draft";
    public const string Verified = "verified";
}
