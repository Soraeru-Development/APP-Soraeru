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
}

public sealed record SaveNotebookCardCommand(
    Guid UserId,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic);

public sealed record NotebookCard(
    Guid Id,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc);
