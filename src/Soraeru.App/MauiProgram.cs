using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Soraeru.ClientLogic.Notebook;
using Soraeru.Pages;
using Soraeru.Services.Api;
using Soraeru.Services.Interfaces;
using Soraeru.Services.Local;
using TesseractOcrMaui;

namespace Soraeru;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Tab reselect on Android does not clear the Shell stack; see SoraeruShellRenderer.
                handlers.AddHandler<Shell, Platforms.Android.SoraeruShellRenderer>();
#endif
            });

        builder.Services.AddTesseractOcr(files =>
        {
            foreach (var trainedData in TessdataCatalog.AllTrainedDataFiles)
                files.AddFile(trainedData);
        });

        builder.Services.AddSingleton<IAuthSessionStore, PreferencesAuthSessionStore>();
        builder.Services.AddSingleton<IAnalyzeFlowStore, AnalyzeFlowStore>();
        builder.Services.AddSingleton<IOcrSessionStore, OcrSessionStore>();
        builder.Services.AddSingleton<IImageCaptureService, MauiImageCaptureService>();
#if ANDROID
        builder.Services.AddSingleton<IOnDeviceMlKitOcr, Platforms.Android.AndroidMlKitMultiScriptOcr>();
#else
        builder.Services.AddSingleton<IOnDeviceMlKitOcr, UnsupportedOnDeviceMlKitOcr>();
#endif
        builder.Services.AddSingleton<IDeviceOcrService, HybridDeviceOcrService>();
        builder.Services.AddSingleton<IFormalTtsService, MauiFormalTtsService>();
        builder.Services.AddSingleton<ILocalWordCardStore>(_ =>
            new JsonFileLocalWordCardStore(
                Path.Combine(FileSystem.AppDataDirectory, "local-wordcards.json")));
        builder.Services.AddSingleton<LocalNotebookService>(sp =>
        {
            var store = sp.GetRequiredService<ILocalWordCardStore>();
            var sessionStore = sp.GetRequiredService<IAuthSessionStore>();
            return new LocalNotebookService(store, async ct =>
            {
                if (!await sessionStore.HasSessionAsync())
                    return LocalSession.Anonymous();

                var userId = await sessionStore.GetUserIdAsync();
                return userId is { } id && id != Guid.Empty
                    ? LocalSession.SignedIn(id)
                    : LocalSession.Anonymous();
            });
        });
#if ANDROID
        builder.Services.AddSingleton<IGoogleSignInService, Platforms.Android.AndroidGoogleSignInService>();
#else
        builder.Services.AddSingleton<IGoogleSignInService, UnsupportedGoogleSignInService>();
#endif
        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddHttpClient<ISoraeruApiClient, SoraeruApiClient>(client =>
            {
#if WINDOWS
                client.BaseAddress = new Uri("http://localhost:5080/");
#else
                // Android emulator → host loopback. Physical device: replace with LAN / deployed API URL.
                client.BaseAddress = new Uri("http://10.0.2.2:5080/");
#endif
                client.Timeout = TimeSpan.FromSeconds(90);
            })
            .AddHttpMessageHandler<AuthHeaderHandler>();
        builder.Services.AddSingleton<ICloudWordCardMirror, HttpCloudWordCardMirror>();
        builder.Services.AddSingleton<NotebookSyncCoordinator>(sp =>
        {
            var store = sp.GetRequiredService<ILocalWordCardStore>();
            var mirror = sp.GetRequiredService<ICloudWordCardMirror>();
            var sessionStore = sp.GetRequiredService<IAuthSessionStore>();
            return new NotebookSyncCoordinator(store, mirror, async ct =>
            {
                if (!await sessionStore.HasSessionAsync())
                    return LocalSession.Anonymous();

                var userId = await sessionStore.GetUserIdAsync();
                return userId is { } id && id != Guid.Empty
                    ? LocalSession.SignedIn(id)
                    : LocalSession.Anonymous();
            });
        });

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<LegalDocumentPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<WordInputPage>();
        builder.Services.AddTransient<ImagePickPage>();
        builder.Services.AddTransient<OcrSelectPage>();
        builder.Services.AddTransient<AnalyzingPage>();
        builder.Services.AddTransient<AnalysisResultPage>();
        builder.Services.AddTransient<NotebookListPage>();
        builder.Services.AddTransient<NotebookDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
