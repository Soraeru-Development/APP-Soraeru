using System.Diagnostics;
using System.Text.Json;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Soraeru.Services.Interfaces;

namespace Soraeru.Platforms.Android;

/// <summary>
/// Uses Play Services Google Sign-In with RequestIdToken(WebClientId) for backend audience validation.
/// </summary>
#pragma warning disable CS0618 // GoogleSignIn / GoogleSignInOptions still required for idToken until Credential Manager slice
public sealed class AndroidGoogleSignInService : IGoogleSignInService
{
    public const int SignInRequestCode = 9101;

    // Keep numeric fallbacks: binding names differ slightly across GPS Auth package versions.
    private const int StatusSignInFailed = 12500;
    private const int StatusSignInCancelled = 12501;
    private const int StatusSignInCurrentlyInProgress = 12502;

    public bool IsSupported => true;

    public async Task<GoogleNativeSignInResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        var webClientId = GoogleAuthClientIds.ResolveWebClientId();
        if (string.IsNullOrWhiteSpace(webClientId)
            || webClientId.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase))
        {
            return GoogleNativeSignInResult.Fail(
                "尚未設定 Google Web Client ID。請參見 docs/dev-setup-google-auth.md。");
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return GoogleNativeSignInResult.Fail("找不到目前 Activity，無法啟動 Google 登入。");
        }

        var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestIdToken(webClientId.Trim())
            .RequestEmail()
            .RequestProfile()
            .Build();

        var client = GoogleSignIn.GetClient(activity, gso);

        try
        {
            // Prefer account chooser so switching developer Gmail accounts is easy.
            await client.SignOutAsync();
        }
        catch
        {
            // Ignore sign-out failures (not signed in yet).
        }

        if (activity is not MainActivity mainActivity)
        {
            return GoogleNativeSignInResult.Fail("MainActivity 未就緒，無法完成 Google 登入。");
        }

        var tcs = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
        mainActivity.BeginGoogleSignIn(tcs);

        try
        {
            activity.StartActivityForResult(client.SignInIntent, SignInRequestCode);
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var data = await tcs.Task.ConfigureAwait(true);

            // Google often returns RESULT_CANCELED with a Status Intent (e.g. DEVELOPER_ERROR).
            // Always parse via GetSignedInAccountFromIntent — do not treat null-only as the only cancel path.
            if (data is null)
            {
                Debug.WriteLine("[GoogleSignIn] Activity result data is null (user back / no Play Services response).");
                return GoogleNativeSignInResult.Fail("已取消 Google 登入。");
            }

            var account = await GoogleSignIn.GetSignedInAccountFromIntentAsync(data);
            if (account is null || string.IsNullOrWhiteSpace(account.IdToken))
            {
                return GoogleNativeSignInResult.Fail(
                    "未取得 Google idToken。請確認 requestIdToken 使用的是 Web Client ID。");
            }

            return GoogleNativeSignInResult.Success(account.IdToken);
        }
        catch (ApiException ex)
        {
            Debug.WriteLine($"[GoogleSignIn] ApiException status={ex.StatusCode} message={ex.Message}");
            return GoogleNativeSignInResult.Fail(MapApiException(ex));
        }
        catch (OperationCanceledException)
        {
            return GoogleNativeSignInResult.Fail("已取消 Google 登入。");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GoogleSignIn] Unexpected: {ex}");
            return GoogleNativeSignInResult.Fail($"Google 登入失敗：{ex.Message}");
        }
        finally
        {
            mainActivity.EndGoogleSignIn(tcs);
        }
    }

    internal static string MapApiException(ApiException ex)
    {
        var code = ex.StatusCode;
        var detail = string.IsNullOrWhiteSpace(ex.Message) ? string.Empty : $" {ex.Message}";

        return code switch
        {
            StatusSignInCancelled or CommonStatusCodes.Canceled =>
                "已取消 Google 登入。",

            CommonStatusCodes.DeveloperError =>
                "Google 設定錯誤（DEVELOPER_ERROR / 10）：請確認 package=com.soraeru.app、debug SHA-1 已登錄 Android OAuth 用戶端，且 RequestIdToken 使用 Web Client ID。詳見 docs/dev-setup-google-auth.md。",

            CommonStatusCodes.NetworkError =>
                $"網路錯誤，無法完成 Google 登入（NETWORK_ERROR / {code}）。{detail}",

            CommonStatusCodes.SignInRequired =>
                $"裝置尚未登入可用的 Google 帳號（SIGN_IN_REQUIRED / {code}）。請在模擬器／實機加入 Google 帳號後重試。",

            CommonStatusCodes.ApiNotConnected =>
                $"Google Play 服務未連線（API_NOT_CONNECTED / {code}）。請改用含 Google Play 的系統映像，並確認 Play 服務已更新。",

            CommonStatusCodes.InternalError =>
                $"Google Play 內部錯誤（INTERNAL_ERROR / {code}）。{detail}",

            StatusSignInFailed =>
                $"Google 登入失敗（SIGN_IN_FAILED / {code}）。常見原因：OAuth 同意畫面未加測試使用者、模擬器無 Google Play。{detail}",

            StatusSignInCurrentlyInProgress =>
                "另一次 Google 登入仍在進行中，請稍候再試。",

            CommonStatusCodes.InvalidAccount =>
                $"無效的 Google 帳號（INVALID_ACCOUNT / {code}）。{detail}",

            CommonStatusCodes.Timeout =>
                $"Google 登入逾時（TIMEOUT / {code}）。{detail}",

            _ =>
                $"Google 登入失敗（status {code}）：{ex.Message}"
        };
    }
}

/// <summary>
/// Resolves Web Client ID for Android RequestIdToken.
/// Prefer local gitignored <c>GoogleAuth.Debug.json</c> (see example beside it); do not commit real IDs.
/// </summary>
public static class GoogleAuthClientIds
{
    public const string PlaceholderWebClientId = "REPLACE_WITH_WEB_CLIENT_ID.apps.googleusercontent.com";

    private const string DebugResourceName = "Soraeru.GoogleAuth.Debug.json";

    private static string? _resolved;

    /// <summary>MUST be the Web application OAuth client ID (not the Android client ID).</summary>
    public static string ResolveWebClientId()
    {
        if (_resolved is not null)
        {
            return _resolved;
        }

        try
        {
            using var stream = typeof(GoogleAuthClientIds).Assembly.GetManifestResourceStream(DebugResourceName);
            if (stream is not null)
            {
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("WebClientId", out var el))
                {
                    var fromFile = el.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(fromFile))
                    {
                        _resolved = fromFile;
                        return _resolved;
                    }
                }
            }
        }
        catch
        {
            // Optional local secret file; fall back to placeholder.
        }

        _resolved = PlaceholderWebClientId;
        return _resolved;
    }
}
#pragma warning restore CS0618
