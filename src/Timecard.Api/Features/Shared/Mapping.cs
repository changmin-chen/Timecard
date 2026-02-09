using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Domain;

namespace Timecard.Api.Features.Shared;

public static class Mapping
{
    public static (DateTimeOffset? start, DateTimeOffset? end, int workedMinutes) DeriveSpan(IEnumerable<PunchEvent> punches)
    {
        var ordered = punches.OrderBy(p => p.At).ToList();
        if (ordered.Count == 0) return (null, null, 0);

        var start = ordered[0].At;
        if (ordered.Count == 1) return (start, null, 0);

        var end = ordered[^1].At;
        var mins = (int)Math.Max(0, (end - start).TotalMinutes);
        return (start, end, mins);
    }

    public static int SumCreditedMinutes(IEnumerable<Adjustment> adjustments)
        => adjustments.Sum(a => a.Minutes);

    public static DayDto ToDayDto(DateOnly date, WorkDay? day)
    {
        var exists = day is not null;
        var isNonWorking = day?.IsNonWorkingDay ?? false;
        var note = day?.Note ?? "";

        var planned = isNonWorking ? 0 : WorkRules.PlannedMinutesPerWorkDay;

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, worked) = DeriveSpan(punches);

        var credited = day is null ? 0 : SumCreditedMinutes(day.Adjustments);
        var computed = WorkRules.ComputeDay(planned, worked, credited);

        return new DayDto(
            Date: date.ToString("yyyy-MM-dd"),
            Exists: exists,
            IsNonWorkingDay: isNonWorking,
            Note: note,

            Start: start,
            End: end,
            PunchCount: punches.Count,

            PlannedMinutes: computed.PlannedMinutes,
            WorkedMinutes: computed.WorkedMinutes,
            CreditedMinutes: computed.CreditedMinutes,
            EffectiveMinutes: computed.EffectiveMinutes,
            DeltaMinutes: computed.DeltaMinutes,
            FlexCandidate: computed.FlexCandidate,

            Punches: punches.Select(p => new PunchDto(p.Id, p.At, p.Note)).ToList(),
            Adjustments: day?.Adjustments
                .OrderBy(a => a.Kind)
                .Select(a => new AdjustmentDto(a.Id, a.Kind, a.Minutes, a.Note))
                .ToList() ?? []
        );
    }

    public static async Task<WorkDay?> LoadDay(TimecardDb db, DateOnly date, CancellationToken ct)
        => await db.WorkDays
            .Include(d => d.Punches)
            .Include(d => d.Adjustments)
            .FirstOrDefaultAsync(d => d.Date == date, ct);

    public static async Task<WorkDay> GetOrCreateDay(TimecardDb db, DateOnly date, CancellationToken ct)
    {
        var day = await db.WorkDays.FirstOrDefaultAsync(d => d.Date == date, ct);
        if (day is not null) return day;

        day = new WorkDay { Date = date };
        db.WorkDays.Add(day);
        await db.SaveChangesAsync(ct);
        return day;
    }
}
