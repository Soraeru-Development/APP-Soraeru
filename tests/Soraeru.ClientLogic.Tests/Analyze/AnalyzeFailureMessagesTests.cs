using Shouldly;
using Soraeru.ClientLogic.Analyze;

namespace Soraeru.ClientLogic.Tests.Analyze;

public sealed class AnalyzeFailureMessagesTests
{
    [Fact]
    public void TitleFor_regeneration_limit_is_clear()
    {
        AnalyzeFailureMessages.TitleFor(AnalyzeFailureMessages.RegenerationLimitCode)
            .ShouldBe("重產已達上限");
    }

    [Fact]
    public void TitleFor_quota_exceeded_is_clear()
    {
        AnalyzeFailureMessages.TitleFor(AnalyzeFailureMessages.QuotaExceededCode)
            .ShouldBe("今日額度已用完");
    }

    [Fact]
    public void MessageOrDefault_hard_gate_guides_regenerate()
    {
        var message = AnalyzeFailureMessages.MessageOrDefault(
            null,
            AnalyzeFailureMessages.HardGateFailedCode);

        message.ShouldContain("請重新產生");
        message.ShouldContain("不可含原文腳本");
    }
}
