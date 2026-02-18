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

        var computedForMonth = allDates.Select(date =>
        {
            workDayMap.TryGetValue(date, out var day);
            var isNonWorking = !calendarDays[date].IsWorking;
            var planned = isNonWorking ? 0 : WorkRules.PlannedMinutesPerWorkDay;
            var (_, _, worked) = day?.DeriveSpan() ?? (null, null, 0);
            var extension = day?.CalculateExtensionMinutes() ?? 0;
            var computed = WorkRules.ComputeDay(planned, worked, extension);
            return new DatedWorkSummary(date, computed);
        });

        var monthReport = WorkRules.ComputeMonth(computedForMonth);

        var dtoDays = monthReport.Days.Select(d =>
        {
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
                PlannedMinutes: d.Day.PlannedMinutes,
                WorkedMinutes: d.Day.WorkedMinutes,
                ExtensionMinutes: d.Day.CreditedMinutes,
                EffectiveMinutes: d.Day.EffectiveMinutes,
                DeltaMinutes: d.Day.DeltaMinutes,
                FlexDeltaMinutes: d.Day.FlexDeltaMinutes,
                FlexUsedMinutes: d.FlexUsedMinutes,
                FlexBankBalance: d.FlexBankBalance,
                DeficitMinutes: d.DeficitMinutes
            );
        }).ToList();

        return Results.Ok(new MonthDto(Year: year, Month: month, FlexBankBalance: monthReport.FlexBankBalance, TotalDeficitMinutes: monthReport.TotalDeficitMinutes, Days: dtoDays));
    }
}
