using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// Default / Windows stub — Google native sign-in is Android-only for this MVP.
/// </summary>
public sealed class UnsupportedGoogleSignInService : IGoogleSignInService
{
    public bool IsSupported => false;

    public Task<GoogleNativeSignInResult> SignInAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(
            GoogleNativeSignInResult.Fail("請在 Android 上使用 Google 登入。"));
}
