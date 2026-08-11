using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Soraeru.Platforms.Android;

namespace Soraeru;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private TaskCompletionSource<Intent?>? _googleSignInTcs;

    public void BeginGoogleSignIn(TaskCompletionSource<Intent?> tcs) =>
        _googleSignInTcs = tcs;

    public void EndGoogleSignIn(TaskCompletionSource<Intent?> tcs)
    {
        if (ReferenceEquals(_googleSignInTcs, tcs))
        {
            _googleSignInTcs = null;
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != AndroidGoogleSignInService.SignInRequestCode)
        {
            return;
        }

        var tcs = _googleSignInTcs;
        if (tcs is null)
        {
            return;
        }

        // Always forward Intent data (even when resultCode is Canceled).
        // Google embeds ApiException / Status in extras for DEVELOPER_ERROR etc.;
        // discarding it caused every failure to surface as「已取消 Google 登入。」
        System.Diagnostics.Debug.WriteLine($"[GoogleSignIn] OnActivityResult resultCode={resultCode} dataNull={data is null}");
        tcs.TrySetResult(data);
    }
}
