namespace Soraeru.Services.Interfaces;

/// <summary>
/// Ensures tessdata_fast packs exist under app cache (shipped core + optional downloads).
/// Never uploads OCR images — only downloads language model files.
/// </summary>
public interface ITessdataPackStore
{
    /// <summary>Directory that holds <c>*.traineddata</c> for Tesseract (app data).</summary>
    string CacheDirectory { get; }

    /// <summary>Whether a pack file is already present (shipped copy or prior download).</summary>
    bool IsPackPresent(string tessLanguageCode);

    /// <summary>
    /// Ensure each '+'separated / enumerable tess language exists locally.
    /// Downloads allowlisted missing packs from tessdata_fast; skips unknown codes.
    /// </summary>
    Task EnsurePacksAsync(
        IEnumerable<string> tessLanguageCodes,
        IProgress<TessdataPackProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record TessdataPackProgress(
    string LanguageCode,
    string Phase,
    double? Fraction);
