namespace Soraeru.Services.Interfaces;

/// <summary>
/// HTTP boundary to Soraeru.Api. UI/ViewModels should call this, not Pages embedding HttpClient.
/// </summary>
public interface ISoraeruApiClient
{
    Task<AuthResult> LoginWithEmailAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterWithEmailAsync(
        string email,
        string password,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task<MeProfileDto?> GetMeAsync(CancellationToken cancellationToken = default);

    Task<MeProfileDto?> PatchMeAsync(bool? onboardingCompleted = null, CancellationToken cancellationToken = default);

    Task<AnalyzeApiResult> AnalyzeWordAsync(AnalyzeRequestDto request, CancellationToken cancellationToken = default);

    Task<NotebookApiResult> SaveNotebookCardAsync(
        SaveNotebookCardRequestDto request,
        CancellationToken cancellationToken = default);

    Task<NotebookListApiResult> ListNotebookCardsAsync(CancellationToken cancellationToken = default);

    Task<NotebookApiResult> GetNotebookCardAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<NotebookActionApiResult> DeleteNotebookCardAsync(Guid cardId, CancellationToken cancellationToken = default);

    /// <summary>GET /api/v1/notebook/mirror — pull cloud mirror rows (includes tombstones).</summary>
    Task<NotebookMirrorPullApiResult> PullNotebookMirrorAsync(CancellationToken cancellationToken = default);

    /// <summary>PUT /api/v1/notebook/mirror — whole-card LWW upsert push.</summary>
    Task<NotebookActionApiResult> PushNotebookMirrorAsync(
        IReadOnlyList<NotebookMirrorCardDto> cards,
        CancellationToken cancellationToken = default);

    /// <summary>DELETE /api/v1/me — removes cloud notebook mirror + account.</summary>
    Task<DeleteAccountApiClientResult> DeleteAccountAsync(CancellationToken cancellationToken = default);
}

public sealed record AuthSessionDto(
    Guid UserId,
    string Email,
    string AccessToken,
    bool OnboardingCompleted,
    bool IsDeveloper = false);

public sealed record MeProfileDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string PlanTier,
    int DailyQuota,
    int RemainingDailyQuota,
    bool IsDeveloper,
    string NotationPref,
    bool OnboardingCompleted,
    bool HasPassword = true,
    bool HasGoogleSubject = false);

public sealed record AnalyzeRequestDto(
    string Text,
    string SourceLanguage,
    string MemoryLanguage,
    string NotationPreference,
    bool ForceRefresh = false);

public sealed record AnalyzeResultDto(
    string SourceText,
    string NormalizedText,
    string SourceLanguage,
    string LanguageDisplayName,
    string Meaning,
    string ReadingText,
    IReadOnlyList<AnalyzeMnemonicDto> Mnemonics,
    string Notice,
    bool Cached,
    int RemainingDailyQuota,
    string MnemonicSource = "llm_draft",
    int RemainingRegenerations = 3);

public sealed record AnalyzeMnemonicDto(
    string DisplayText,
    string NotationType,
    string NotationText,
    string Explanation);

public enum AnalyzeFailureKind
{
    None,
    Validation,
    QuotaExceeded,
    RegenerationLimit,
    AnalyzeFailed,
    Unauthorized,
    LlmNotConfigured,
    ServerError,
    Network
}

public sealed record AnalyzeApiResult(AnalyzeResultDto? Result, AnalyzeFailureKind Failure, string? Message)
{
    public bool IsSuccess => Result is not null;

    public static AnalyzeApiResult Success(AnalyzeResultDto result) =>
        new(result, AnalyzeFailureKind.None, null);

    public static AnalyzeApiResult Fail(AnalyzeFailureKind kind, string message) =>
        new(null, kind, message);
}

public sealed record SaveNotebookCardRequestDto(
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic);

public sealed record NotebookCardDto(
    Guid Id,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc = default);

public sealed record NotebookMirrorCardDto(
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

public sealed record NotebookMirrorPullApiResult(
    IReadOnlyList<NotebookMirrorCardDto>? Cards,
    NotebookFailureKind Failure,
    string? Message)
{
    public bool IsSuccess => Cards is not null;

    public static NotebookMirrorPullApiResult Success(IReadOnlyList<NotebookMirrorCardDto> cards) =>
        new(cards, NotebookFailureKind.None, null);

    public static NotebookMirrorPullApiResult Fail(NotebookFailureKind kind, string message) =>
        new(null, kind, message);
}

public enum NotebookFailureKind
{
    None,
    Validation,
    NotFound,
    Unauthorized,
    ServerError,
    Network
}

public sealed record NotebookApiResult(NotebookCardDto? Card, NotebookFailureKind Failure, string? Message)
{
    public bool IsSuccess => Card is not null;

    public static NotebookApiResult Success(NotebookCardDto card) =>
        new(card, NotebookFailureKind.None, null);

    public static NotebookApiResult Fail(NotebookFailureKind kind, string message) =>
        new(null, kind, message);
}

public sealed record NotebookListApiResult(
    IReadOnlyList<NotebookCardDto>? Cards,
    NotebookFailureKind Failure,
    string? Message)
{
    public bool IsSuccess => Cards is not null;

    public static NotebookListApiResult Success(IReadOnlyList<NotebookCardDto> cards) =>
        new(cards, NotebookFailureKind.None, null);

    public static NotebookListApiResult Fail(NotebookFailureKind kind, string message) =>
        new(null, kind, message);
}

public sealed record NotebookActionApiResult(bool Ok, NotebookFailureKind Failure, string? Message)
{
    public bool IsSuccess => Ok;

    public static NotebookActionApiResult Success() =>
        new(true, NotebookFailureKind.None, null);

    public static NotebookActionApiResult Fail(NotebookFailureKind kind, string message) =>
        new(false, kind, message);
}

public enum DeleteAccountFailureKind
{
    None,
    Unauthorized,
    ServerError,
    Network
}

public sealed record DeleteAccountApiClientResult(bool Ok, DeleteAccountFailureKind Failure, string? Message)
{
    public bool IsSuccess => Ok;

    public static DeleteAccountApiClientResult Success() =>
        new(true, DeleteAccountFailureKind.None, null);

    public static DeleteAccountApiClientResult Fail(DeleteAccountFailureKind kind, string message) =>
        new(false, kind, message);
}

public enum AuthFailureKind
{
    None,
    InvalidCredentials,
    Conflict,
    ServerRejected,
    Network
}

public sealed record AuthResult(AuthSessionDto? Session, AuthFailureKind Failure, string? Message)
{
    public bool IsSuccess => Session is not null;

    public static AuthResult Success(AuthSessionDto session) =>
        new(session, AuthFailureKind.None, null);

    public static AuthResult Fail(AuthFailureKind kind, string message) =>
        new(null, kind, message);
}
