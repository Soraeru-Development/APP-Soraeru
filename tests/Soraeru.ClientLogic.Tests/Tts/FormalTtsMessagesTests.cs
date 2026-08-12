using Shouldly;
using Soraeru.ClientLogic.Tts;

namespace Soraeru.ClientLogic.Tests.Tts;

public sealed class FormalTtsMessagesTests
{
    [Fact]
    public void LocaleUnavailable_mentions_voice_pack_and_reading_text()
    {
        FormalTtsMessages.LocaleUnavailable.ShouldContain("語音包");
        FormalTtsMessages.LocaleUnavailable.ShouldContain("正式讀音");
    }

    [Fact]
    public void SpeakFailed_keeps_reading_text_guidance()
    {
        FormalTtsMessages.SpeakFailed.ShouldContain("播放失敗");
        FormalTtsMessages.SpeakFailed.ShouldContain("正式讀音");
    }
}
