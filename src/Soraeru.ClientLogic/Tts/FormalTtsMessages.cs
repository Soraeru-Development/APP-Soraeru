namespace Soraeru.ClientLogic.Tts;

/// <summary>User-facing copy for formal TTS outcomes (reading text stays on screen).</summary>
public static class FormalTtsMessages
{
    public const string LocaleUnavailable =
        "裝置沒有此語言的語音包。請到系統設定安裝語音資料後再試；畫面上的正式讀音文字仍可查看。";

    public const string SpeakFailed =
        "播放失敗。請稍後再試，或檢查系統語音設定；畫面上的正式讀音文字仍可查看。";
}
