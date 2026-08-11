using System.Security.Claims;
using Soraeru.Application.Analyze;

namespace Soraeru.Api.Endpoints;

public static class AnalyzeEndpoints
{
    public static RouteGroupBuilder MapAnalyzeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/word").WithTags("Analyze").RequireAuthorization();

        group.MapPost("/analyze", async (
            AnalyzeRequest body,
            IAnalyzeWordService analyze,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = ResolveUserId(user);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await analyze.AnalyzeAsync(
                new AnalyzeWordCommand(
                    userId.Value,
                    body.Text,
                    body.SourceLanguage ?? body.LanguageOverride,
                    body.MemoryLanguage,
                    body.NotationPreference,
                    body.ForceRefresh),
                ct);

            if (!result.IsSuccess || result.Value is null)
            {
                var status = result.ErrorCode switch
                {
                    "QUOTA_EXCEEDED" => StatusCodes.Status429TooManyRequests,
                    "LLM_NOT_CONFIGURED" => StatusCodes.Status503ServiceUnavailable,
                    "NOT_FOUND" => StatusCodes.Status404NotFound,
                    "UNANALYZABLE" or "SCHEMA_INVALID" or "ANALYZE_FAILED" or "LLM_HTTP_ERROR"
                        or "LLM_PARSE_ERROR" or "LLM_EMPTY" or "HARD_GATE_FAILED" => StatusCodes.Status502BadGateway,
                    _ => StatusCodes.Status400BadRequest
                };

                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Analyze failed."),
                    statusCode: status);
            }

            var value = result.Value;
            return Results.Ok(new AnalyzeResponse(
                value.SourceText,
                value.NormalizedText,
                value.SourceLanguage,
                value.LanguageDisplayName,
                value.Meaning,
                value.ReadingText,
                value.Mnemonics
                    .Select(m => new AnalyzeMnemonicResponse(
                        m.DisplayText,
                        m.NotationType,
                        m.NotationText,
                        m.Explanation))
                    .ToList(),
                value.Notice,
                value.Cached,
                value.RemainingDailyQuota,
                value.MnemonicSource));
        });

        return group;
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var fromClaim) ? fromClaim : null;
    }
}

public sealed record AnalyzeRequest(
    string Text,
    string? SourceLanguage = null,
    string? MemoryLanguage = null,
    string? NotationPreference = null,
    /// <summary>Legacy alias for SourceLanguage.</summary>
    string? LanguageOverride = null,
    bool ForceRefresh = false);

public sealed record AnalyzeResponse(
    string SourceText,
    string NormalizedText,
    string SourceLanguage,
    string LanguageDisplayName,
    string Meaning,
    string ReadingText,
    IReadOnlyList<AnalyzeMnemonicResponse> Mnemonics,
    string Notice,
    bool Cached,
    int RemainingDailyQuota,
    string MnemonicSource);

public sealed record AnalyzeMnemonicResponse(
    string DisplayText,
    string NotationType,
    string NotationText,
    string Explanation);
