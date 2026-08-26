namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Client-first notebook: local SoT with write gate = authenticated session.
/// </summary>
public sealed class LocalNotebookService
{
    private readonly ILocalWordCardStore _store;
    private readonly Func<CancellationToken, Task<LocalSession>> _session;

    public LocalNotebookService(
        ILocalWordCardStore store,
        Func<CancellationToken, Task<LocalSession>> session)
    {
        _store = store;
        _session = session;
    }

    /// <summary>Convenience for sync test/session sources.</summary>
    public LocalNotebookService(ILocalWordCardStore store, Func<LocalSession> session)
        : this(store, _ => Task.FromResult(session()))
    {
    }

    public async Task<bool> CanWriteAsync(CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        return session.IsAuthenticated && session.UserId is { } id && id != Guid.Empty;
    }

    public async Task<IReadOnlyList<LocalWordCard>> ListAsync(CancellationToken cancellationToken = default)
    {
        var all = await _store.LoadAllAsync(cancellationToken);
        var session = await _session(cancellationToken);

        IEnumerable<LocalWordCard> active = all.Where(c => c.DeletedAtUtc is null);

        if (session.IsAuthenticated && session.UserId is { } userId)
            active = active.Where(c => c.OwnerUserId == userId);

        return active
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToList();
    }

    public async Task<LocalNotebookResult<LocalWordCard>> SaveAsync(
        SaveLocalWordCardCommand command,
        CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        if (!session.IsAuthenticated || session.UserId is not { } userId || userId == Guid.Empty)
        {
            return LocalNotebookResult<LocalWordCard>.Failure(
                "UNAUTHORIZED",
                "登入後才能儲存單字卡。");
        }

        if (string.IsNullOrWhiteSpace(command.SourceText))
        {
            return LocalNotebookResult<LocalWordCard>.Failure(
                "VALIDATION",
                "原文不可為空。");
        }

        if (string.IsNullOrWhiteSpace(command.SelectedMnemonic))
        {
            return LocalNotebookResult<LocalWordCard>.Failure(
                "VALIDATION",
                "請選擇空耳候選。");
        }

        var language = string.IsNullOrWhiteSpace(command.DetectedLanguage)
            ? "und"
            : command.DetectedLanguage.Trim();
        var sourceText = command.SourceText.Trim();
        var normalizedText = string.IsNullOrWhiteSpace(command.NormalizedText)
            ? NormalizeText(sourceText)
            : NormalizeText(command.NormalizedText);

        var all = (await _store.LoadAllAsync(cancellationToken)).ToList();
        var existing = all.FirstOrDefault(c =>
            c.DeletedAtUtc is null
            && c.OwnerUserId == userId
            && string.Equals(c.DetectedLanguage, language, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.NormalizedText, normalizedText, StringComparison.Ordinal));

        // 同鍵已有卡：回傳既有列，不覆寫個人空耳（ADR-0007／票 17：金標或再存不得強蓋）。
        // 個人空耳編修走 UpdateSelectedMnemonicAsync（票 16）。
        if (existing is not null)
        {
            return LocalNotebookResult<LocalWordCard>.Success(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var meaningZh = command.MeaningZh?.Trim() ?? string.Empty;
        var pronunciation = command.Pronunciation?.Trim() ?? string.Empty;
        var selectedMnemonic = command.SelectedMnemonic.Trim();

        var card = new LocalWordCard(
            Guid.NewGuid(),
            userId,
            sourceText,
            normalizedText,
            language,
            meaningZh,
            pronunciation,
            selectedMnemonic,
            now,
            now,
            DeletedAtUtc: null);

        all.Add(card);
        await _store.SaveAllAsync(all, cancellationToken);
        return LocalNotebookResult<LocalWordCard>.Success(card);
    }

    public async Task<LocalNotebookResult<LocalWordCard>> UpdateSelectedMnemonicAsync(
        Guid cardId,
        string selectedMnemonic,
        CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        if (!session.IsAuthenticated || session.UserId is not { } userId || userId == Guid.Empty)
        {
            return LocalNotebookResult<LocalWordCard>.Failure(
                "UNAUTHORIZED",
                "登入後才能編修個人空耳。");
        }

        if (cardId == Guid.Empty)
        {
            return LocalNotebookResult<LocalWordCard>.Failure("VALIDATION", "單字卡編號無效。");
        }

        if (string.IsNullOrWhiteSpace(selectedMnemonic))
        {
            return LocalNotebookResult<LocalWordCard>.Failure(
                "VALIDATION",
                "個人空耳不可為空。");
        }

        var all = (await _store.LoadAllAsync(cancellationToken)).ToList();
        var index = all.FindIndex(c =>
            c.Id == cardId
            && c.OwnerUserId == userId
            && c.DeletedAtUtc is null);

        if (index < 0)
        {
            return LocalNotebookResult<LocalWordCard>.Failure("NOT_FOUND", "找不到單字卡。");
        }

        var now = DateTimeOffset.UtcNow;
        var existing = all[index];
        var updated = existing with
        {
            SelectedMnemonic = selectedMnemonic.Trim(),
            UpdatedAtUtc = now
        };
        all[index] = updated;
        await _store.SaveAllAsync(all, cancellationToken);
        return LocalNotebookResult<LocalWordCard>.Success(updated);
    }

    public async Task<LocalNotebookResult<bool>> DeleteAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        if (!session.IsAuthenticated || session.UserId is not { } userId || userId == Guid.Empty)
        {
            return LocalNotebookResult<bool>.Failure(
                "UNAUTHORIZED",
                "登入後才能刪除單字卡。");
        }

        if (cardId == Guid.Empty)
        {
            return LocalNotebookResult<bool>.Failure("VALIDATION", "單字卡編號無效。");
        }

        var all = (await _store.LoadAllAsync(cancellationToken)).ToList();
        var index = all.FindIndex(c =>
            c.Id == cardId
            && c.OwnerUserId == userId
            && c.DeletedAtUtc is null);

        if (index < 0)
        {
            return LocalNotebookResult<bool>.Failure("NOT_FOUND", "找不到單字卡。");
        }

        var now = DateTimeOffset.UtcNow;
        var existing = all[index];
        all[index] = existing with { DeletedAtUtc = now, UpdatedAtUtc = now };
        await _store.SaveAllAsync(all, cancellationToken);
        return LocalNotebookResult<bool>.Success(true);
    }

    /// <summary>
    /// Remove only the current session owner's rows (delete-account). Other users' cards stay.
    /// When anonymous / no usable user id, this is a no-op (never wipe the whole DB).
    /// </summary>
    public async Task ClearLocalNotebookAsync(CancellationToken cancellationToken = default)
    {
        var session = await _session(cancellationToken);
        if (!session.IsAuthenticated || session.UserId is not { } userId || userId == Guid.Empty)
            return;

        var all = await _store.LoadAllAsync(cancellationToken);
        var remaining = all.Where(c => c.OwnerUserId != userId).ToList();
        if (remaining.Count == all.Count)
            return;

        await _store.SaveAllAsync(remaining, cancellationToken);
    }

    /// <summary>
    /// Session-filter safety: list already scopes by OwnerUserId. Multi-user rows coexist in SoT;
    /// this no longer deletes other owners' cards.
    /// </summary>
    public Task EnsureOwnerIsolationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = userId;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    public async Task<LocalWordCard?> GetAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        var list = await ListAsync(cancellationToken);
        return list.FirstOrDefault(c => c.Id == cardId);
    }

    /// <summary>
    /// Local SoT lookup for short-circuit (ticket 18). Never queries cloud mirror.
    /// Requires a usable language code; otherwise returns null (caller must analyze).
    /// </summary>
    public async Task<LocalWordCard?> FindActiveByLookupKeyAsync(
        string text,
        string? detectedLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!LocalNotebookLookupKey.HasUsableLanguageCode(detectedLanguage))
            return null;

        var language = detectedLanguage!.Trim();
        var normalized = LocalNotebookLookupKey.NormalizeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var list = await ListAsync(cancellationToken);
        return list.FirstOrDefault(c =>
            string.Equals(c.DetectedLanguage, language, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.NormalizedText, normalized, StringComparison.Ordinal));
    }

    private static string NormalizeText(string text) => LocalNotebookLookupKey.NormalizeText(text);
}
