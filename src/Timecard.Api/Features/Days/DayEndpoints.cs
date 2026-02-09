using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Days;

public static class DayEndpoints
{
    public static IEndpointRouteBuilder MapDayEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/day").WithTags("Day");

        g.MapGet("/today", GetToday);
        g.MapGet("/{date}", GetByDate);
        g.MapPut("/{date}/nonworking", SetNonWorking);

        return app;
    }

    public sealed record NonWorkingRequest(bool IsNonWorkingDay, string? Note);

    private static async Task<IResult> GetToday(TimecardDb db, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        var day = await Mapping.LoadDay(db, date, ct);
        return Results.Ok(Mapping.ToDayDto(date, day));
    }

    private static async Task<IResult> GetByDate(TimecardDb db, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await Mapping.LoadDay(db, d, ct);
        return Results.Ok(Mapping.ToDayDto(d, day));
    }

    private static async Task<IResult> SetNonWorking(TimecardDb db, string date, NonWorkingRequest req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await Mapping.GetOrCreateDay(db, d, ct);
        day.IsNonWorkingDay = req.IsNonWorkingDay;
        day.Note = req.Note ?? day.Note;

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, d, ct);
        return Results.Ok(Mapping.ToDayDto(d, full));
    }
}
