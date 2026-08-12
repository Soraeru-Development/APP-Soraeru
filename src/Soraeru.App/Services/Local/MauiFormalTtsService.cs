using Soraeru.ClientLogic.Tts;
using Soraeru.Services.Interfaces;

namespace Soraeru.Services.Local;

/// <summary>
/// System <see cref="ITextToSpeech"/> wrapper: speaks formal source text only.
/// </summary>
public sealed class MauiFormalTtsService : IFormalTtsService
{
    private readonly ITextToSpeech _tts;

    public MauiFormalTtsService()
        : this(TextToSpeech.Default)
    {
    }

    public MauiFormalTtsService(ITextToSpeech tts)
    {
        _tts = tts;
    }

    public async Task<FormalTtsPlayResult> SpeakFormalSourceAsync(
        string? sourceText,
        string? sourceLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!FormalTtsRequest.TryPrepare(sourceText, sourceLanguage, out var utterance, out var prepareError)
            || utterance is null)
        {
            return FormalTtsPlayResult.Fail(
                FormalTtsFailureKind.EmptySourceText,
                prepareError ?? FormalTtsRequest.ErrorEmptySource);
        }

        try
        {
            var locales = await _tts.GetLocalesAsync().ConfigureAwait(false);
            var deviceLocales = locales
                .Select(l => new FormalTtsDeviceLocale(l.Language, l.Id))
                .ToList();

            var useDefaultVoice = string.Equals(
                utterance.LanguageFamily,
                "und",
                StringComparison.OrdinalIgnoreCase);

            Locale? matched = null;
            if (!useDefaultVoice)
            {
                var localeId = FormalTtsLocale.PickLocaleId(
                    utterance.LanguageFamily,
                    utterance.PreferredLanguageTag,
                    deviceLocales);

                if (localeId is null)
                {
                    return FormalTtsPlayResult.Fail(
                        FormalTtsFailureKind.LocaleUnavailable,
                        FormalTtsMessages.LocaleUnavailable);
                }

                matched = locales.FirstOrDefault(l =>
                    string.Equals(l.Id, localeId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(l.Language, localeId, StringComparison.OrdinalIgnoreCase));

                if (matched is null)
                {
                    return FormalTtsPlayResult.Fail(
                        FormalTtsFailureKind.LocaleUnavailable,
                        FormalTtsMessages.LocaleUnavailable);
                }
            }

            var options = matched is null
                ? null
                : new SpeechOptions { Locale = matched };

            await _tts.SpeakAsync(utterance.SpeechText, options, cancellationToken)
                .ConfigureAwait(false);

            return FormalTtsPlayResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return FormalTtsPlayResult.Fail(
                FormalTtsFailureKind.SpeakFailed,
                FormalTtsMessages.SpeakFailed);
        }
    }
}
