using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Domain;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Month;

public static class MonthEndpoints
{
    public static IEndpointRouteBuilder MapMonthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/month").WithTags("Month");
        g.MapGet("/{year:int}/{month:int}", GetMonth);
        return app;
    }

    private static async Task<IResult> GetMonth(TimecardDb db, int year, int month, bool includeEmpty, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) return Results.BadRequest(new { error = "year out of range." });
        if (month is < 1 or > 12) return Results.BadRequest(new { error = "month out of range." });

        var start = new DateOnly(year, month, 1);
        var endExclusive = start.AddMonths(1);

        var workDays = await db.WorkDays
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

        var computedForMonth = allDates.Select(date =>
        {
            workDayMap.TryGetValue(date, out var day);
            var isNonWorking = day?.IsNonWorkingDay ?? false;
            var planned = isNonWorking ? 0 : WorkRules.PlannedMinutesPerWorkDay;
            var (_, _, worked) = day?.DeriveSpan() ?? (null, null, 0);
            var extension = day?.CalculateExtensionMinutes() ?? 0;
            var computed = WorkRules.ComputeDay(planned, worked, extension);
            return new DayWithComputed(date, computed);
        });

        var monthComputed = WorkRules.ComputeMonth(computedForMonth);

        var dtoDays = monthComputed.Days.Select(d =>
        {
            var exists = existingDates.Contains(d.Date);
            workDayMap.TryGetValue(d.Date, out var src);

            return new MonthDayDto(
                Date: d.Date.ToString("yyyy-MM-dd"),
                Exists: exists,
                IsNonWorkingDay: src?.IsNonWorkingDay ?? false,
                Note: src?.Note ?? "",
                PunchCount: src?.Punches.Count ?? 0,
                PlannedMinutes: d.Day.PlannedMinutes,
                WorkedMinutes: d.Day.WorkedMinutes,
                ExtensionMinutes: d.Day.CreditedMinutes,
                EffectiveMinutes: d.Day.EffectiveMinutes,
                DeltaMinutes: d.Day.DeltaMinutes,
                FlexCandidate: d.Day.FlexCandidate,
                FlexApplied: d.FlexApplied,
                FlexBankEnd: d.FlexBankEnd,
                DeficitMinutes: d.DeficitMinutes
            );
        }).ToList();

        return Results.Ok(new MonthDto(Year: year, Month: month, FlexBankEnd: monthComputed.FlexBankEnd, Days: dtoDays));
    }
}
