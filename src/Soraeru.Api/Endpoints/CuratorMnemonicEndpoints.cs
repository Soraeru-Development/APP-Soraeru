using System.Security.Claims;
using Soraeru.Application.Curator;

namespace Soraeru.Api.Endpoints;

public static class CuratorMnemonicEndpoints
{
    public static RouteGroupBuilder MapCuratorMnemonicEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/curator/verified-mnemonics")
            .WithTags("Curator")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? language,
            string? q,
            ICuratorMnemonicService curator,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await curator.ListAsync(id.Value, language, q, ct);
            return ToHttp(result, list => Results.Ok(list.Select(ToResponse).ToList()));
        });

        group.MapGet("/{entryId:guid}", async (
            Guid entryId,
            ICuratorMnemonicService curator,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await curator.GetAsync(id.Value, entryId, ct);
            return ToHttp(result, dto => Results.Ok(ToResponse(dto)));
        });

        group.MapPost("/", async (
            CreateVerifiedMnemonicRequest body,
            ICuratorMnemonicService curator,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await curator.CreateAsync(
                new CreateVerifiedMnemonicCommand(
                    id.Value,
                    body.Language,
                    body.SourceText,
                    body.DisplayText,
                    body.NotationText,
                    body.Explanation,
                    body.IsEnabled ?? true),
                ct);

            return ToHttp(
                result,
                dto => Results.Created($"/api/v1/curator/verified-mnemonics/{dto.Id}", ToResponse(dto)));
        });

        group.MapPut("/{entryId:guid}", async (
            Guid entryId,
            UpdateVerifiedMnemonicRequest body,
            ICuratorMnemonicService curator,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await curator.UpdateAsync(
                new UpdateVerifiedMnemonicCommand(
                    id.Value,
                    entryId,
                    body.DisplayText,
                    body.NotationText,
                    body.Explanation,
                    body.IsEnabled),
                ct);

            return ToHttp(result, dto => Results.Ok(ToResponse(dto)));
        });

        group.MapPost("/{entryId:guid}/enabled", async (
            Guid entryId,
            SetVerifiedMnemonicEnabledRequest body,
            ICuratorMnemonicService curator,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await curator.SetEnabledAsync(
                new SetVerifiedMnemonicEnabledCommand(id.Value, entryId, body.IsEnabled),
                ct);

            return ToHttp(result, dto => Results.Ok(ToResponse(dto)));
        });

        return group;
    }

    private static IResult ToHttp<T>(
        Application.Common.ServiceResult<T> result,
        Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return onSuccess(result.Value);
        }

        var status = result.ErrorCode switch
        {
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(
            new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Request failed."),
            statusCode: status);
    }

    private static VerifiedMnemonicResponse ToResponse(VerifiedMnemonicDto dto) =>
        new(
            dto.Id,
            dto.Language,
            dto.SourceText,
            dto.NormalizedSource,
            dto.DisplayText,
            dto.NotationText,
            dto.Explanation,
            dto.IsEnabled,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc);

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var fromClaim) ? fromClaim : null;
    }
}

public sealed record CreateVerifiedMnemonicRequest(
    string Language,
    string SourceText,
    string DisplayText,
    string NotationText,
    string Explanation,
    bool? IsEnabled = true);

public sealed record UpdateVerifiedMnemonicRequest(
    string DisplayText,
    string NotationText,
    string Explanation,
    bool? IsEnabled = null);

public sealed record SetVerifiedMnemonicEnabledRequest(bool IsEnabled);

public sealed record VerifiedMnemonicResponse(
    Guid Id,
    string Language,
    string SourceText,
    string NormalizedSource,
    string DisplayText,
    string NotationText,
    string Explanation,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
