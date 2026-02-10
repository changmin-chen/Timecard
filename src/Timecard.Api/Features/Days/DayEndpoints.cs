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

    private static async Task<IResult> GetToday(WorkDayRepository repo, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        var day = await repo.LoadDay(date, ct);
        return Results.Ok(Mapping.ToDayDto(date, day));
    }

    private static async Task<IResult> GetByDate(WorkDayRepository repo, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await repo.LoadDay(d, ct);
        return Results.Ok(Mapping.ToDayDto(d, day));
    }

    private static async Task<IResult> SetNonWorking(WorkDayRepository repo, string date, NonWorkingRequest req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await repo.GetOrCreateDay(d, ct);
        day.SetNonWorking(req.IsNonWorkingDay, req.Note);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }
}
