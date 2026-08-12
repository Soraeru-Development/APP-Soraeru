namespace Soraeru.ClientLogic.Notebook;

/// <summary>
/// Placeholder mirror until ticket 15 ships push/pull API — sync becomes a no-op offline skip.
/// </summary>
public sealed class UnavailableCloudWordCardMirror : ICloudWordCardMirror
{
    public Task<CloudMirrorPullResult> PullAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CloudMirrorPullResult.Failure("MIRROR_UNAVAILABLE"));

    public Task<CloudMirrorPushResult> PushAsync(
        IReadOnlyList<LocalWordCard> cards,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CloudMirrorPushResult.Failure("MIRROR_UNAVAILABLE"));
}
