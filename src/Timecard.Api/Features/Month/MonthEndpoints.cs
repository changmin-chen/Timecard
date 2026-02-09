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

        // 只抓有資料的日子（預設），避免你沒打卡的週末被算成「欠工時」
        var query = db.WorkDays
            .Where(d => d.Date >= start && d.Date < endExclusive)
            .Include(d => d.Sessions)
            .Include(d => d.Adjustments);

        var existingDays = await query.ToListAsync(ct);

        List<(DateOnly date, WorkDay? day)> days;
        if (!includeEmpty)
        {
            days = existingDays
                .Select(d => (d.Date, (WorkDay?)d))
                .OrderBy(x => x.Date)
                .ToList();
        }
        else
        {
            var map = existingDays.ToDictionary(d => d.Date, d => (WorkDay?)d);
            days = new List<(DateOnly, WorkDay?)>();
            for (var d = start; d < endExclusive; d = d.AddDays(1))
            {
                map.TryGetValue(d, out var wd);
                days.Add((d, wd));
            }
        }

        var computedForMonth = days.Select(x => {
            var dayDto = Mapping.ToDayDto(x.date, x.day);
            var dayComputed = WorkRules.ComputeDay(dayDto.PlannedMinutes, dayDto.WorkedMinutes, dayDto.CreditedMinutes);
            return new DayWithComputed(x.date, dayComputed);
        });

        var monthComputed = WorkRules.ComputeMonth(computedForMonth);

        var dtoDays = monthComputed.Days.Select(d => {
            // 重新抓 day 的 meta（nonworking/note/exists）
            var day = days.First(x => x.date == d.Date).day;
            var exists = day is not null;
            var isNonWorking = day?.IsNonWorkingDay ?? false;
            var note = day?.Note ?? "";

            return new MonthDayDto(
            Date: d.Date.ToString("yyyy-MM-dd"),
            Exists: exists,
            IsNonWorkingDay: isNonWorking,
            Note: note,
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

        return Results.Ok(new MonthDto(year, month, monthComputed.FlexBankEnd, dtoDays));
    }
}
