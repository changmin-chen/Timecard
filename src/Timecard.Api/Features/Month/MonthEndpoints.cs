using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain;
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

    private static async Task<IResult> GetMonth(TimecardDb db, IWorkCalendar calendar, HttpContext http, int year, int month, bool includeEmpty, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) return Results.BadRequest(new { error = "year out of range." });
        if (month is < 1 or > 12) return Results.BadRequest(new { error = "month out of range." });

        var start = new DateOnly(year, month, 1);
        var endExclusive = start.AddMonths(1);

        var workDays = await db.WorkDays
            .AsNoTracking()
            .Where(d => d.Date >= start && d.Date < endExclusive)
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .ToListAsync(ct);

        var workDayMap = workDays.ToDictionary(d => d.Date);
        var existingDates = workDays.Select(d => d.Date).ToHashSet();

        List<DateOnly> allDates;
        if (!includeEmpty)
        {
            allDates = workDays.Select(d => d.Date).OrderBy(d => d).ToList();
        }
        else
        {
            allDates = [];
            for (var d = start; d < endExclusive; d = d.AddDays(1))
                allDates.Add(d);
        }

        var calendarResult = await calendar.GetRequiredDaysAsync(CalendarId, start, endExclusive, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDays = calendarResult.Value!;

        var computedForMonth = allDates.Select(date => {
            workDayMap.TryGetValue(date, out var day);
            bool isWorkingDay = calendarDays[date].IsWorking;

            var facts = day is not null
                ? DailySettlementFacts.FromWorkday(day, isWorkingDay: calendarDays[date].IsWorking)
                : DailySettlementFacts.ForAbsence(date, isWorkingDay);
            return FlexTimePolicy.ComputeDay(facts);
        });

        var monthReport = FlexTimePolicy.ComputeMonth(computedForMonth);

        var dtoDays = monthReport.Days.Select(d => {
            var exists = existingDates.Contains(d.Date);
            workDayMap.TryGetValue(d.Date, out var src);
            var calendarDay = calendarDays[d.Date];

            return new MonthDayDto(
            Date: d.Date.ToString("yyyy-MM-dd"),
            Exists: exists,
            IsNonWorkingDay: !calendarDay.IsWorking,
            Note: calendarDay.Note,
            CalendarKind: calendarDay.Kind,
            CalendarSource: calendarDay.Source,
            PunchCount: src?.Punches.Count ?? 0,
            PlannedMinutes: d.Summary.PlannedMinutes,
            PunchedMinutes: d.Summary.PunchedMinutes,
            EligibleMinutes: d.Summary.EligibleMinutes,
            AttendanceDeltaMinutes: d.Summary.AttendanceDeltaMinutes,
            FlexDeltaMinutes: d.Summary.FlexDeltaMinutes,
            FlexBankMinutes: d.FlexBankMinutes,
            DeficitMinutes: d.Summary.DeficitMinutes
            );
        }).ToList();

        return Results.Ok(new MonthResponse(Year: year, Month: month, TotalFlexBankBalance: monthReport.TotalFlexBankMinutes, TotalDeficitMinutes: monthReport.TotalDeficitMinutes, Days: dtoDays));
    }
}
