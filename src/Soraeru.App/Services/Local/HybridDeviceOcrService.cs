using Soraeru.Services.Interfaces;
using TesseractOcrMaui;
using TesseractOcrMaui.Results;
using TesseractOcrMaui.Tessdata;

namespace Soraeru.Services.Local;

/// <summary>
/// Hybrid on-device OCR: ML Kit (Latin/CJK/Devanagari) first, then Tesseract tessdata_fast
/// for scripts without ML Kit modules. Images never leave the device.
/// </summary>
public sealed class HybridDeviceOcrService : IDeviceOcrService
{
    private readonly IOnDeviceMlKitOcr _mlKit;
    private readonly ITesseract _tesseract;
    private readonly SemaphoreSlim _tessInitLock = new(1, 1);
    private bool _tessDataLoaded;

    public HybridDeviceOcrService(IOnDeviceMlKitOcr mlKit, ITesseract tesseract)
    {
        _mlKit = mlKit;
        _tesseract = tesseract;
    }

    public async Task<DeviceOcrResult> RecognizeAsync(string localImagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                "找不到圖片，請重新拍攝或從相簿選擇。");
        }

        try
        {
            var mlKit = await _mlKit.RecognizeBestAsync(localImagePath, cancellationToken)
                .ConfigureAwait(false);
            if (mlKit.Success && mlKit.FullText.Length > 0)
                return mlKit;

            await EnsureTessDataLoadedAsync(cancellationToken).ConfigureAwait(false);

            var primary = await RecognizeWithTesseractAsync(
                    localImagePath,
                    TessdataCatalog.TesseractPrimaryLanguages,
                    cancellationToken)
                .ConfigureAwait(false);
            if (primary.Success && primary.FullText.Length > 0)
                return primary;

            var broad = await RecognizeWithTesseractAsync(
                    localImagePath,
                    TessdataCatalog.TesseractBroadFallbackLanguages,
                    cancellationToken)
                .ConfigureAwait(false);
            if (broad.Success && broad.FullText.Length > 0)
                return broad;

            if (mlKit.Failure != DeviceOcrFailureKind.Unsupported)
            {
                return DeviceOcrResult.Fail(
                    DeviceOcrFailureKind.EmptyResult,
                    "無法辨識文字。若語系腳本仍失敗，請改手動輸入。");
            }

            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EmptyResult,
                primary.Message
                ?? broad.Message
                ?? "無法辨識文字。請改手動輸入，或換一張更清楚的圖重試。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                $"裝置端辨識失敗：{ex.Message}。請改手動輸入，或換一張更清楚的圖重試。");
        }
    }

    async Task EnsureTessDataLoadedAsync(CancellationToken cancellationToken)
    {
        if (_tessDataLoaded)
            return;

        await _tessInitLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_tessDataLoaded)
                return;

            var load = await _tesseract.LoadTraineddataAsync().ConfigureAwait(false);
            if (load.NotSuccess())
            {
                throw new InvalidOperationException(
                    load.Message ?? "無法載入 Tesseract 語言包（tessdata）。");
            }

            _tessDataLoaded = true;
        }
        finally
        {
            _tessInitLock.Release();
        }
    }

    async Task<DeviceOcrResult> RecognizeWithTesseractAsync(
        string localImagePath,
        string languages,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // ITesseract uses languages from the DI TessDataProvider (all packaged packs).
        // Restrict recognition language string via a one-off engine when possible.
        if (_tesseract is ITessDataProviderSwappable swappable
            && _tesseract is ITessdataProviderExposingTesseract exposing)
        {
            var previous = exposing.GetTessdataProvideInstance();
            var folder = previous.TessDataFolder;
            try
            {
                swappable.SwapTessdataProvider(
                    new RestrictedTessDataProvider(folder, languages));
                var result = await _tesseract.RecognizeTextAsync(localImagePath)
                    .ConfigureAwait(false);
                return MapTesseract(result);
            }
            finally
            {
                swappable.SwapTessdataProvider(previous);
            }
        }

        var fallback = await _tesseract.RecognizeTextAsync(localImagePath)
            .ConfigureAwait(false);
        return MapTesseract(fallback);
    }

    static DeviceOcrResult MapTesseract(RecognizionResult raw)
    {
        if (raw.NotSuccess())
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                $"Tesseract 辨識失敗：{raw.Status}。請改手動輸入。");
        }

        var fullText = (raw.RecognisedText ?? string.Empty).Trim();
        if (fullText.Length == 0)
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EmptyResult,
                "Tesseract 無法辨識文字。請改手動輸入。");
        }

        var elements = fullText
            .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return DeviceOcrResult.Ok(fullText, elements);
    }

    /// <summary>
    /// Points at already-copied tessdata folder with a restricted '+' language list.
    /// </summary>
    sealed class RestrictedTessDataProvider : ITessDataProvider
    {
        readonly string _folder;
        readonly string[] _files;

        public RestrictedTessDataProvider(string folder, string languagesPlusSeparated)
        {
            _folder = folder;
            _files = languagesPlusSeparated
                .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.EndsWith(".traineddata", StringComparison.OrdinalIgnoreCase)
                    ? l
                    : l + ".traineddata")
                .ToArray();
        }

        public string TessDataFolder => _folder;

        public string[] AvailableLanguages => _files;

        public bool IsAllDataLoaded => true;

        public Task<DataLoadResult> LoadFromPackagesAsync() =>
            Task.FromResult(new DataLoadResult
            {
                State = TessDataState.AllValid,
                Message = "Using preloaded tessdata subset."
            });

        public string[] GetAllFileNames() => _files;

        public string GetLanguagesString() =>
            string.Join('+', _files.Select(Path.GetFileNameWithoutExtension));
    }
}
