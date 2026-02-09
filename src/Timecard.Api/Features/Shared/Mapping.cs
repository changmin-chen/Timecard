using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Domain;

namespace Timecard.Api.Features.Shared;

public static class Mapping
{
    public static int SumWorkedMinutes(IEnumerable<WorkSession> sessions)
        => sessions.Where(s => s.End is not null)
                   .Sum(s => (int)Math.Max(0, (s.End!.Value - s.Start).TotalMinutes));

    public static int SumCreditedMinutes(IEnumerable<Adjustment> adjustments)
        => adjustments.Sum(a => a.Minutes);

    public static DayDto ToDayDto(DateOnly date, WorkDay? day)
    {
        var exists = day is not null;
        var isNonWorking = day?.IsNonWorkingDay ?? false;
        var note = day?.Note ?? "";

        var planned = isNonWorking ? 0 : WorkRules.PlannedMinutesPerWorkDay;
        var worked = day is null ? 0 : SumWorkedMinutes(day.Sessions);
        var credited = day is null ? 0 : SumCreditedMinutes(day.Adjustments);

        var computed = WorkRules.ComputeDay(planned, worked, credited);

        return new DayDto(
            date: date.ToString("yyyy-MM-dd"),
            exists: exists,
            isNonWorkingDay: isNonWorking,
            note: note,

            plannedMinutes: computed.PlannedMinutes,
            workedMinutes: computed.WorkedMinutes,
            creditedMinutes: computed.CreditedMinutes,
            effectiveMinutes: computed.EffectiveMinutes,
            deltaMinutes: computed.DeltaMinutes,
            flexCandidate: computed.FlexCandidate,

            sessions: day?.Sessions
                .OrderBy(s => s.Start)
                .Select(s => new SessionDto(s.Id, s.Start, s.End))
                .ToList() ?? [],

            adjustments: day?.Adjustments
                .OrderBy(a => a.Kind)
                .Select(a => new AdjustmentDto(a.Id, a.Kind, a.Minutes, a.Note))
                .ToList() ?? []
        );
    }

    public static async Task<WorkDay?> LoadDay(TimecardDb db, DateOnly date, CancellationToken ct)
        => await db.WorkDays
            .Include(d => d.Sessions)
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
