namespace Soraeru.ClientLogic.Notebook;

public sealed record SaveLocalWordCardCommand(
    string SourceText,
    string NormalizedText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic);

public sealed record LocalSession(bool IsAuthenticated, Guid? UserId)
{
    public static LocalSession Anonymous() => new(false, null);

    public static LocalSession SignedIn(Guid userId) => new(true, userId);
}

public sealed record LocalNotebookResult<T>(bool IsSuccess, T? Value, string? ErrorCode, string? Message)
{
    public static LocalNotebookResult<T> Success(T value) => new(true, value, null, null);

    public static LocalNotebookResult<T> Failure(string errorCode, string message) =>
        new(false, default, errorCode, message);
}
