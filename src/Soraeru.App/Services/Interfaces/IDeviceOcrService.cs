namespace Soraeru.Services.Interfaces;

public enum DeviceOcrFailureKind
{
    None,
    EmptyResult,
    Unsupported,
    EngineError
}

public sealed record DeviceOcrResult(
    bool Success,
    string FullText,
    IReadOnlyList<string> ElementTexts,
    DeviceOcrFailureKind Failure,
    string? Message)
{
    public static DeviceOcrResult Ok(string fullText, IReadOnlyList<string>? elementTexts = null) =>
        new(true, fullText, elementTexts ?? Array.Empty<string>(), DeviceOcrFailureKind.None, null);

    public static DeviceOcrResult Fail(DeviceOcrFailureKind kind, string message) =>
        new(false, string.Empty, Array.Empty<string>(), kind, message);
}

/// <summary>
/// On-device OCR only. Must not upload images to cloud OCR.
/// </summary>
public interface IDeviceOcrService
{
    Task<DeviceOcrResult> RecognizeAsync(string localImagePath, CancellationToken cancellationToken = default);
}
