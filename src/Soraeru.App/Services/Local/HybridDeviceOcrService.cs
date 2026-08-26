using Soraeru.ClientLogic.Ocr;
using Soraeru.Services.Interfaces;
using TesseractOcrMaui;
using TesseractOcrMaui.Enums;
using TesseractOcrMaui.Results;
using TesseractOcrMaui.Tessdata;

namespace Soraeru.Services.Local;

/// <summary>
/// Hybrid on-device OCR: ML Kit (Latin/CJK/Devanagari) first, then Tesseract tessdata_fast
/// for scripts without ML Kit modules. Images never leave the device.
/// Routing heuristics live in <see cref="OcrEngineRouter"/> (ClientLogic).
/// </summary>
public sealed class HybridDeviceOcrService : IDeviceOcrService
{
    private static readonly PageSegmentationMode[] CyrillicAltPsmModes =
    [
        PageSegmentationMode.SparseText,
        PageSegmentationMode.SingleLine,
        PageSegmentationMode.RawLine,
        PageSegmentationMode.SingleWord
    ];

    private readonly IOnDeviceMlKitOcr _mlKit;
    private readonly ITesseract _tesseract;
    private readonly ITessdataPackStore _packStore;
    private readonly IOcrImagePreprocessor _imagePreprocessor;
    private readonly SemaphoreSlim _tessInitLock = new(1, 1);
    private bool _tessDataLoaded;

    public HybridDeviceOcrService(
        IOnDeviceMlKitOcr mlKit,
        ITesseract tesseract,
        ITessdataPackStore packStore,
        IOcrImagePreprocessor imagePreprocessor)
    {
        _mlKit = mlKit;
        _tesseract = tesseract;
        _packStore = packStore;
        _imagePreprocessor = imagePreprocessor;
    }

    public Task<DeviceOcrResult> RecognizeAsync(
        string localImagePath,
        CancellationToken cancellationToken = default) =>
        RecognizeAsync(localImagePath, OcrScriptFamilyHint.Auto, progress: null, cancellationToken);

    public Task<DeviceOcrResult> RecognizeAsync(
        string localImagePath,
        OcrScriptFamilyHint scriptHint,
        CancellationToken cancellationToken = default) =>
        RecognizeAsync(localImagePath, scriptHint, progress: null, cancellationToken);

    public async Task<DeviceOcrResult> RecognizeAsync(
        string localImagePath,
        OcrScriptFamilyHint scriptHint,
        IProgress<string>? progress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                "找不到圖片，請重新拍攝或從相簿選擇。");
        }

        var plan = OcrEngineRouter.Plan(scriptHint);

        try
        {
            DeviceOcrResult? demotedMlKit = null;

            if (!plan.SkipMlKit)
            {
                progress?.Report("裝置端辨識中（ML Kit，不上傳原圖）…");
                var mlKit = await _mlKit.RecognizeBestAsync(localImagePath, cancellationToken)
                    .ConfigureAwait(false);

                // Cyrillic button chips: ML Kit often returns partial Cyrillic (drops middle "ест").
                // Never early-accept — demote and enrich with rus Tesseract + strip OCR.
                if (mlKit.Success
                    && mlKit.FullText.Length > 0
                    && OcrScriptQuality.ContainsCyrillic(mlKit.FullText))
                {
                    demotedMlKit = mlKit;
                    plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Cyrillic);
                    scriptHint = OcrScriptFamilyHint.Cyrillic;
                }
                else if (mlKit.Success
                    && mlKit.FullText.Length > 0
                    && OcrEngineRouter.ShouldAcceptMlKitResult(mlKit.FullText, scriptHint))
                {
                    return ApplyCyrillicHomoglyphPass(mlKit);
                }
                else if (mlKit.Success && mlKit.FullText.Length > 0)
                {
                    demotedMlKit = mlKit;
                }
            }

            if (scriptHint == OcrScriptFamilyHint.Auto && demotedMlKit is not null)
            {
                if (OcrScriptQuality.LooksLikeCyrillicScriptHallucination(demotedMlKit.FullText)
                    || OcrScriptQuality.IsSuspiciousLatinOcr(demotedMlKit.FullText))
                {
                    plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Cyrillic);
                    scriptHint = OcrScriptFamilyHint.Cyrillic;
                }
                else
                {
                    var refined = OcrEngineRouter.ResolveEffectiveHint(
                        OcrScriptFamilyHint.Auto,
                        demotedMlKit.FullText);
                    if (refined != OcrScriptFamilyHint.Auto)
                    {
                        plan = OcrEngineRouter.Plan(refined);
                        scriptHint = refined;
                    }
                }
            }

            await EnsureRequiredPacksAsync(scriptHint, progress, cancellationToken)
                .ConfigureAwait(false);
            await EnsureTessDataLoadedAsync(cancellationToken).ConfigureAwait(false);

            progress?.Report("裝置端影像調整（本機，不上傳）…");
            var prepared = await _imagePreprocessor
                .PrepareForTesseractAsync(localImagePath, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var tessPath = prepared.PathForOcr;
                var primaryLangs = BuildPrimaryLanguages(plan, scriptHint);
                progress?.Report("裝置端辨識中（Tesseract，不上傳原圖）…");
                var primary = await RecognizeWithTesseractAsync(
                        tessPath,
                        primaryLangs,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (primary.Success && primary.FullText.Length > 0)
                {
                    return await FinalizeTesseractResultAsync(
                            primary,
                            demotedMlKit,
                            scriptHint,
                            tessPath,
                            primaryLangs,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                // If enhance changed the file and still empty, try original once.
                if (prepared.IsTemporary
                    && !string.Equals(tessPath, localImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    primary = await RecognizeWithTesseractAsync(
                            localImagePath,
                            primaryLangs,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (primary.Success && primary.FullText.Length > 0)
                    {
                        return await FinalizeTesseractResultAsync(
                                primary,
                                demotedMlKit,
                                scriptHint,
                                localImagePath,
                                primaryLangs,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                var broad = await RecognizeWithTesseractAsync(
                        tessPath,
                        TessdataCatalog.TesseractBroadFallbackLanguages,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (broad.Success && broad.FullText.Length > 0)
                {
                    return await FinalizeTesseractResultAsync(
                            broad,
                            demotedMlKit,
                            scriptHint,
                            tessPath,
                            TessdataCatalog.TesseractBroadFallbackLanguages,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                if (demotedMlKit is not null)
                    return ApplyCyrillicHomoglyphPass(demotedMlKit);

                return DeviceOcrResult.Fail(
                    DeviceOcrFailureKind.EmptyResult,
                    OcrImageEnhanceHints.EmptyResultGuidance(scriptHint));
            }
            finally
            {
                if (prepared.IsTemporary)
                {
                    try
                    {
                        if (File.Exists(prepared.PathForOcr))
                            File.Delete(prepared.PathForOcr);
                    }
                    catch
                    {
                        // best-effort cache cleanup
                    }
                }
            }
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

    async Task EnsureRequiredPacksAsync(
        OcrScriptFamilyHint scriptHint,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var required = OcrEngineRouter.RequiredTessPacks(scriptHint);
        var packProgress = progress is null
            ? null
            : new Progress<TessdataPackProgress>(p =>
            {
                progress.Report(p.Phase switch
                {
                    "downloading" => $"下載語言包 {p.LanguageCode}（僅模型，不上傳圖片）…",
                    "ready" => $"語言包 {p.LanguageCode} 已就緒",
                    "present" => $"語言包 {p.LanguageCode} 已在本機",
                    _ => $"語言包 {p.LanguageCode}：{p.Phase}"
                });
            });

        await _packStore.EnsurePacksAsync(required, packProgress, cancellationToken)
            .ConfigureAwait(false);

        // Copy any downloaded packs into the Tesseract runtime folder when available.
        if (_tesseract is ITessdataProviderExposingTesseract exposing)
        {
            var folder = exposing.GetTessdataProvideInstance().TessDataFolder;
            Directory.CreateDirectory(folder);
            foreach (var code in TessdataPackStore.ExpandCodes(required))
            {
                var file = TessdataPackStore.PackFileName(code);
                var src = Path.Combine(_packStore.CacheDirectory, file);
                var dst = Path.Combine(folder, file);
                if (File.Exists(src) && !File.Exists(dst))
                    File.Copy(src, dst);
            }
        }
    }

    string BuildPrimaryLanguages(OcrEngineRoutePlan plan, OcrScriptFamilyHint hint)
    {
        var langs = plan.TesseractPrimaryLanguages;
        if (hint != OcrScriptFamilyHint.Latin)
            return langs;

        var extras = new List<string>();
        if (_packStore.IsPackPresent("deu"))
            extras.Add("deu");
        if (_packStore.IsPackPresent("fra"))
            extras.Add("fra");
        return extras.Count == 0 ? langs : langs + "+" + string.Join('+', extras);
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
        CancellationToken cancellationToken,
        PageSegmentationMode? segmentationMode = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // EngineConfiguration is set-only on ITesseract; clear after use.
        if (segmentationMode is { } mode)
        {
            _tesseract.EngineConfiguration = engine =>
            {
                engine.DefaultSegmentationMode = mode;
            };
        }

        try
        {
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
        finally
        {
            if (segmentationMode is not null)
                _tesseract.EngineConfiguration = null;
        }
    }

    async Task<DeviceOcrResult> FinalizeTesseractResultAsync(
        DeviceOcrResult tess,
        DeviceOcrResult? demotedMlKit,
        OcrScriptFamilyHint scriptHint,
        string imagePath,
        string languages,
        CancellationToken cancellationToken)
    {
        var text = tess.FullText;
        var isCyrillicPath = scriptHint is OcrScriptFamilyHint.Cyrillic
            || OcrScriptQuality.ContainsCyrillic(text)
            || (demotedMlKit is not null && OcrScriptQuality.ContainsCyrillic(demotedMlKit.FullText));

        var extras = new List<string>();
        if (isCyrillicPath)
        {
            foreach (var mode in CyrillicAltPsmModes)
            {
                var alt = await RecognizeWithTesseractAsync(
                        imagePath,
                        languages,
                        cancellationToken,
                        mode)
                    .ConfigureAwait(false);
                if (alt.Success && alt.FullText.Length > 0)
                    extras.Add(alt.FullText);
            }

            extras.AddRange(await RecognizeVerticalStripTextsAsync(
                    imagePath,
                    languages,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        if (demotedMlKit is not null && demotedMlKit.FullText.Length > 0)
            extras.Add(demotedMlKit.FullText);

        foreach (var extra in extras)
            text = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(text, extra);

        text = extras.Count > 0
            ? OcrCyrillicHomoglyphNormalizer.UnionMissingLookalikeTokens(text, extras.ToArray())
            : OcrCyrillicHomoglyphNormalizer.ReconcileButtonRowMiddle(
                OcrCyrillicHomoglyphNormalizer.NormalizeMixedScript(text));

        return RebuildOkResult(text);
    }

    async Task<IReadOnlyList<string>> RecognizeVerticalStripTextsAsync(
        string imagePath,
        string languages,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OcrPreparedImage> strips;
        try
        {
            strips = await _imagePreprocessor
                .CreateVerticalStripsAsync(imagePath, stripCount: 3, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return Array.Empty<string>();
        }

        if (strips.Count == 0)
            return Array.Empty<string>();

        var stripTexts = new List<string>(strips.Count);
        try
        {
            foreach (var strip in strips)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var piece = await RecognizeStripBestAsync(strip.PathForOcr, languages, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(piece))
                    stripTexts.Add(piece.Trim());
            }
        }
        finally
        {
            foreach (var strip in strips)
            {
                if (!strip.IsTemporary)
                    continue;
                try
                {
                    if (File.Exists(strip.PathForOcr))
                        File.Delete(strip.PathForOcr);
                }
                catch
                {
                    // best-effort
                }
            }
        }

        if (stripTexts.Count == 0)
            return Array.Empty<string>();

        var joined = string.Join(' ', stripTexts);
        var all = new List<string>(stripTexts.Count + 1) { joined };
        all.AddRange(stripTexts);
        return all;
    }

    async Task<string> RecognizeStripBestAsync(
        string stripPath,
        string languages,
        CancellationToken cancellationToken)
    {
        PageSegmentationMode[] modes =
        [
            PageSegmentationMode.SingleWord,
            PageSegmentationMode.RawLine,
            PageSegmentationMode.SparseText,
            PageSegmentationMode.SingleLine
        ];

        var shortCandidates = new List<string>();
        foreach (var mode in modes)
        {
            var result = await RecognizeWithTesseractAsync(
                    stripPath,
                    languages,
                    cancellationToken,
                    mode)
                .ConfigureAwait(false);
            if (!result.Success || result.FullText.Length == 0)
                continue;

            shortCandidates.Add(result.FullText.Trim());
        }

        if (LooksLikeShortChipOnly(shortCandidates))
        {
            var voted = OcrCyrillicHomoglyphNormalizer.PreferBestShortToken(shortCandidates.ToArray());
            if (!string.IsNullOrEmpty(voted))
                return voted;
        }

        string best = string.Empty;
        foreach (var trimmed in shortCandidates)
            best = OcrCyrillicHomoglyphNormalizer.PreferRicherCyrillic(best, trimmed);

        return OcrCyrillicHomoglyphNormalizer.NormalizeMixedScript(best);
    }

    static bool LooksLikeShortChipOnly(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0)
            return false;

        foreach (var raw in candidates)
        {
            foreach (var token in raw.Split(
                         (char[]?)null,
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var letters = 0;
                foreach (var rune in token.EnumerateRunes())
                {
                    if (OcrScriptQuality.IsLatinLetter(rune.Value)
                        || OcrScriptQuality.IsCyrillicScript(rune.Value))
                    {
                        letters++;
                    }
                }

                if (letters > OcrCyrillicHomoglyphNormalizer.MaxLookalikeTokenLength)
                    return false;
            }
        }

        return true;
    }

    static DeviceOcrResult ApplyCyrillicHomoglyphPass(DeviceOcrResult result)
    {
        if (!result.Success || result.FullText.Length == 0)
            return result;

        var text = OcrCyrillicHomoglyphNormalizer.UnionMissingLookalikeTokens(result.FullText);
        return RebuildOkResult(text);
    }

    static DeviceOcrResult RebuildOkResult(string fullText)
    {
        fullText = OcrTextTokenizer.StripNoiseTokens(fullText).Trim();
        if (fullText.Length == 0)
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EmptyResult,
                "Tesseract 無法辨識文字。請改手動輸入。");
        }

        var elements = OcrTextTokenizer.Tokenize(fullText).ToList();

        return DeviceOcrResult.Ok(fullText, elements);
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

        return RebuildOkResult(fullText);
    }

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