using Plugin.Maui.OCR;
using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Wraps Plugin.Maui.OCR with TryHard=false so Android uses on-device ML Kit (not cloud).
/// </summary>
public sealed class PluginDeviceOcrService : IDeviceOcrService
{
    private readonly IOcrService _ocr;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public PluginDeviceOcrService(IOcrService ocr)
    {
        _ocr = ocr;
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
            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var bytes = await File.ReadAllBytesAsync(localImagePath, cancellationToken).ConfigureAwait(false);
            // TryHard=false ⇒ on-device only (Android cloud OCR must stay out of MVP scope).
            var options = new OcrOptions.Builder()
                .SetTryHard(false)
                .Build();

            var raw = await _ocr.RecognizeTextAsync(bytes, options, cancellationToken).ConfigureAwait(false);
            var fullText = (raw.AllText ?? string.Empty).Trim();
            var elements = raw.Elements?
                .Select(e => e.Text?.Trim() ?? string.Empty)
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? [];

            if (!raw.Success || fullText.Length == 0)
            {
                return DeviceOcrResult.Fail(
                    DeviceOcrFailureKind.EmptyResult,
                    "無法辨識文字。此語系腳本可能不受裝置 OCR 支援，請改手動輸入。");
            }

            return DeviceOcrResult.Ok(fullText, elements);
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

    async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;
            await _ocr.InitAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
