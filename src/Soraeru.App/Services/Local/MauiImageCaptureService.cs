using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

public sealed class MauiImageCaptureService : IImageCaptureService
{
    public async Task<CapturedImage?> CaptureAsync(ImageCaptureKind kind, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        FileResult? file = kind switch
        {
            ImageCaptureKind.Camera => await MediaPicker.Default.CapturePhotoAsync(),
            ImageCaptureKind.Gallery => await PickSinglePhotoAsync(),
            _ => null
        };

        if (file is null || string.IsNullOrWhiteSpace(file.FullPath))
            return null;

        return new CapturedImage(file.FullPath);
    }

    static async Task<FileResult?> PickSinglePhotoAsync()
    {
        var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
        {
            SelectionLimit = 1
        });

        return results.Count > 0 ? results[0] : null;
    }
}
