using Shouldly;
using Soraeru.ClientLogic.Tts;

namespace Soraeru.ClientLogic.Tests.Tts;

public sealed class FormalTtsRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryPrepare_rejects_empty_source_text(string? sourceText)
    {
        FormalTtsRequest.TryPrepare(sourceText, "ja", out var utterance, out var error)
            .ShouldBeFalse();
        utterance.ShouldBeNull();
        error.ShouldBe(FormalTtsRequest.ErrorEmptySource);
    }

    [Fact]
    public void TryPrepare_uses_trimmed_source_text_never_mnemonic()
    {
        FormalTtsRequest.TryPrepare("  ありがとう  ", "ja", out var utterance, out var error)
            .ShouldBeTrue();
        error.ShouldBeNull();
        utterance.ShouldNotBeNull();
        utterance!.SpeechText.ShouldBe("ありがとう");
        // Spec: 只播正式原文，不播空耳候選 — API 根本不接受 mnemonic 參數。
        utterance.SpeechText.ShouldNotBe("阿利嘎多");
    }

    [Fact]
    public void TryPrepare_maps_japanese_to_ja_JP_family()
    {
        FormalTtsRequest.TryPrepare("こんにちは", "ja", out var utterance, out _)
            .ShouldBeTrue();
        utterance!.LanguageFamily.ShouldBe("ja");
        utterance.PreferredLanguageTag.ShouldBe("ja-JP");
    }

    [Fact]
    public void TryPrepare_normalizes_aliases_and_bcp47()
    {
        FormalTtsRequest.TryPrepare("hello", "en-US", out var en, out _).ShouldBeTrue();
        en!.LanguageFamily.ShouldBe("en");
        en.PreferredLanguageTag.ShouldBe("en-US");

        FormalTtsRequest.TryPrepare("สวัสดี", "tha", out var th, out _).ShouldBeTrue();
        th!.LanguageFamily.ShouldBe("th");
        th.PreferredLanguageTag.ShouldBe("th-TH");
    }

    [Fact]
    public void TryPrepare_unknown_language_keeps_family_for_matching()
    {
        FormalTtsRequest.TryPrepare("слово", "ru", out var utterance, out _)
            .ShouldBeTrue();
        utterance!.LanguageFamily.ShouldBe("ru");
        utterance.PreferredLanguageTag.ShouldBe("ru");
    }
}
