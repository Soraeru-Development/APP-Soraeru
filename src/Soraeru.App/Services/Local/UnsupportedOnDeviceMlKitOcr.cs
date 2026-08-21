using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Non-Android stub: ML Kit multi-script bindings are Android-only.
/// Hybrid OCR falls through to Tesseract on these platforms.
/// </summary>
public sealed class UnsupportedOnDeviceMlKitOcr : IOnDeviceMlKitOcr
{
    public Task<DeviceOcrResult> RecognizeBestAsync(string localImagePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeviceOcrResult.Fail(
            DeviceOcrFailureKind.Unsupported,
            "此平台未接 ML Kit 多腳本辨識；將改用本機 Tesseract。"));
    }
}
