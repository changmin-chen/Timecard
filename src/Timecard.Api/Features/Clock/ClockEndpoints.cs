using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Clock;

public static class ClockEndpoints
{
    public static IEndpointRouteBuilder MapClockEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/clock").WithTags("Clock");

        g.MapPost("/in", ClockIn);
        g.MapPost("/out", ClockOut);
        g.MapGet("/status", GetStatus);

        return app;
    }

    public sealed record ClockRequest(DateTimeOffset? at);

    private static async Task<IResult> ClockIn(TimecardDb db, ClockRequest? req, CancellationToken ct)
    {
        var now = req?.at ?? DateTimeOffset.Now;
        var date = DateOnly.FromDateTime(now.LocalDateTime);

        var day = await Mapping.GetOrCreateDay(db, date, ct);

        var hasOpen = await db.Sessions.AnyAsync(s => s.WorkDayId == day.Id && s.End == null, ct);
        if (hasOpen) return Results.BadRequest(new { error = "Already clocked in (open session exists)." });

        db.Sessions.Add(new WorkSession
        {
            WorkDayId = day.Id,
            Start = now,
            End = null
        });

        await db.SaveChangesAsync(ct);

        var fullDay = await Mapping.LoadDay(db, date, ct);
        return Results.Ok(Mapping.ToDayDto(date, fullDay));
    }

    private static async Task<IResult> ClockOut(TimecardDb db, ClockRequest? req, CancellationToken ct)
    {
        var now = req?.at ?? DateTimeOffset.Now;
        var date = DateOnly.FromDateTime(now.LocalDateTime);

        var day = await db.WorkDays.FirstOrDefaultAsync(d => d.Date == date, ct);
        if (day is null) return Results.BadRequest(new { error = "No WorkDay for this date. Clock in first." });

        var open = (await db.Sessions
                .Where(s => s.WorkDayId == day.Id && s.End == null)
                .ToListAsync(ct))  // client side ordering
            .OrderByDescending(s => s.Start)
            .FirstOrDefault();

        if (open is null) return Results.BadRequest(new { error = "No open session." });

        if (now < open.Start) return Results.BadRequest(new { error = "Clock-out time cannot be earlier than clock-in time." });

        open.End = now;
        await db.SaveChangesAsync(ct);

        var fullDay = await Mapping.LoadDay(db, date, ct);
        return Results.Ok(Mapping.ToDayDto(date, fullDay));
    }

    private static async Task<IResult> GetStatus(TimecardDb db, string? date, CancellationToken ct)
    {
        var d = ParseDateOrToday(date);
        var day = await Mapping.LoadDay(db, d, ct);

        var open = day?.Sessions
            .Where(s => s.End == null)
            .OrderByDescending(s => s.Start)
            .Select(s => new { sessionId = s.Id, start = s.Start })
            .FirstOrDefault();

        return Results.Ok(new
        {
            date = d.ToString("yyyy-MM-dd"),
            hasOpenSession = open is not null,
            openSession = open
        });
    }

    private static DateOnly ParseDateOrToday(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s) && DateOnly.TryParse(s, out var d)) return d;
        return DateOnly.FromDateTime(DateTime.Now);
    }
}
