using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Adjustments;

public static class AdjustmentEndpoints
{
    public static IEndpointRouteBuilder MapAdjustmentEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/adjustments").WithTags("Adjustments");

        g.MapPost("", Create);
        g.MapPut("/{id:int}", Update);
        g.MapDelete("/{id:int}", Delete);

        return app;
    }

    public sealed record AdjustmentCreate(string Date, string Kind, int Minutes, string? Note);
    public sealed record AdjustmentUpdate(string Kind, int Minutes, string? Note);

    private static async Task<IResult> Create(WorkDayRepository repo, AdjustmentCreate req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.Date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await repo.GetOrCreateDay(d, ct);

        try
        {
            day.AddAdjustment(req.Kind, req.Minutes, req.Note);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> Update(WorkDayRepository repo, int id, AdjustmentUpdate req, CancellationToken ct)
    {
        var day = await repo.LoadByAdjustmentId(id, ct);
        if (day is null) return Results.NotFound();

        try
        {
            day.UpdateAdjustment(id, req.Kind, req.Minutes, req.Note);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> Delete(WorkDayRepository repo, int id, CancellationToken ct)
    {
        var day = await repo.LoadByAdjustmentId(id, ct);
        if (day is null) return Results.NotFound();

        try
        {
            day.RemoveAdjustment(id);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }
}
