namespace Soraeru.Services.Interfaces;

public enum ImageCaptureKind
{
    Camera,
    Gallery
}

public sealed record CapturedImage(string LocalPath);

/// <summary>
/// Camera / gallery capture. Images stay on device for OCR only.
/// </summary>
public interface IImageCaptureService
{
    Task<CapturedImage?> CaptureAsync(ImageCaptureKind kind, CancellationToken cancellationToken = default);
}
