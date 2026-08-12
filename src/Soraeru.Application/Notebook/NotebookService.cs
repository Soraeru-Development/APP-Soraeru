using System.Text;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Common;

namespace Soraeru.Application.Notebook;

/// <summary>
/// Cloud notebook mirror use cases (ADR-0007). CRUD hides tombstones for Web transition.
/// Save 同鍵回傳既有鏡像列、不覆寫 SelectedMnemonic（金標／再存不得強蓋個人空耳）。
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
        IReadOnlyList<NotebookCard> cards = records
            .Where(r => r.DeletedAtUtc is null)
            .Select(ToCard)
            .ToList();
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
        if (record is null || record.DeletedAtUtc is not null)
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
        if (existing is not null && existing.DeletedAtUtc is null)
        {
            return ServiceResult<NotebookCard>.Success(ToCard(existing));
        }

        var now = DateTimeOffset.UtcNow;
        var record = new WordCardRecord(
            Guid.NewGuid(),
            command.UserId,
            sourceText,
            normalizedText,
            language,
            command.MeaningZh?.Trim() ?? string.Empty,
            command.Pronunciation?.Trim() ?? string.Empty,
            command.SelectedMnemonic.Trim(),
            now,
            now);

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
        if (record is null || record.DeletedAtUtc is not null)
        {
            return ServiceResult<bool>.Failure("NOT_FOUND", "Word card not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var tombstone = record with { DeletedAtUtc = now, UpdatedAtUtc = now };
        await _cards.UpsertAsync(tombstone, cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyList<MirrorWordCard>>> PullMirrorAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<IReadOnlyList<MirrorWordCard>>.Failure("VALIDATION", "User id is required.");
        }

        var records = await _cards.ListByUserAsync(userId, cancellationToken);
        IReadOnlyList<MirrorWordCard> cards = records.Select(ToMirror).ToList();
        return ServiceResult<IReadOnlyList<MirrorWordCard>>.Success(cards);
    }

    public async Task<ServiceResult<bool>> PushMirrorAsync(
        Guid userId,
        IReadOnlyList<MirrorWordCard> cards,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("VALIDATION", "User id is required.");
        }

        cards ??= Array.Empty<MirrorWordCard>();
        foreach (var card in cards)
        {
            if (card.Id == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("VALIDATION", "Card id is required.");
            }

            if (card.OwnerUserId != userId)
            {
                return ServiceResult<bool>.Failure("FORBIDDEN", "Cannot write another user's mirror.");
            }
        }

        // Pre-check global Id occupancy so a mid-batch conflict does not partially write.
        foreach (var card in cards)
        {
            var byId = await _cards.GetByIdAsync(card.Id, cancellationToken);
            if (byId is not null && byId.UserId != userId)
            {
                return ServiceResult<bool>.Failure(
                    "CONFLICT",
                    "Card id is already owned by another user.");
            }
        }

        foreach (var card in cards)
        {
            var incoming = ToRecord(userId, card);
            var existing = await _cards.GetAsync(userId, card.Id, cancellationToken);
            if (existing is null || card.UpdatedAtUtc > existing.UpdatedAtUtc)
            {
                await _cards.UpsertAsync(incoming, cancellationToken);
            }
        }

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
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static MirrorWordCard ToMirror(WordCardRecord record) =>
        new(
            record.Id,
            record.UserId,
            record.SourceText,
            record.NormalizedText,
            record.DetectedLanguage,
            record.MeaningZh,
            record.Pronunciation,
            record.SelectedMnemonic,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.DeletedAtUtc);

    private static WordCardRecord ToRecord(Guid userId, MirrorWordCard card) =>
        new(
            card.Id,
            userId,
            card.SourceText,
            card.NormalizedText,
            card.DetectedLanguage,
            card.MeaningZh,
            card.Pronunciation,
            card.SelectedMnemonic,
            card.CreatedAtUtc,
            card.UpdatedAtUtc,
            card.DeletedAtUtc);

    private static string NormalizeText(string text)
    {
        var collapsed = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Normalize(NormalizationForm.FormC);
    }
}
