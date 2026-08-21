using Shouldly;
using Soraeru.ClientLogic.Analyze;

namespace Soraeru.ClientLogic.Tests.Analyze;

public sealed class RegenerateActionPresentationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ForRemaining_at_limit_disables_and_shows_limit_label(int remaining)
    {
        var (text, enabled) = RegenerateActionPresentation.ForRemaining(remaining);

        text.ShouldBe(RegenerateActionPresentation.LimitLabel);
        enabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ForRemaining_with_quota_enables_regenerate_label(int remaining)
    {
        var (text, enabled) = RegenerateActionPresentation.ForRemaining(remaining);

        text.ShouldBe(RegenerateActionPresentation.DefaultLabel);
        enabled.ShouldBeTrue();
    }
}
