using Soraeru.Services.Interfaces;

namespace Soraeru.Pages;

public partial class ImagePickPage : ContentPage
{
    private readonly IImageCaptureService _capture;
    private readonly IDeviceOcrService _ocr;
    private readonly IOcrSessionStore _ocrSession;
    private bool _busy;

    public ImagePickPage(
        IImageCaptureService capture,
        IDeviceOcrService ocr,
        IOcrSessionStore ocrSession)
    {
        InitializeComponent();
        _capture = capture;
        _ocr = ocr;
        _ocrSession = ocrSession;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshPreviewFromSession();
    }

    async void OnCameraClicked(object? sender, EventArgs e) =>
        await PickAsync(ImageCaptureKind.Camera);

    async void OnGalleryClicked(object? sender, EventArgs e) =>
        await PickAsync(ImageCaptureKind.Gallery);

    async Task PickAsync(ImageCaptureKind kind)
    {
        if (_busy)
            return;

        try
        {
            SetBusy(true, kind == ImageCaptureKind.Camera ? "開啟相機…" : "開啟相簿…");
            var captured = await _capture.CaptureAsync(kind);
            if (captured is null)
            {
                StatusLabel.Text = "未選取圖片。";
                return;
            }

            _ocrSession.LocalImagePath = captured.LocalPath;
            _ocrSession.RecognizedText = null;
            _ocrSession.StatusMessage = null;
            RefreshPreviewFromSession();
            StatusLabel.Text = "已選圖（僅本機）。接著可開始辨識。";
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlertAsync(
                "無法使用",
                kind == ImageCaptureKind.Camera
                    ? "此裝置不支援相機拍照。請改用相簿，或改手動輸入。"
                    : "此裝置不支援相簿選圖。請改手動輸入。",
                "了解");
        }
        catch (PermissionException)
        {
            await DisplayAlertAsync(
                "需要權限",
                kind == ImageCaptureKind.Camera
                    ? "請允許相機權限後再試，或改用相簿／手動輸入。"
                    : "請允許相片存取後再試，或改手動輸入。",
                "了解");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("取圖失敗", ex.Message, "了解");
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void OnOcrClicked(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        var path = _ocrSession.LocalImagePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            await DisplayAlertAsync("提醒", "請先拍照或從相簿選擇圖片。", "了解");
            return;
        }

        try
        {
            SetBusy(true, "裝置端辨識中（不上傳原圖）…");
            var result = await _ocr.RecognizeAsync(path);
            if (!result.Success)
            {
                var goManual = await DisplayAlertAsync(
                    "辨識失敗",
                    result.Message ?? "無法辨識文字。",
                    "改手動輸入",
                    "留下重試");
                if (goManual)
                    await Routes.GoAsync(Routes.WordInput);
                return;
            }

            _ocrSession.RecognizedText = MergeRecognizedText(result);
            _ocrSession.StatusMessage = null;
            await Routes.GoAsync(Routes.OcrSelect);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            var goManual = await DisplayAlertAsync(
                "辨識失敗",
                $"{ex.Message}\n\n原圖不會上傳。可改手動輸入。",
                "改手動輸入",
                "留下重試");
            if (goManual)
                await Routes.GoAsync(Routes.WordInput);
        }
        finally
        {
            SetBusy(false);
        }
    }

    static string MergeRecognizedText(DeviceOcrResult result)
    {
        if (result.ElementTexts.Count == 0)
            return result.FullText;

        // Prefer full text for editable correction; elements still drive token chips via tokenizer.
        return result.FullText;
    }

    void RefreshPreviewFromSession()
    {
        var path = _ocrSession.LocalImagePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            PreviewImage.Source = ImageSource.FromFile(path);
            PreviewImage.IsVisible = true;
            PreviewPlaceholder.IsVisible = false;
        }
        else
        {
            PreviewImage.Source = null;
            PreviewImage.IsVisible = false;
            PreviewPlaceholder.IsVisible = true;
        }
    }

    void SetBusy(bool busy, string? status = null)
    {
        _busy = busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        StartOcrButton.IsEnabled = !busy;
        if (status is not null)
            StatusLabel.Text = status;
        else if (!busy && string.IsNullOrWhiteSpace(StatusLabel.Text))
            StatusLabel.Text = string.Empty;
    }
}
