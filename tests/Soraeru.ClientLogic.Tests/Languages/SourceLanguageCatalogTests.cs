using Shouldly;
using Soraeru.ClientLogic.Languages;

namespace Soraeru.ClientLogic.Tests.Languages;

public sealed class SourceLanguageCatalogTests
{
    [Fact]
    public void Curated_includes_common_iso_codes_beyond_mvp_eight()
    {
        var codes = SourceLanguageCatalog.CuratedCodes;
        codes.ShouldContain("ar");
        codes.ShouldContain("hi");
        codes.ShouldContain("ru");
        codes.ShouldContain("es");
        codes.ShouldContain("fr");
        codes.ShouldContain("de");
        codes.ShouldContain("pt");
        codes.ShouldContain("id");
        codes.ShouldContain("ms");
        codes.ShouldContain("tr");
        codes.ShouldContain("uk");
        codes.ShouldContain("he");
        codes.ShouldContain("fa");
        codes.ShouldContain("bn");
        codes.ShouldContain("ta");
        codes.ShouldContain("my");
        codes.ShouldContain("km");
        codes.ShouldContain("lo");
        codes.ShouldContain("ne");
        codes.ShouldContain("bo");
        codes.Count.ShouldBeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void Favorites_start_with_auto_then_prior_mvp_set()
    {
        SourceLanguageCatalog.FavoriteCodes[0].ShouldBe("auto");
        SourceLanguageCatalog.FavoriteCodes.ShouldContain("ja");
        SourceLanguageCatalog.FavoriteCodes.ShouldContain("ru");
        SourceLanguageCatalog.FavoriteCodes.ShouldContain("es");
    }

    [Theory]
    [InlineData("arab", "ar")]
    [InlineData("हिन्दी", "hi")]
    [InlineData("russian", "ru")]
    [InlineData("德", "de")]
    [InlineData("français", "fr")]
    public void Search_matches_code_english_or_chinese(string query, string expectedCode)
    {
        var hits = SourceLanguageCatalog.Search(query);
        hits.ShouldContain(e => e.Code == expectedCode);
    }

    [Fact]
    public void Search_empty_returns_favorites_then_rest_without_dupes()
    {
        var list = SourceLanguageCatalog.Search(null);
        list[0].Code.ShouldBe("auto");
        list.Select(e => e.Code).ShouldBeUnique();
        list.Count.ShouldBe(SourceLanguageCatalog.CuratedCodes.Count + 1); // + auto
    }

    [Fact]
    public void Resolve_known_code_has_chinese_display_name()
    {
        var ar = SourceLanguageCatalog.Resolve("ar");
        ar.ChineseName.ShouldContain("阿拉伯");
        ar.EnglishName.ShouldBe("Arabic");
    }
}
