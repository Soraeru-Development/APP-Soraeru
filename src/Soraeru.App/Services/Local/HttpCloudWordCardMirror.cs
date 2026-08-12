using Soraeru.ClientLogic.Notebook;
using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Real cloud mirror over GET/PUT /api/v1/notebook/mirror (ticket 15).
/// </summary>
public sealed class HttpCloudWordCardMirror : ICloudWordCardMirror
{
    private readonly ISoraeruApiClient _api;

    public HttpCloudWordCardMirror(ISoraeruApiClient api)
    {
        _api = api;
    }

    public async Task<CloudMirrorPullResult> PullAsync(CancellationToken cancellationToken = default)
    {
        var result = await _api.PullNotebookMirrorAsync(cancellationToken);
        if (!result.IsSuccess || result.Cards is null)
        {
            return CloudMirrorPullResult.Failure(MapError(result.Failure));
        }

        return CloudMirrorPullResult.Success(result.Cards.Select(ToLocal).ToList());
    }

    public async Task<CloudMirrorPushResult> PushAsync(
        IReadOnlyList<LocalWordCard> cards,
        CancellationToken cancellationToken = default)
    {
        var payload = cards.Select(ToDto).ToList();
        var result = await _api.PushNotebookMirrorAsync(payload, cancellationToken);
        return result.IsSuccess
            ? CloudMirrorPushResult.Success()
            : CloudMirrorPushResult.Failure(MapError(result.Failure));
    }

    private static LocalWordCard ToLocal(NotebookMirrorCardDto dto) =>
        new(
            dto.Id,
            dto.OwnerUserId,
            dto.SourceText,
            dto.NormalizedText,
            dto.DetectedLanguage,
            dto.MeaningZh,
            dto.Pronunciation,
            dto.SelectedMnemonic,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            dto.DeletedAtUtc);

    private static NotebookMirrorCardDto ToDto(LocalWordCard card) =>
        new(
            card.Id,
            card.OwnerUserId,
            card.SourceText,
            card.NormalizedText,
            card.DetectedLanguage,
            card.MeaningZh,
            card.Pronunciation,
            card.SelectedMnemonic,
            card.CreatedAtUtc,
            card.UpdatedAtUtc,
            card.DeletedAtUtc);

    private static string MapError(NotebookFailureKind failure) =>
        failure switch
        {
            NotebookFailureKind.Unauthorized => "UNAUTHORIZED",
            NotebookFailureKind.Network => "OFFLINE",
            NotebookFailureKind.Validation => "VALIDATION",
            _ => "MIRROR_FAILED"
        };
}
