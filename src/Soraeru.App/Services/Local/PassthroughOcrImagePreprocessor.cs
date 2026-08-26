using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>No-op preprocessor for non-Android targets / designer hosts.</summary>
public sealed class PassthroughOcrImagePreprocessor : IOcrImagePreprocessor
{
    public Task<OcrPreparedImage> PrepareForTesseractAsync(
        string localImagePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new OcrPreparedImage(localImagePath, IsTemporary: false));
    }

    public Task<IReadOnlyList<OcrPreparedImage>> CreateVerticalStripsAsync(
        string localImagePath,
        int stripCount = 3,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<OcrPreparedImage>>(Array.Empty<OcrPreparedImage>());
    }
}
