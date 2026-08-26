namespace Soraeru;

/// <summary>
/// Settings「關於」：版本與這次編譯寫入的 APK 成型時間。
/// </summary>
internal static class AppReleaseInfo
{
    public static string VersionLabel
    {
        get
        {
            var v = AppInfo.Current.VersionString;
            if (string.IsNullOrWhiteSpace(v))
                v = "1.0.1";

            return v[0] is 'v' or 'V' ? "v" + v[1..] : "v" + v;
        }
    }

    public static string BuiltAtLabel =>
        string.IsNullOrWhiteSpace(SoraeruBuildStamp.BuiltAtLocal)
            ? "—"
            : SoraeruBuildStamp.BuiltAtLocal;
}
