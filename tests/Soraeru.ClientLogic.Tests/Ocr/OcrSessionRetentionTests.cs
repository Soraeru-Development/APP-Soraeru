using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrSessionRetentionTests
{
    [Fact]
    public void ShouldShowContinueOcrCta_requires_recognized_text()
    {
        OcrSessionRetention.ShouldShowContinueOcrCta(recognizedText: null, localImagePath: "/tmp/a.jpg")
            .ShouldBeFalse();
        OcrSessionRetention.ShouldShowContinueOcrCta("   ", "/tmp/a.jpg").ShouldBeFalse();
        OcrSessionRetention.ShouldShowContinueOcrCta("สวัสดี", localImagePath: null).ShouldBeTrue();
        OcrSessionRetention.ShouldShowContinueOcrCta("สวัสดี", "/tmp/a.jpg").ShouldBeTrue();
    }

    [Fact]
    public void ShouldReturnToOcrSelectOnBack_matches_live_recognized_text()
    {
        OcrSessionRetention.ShouldReturnToOcrSelectOnBack(null).ShouldBeFalse();
        OcrSessionRetention.ShouldReturnToOcrSelectOnBack("한").ShouldBeTrue();
    }

    [Theory]
    [InlineData(OcrSessionLeaveTarget.Home, true)]
    [InlineData(OcrSessionLeaveTarget.WordInput, true)]
    [InlineData(OcrSessionLeaveTarget.NewImagePick, true)]
    [InlineData(OcrSessionLeaveTarget.AnalysisResult, false)]
    [InlineData(OcrSessionLeaveTarget.NotebookDetail, false)]
    [InlineData(OcrSessionLeaveTarget.Login, false)]
    [InlineData(OcrSessionLeaveTarget.Analyzing, false)]
    [InlineData(OcrSessionLeaveTarget.OcrSelect, false)]
    [InlineData(OcrSessionLeaveTarget.LocalShortCircuit, false)]
    public void ShouldClearOn_only_home_word_input_and_new_pick(OcrSessionLeaveTarget target, bool expected)
    {
        OcrSessionRetention.ShouldClearOn(target).ShouldBe(expected);
    }

    [Fact]
    public void ShouldClearWhenHomeAppears_only_at_home_root()
    {
        OcrSessionRetention.ShouldClearWhenHomeAppears("//main/HomePage").ShouldBeTrue();
        OcrSessionRetention.ShouldClearWhenHomeAppears("//HomePage").ShouldBeTrue();
        OcrSessionRetention.ShouldClearWhenHomeAppears("//main/HomePage/OcrSelectPage").ShouldBeFalse();
        OcrSessionRetention.ShouldClearWhenHomeAppears("//main/HomePage/ImagePickPage").ShouldBeFalse();
        OcrSessionRetention.ShouldClearWhenHomeAppears("//main/HomePage/AnalysisResultPage").ShouldBeFalse();
        OcrSessionRetention.ShouldClearWhenHomeAppears(null).ShouldBeFalse();
        OcrSessionRetention.ShouldClearWhenHomeAppears("").ShouldBeFalse();
    }

    [Fact]
    public void ResolvePostLoginDestination_keeps_ocr_when_session_live()
    {
        OcrSessionRetention.ResolvePostLoginDestination(onboardingCompleted: false, ocrSessionActive: true)
            .ShouldBe(OcrPostLoginDestination.Onboarding);
        OcrSessionRetention.ResolvePostLoginDestination(onboardingCompleted: true, ocrSessionActive: true)
            .ShouldBe(OcrPostLoginDestination.OcrSelect);
        OcrSessionRetention.ResolvePostLoginDestination(onboardingCompleted: true, ocrSessionActive: false)
            .ShouldBe(OcrPostLoginDestination.Home);
    }

    [Fact]
    public void ContinueSamePhotoCta_is_the_product_label()
    {
        OcrSessionRetention.ContinueSamePhotoCta.ShouldBe("繼續選同圖其他字");
    }
}
