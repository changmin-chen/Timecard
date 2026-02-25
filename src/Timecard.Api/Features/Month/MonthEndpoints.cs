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

        var workDayMap = workDays.ToDictionary(d => d.Date);

        var calendarResult = await calendar.GetRequiredDaysAsync(CalendarId, start, endExclusive, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDays = calendarResult.Value!;

        IEnumerable<DateOnly> dates = includeEmpty
            ? Enumerable.Range(0, endExclusive.DayNumber - start.DayNumber).Select(i => start.AddDays(i))
            : workDays.Select(d => d.Date);

        var dailySummaries = dates.Select(date => {
            workDayMap.TryGetValue(date, out var day);
            bool isWorking = calendarDays[date].IsWorking;
            var facts = day is not null
                ? DailySettlementFacts.FromWorkday(day, isWorking)
                : DailySettlementFacts.FromAbsence(date, isWorking);
            return FlexTimePolicy.ComputeDay(facts);
        });

        var monthReport = FlexTimePolicy.ComputeMonth(dailySummaries);

        var today = TaiwanTime.ToDate(clock.UtcNow);
        var settledFlexBank = monthReport.Days.FlexBalanceMinutes(today);
        var settledDeficit = monthReport.Days.DeficitBalanceMinutes(today);

        var dtoDays = monthReport.Days.Select(d => {
            workDayMap.TryGetValue(d.Date, out WorkDay? src);
            ResolvedCalendarDay calendarDay = calendarDays[d.Date];
            var (punchStart, punchEnd) = src?.GetPunchTimestamps() ?? (null, null);

            return new MonthDayDto(
            Date: d.Date,
            Exists: src is not null,
            IsNonWorkingDay: !calendarDay.IsWorking,
            Note: calendarDay.Note,
            CalendarKind: calendarDay.Kind,
            Start: punchStart,
            End: punchEnd,
            PunchCount: src?.Punches.Count ?? 0,
            PlannedMinutes: d.PlannedMinutes,
            PunchedMinutes: d.PunchedMinutes,
            EligibleMinutes: d.EligibleMinutes,
            EligibleDeltaMinutes: d.EligibleDeltaMinutes,
            FlexDeltaMinutes: d.FlexDeltaMinutes,
            DeficitMinutes: d.DeficitMinutes,
            AttendanceRequests: src?.AttendanceRequests
                .Select(r => new MonthDayAttendanceDto(r.Category,
                    r.Range.Start,
                    r.Range.End,
                    r.Note))
                .ToList() ?? []
            );
        }).ToList();

        return Results.Ok(new MonthResponse(Year: year, Month: month, AsOf: today, SettledFlexBankMinutes: settledFlexBank, SettledDeficitMinutes: settledDeficit, Days: dtoDays));
    }
}
