using Shouldly;
using Soraeru.Application.Analyze;

namespace Soraeru.Application.Tests.Analyze;

public sealed class MnemonicHardGateTests
{
    [Theory]
    [InlineData("薩瓦地")]
    [InlineData("哈－囉")]
    [InlineData("馬k")]
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
}
