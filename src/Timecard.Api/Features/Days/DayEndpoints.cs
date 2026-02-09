using Microsoft.EntityFrameworkCore;
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

        // sessions CRUD（最小但夠用）
        var s = app.MapGroup("/api/sessions").WithTags("Sessions");
        s.MapPost("", CreateSession);
        s.MapPut("/{id:int}", UpdateSession);
        s.MapDelete("/{id:int}", DeleteSession);

        return app;
    }

    public sealed record NonWorkingRequest(bool isNonWorkingDay, string? note);

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
        day.IsNonWorkingDay = req.isNonWorkingDay;
        day.Note = req.note ?? day.Note;

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, d, ct);
        return Results.Ok(Mapping.ToDayDto(d, full));
    }

    public sealed record SessionCreate(DateTimeOffset start, DateTimeOffset end);

    private static async Task<IResult> CreateSession(TimecardDb db, SessionCreate req, CancellationToken ct)
    {
        if (req.end <= req.start)
            return Results.BadRequest(new { error = "end must be later than start." });

        var startDate = DateOnly.FromDateTime(req.start.LocalDateTime);
        var endDate = DateOnly.FromDateTime(req.end.LocalDateTime);
        if (startDate != endDate)
            return Results.BadRequest(new { error = "Session cannot跨日（MVP 限制）。" });

        var day = await Mapping.GetOrCreateDay(db, startDate, ct);

        db.Sessions.Add(new WorkSession
        {
            WorkDayId = day.Id,
            Start = req.start,
            End = req.end
        });

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, startDate, ct);
        return Results.Ok(Mapping.ToDayDto(startDate, full));
    }

    public sealed record SessionUpdate(DateTimeOffset start, DateTimeOffset? end);

    private static async Task<IResult> UpdateSession(TimecardDb db, int id, SessionUpdate req, CancellationToken ct)
    {
        var s = await db.Sessions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return Results.NotFound();

        if (req.end is not null && req.end <= req.start)
            return Results.BadRequest(new { error = "end must be later than start." });

        var startDate = DateOnly.FromDateTime(req.start.LocalDateTime);
        var endDate = req.end is null ? startDate : DateOnly.FromDateTime(req.end.Value.LocalDateTime);
        if (startDate != endDate)
            return Results.BadRequest(new { error = "Session cannot跨日（MVP 限制）。" });

        // 如果改到不同日期：簡化做法，直接阻擋（不想寫搬移邏輯）
        var originalDate = await db.WorkDays.Where(d => d.Id == s.WorkDayId).Select(d => d.Date).FirstAsync(ct);
        if (startDate != originalDate)
            return Results.BadRequest(new { error = "Changing session date is not supported in MVP." });

        s.Start = req.start;
        s.End = req.end;

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, originalDate, ct);
        return Results.Ok(Mapping.ToDayDto(originalDate, full));
    }

    private static async Task<IResult> DeleteSession(TimecardDb db, int id, CancellationToken ct)
    {
        var s = await db.Sessions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return Results.NotFound();

        var dayDate = await db.WorkDays.Where(d => d.Id == s.WorkDayId).Select(d => d.Date).FirstAsync(ct);

        db.Sessions.Remove(s);
        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, dayDate, ct);
        return Results.Ok(Mapping.ToDayDto(dayDate, full));
    }
}
