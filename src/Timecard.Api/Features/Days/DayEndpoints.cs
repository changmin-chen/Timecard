using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Days;

public static class DayEndpoints
{
    private const string CalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapDayEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/day").WithTags("Day");

        g.MapGet("/today", GetToday);
        g.MapGet("/{date}", GetByDate);

        return app;
    }

    private static async Task<IResult> GetToday(WorkDayRepository repo, IWorkCalendar calendar, HttpContext http, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        var day = await repo.LoadDay(date, ct);

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);

        return Results.Ok(DayMapping.ToDayDto(date, day, calendarResult.Value!));
    }

    private static async Task<IResult> GetByDate(WorkDayRepository repo, IWorkCalendar calendar, HttpContext http, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await repo.LoadDay(d, ct);

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, d, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);

        return Results.Ok(DayMapping.ToDayDto(d, day, calendarResult.Value!));
    }
}
