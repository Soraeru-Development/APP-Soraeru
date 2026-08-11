using System.Security.Claims;

using Soraeru.Application.Auth;

namespace Soraeru.Api.Endpoints;

public static class MeEndpoints
{
    public static RouteGroupBuilder MapMeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me").WithTags("Me").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal principal, IMeService me, CancellationToken ct) =>
        {
            var userId = ResolveUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await me.GetMeAsync(userId.Value, ct);
            return ToHttp(result);
        });

        group.MapPatch("/", async (
            PatchMeRequest body,
            ClaimsPrincipal principal,
            IMeService me,
            CancellationToken ct) =>
        {
            var userId = ResolveUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await me.PatchMeAsync(
                userId.Value,
                new PatchMeCommand(body.OnboardingCompleted),
                ct);
            return ToHttp(result);
        });

        // ADR-0007: delete cloud notebook mirror + account. Client clears local notebook + session after success.
        group.MapDelete("/", async (ClaimsPrincipal principal, IMeService me, CancellationToken ct) =>
        {
            var userId = ResolveUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await me.DeleteAccountAsync(userId.Value, ct);
            if (!result.IsSuccess)
            {
                var status = result.ErrorCode == "NOT_FOUND"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;
                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Request failed."),
                    statusCode: status);
            }

            return Results.NoContent();
        });

        return group;
    }

    private static IResult ToHttp(Application.Common.ServiceResult<MeProfile> result)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            var status = result.ErrorCode == "NOT_FOUND"
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return Results.Json(
                new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Request failed."),
                statusCode: status);
        }

        var profile = result.Value;
        return Results.Ok(new MeResponse(
            profile.UserId,
            profile.Email,
            profile.DisplayName,
            profile.PlanTier,
            profile.DailyQuota,
            profile.RemainingDailyQuota,
            profile.IsDeveloper,
            profile.NotationPref,
            profile.OnboardingCompleted,
            profile.HasPassword,
            profile.HasGoogleSubject));
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record PatchMeRequest(bool? OnboardingCompleted);

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string PlanTier,
    int DailyQuota,
    int RemainingDailyQuota,
    bool IsDeveloper,
    string NotationPref,
    bool OnboardingCompleted,
    bool HasPassword,
    bool HasGoogleSubject);
