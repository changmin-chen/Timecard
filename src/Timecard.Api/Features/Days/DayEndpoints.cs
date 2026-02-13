using Timecard.Api.Features.Calendar;
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

    private static async Task<IResult> GetToday(WorkDayRepository repo, IWorkCalendar calendar, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(DateTime.Now);
        var day = await repo.LoadDay(date, ct);

        try
        {
            var calendarDay = await calendar.GetRequiredDayAsync(CalendarId, date, ct);
            return Results.Ok(DayMapping.ToDayDto(date, day, calendarDay));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Calendar data missing", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> GetByDate(WorkDayRepository repo, IWorkCalendar calendar, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await repo.LoadDay(d, ct);

        try
        {
            var calendarDay = await calendar.GetRequiredDayAsync(CalendarId, d, ct);
            return Results.Ok(DayMapping.ToDayDto(d, day, calendarDay));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "Calendar data missing", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
