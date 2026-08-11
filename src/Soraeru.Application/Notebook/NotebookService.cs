using System.Text;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Common;

namespace Soraeru.Application.Notebook;

/// <summary>
/// Notebook (word card) CRUD use cases for the signed-in learner.
/// </summary>
public sealed class NotebookService : INotebookService
{
    private readonly IWordCardRepository _cards;

    public NotebookService(IWordCardRepository cards)
    {
        _cards = cards;
    }

    public async Task<ServiceResult<IReadOnlyList<NotebookCard>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<IReadOnlyList<NotebookCard>>.Failure("VALIDATION", "User id is required.");
        }

        var records = await _cards.ListByUserAsync(userId, cancellationToken);
        IReadOnlyList<NotebookCard> cards = records.Select(ToCard).ToList();
        return ServiceResult<IReadOnlyList<NotebookCard>>.Success(cards);
    }

    public async Task<ServiceResult<NotebookCard>> GetAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || cardId == Guid.Empty)
        {
            return ServiceResult<NotebookCard>.Failure("VALIDATION", "User id and card id are required.");
        }

        var record = await _cards.GetAsync(userId, cardId, cancellationToken);
        if (record is null)
        {
            return ServiceResult<NotebookCard>.Failure("NOT_FOUND", "Word card not found.");
        }

        return ServiceResult<NotebookCard>.Success(ToCard(record));
    }

    public async Task<ServiceResult<NotebookCard>> SaveAsync(
        SaveNotebookCardCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty || string.IsNullOrWhiteSpace(command.SourceText))
        {
            return ServiceResult<NotebookCard>.Failure("VALIDATION", "User id and source text are required.");
        }

        if (string.IsNullOrWhiteSpace(command.SelectedMnemonic))
        {
            return ServiceResult<NotebookCard>.Failure("VALIDATION", "Selected mnemonic is required.");
        }

        var language = string.IsNullOrWhiteSpace(command.DetectedLanguage)
            ? "und"
            : command.DetectedLanguage.Trim();
        var sourceText = command.SourceText.Trim();
        var normalizedText = NormalizeText(sourceText);

        var existing = await _cards.FindByUserLanguageAndNormalizedAsync(
            command.UserId,
            language,
            normalizedText,
            cancellationToken);
        if (existing is not null)
        {
            return ServiceResult<NotebookCard>.Success(ToCard(existing));
        }

        var record = new WordCardRecord(
            Guid.NewGuid(),
            command.UserId,
            sourceText,
            normalizedText,
            language,
            command.MeaningZh?.Trim() ?? string.Empty,
            command.Pronunciation?.Trim() ?? string.Empty,
            command.SelectedMnemonic.Trim(),
            DateTimeOffset.UtcNow);

        var saved = await _cards.AddAsync(record, cancellationToken);
        return ServiceResult<NotebookCard>.Success(ToCard(saved));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || cardId == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("VALIDATION", "User id and card id are required.");
        }

        var record = await _cards.GetAsync(userId, cardId, cancellationToken);
        if (record is null)
        {
            return ServiceResult<bool>.Failure("NOT_FOUND", "Word card not found.");
        }

        await _cards.DeleteAsync(userId, cardId, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    private static NotebookCard ToCard(WordCardRecord record) =>
        new(
            record.Id,
            record.SourceText,
            record.DetectedLanguage,
            record.MeaningZh,
            record.Pronunciation,
            record.SelectedMnemonic,
            record.CreatedAtUtc);

    private static string NormalizeText(string text)
    {
        var collapsed = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Normalize(NormalizationForm.FormC);
    }
}
