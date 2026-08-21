namespace Soraeru.Services.Interfaces;

/// <summary>
/// Android ML Kit on-device multi-script OCR (Latin / Chinese / Japanese / Korean / Devanagari).
/// Must never upload images to cloud OCR.
/// </summary>
public interface IOnDeviceMlKitOcr
{
    /// <summary>
    /// Runs supported on-device script recognizers and returns the best non-empty result.
    /// </summary>
    Task<DeviceOcrResult> RecognizeBestAsync(string localImagePath, CancellationToken cancellationToken = default);
}
