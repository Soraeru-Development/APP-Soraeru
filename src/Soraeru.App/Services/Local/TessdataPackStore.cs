using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Core packs ship in the APK; allowlisted extras download on demand from tessdata_fast (GitHub raw).
/// </summary>
public sealed class TessdataPackStore : ITessdataPackStore
{
    public const string TessdataFastRawBaseUrl =
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/";

    /// <summary>
    /// Packs that may be downloaded when missing (not all need to ship in APK).
    /// Proof set: ara (usually already shipped), deu / fra (Latin extras).
    /// </summary>
    public static readonly HashSet<string> DownloadAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "ara",
        "deu",
        "fra",
    };

    readonly HttpClient _http;
    readonly string _cacheDirectory;
    readonly Func<string, string?> _packagedAssetPathResolver;

    public TessdataPackStore(
        HttpClient http,
        string cacheDirectory,
        Func<string, string?>? packagedAssetPathResolver = null)
    {
        _http = http;
        _cacheDirectory = cacheDirectory;
        _packagedAssetPathResolver = packagedAssetPathResolver ?? (_ => null);
        Directory.CreateDirectory(_cacheDirectory);
    }

    public string CacheDirectory => _cacheDirectory;

    public bool IsPackPresent(string tessLanguageCode)
    {
        var file = PackFileName(tessLanguageCode);
        if (File.Exists(Path.Combine(_cacheDirectory, file)))
            return true;

        var packaged = _packagedAssetPathResolver(file);
        return !string.IsNullOrWhiteSpace(packaged) && File.Exists(packaged);
    }

    public async Task EnsurePacksAsync(
        IEnumerable<string> tessLanguageCodes,
        IProgress<TessdataPackProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var code in ExpandCodes(tessLanguageCodes))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsPackPresent(code))
            {
                progress?.Report(new TessdataPackProgress(code, "present", 1));
                continue;
            }

            if (!DownloadAllowlist.Contains(code))
            {
                progress?.Report(new TessdataPackProgress(code, "skipped", null));
                continue;
            }

            progress?.Report(new TessdataPackProgress(code, "downloading", 0));
            await DownloadPackAsync(code, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report(new TessdataPackProgress(code, "ready", 1));
        }
    }

    async Task DownloadPackAsync(
        string code,
        IProgress<TessdataPackProgress>? progress,
        CancellationToken cancellationToken)
    {
        var fileName = PackFileName(code);
        var url = TessdataFastRawBaseUrl + fileName;
        var target = Path.Combine(_cacheDirectory, fileName);
        var temp = target + ".tmp";

        await using (var remote = await _http.GetStreamAsync(url, cancellationToken).ConfigureAwait(false))
        await using (var local = File.Create(temp))
        {
            var buffer = new byte[81920];
            long copied = 0;
            int read;
            while ((read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                copied += read;
                // Unknown content-length → pulse progress without a hard fraction.
                progress?.Report(new TessdataPackProgress(code, "downloading", null));
                _ = copied;
            }
        }

        File.Move(temp, target, overwrite: true);
    }

    public static IEnumerable<string> ExpandCodes(IEnumerable<string> codes) =>
        codes
            .SelectMany(c => c.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeCode)
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static string PackFileName(string tessLanguageCode) =>
        NormalizeCode(tessLanguageCode) + ".traineddata";

    static string NormalizeCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.EndsWith(".traineddata", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^".traineddata".Length];
        return trimmed.ToLowerInvariant();
    }
}
