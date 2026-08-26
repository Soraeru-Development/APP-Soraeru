using Shouldly;
using Soraeru.ClientLogic.Ocr;

namespace Soraeru.ClientLogic.Tests.Ocr;

/// <summary>
/// Pure helpers for which tess packs a script-family plan needs (App downloads via ITessdataPackStore).
/// </summary>
public sealed class OcrTessPackRequirementsTests
{
    [Theory]
    [InlineData(OcrScriptFamilyHint.Arabic, "ara")]
    [InlineData(OcrScriptFamilyHint.Cyrillic, "rus")]
    [InlineData(OcrScriptFamilyHint.Latin, "eng")]
    [InlineData(OcrScriptFamilyHint.SoutheastAsian, "tha")]
    public void RequiredPacks_for_family_include_primary(OcrScriptFamilyHint hint, string expected)
    {
        var packs = OcrEngineRouter.RequiredTessPacks(hint);
        packs.ShouldContain(expected);
    }

    [Fact]
    public void RequiredPacks_latin_includes_on_demand_deu_fra_proof()
    {
        var packs = OcrEngineRouter.RequiredTessPacks(OcrScriptFamilyHint.Latin);
        packs.ShouldContain("deu");
        packs.ShouldContain("fra");
    }
}
