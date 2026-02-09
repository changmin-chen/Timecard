using Microsoft.EntityFrameworkCore;
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

    private static async Task<IResult> Create(TimecardDb db, AdjustmentCreate req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.Date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        if (string.IsNullOrWhiteSpace(req.Kind))
            return Results.BadRequest(new { error = "kind is required." });

        var day = await Mapping.GetOrCreateDay(db, d, ct);

        db.Adjustments.Add(new Adjustment
        {
            WorkDayId = day.Id,
            Kind = req.Kind.Trim(),
            Minutes = req.Minutes,
            Note = req.Note?.Trim() ?? ""
        });

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, d, ct);
        return Results.Ok(Mapping.ToDayDto(d, full));
    }

    private static async Task<IResult> Update(TimecardDb db, int id, AdjustmentUpdate req, CancellationToken ct)
    {
        var a = await db.Adjustments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(req.Kind))
            return Results.BadRequest(new { error = "kind is required." });

        a.Kind = req.Kind.Trim();
        a.Minutes = req.Minutes;
        a.Note = req.Note?.Trim() ?? "";

        await db.SaveChangesAsync(ct);

        var dayDate = await db.WorkDays.Where(d => d.Id == a.WorkDayId).Select(d => d.Date).FirstAsync(ct);
        var full = await Mapping.LoadDay(db, dayDate, ct);
        return Results.Ok(Mapping.ToDayDto(dayDate, full));
    }

    private static async Task<IResult> Delete(TimecardDb db, int id, CancellationToken ct)
    {
        var a = await db.Adjustments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return Results.NotFound();

        var dayDate = await db.WorkDays.Where(d => d.Id == a.WorkDayId).Select(d => d.Date).FirstAsync(ct);

        db.Adjustments.Remove(a);
        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, dayDate, ct);
        return Results.Ok(Mapping.ToDayDto(dayDate, full));
    }
}
