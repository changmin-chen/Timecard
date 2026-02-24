using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Domain;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Auth;
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
        
        var p = app.MapGroup("/api/punches").WithTags("Punches");
        p.MapDelete("/{id:int}", DeletePunch);

        return app;
    }

    public sealed record PunchCreate(DateTimeOffset? At, string? Note, bool Force);

    private static async Task<IResult> AddPunch(WorkDayRepository repo, IWorkCalendar calendar, ICurrentUser currentUser, PunchCreate? req, HttpContext http, CancellationToken ct)
    {
        var now = req?.At ?? DateTimeOffset.UtcNow;
        var date = TaiwanTime.ToDate(now);

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var day = await repo.GetOrCreateDay(currentUser.UserId, date, ct);

        var result = day.AddPunch(now, req?.Note, MinInterval, req?.Force == true);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayResponse(day, calendarDay));
    }

    private static async Task<IResult> DeletePunch(WorkDayRepository repo, IWorkCalendar calendar, ICurrentUser currentUser, int id, HttpContext http, CancellationToken ct)
    {
        WorkDay? day = await repo.LoadByPunchId(currentUser.UserId, id, ct);
        if (day is null) return Results.NotFound();

        var calendarResult = await calendar.GetRequiredDayAsync(CalendarId, day.Date, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDay = calendarResult.Value!;

        var result = day.RemovePunch(id);
        if (!result.IsSuccess) return result.Error!.ToProblem(http);

        await repo.SaveChangesAsync(ct);
        return Results.Ok(DayMapping.ToDayResponse(day, calendarDay));
    }
}
