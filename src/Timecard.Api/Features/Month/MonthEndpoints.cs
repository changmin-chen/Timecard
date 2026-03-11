using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Auth;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Month;

public static class MonthEndpoints
{
    private const string CalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapMonthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/month").WithTags("Month");
        g.MapGet("/{year:int}/{month:int}", GetMonth);
        return app;
    }

    private static async Task<IResult> GetMonth(TimecardDb db, IWorkCalendar calendar, ICurrentUser currentUser, IClock clock, HttpContext http, int year, int month, bool includeEmpty, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) return Results.BadRequest(new { error = "year out of range." });
        if (month is < 1 or > 12) return Results.BadRequest(new { error = "month out of range." });

        var start = new DateOnly(year, month, 1);
        var endExclusive = start.AddMonths(1);

        List<WorkDay> workDays = await db.WorkDays
            .AsNoTracking()
            .Where(d => d.UserId == currentUser.UserId && d.Date >= start && d.Date < endExclusive)
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .ToListAsync(ct);

        var calendarResult = await calendar.GetRequiredDaysAsync(CalendarId, start, endExclusive, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDays = calendarResult.Value!;

        var computedDays = MonthReportBuilder.Build(workDays, calendarDays, start, endExclusive);
        var monthReport = FlexTimePolicy.ComputeMonth(computedDays.Select(d => d.Summary));

        var today = TaiwanTime.ToDate(clock.UtcNow);
        var settlementCutoff = today.AddDays(-1); // 截至昨日，避免今日進行中工作日顯示假赤字
        var settledFlexBank = monthReport.Days.FlexBalanceMinutes(settlementCutoff);
        var settledDeficit  = monthReport.Days.DeficitBalanceMinutes(settlementCutoff);

        IEnumerable<ComputedDay> dtoSource = includeEmpty
            ? computedDays
            : computedDays.Where(d => d.Source is not null);

        var dtoDays = dtoSource.Select(d => new MonthDayDto(
            Date: d.Summary.Date,
            Exists: d.Source is not null,
            IsNonWorkingDay: !d.CalendarDay.IsWorking,
            Note: d.CalendarDay.Note,
            CalendarKind: d.CalendarDay.Kind,
            Start: d.PunchStart,
            End: d.PunchEnd,
            PunchCount: d.Source?.Punches.Count ?? 0,
            PlannedMinutes: d.Summary.PlannedMinutes,
            PunchedMinutes: d.Summary.PunchedMinutes,
            EligibleMinutes: d.Summary.EligibleMinutes,
            EligibleDeltaMinutes: d.Summary.EligibleDeltaMinutes,
            FlexDeltaMinutes: d.Summary.FlexDeltaMinutes,
            DeficitMinutes: d.Summary.DeficitMinutes,
            AttendanceRequests: d.Source?.AttendanceRequests
                .Select(r => new MonthDayAttendanceDto(r.Category, r.Range.Start, r.Range.End, r.Note))
                .ToList() ?? []
        )).ToList();

        return Results.Ok(new MonthResponse(Year: year, Month: month, AsOf: today, SettledFlexBankMinutes: settledFlexBank, SettledDeficitMinutes: settledDeficit, Days: dtoDays));
    }
}
