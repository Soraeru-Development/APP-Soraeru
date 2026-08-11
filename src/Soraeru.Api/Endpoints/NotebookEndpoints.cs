using System.Security.Claims;
using Soraeru.Application.Notebook;

namespace Soraeru.Api.Endpoints;

public static class NotebookEndpoints
{
    public static RouteGroupBuilder MapNotebookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/notebook").WithTags("Notebook").RequireAuthorization();

        group.MapGet("/", async (INotebookService notebook, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await notebook.ListAsync(id.Value, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "List failed."),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Ok(result.Value.Select(ToResponse).ToList());
        });

        group.MapGet("/{cardId:guid}", async (
            Guid cardId,
            INotebookService notebook,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await notebook.GetAsync(id.Value, cardId, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Not found."),
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(ToResponse(result.Value));
        });

        group.MapPost("/", async (
            SaveNotebookRequest body,
            INotebookService notebook,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await notebook.SaveAsync(
                new SaveNotebookCardCommand(
                    id.Value,
                    body.SourceText,
                    body.DetectedLanguage,
                    body.MeaningZh,
                    body.Pronunciation,
                    body.SelectedMnemonic),
                ct);

            if (!result.IsSuccess || result.Value is null)
            {
                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Save failed."),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Created($"/api/v1/notebook/{result.Value.Id}", ToResponse(result.Value));
        });

        group.MapDelete("/{cardId:guid}", async (
            Guid cardId,
            INotebookService notebook,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var id = ResolveUserId(user);
            if (id is null)
            {
                return Results.Unauthorized();
            }

            var result = await notebook.DeleteAsync(id.Value, cardId, ct);
            if (!result.IsSuccess)
            {
                var status = result.ErrorCode == "NOT_FOUND"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;
                return Results.Json(
                    new ErrorResponse(result.ErrorCode ?? "ERROR", result.ErrorMessage ?? "Delete failed."),
                    statusCode: status);
            }

            return Results.NoContent();
        });

        return group;
    }

    private static NotebookCardResponse ToResponse(NotebookCard card) =>
        new(
            card.Id,
            card.SourceText,
            card.DetectedLanguage,
            card.MeaningZh,
            card.Pronunciation,
            card.SelectedMnemonic,
            card.CreatedAtUtc);

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var fromClaim) ? fromClaim : null;
    }
}

public sealed record SaveNotebookRequest(
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic);

public sealed record NotebookCardResponse(
    Guid Id,
    string SourceText,
    string DetectedLanguage,
    string MeaningZh,
    string Pronunciation,
    string SelectedMnemonic,
    DateTimeOffset CreatedAtUtc);
