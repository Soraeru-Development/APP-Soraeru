using Shouldly;
using Soraeru.Application.Analyze;

namespace Soraeru.Application.Tests.Analyze;

public sealed class MnemonicHardGateTests
{
    [Theory]
    [InlineData("薩瓦地")]
    [InlineData("哈－囉")]
    [InlineData("馬k")]
    [InlineData("卡姆薩哈米達")]
    [InlineData("ㄎㄚˇㄇㄨˇ")]
    [InlineData("甘薩－哈米達")]
    public void TryValidate_accepts_compliant_display_text(string displayText)
    {
        MnemonicHardGate.TryValidate(displayText, out var reason).ShouldBeTrue();
        reason.ShouldBeNull();
    }

    [Theory]
    [InlineData("瓦兒", "erhua")]
    [InlineData("福爾", "erhua")]
    [InlineData("k些", "latin")]
    [InlineData("l", "latin")]
    [InlineData("dei", "latin")]
    [InlineData("hello", "latin")]
    [InlineData("ст", "script")]
    public void TryValidate_rejects_violations(string displayText, string _)
    {
        MnemonicHardGate.TryValidate(displayText, out var reason).ShouldBeFalse();
        reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("감사합니다")]
    [InlineData("卡姆사미다")]
    [InlineData("ありがと")]
    [InlineData("สวัสดี")]
    public void TryValidate_rejects_source_scripts_with_clear_regenerate_hint(string displayText)
    {
        MnemonicHardGate.TryValidate(displayText, out var reason).ShouldBeFalse();
        reason.ShouldBe(MnemonicHardGate.DisallowedScriptReason);
        reason.ShouldContain("請重新產生");
        reason.ShouldContain("不可含原文腳本");
    }

    [Fact]
    public void TryValidateAll_rejects_when_any_candidate_has_hangul()
    {
        // U+AC10 U+C0AC U+D569 U+B2C8 U+B2E4 = 감사합니다
        var hangul = "\uAC10\uC0AC\uD569\uB2C8\uB2E4";
        var ok = MnemonicHardGate.TryValidateAll(
            ["卡姆薩哈米達", hangul],
            out var reason);

        ok.ShouldBeFalse();
        reason.ShouldBe(MnemonicHardGate.DisallowedScriptReason);
    }
}
