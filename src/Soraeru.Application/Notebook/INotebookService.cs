using Soraeru.Application.Common;

namespace Soraeru.Application.Notebook;

public interface INotebookService
{
    Task<ServiceResult<IReadOnlyList<NotebookCard>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<NotebookCard>> GetAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<NotebookCard>> SaveAsync(
        SaveNotebookCardCommand command,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    /// <summary>Pull cloud mirror rows for the user (includes tombstones).</summary>
    Task<ServiceResult<IReadOnlyList<MirrorWordCard>>> PullMirrorAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Push cards with whole-card LWW upsert by stable id (not replace-all).</summary>
    Task<ServiceResult<bool>> PushMirrorAsync(
        Guid userId,
        IReadOnlyList<MirrorWordCard> cards,
        CancellationToken cancellationToken = default);
}

public sealed record SaveNotebookCardCommand(
    Guid UserId,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic);

/// <summary>Web-transition CRUD view (tombstones hidden).</summary>
public sealed record NotebookCard(
    Guid Id,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Cloud mirror row aligned with App <c>LocalWordCard</c> sync fields.</summary>
public sealed record MirrorWordCard(
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
