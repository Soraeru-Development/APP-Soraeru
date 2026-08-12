using Shouldly;
using Soraeru.ClientLogic.Legal;

namespace Soraeru.ClientLogic.Tests.Legal;

public sealed class LegalDocumentsTests
{
    [Fact]
    public void Privacy_policy_is_real_content_not_placeholder()
    {
        LegalDocuments.PrivacyTitle.ShouldBe("隱私權政策");
        LegalDocuments.PrivacyBody.ShouldNotBeNullOrWhiteSpace();
        LegalDocuments.PrivacyBody.ShouldNotContain("MVP 示範");
        LegalDocuments.PrivacyBody.ShouldNotContain("稍後再補");
        LegalDocuments.PrivacyBody.ShouldContain("圖片");
        LegalDocuments.PrivacyBody.ShouldContain("不上傳");
        LegalDocuments.PrivacyBody.ShouldContain("AI");
    }

    [Fact]
    public void Ai_disclaimer_states_memory_aid_and_formal_reading()
    {
        LegalDocuments.AiDisclaimerTitle.ShouldBe("AI 內容聲明");
        LegalDocuments.AiDisclaimerBody.ShouldContain("僅供記憶");
        LegalDocuments.AiDisclaimerBody.ShouldContain("正式發音");
        LegalDocuments.AiDisclaimerBody.ShouldContain("AI 可能有誤");
        LegalDocuments.AiDisclaimerBody.ShouldContain("多語");
    }

    [Fact]
    public void Resolve_selects_document_by_key()
    {
        var privacy = LegalDocuments.Resolve(LegalDocuments.PrivacyDocKey);
        privacy.Title.ShouldBe(LegalDocuments.PrivacyTitle);
        privacy.Body.ShouldBe(LegalDocuments.PrivacyBody);

        var ai = LegalDocuments.Resolve(LegalDocuments.AiDisclaimerDocKey);
        ai.Title.ShouldBe(LegalDocuments.AiDisclaimerTitle);
        ai.Body.ShouldBe(LegalDocuments.AiDisclaimerBody);
    }
}
