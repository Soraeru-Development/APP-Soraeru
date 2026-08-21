namespace Soraeru.ClientLogic.Analyze;

/// <summary>
/// Result-page「重新產生」button label／enabled state (ticket 09).
/// </summary>
public static class RegenerateActionPresentation
{
    public const string DefaultLabel = "重新產生";
    public const string LimitLabel = "已達分析上限";

    public static (string Text, bool IsEnabled) ForRemaining(int remainingRegenerations)
    {
        if (remainingRegenerations <= 0)
            return (LimitLabel, false);

        return (DefaultLabel, true);
    }
}
