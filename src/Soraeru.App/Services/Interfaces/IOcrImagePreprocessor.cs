namespace Soraeru.Services.Interfaces;

/// <summary>
/// Optional local image enhance before Tesseract. Never uploads; may write a temp file under app cache.
/// </summary>
public interface IOcrImagePreprocessor
{
    /// <summary>
    /// Returns a path suitable for OCR. If <see cref="OcrPreparedImage.IsTemporary"/>,
    /// the caller must delete the file when finished.
    /// </summary>
    Task<OcrPreparedImage> PrepareForTesseractAsync(
        string localImagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits the image into left-to-right vertical strips (button-chip rows) for separate OCR.
    /// Empty list = platform does not support strips (caller skips). Temporary paths must be deleted by caller.
    /// </summary>
    Task<IReadOnlyList<OcrPreparedImage>> CreateVerticalStripsAsync(
        string localImagePath,
        int stripCount = 3,
        CancellationToken cancellationToken = default);
}

public sealed record OcrPreparedImage(string PathForOcr, bool IsTemporary);
