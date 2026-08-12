using Shouldly;
using Soraeru.ClientLogic.Tts;

namespace Soraeru.ClientLogic.Tests.Tts;

public sealed class FormalTtsLocaleTests
{
    [Fact]
    public void PickLocaleId_prefers_exact_preferred_tag()
    {
        var available = new[]
        {
            new FormalTtsDeviceLocale("ja", "ja-JP"),
            new FormalTtsDeviceLocale("ja", "ja-JP-x-formal"),
            new FormalTtsDeviceLocale("en", "en-US"),
        };

        FormalTtsLocale.PickLocaleId("ja", "ja-JP", available)
            .ShouldBe("ja-JP");
    }

    [Fact]
    public void PickLocaleId_falls_back_to_same_language_family()
    {
        var available = new[]
        {
            new FormalTtsDeviceLocale("en", "en-GB"),
            new FormalTtsDeviceLocale("ja", "ja-JP"),
        };

        FormalTtsLocale.PickLocaleId("en", "en-US", available)
            .ShouldBe("en-GB");
    }

    [Fact]
    public void PickLocaleId_returns_null_when_language_missing()
    {
        var available = new[]
        {
            new FormalTtsDeviceLocale("en", "en-US"),
            new FormalTtsDeviceLocale("ja", "ja-JP"),
        };

        FormalTtsLocale.PickLocaleId("th", "th-TH", available)
            .ShouldBeNull();
    }

    [Fact]
    public void PickLocaleId_und_uses_system_default()
    {
        var available = new[]
        {
            new FormalTtsDeviceLocale("en", "en-US"),
        };

        FormalTtsLocale.PickLocaleId("und", "und", available)
            .ShouldBeNull();
    }

    [Fact]
    public void NormalizeFamily_maps_mvp_languages()
    {
        FormalTtsLocale.NormalizeFamily("ko-KR").ShouldBe("ko");
        FormalTtsLocale.NormalizeFamily("fil").ShouldBe("tl");
        FormalTtsLocale.PreferredTag("ko").ShouldBe("ko-KR");
        FormalTtsLocale.PreferredTag("tl").ShouldBe("fil-PH");
    }
}
