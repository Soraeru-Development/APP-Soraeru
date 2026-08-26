#if ANDROID
using Android.Graphics;
using Soraeru.ClientLogic.Ocr;
using Soraeru.Services.Interfaces;
using AColor = Android.Graphics.Color;
using IOPath = System.IO.Path;

namespace Soraeru.Platforms.Android;

/// <summary>
/// Local Tesseract prep: upscale small crops; invert dark-dominant (glowing screen text) images;
/// contrast-boost bright low-contrast pastel ink (e.g. cyan on white).
/// Original stays on device; only a cache JPEG may be written for OCR.
/// </summary>
public sealed class AndroidOcrImagePreprocessor : IOcrImagePreprocessor
{
    public Task<OcrPreparedImage> PrepareForTesseractAsync(
        string localImagePath,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => PrepareCore(localImagePath, cancellationToken), cancellationToken);

    public Task<IReadOnlyList<OcrPreparedImage>> CreateVerticalStripsAsync(
        string localImagePath,
        int stripCount = 3,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => CreateVerticalStripsCore(localImagePath, stripCount, cancellationToken), cancellationToken);

    static IReadOnlyList<OcrPreparedImage> CreateVerticalStripsCore(
        string localImagePath,
        int stripCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(localImagePath)
            || !File.Exists(localImagePath)
            || stripCount < 2)
        {
            return Array.Empty<OcrPreparedImage>();
        }

        Bitmap? source = null;
        var written = new List<string>();
        try
        {
            source = BitmapFactory.DecodeFile(localImagePath);
            if (source is null || source.Width < 24 || source.Height < 8)
                return Array.Empty<OcrPreparedImage>();

            var w = source.Width;
            var h = source.Height;
            // Outer strips: tiny overlap toward the middle so chip text is not clipped.
            // Middle strip: inset so adjacent button edges do not bleed into the short chip.
            var overlap = Math.Max(4, w / (stripCount * 12));
            var middleInset = Math.Max(overlap, w / (stripCount * 8));
            var results = new List<OcrPreparedImage>(stripCount);
            var dir = IOPath.Combine(FileSystem.CacheDirectory, "ocr-strips");
            Directory.CreateDirectory(dir);

            for (var i = 0; i < stripCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var start = (i * w) / stripCount;
                var end = ((i + 1) * w) / stripCount;
                var isMiddle = i > 0 && i < stripCount - 1;
                if (isMiddle)
                {
                    start += middleInset;
                    end -= middleInset;
                }
                else
                {
                    // Overlap only toward the middle; do not eat past the image edge.
                    if (i == 0)
                        end = Math.Min(w, end + overlap);
                    else
                        start = Math.Max(0, start - overlap);
                }

                if (start < 0)
                    start = 0;
                if (end > w)
                    end = w;
                var stripW = end - start;
                if (stripW <= 0)
                    continue;

                using var strip = Bitmap.CreateBitmap(source, start, 0, stripW, h);
                // Upscale narrow chips so Tesseract SingleWord has enough pixels.
                Bitmap export = strip;
                Bitmap? scaled = null;
                try
                {
                    if (stripW < 160 || h < 80)
                    {
                        var scale = Math.Max(2, (int)Math.Ceiling(160.0 / Math.Max(1, stripW)));
                        scaled = Bitmap.CreateScaledBitmap(strip, stripW * scale, h * scale, filter: true);
                        export = scaled;
                    }

                    var outPath = IOPath.Combine(dir, $"strip-{i}-{Guid.NewGuid():N}.jpg");
                    using (var stream = File.Create(outPath))
                    {
                        if (!export.Compress(Bitmap.CompressFormat.Jpeg!, 92, stream))
                        {
                            try { File.Delete(outPath); } catch { /* ignore */ }
                            continue;
                        }
                    }

                    written.Add(outPath);
                    results.Add(new OcrPreparedImage(outPath, IsTemporary: true));
                }
                finally
                {
                    scaled?.Recycle();
                }
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            foreach (var path in written)
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }

            throw;
        }
        catch
        {
            foreach (var path in written)
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }

            return Array.Empty<OcrPreparedImage>();
        }
        finally
        {
            source?.Recycle();
        }
    }

    static OcrPreparedImage PrepareCore(string localImagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
            return new OcrPreparedImage(localImagePath, IsTemporary: false);

        Bitmap? source = null;
        Bitmap? working = null;
        try
        {
            source = BitmapFactory.DecodeFile(localImagePath);
            if (source is null)
                return new OcrPreparedImage(localImagePath, IsTemporary: false);

            working = source;
            var (mean, stdDev) = SampleLuminanceStats(working);
            var invert = OcrImageEnhanceHints.ShouldInvertForOcr(mean);
            var boostContrast = invert
                || OcrImageEnhanceHints.ShouldBoostContrastForOcr(mean, stdDev);
            var upscale = OcrImageEnhanceHints.ShouldUpscale(working.Width, working.Height);

            if (!invert && !boostContrast && !upscale)
                return new OcrPreparedImage(localImagePath, IsTemporary: false);

            if (upscale)
            {
                var scaled = Bitmap.CreateScaledBitmap(
                    working,
                    working.Width * 2,
                    working.Height * 2,
                    filter: true);
                if (!ReferenceEquals(working, source))
                    working.Recycle();
                working = scaled;
            }

            if (invert)
                InvertInPlace(working);

            if (boostContrast)
                BoostContrastInPlace(working);

            cancellationToken.ThrowIfCancellationRequested();
            var dir = IOPath.Combine(FileSystem.CacheDirectory, "ocr-prep");
            Directory.CreateDirectory(dir);
            var outPath = IOPath.Combine(dir, $"tess-{Guid.NewGuid():N}.jpg");
            using (var stream = File.Create(outPath))
            {
                if (!working.Compress(Bitmap.CompressFormat.Jpeg!, 92, stream))
                {
                    try { File.Delete(outPath); } catch { /* ignore */ }
                    return new OcrPreparedImage(localImagePath, IsTemporary: false);
                }
            }

            return new OcrPreparedImage(outPath, IsTemporary: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new OcrPreparedImage(localImagePath, IsTemporary: false);
        }
        finally
        {
            if (working is not null && !ReferenceEquals(working, source))
                working.Recycle();
            source?.Recycle();
        }
    }

    static (double Mean, double StdDev) SampleLuminanceStats(Bitmap bitmap)
    {
        var w = bitmap.Width;
        var h = bitmap.Height;
        if (w <= 0 || h <= 0)
            return (128, 0);

        // Sparse grid sample — enough to decide dark vs light / flat vs contrasty.
        var stepX = Math.Max(1, w / 24);
        var stepY = Math.Max(1, h / 24);
        long sum = 0;
        long sumSq = 0;
        var count = 0;
        for (var y = 0; y < h; y += stepY)
        {
            for (var x = 0; x < w; x += stepX)
            {
                var c = new AColor(bitmap.GetPixel(x, y));
                // Rec. 601 luma
                var luma = (299 * c.R + 587 * c.G + 114 * c.B) / 1000;
                sum += luma;
                sumSq += luma * luma;
                count++;
            }
        }

        if (count == 0)
            return (128, 0);

        var mean = (double)sum / count;
        var variance = Math.Max(0, (double)sumSq / count - mean * mean);
        return (mean, Math.Sqrt(variance));
    }

    static void InvertInPlace(Bitmap bitmap)
    {
        var w = bitmap.Width;
        var h = bitmap.Height;
        var pixels = new int[w * h];
        bitmap.GetPixels(pixels, 0, w, 0, 0, w, h);
        for (var i = 0; i < pixels.Length; i++)
        {
            var c = new AColor(pixels[i]);
            pixels[i] = AColor.Argb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
        }

        bitmap.SetPixels(pixels, 0, w, 0, 0, w, h);
    }

    /// <summary>Mild linear contrast stretch around mid-gray (pastel ink / post-invert).</summary>
    static void BoostContrastInPlace(Bitmap bitmap)
    {
        const float factor = 1.55f;
        var w = bitmap.Width;
        var h = bitmap.Height;
        var pixels = new int[w * h];
        bitmap.GetPixels(pixels, 0, w, 0, 0, w, h);
        for (var i = 0; i < pixels.Length; i++)
        {
            var c = new AColor(pixels[i]);
            pixels[i] = AColor.Argb(
                c.A,
                ClampByte((c.R - 128) * factor + 128),
                ClampByte((c.G - 128) * factor + 128),
                ClampByte((c.B - 128) * factor + 128));
        }

        bitmap.SetPixels(pixels, 0, w, 0, 0, w, h);
    }

    static int ClampByte(float v) =>
        v < 0 ? 0 : v > 255 ? 255 : (int)(v + 0.5f);
}
#endif
