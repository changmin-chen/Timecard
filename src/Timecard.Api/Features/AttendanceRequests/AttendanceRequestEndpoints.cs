using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.AttendanceRequests;

public static class AttendanceRequestEndpoints
{
    private const string CalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapAttendanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/attendance-requests").WithTags("AttendanceRequests");

        g.MapPost("", Create);
        g.MapPut("/{id:int}", Update);
        g.MapDelete("/{id:int}", Delete);

        return app;
    }

    public sealed record AttendanceRequestCreate(string Date, string Category, string Start, string End, string? Note);
    public sealed record AttendanceRequestUpdate(string Category, string Start, string End, string? Note);

    private static async Task<IResult> Create(WorkDayRepository repo, IWorkCalendar calendar, AttendanceRequestCreate req, HttpContext http, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.Date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        if (!TimeOnly.TryParse(req.Start, out var start))
            return Results.BadRequest(new { error = "Invalid start time. Use HH:mm." });

        if (!TimeOnly.TryParse(req.End, out var end))
            return Results.BadRequest(new { error = "Invalid end time. Use HH:mm." });

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, d, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var day = await repo.GetOrCreateDay(d, ct);

        var rangeResult = TimeRange.Create(start, end);
        if (!rangeResult.IsSuccess) return rangeResult.Error!.ToProblem(http);

        var result = day.AddAttendanceRequest(req.Category, rangeResult.Value!, req.Note);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayResponse(day, calendarDay));
    }

    private static async Task<IResult> Update(WorkDayRepository repo, IWorkCalendar calendar, int id, AttendanceRequestUpdate req, HttpContext http, CancellationToken ct)
    {
        if (!TimeOnly.TryParse(req.Start, out var start))
            return Results.BadRequest(new { error = "Invalid start time. Use HH:mm." });

        if (!TimeOnly.TryParse(req.End, out var end))
            return Results.BadRequest(new { error = "Invalid end time. Use HH:mm." });

        var day = await repo.LoadByAttendanceRequestId(id, ct);
        if (day is null) return Results.NotFound();

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, day.Date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var rangeResult = TimeRange.Create(start, end);
        if (!rangeResult.IsSuccess) return rangeResult.Error!.ToProblem(http);

        var result = day.UpdateAttendanceRequest(id, req.Category, rangeResult.Value!, req.Note);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayResponse(day, calendarDay));
    }

    private static async Task<IResult> Delete(WorkDayRepository repo, IWorkCalendar calendar, int id, HttpContext http, CancellationToken ct)
    {
        var day = await repo.LoadByAttendanceRequestId(id, ct);
        if (day is null) return Results.NotFound();

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, day.Date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var result = day.RemoveAttendanceRequest(id);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayResponse(day, calendarDay));
    }
}
