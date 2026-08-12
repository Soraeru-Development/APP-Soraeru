using Soraeru.ClientLogic.Tts;

namespace Soraeru.Services.Interfaces;

public enum FormalTtsFailureKind
{
    None,
    EmptySourceText,
    LocaleUnavailable,
    SpeakFailed
}

public sealed record FormalTtsPlayResult(
    bool Success,
    FormalTtsFailureKind Failure,
    string? Message)
{
    public static FormalTtsPlayResult Ok() =>
        new(true, FormalTtsFailureKind.None, null);

    public static FormalTtsPlayResult Fail(FormalTtsFailureKind kind, string message) =>
        new(false, kind, message);
}

/// <summary>
/// Plays formal source text via on-device system TTS only (no cloud TTS).
/// </summary>
public interface IFormalTtsService
{
    Task<FormalTtsPlayResult> SpeakFormalSourceAsync(
        string? sourceText,
        string? sourceLanguage,
        CancellationToken cancellationToken = default);
}
