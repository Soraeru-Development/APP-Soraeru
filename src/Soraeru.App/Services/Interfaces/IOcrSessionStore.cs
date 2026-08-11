namespace Soraeru.Services.Interfaces;

/// <summary>
/// Holds locally selected image + on-device OCR text between L07 and L08.
/// Never sends image bytes to the API.
/// </summary>
public interface IOcrSessionStore
{
    string? LocalImagePath { get; set; }

    string? RecognizedText { get; set; }

    string? StatusMessage { get; set; }

    void Clear();
}

public sealed class OcrSessionStore : IOcrSessionStore
{
    public string? LocalImagePath { get; set; }

    public string? RecognizedText { get; set; }

    public string? StatusMessage { get; set; }

    public void Clear()
    {
        LocalImagePath = null;
        RecognizedText = null;
        StatusMessage = null;
    }
}
