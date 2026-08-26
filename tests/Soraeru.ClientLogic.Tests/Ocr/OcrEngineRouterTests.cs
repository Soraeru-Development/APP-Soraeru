using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

public sealed class OcrEngineRouterTests
{
    [Fact]
    public void Plan_cyrillic_skips_mlkit_and_forces_rus()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Cyrillic);
        plan.SkipMlKit.ShouldBeTrue();
        plan.TesseractPrimaryLanguages.ShouldContain("rus");
        plan.TesseractPrimaryLanguages.ShouldNotContain("eng");
    }

    [Fact]
    public void Plan_latin_keeps_mlkit_and_prefers_latin_tess()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Latin);
        plan.SkipMlKit.ShouldBeFalse();
        plan.TesseractPrimaryLanguages.ShouldContain("spa");
        plan.TesseractPrimaryLanguages.ShouldContain("eng");
    }

    [Fact]
    public void Plan_cjk_keeps_mlkit_and_prefers_cjk_tess()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Cjk);
        plan.SkipMlKit.ShouldBeFalse();
        plan.TesseractPrimaryLanguages.ShouldContain("jpn");
        plan.TesseractPrimaryLanguages.ShouldContain("chi_tra");
    }

    [Fact]
    public void Plan_auto_does_not_skip_mlkit()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Auto);
        plan.SkipMlKit.ShouldBeFalse();
        plan.TesseractPrimaryLanguages.ShouldContain("rus");
    }

    [Fact]
    public void Plan_arabic_skips_mlkit_and_forces_ara()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Arabic);
        plan.SkipMlKit.ShouldBeTrue();
        plan.TesseractPrimaryLanguages.ShouldBe("ara");
    }

    [Fact]
    public void Plan_devanagari_keeps_mlkit_and_prefers_hin()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Devanagari);
        plan.SkipMlKit.ShouldBeFalse();
        plan.TesseractPrimaryLanguages.ShouldContain("hin");
        plan.TesseractPrimaryLanguages.ShouldContain("nep");
    }

    [Fact]
    public void Plan_southeast_asian_skips_mlkit_and_prefers_sea_packs()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.SoutheastAsian);
        plan.SkipMlKit.ShouldBeTrue();
        plan.TesseractPrimaryLanguages.ShouldContain("tha");
        plan.TesseractPrimaryLanguages.ShouldContain("mya");
        plan.TesseractPrimaryLanguages.ShouldContain("lao");
        plan.TesseractPrimaryLanguages.ShouldContain("khm");
    }

    [Fact]
    public void Plan_other_keeps_mlkit_and_uses_auto_primary()
    {
        var plan = OcrEngineRouter.Plan(OcrScriptFamilyHint.Other);
        plan.SkipMlKit.ShouldBeFalse();
        plan.TesseractPrimaryLanguages.ShouldBe(OcrEngineRouter.AutoPrimaryLanguages);
    }

    [Theory]
    [InlineData("مرحبا", OcrScriptFamilyHint.Arabic)]
    [InlineData("नमस्ते", OcrScriptFamilyHint.Devanagari)]
    [InlineData("สวัสดี", OcrScriptFamilyHint.SoutheastAsian)]
    [InlineData("Я женщина.", OcrScriptFamilyHint.Cyrillic)]
    [InlineData("こんにちは漢字", OcrScriptFamilyHint.Cjk)]
    [InlineData("hola amigo", OcrScriptFamilyHint.Latin)]
    [InlineData("", OcrScriptFamilyHint.Auto)]
    public void DetectDominantScriptFamily_from_ocr_text(string text, OcrScriptFamilyHint expected) =>
        OcrEngineRouter.DetectDominantScriptFamily(text).ShouldBe(expected);

    [Theory]
    [InlineData("weHUAMHa.", OcrScriptFamilyHint.Auto, false)]
    [InlineData("Я женщина.", OcrScriptFamilyHint.Auto, true)]
    [InlineData("hola amigo", OcrScriptFamilyHint.Auto, true)]
    [InlineData("こんにちは", OcrScriptFamilyHint.Auto, true)]
    [InlineData("مرحبا", OcrScriptFamilyHint.Auto, true)]
    [InlineData("weHUAMHa.", OcrScriptFamilyHint.Cyrillic, false)]
    [InlineData("Я женщина.", OcrScriptFamilyHint.Cyrillic, true)]
    [InlineData("weHUAMHa.", OcrScriptFamilyHint.Latin, false)]
    [InlineData("Buenos días", OcrScriptFamilyHint.Latin, true)]
    [InlineData("hello", OcrScriptFamilyHint.Arabic, false)]
    [InlineData("مرحبا بالعالم", OcrScriptFamilyHint.Arabic, true)]
    [InlineData("hello", OcrScriptFamilyHint.SoutheastAsian, false)]
    [InlineData("สวัสดีครับ", OcrScriptFamilyHint.SoutheastAsian, true)]
    [InlineData("hello", OcrScriptFamilyHint.Devanagari, false)]
    [InlineData("नमस्ते", OcrScriptFamilyHint.Devanagari, true)]
    public void ShouldAcceptMlKitResult_respects_hint_and_hallucination(
        string text,
        OcrScriptFamilyHint hint,
        bool expected) =>
        OcrEngineRouter.ShouldAcceptMlKitResult(text, hint).ShouldBe(expected);

    [Fact]
    public void ResolveEffectiveHint_auto_prefers_detected_script_family()
    {
        OcrEngineRouter.ResolveEffectiveHint(OcrScriptFamilyHint.Auto, "مرحبا")
            .ShouldBe(OcrScriptFamilyHint.Arabic);
        OcrEngineRouter.ResolveEffectiveHint(OcrScriptFamilyHint.Latin, "مرحبا")
            .ShouldBe(OcrScriptFamilyHint.Latin);
    }
}
