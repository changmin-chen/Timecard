using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Punch;

public static class PunchEndpoints
{
    public static IEndpointRouteBuilder MapPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/punch").WithTags("Punch");

        g.MapPost("", AddPunch);
        g.MapGet("/status", GetStatus);

        var p = app.MapGroup("/api/punches").WithTags("Punches");
        p.MapPut("/{id:int}", UpdatePunch);
        p.MapDelete("/{id:int}", DeletePunch);

        return app;
    }

    public sealed record PunchCreate(DateTimeOffset? At, string? Note, bool Force);

    public sealed record PunchUpdate(DateTimeOffset At, string? Note);

    // MVP 反手滑：同一天距離上一筆 < 30 秒就擋掉（除非 force=true）
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    private static async Task<IResult> AddPunch(TimecardDb db, PunchCreate? req, CancellationToken ct)
    {
        var now = req?.At ?? DateTimeOffset.Now;
        var date = DateOnly.FromDateTime(now.LocalDateTime);

        var day = await Mapping.GetOrCreateDay(db, date, ct);

        var lastAt = (await db.Punches
                .Where(p => p.WorkDayId == day.Id)
                .ToListAsync(ct))  //  in-memory ordering
            .OrderByDescending(p => p.At)
            .Select(p => (DateTimeOffset?)p.At)
            .FirstOrDefault();

        if (lastAt is not null && (now - lastAt.Value) < MinInterval && (req?.Force != true))
        {
            return Results.BadRequest(new { error = $"Too fast. Last punch was {(int)(now - lastAt.Value).TotalSeconds}s ago. Use force=true to override." });
        }

        db.Punches.Add(new PunchEvent
        {
            WorkDayId = day.Id,
            At = now,
            Note = req?.Note?.Trim() ?? ""
        });

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, date, ct);
        return Results.Ok(Mapping.ToDayDto(date, full));
    }

    private static async Task<IResult> UpdatePunch(TimecardDb db, int id, PunchUpdate req, CancellationToken ct)
    {
        var p = await db.Punches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();

        var newDate = DateOnly.FromDateTime(req.At.LocalDateTime);
        var oldDate = await db.WorkDays.Where(d => d.Id == p.WorkDayId).Select(d => d.Date).FirstAsync(ct);

        if (newDate != oldDate)
            return Results.BadRequest(new { error = "Changing punch date is not supported in MVP." });

        p.At = req.At;
        p.Note = req.Note?.Trim() ?? "";

        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, oldDate, ct);
        return Results.Ok(Mapping.ToDayDto(oldDate, full));
    }

    private static async Task<IResult> DeletePunch(TimecardDb db, int id, CancellationToken ct)
    {
        var p = await db.Punches.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Results.NotFound();

        var dayDate = await db.WorkDays.Where(d => d.Id == p.WorkDayId).Select(d => d.Date).FirstAsync(ct);

        db.Punches.Remove(p);
        await db.SaveChangesAsync(ct);

        var full = await Mapping.LoadDay(db, dayDate, ct);
        return Results.Ok(Mapping.ToDayDto(dayDate, full));
    }

    private static async Task<IResult> GetStatus(TimecardDb db, string? date, CancellationToken ct)
    {
        var d = ParseDateOrToday(date);
        var day = await Mapping.LoadDay(db, d, ct);

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, worked) = Mapping.DeriveSpan(punches);

        return Results.Ok(new
        {
            Date = d.ToString("yyyy-MM-dd"),
            PunchCount = punches.Count,
            Start = start,
            End = end,
            WorkedMinutes = worked
        });
    }

    private static DateOnly ParseDateOrToday(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s) && DateOnly.TryParse(s, out var d)) return d;
        return DateOnly.FromDateTime(DateTime.Now);
    }
}
