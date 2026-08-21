#if ANDROID
using Android.Graphics;
using Android.Gms.Extensions;
using Soraeru.Services.Interfaces;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Chinese;
using Xamarin.Google.MLKit.Vision.Text.Devanagari;
using Xamarin.Google.MLKit.Vision.Text.Japanese;
using Xamarin.Google.MLKit.Vision.Text.Korean;
using Xamarin.Google.MLKit.Vision.Text.Latin;
using MlKitText = Xamarin.Google.MLKit.Vision.Text.Text;

namespace Soraeru.Platforms.Android;

/// <summary>
/// On-device ML Kit Text Recognition v2 for Latin / Chinese / Japanese / Korean / Devanagari.
/// Images stay on device (never cloud OCR).
/// </summary>
public sealed class AndroidMlKitMultiScriptOcr : IOnDeviceMlKitOcr
{
    public async Task<DeviceOcrResult> RecognizeBestAsync(string localImagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(localImagePath) || !File.Exists(localImagePath))
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                "找不到圖片，請重新拍攝或從相簿選擇。");
        }

        Bitmap? bitmap = null;
        InputImage? image = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            bitmap = await Task.Run(() => BitmapFactory.DecodeFile(localImagePath), cancellationToken)
                .ConfigureAwait(false);
            if (bitmap is null)
            {
                return DeviceOcrResult.Fail(
                    DeviceOcrFailureKind.EngineError,
                    "無法讀取圖片，請換一張圖重試。");
            }

            image = InputImage.FromBitmap(bitmap, 0);
            DeviceOcrResult? best = null;

            foreach (var client in CreateRecognizers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using (client)
                    {
                        var raw = await client.Process(image).AsAsync<MlKitText>().ConfigureAwait(false);
                        var mapped = MapResult(raw);
                        if (!mapped.Success)
                            continue;
                        if (best is null || mapped.FullText.Length > best.FullText.Length)
                            best = mapped;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Script model may be missing on device; try remaining recognizers / Tesseract.
                }
            }

            return best ?? DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EmptyResult,
                "ML Kit 無法辨識此圖文字（將嘗試 Tesseract）。");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EngineError,
                $"ML Kit 辨識失敗：{ex.Message}");
        }
        finally
        {
            image?.Dispose();
            bitmap?.Recycle();
            bitmap?.Dispose();
        }
    }

    static IEnumerable<ITextRecognizer> CreateRecognizers()
    {
        yield return TextRecognition.GetClient(new TextRecognizerOptions.Builder().Build());
        yield return TextRecognition.GetClient(new ChineseTextRecognizerOptions.Builder().Build());
        yield return TextRecognition.GetClient(new JapaneseTextRecognizerOptions.Builder().Build());
        yield return TextRecognition.GetClient(new KoreanTextRecognizerOptions.Builder().Build());
        yield return TextRecognition.GetClient(new DevanagariTextRecognizerOptions.Builder().Build());
    }

    static DeviceOcrResult MapResult(MlKitText? raw)
    {
        var fullText = (raw?.GetText() ?? string.Empty).Trim();
        if (fullText.Length == 0)
        {
            return DeviceOcrResult.Fail(
                DeviceOcrFailureKind.EmptyResult,
                "ML Kit 結果為空。");
        }

        var elements = new List<string>();
        var blocks = raw?.TextBlocks;
        if (blocks is not null)
        {
            foreach (var block in blocks)
            {
                var lines = block?.Lines;
                if (lines is null)
                    continue;
                foreach (var line in lines)
                {
                    var elementsInLine = line?.Elements;
                    if (elementsInLine is null)
                        continue;
                    foreach (var element in elementsInLine)
                    {
                        var t = element?.Text?.Trim();
                        if (!string.IsNullOrEmpty(t))
                            elements.Add(t);
                    }
                }
            }
        }

        var distinct = elements
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return DeviceOcrResult.Ok(fullText, distinct);
    }
}
#endif
