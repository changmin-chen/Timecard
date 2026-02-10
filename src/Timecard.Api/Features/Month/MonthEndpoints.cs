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

        var projected = await db.WorkDays
            .Where(d => d.Date >= start && d.Date < endExclusive)
            .Select(d => new MonthDayProjection(
                d.Date,
                d.IsNonWorkingDay,
                d.Note,
                d.Punches.Count,
                d.Punches.Min(p => (DateTimeOffset?)p.At),
                d.Punches.Max(p => (DateTimeOffset?)p.At),
                d.Adjustments.Sum(a => (int?)a.Minutes) ?? 0
            ))
            .ToListAsync(ct);

        List<MonthDayProjection> days;
        if (!includeEmpty)
        {
            days = projected.OrderBy(x => x.Date).ToList();
        }
        else
        {
            var map = projected.ToDictionary(x => x.Date);
            days = [];
            for (var d = start; d < endExclusive; d = d.AddDays(1))
            {
                if (map.TryGetValue(d, out var existing))
                {
                    days.Add(existing);
                    continue;
                }

                days.Add(new MonthDayProjection(d, false, "", 0, null, null, 0));
            }
        }

        var computedForMonth = days.Select(d =>
        {
            var planned = d.IsNonWorkingDay ? 0 : WorkRules.PlannedMinutesPerWorkDay;
            var worked = DeriveWorkedMinutes(d.Start, d.End, d.PunchCount);
            var computed = WorkRules.ComputeDay(planned, worked, d.CreditedMinutes);
            return new DayWithComputed(d.Date, computed);
        });

        var monthComputed = WorkRules.ComputeMonth(computedForMonth);
        var dayMap = days.ToDictionary(d => d.Date);
        var existingDates = projected.Select(x => x.Date).ToHashSet();

        var dtoDays = monthComputed.Days.Select(d =>
        {
            var src = dayMap[d.Date];
            var exists = existingDates.Contains(d.Date);

            return new MonthDayDto(
                Date: d.Date.ToString("yyyy-MM-dd"),
                Exists: exists,
                IsNonWorkingDay: src.IsNonWorkingDay,
                Note: src.Note,
                PunchCount: src.PunchCount,
                PlannedMinutes: d.Day.PlannedMinutes,
                WorkedMinutes: d.Day.WorkedMinutes,
                CreditedMinutes: d.Day.CreditedMinutes,
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

    private static int DeriveWorkedMinutes(DateTimeOffset? start, DateTimeOffset? end, int punchCount)
    {
        if (punchCount < 2 || start is null || end is null) return 0;
        return (int)Math.Max(0, (end.Value - start.Value).TotalMinutes);
    }

    private sealed record MonthDayProjection(
        DateOnly Date,
        bool IsNonWorkingDay,
        string Note,
        int PunchCount,
        DateTimeOffset? Start,
        DateTimeOffset? End,
        int CreditedMinutes
    );
}
