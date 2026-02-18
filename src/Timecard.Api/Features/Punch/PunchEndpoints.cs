using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Punch;

public static class PunchEndpoints
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);
    private const string CalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/punch").WithTags("Punch");

        g.MapPost("", AddPunch);
        g.MapGet("/status", GetStatus);

        var p = app.MapGroup("/api/punches").WithTags("Punches");
        p.MapDelete("/{id:int}", DeletePunch);

        return app;
    }

    public sealed record PunchCreate(DateTimeOffset? At, string? Note, bool Force);

    private static async Task<IResult> AddPunch(WorkDayRepository repo, IWorkCalendar calendar, PunchCreate? req, HttpContext http, CancellationToken ct)
    {
        var now = req?.At ?? DateTimeOffset.UtcNow;
        var date = DateOnly.FromDateTime(now.LocalDateTime);

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var day = await repo.GetOrCreateDay(date, ct);

        var result = day.AddPunch(now, req?.Note, MinInterval, req?.Force == true);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayDto(day, calendarDay));
    }

    private static async Task<IResult> DeletePunch(WorkDayRepository repo, IWorkCalendar calendar, int id, HttpContext http, CancellationToken ct)
    {
        var day = await repo.LoadByPunchId(id, ct);
        if (day is null) return Results.NotFound();

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, day.Date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var result = day.RemovePunch(id);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayDto(day, calendarDay));
    }

    private static async Task<IResult> GetStatus(WorkDayRepository repo, string? date, CancellationToken ct)
    {
        var d = ParseDateOrToday(date);
        var day = await repo.LoadDay(d, ct);

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, worked) = day?.DeriveSpan() ?? (null, null, 0);

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
