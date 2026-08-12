namespace Soraeru.ClientLogic.Analyze;

/// <summary>
/// Maps analyze API failure codes／kinds to user-facing Traditional Chinese copy (ticket 09).
/// </summary>
public static class AnalyzeFailureMessages
{
    public const string RegenerationLimitCode = "REGENERATION_LIMIT_EXCEEDED";
    public const string QuotaExceededCode = "QUOTA_EXCEEDED";
    public const string HardGateFailedCode = "HARD_GATE_FAILED";

    public static string TitleFor(string? code, string? fallbackKind = null)
    {
        if (string.Equals(code, RegenerationLimitCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackKind, "RegenerationLimit", StringComparison.OrdinalIgnoreCase))
        {
            return "重產已達上限";
        }

        if (string.Equals(code, QuotaExceededCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackKind, "QuotaExceeded", StringComparison.OrdinalIgnoreCase))
        {
            return "今日額度已用完";
        }

        if (string.Equals(code, HardGateFailedCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "SCHEMA_INVALID", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackKind, "AnalyzeFailed", StringComparison.OrdinalIgnoreCase))
        {
            return "分析未完成";
        }

        if (string.Equals(fallbackKind, "Network", StringComparison.OrdinalIgnoreCase))
        {
            return "無法連線";
        }

        return "分析失敗";
    }

    public static string MessageOrDefault(string? apiMessage, string? code, string? fallbackKind = null)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage))
            return apiMessage.Trim();

        if (string.Equals(code, RegenerationLimitCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackKind, "RegenerationLimit", StringComparison.OrdinalIgnoreCase))
        {
            return "同一單字最多重新產生 3 次。請稍後再試，或改手動輸入空耳。";
        }

        if (string.Equals(code, QuotaExceededCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fallbackKind, "QuotaExceeded", StringComparison.OrdinalIgnoreCase))
        {
            return "今日分析次數已用完，請明日再試。";
        }

        if (string.Equals(code, HardGateFailedCode, StringComparison.OrdinalIgnoreCase))
        {
            return "空耳未通過聽感檢查，請返回重試或稍後再試。";
        }

        if (string.Equals(fallbackKind, "Network", StringComparison.OrdinalIgnoreCase))
        {
            return "無法連線 API，請確認網路後再試。";
        }

        return "分析失敗，請返回重試或稍後再試。";
    }
}
