using Timecard.Api.Data.Entities;
using Timecard.Api.Domain;

namespace Timecard.Api.Features.Shared;

public static class Mapping
{
    public static DayDto ToDayDto(DateOnly date, WorkDay? day)
    {
        var exists = day is not null;
        var isNonWorking = day?.IsNonWorkingDay ?? false;
        var note = day?.Note ?? "";

        var planned = isNonWorking ? 0 : WorkRules.PlannedMinutesPerWorkDay;

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, worked) = day?.DeriveSpan() ?? (null, null, 0);

        var extension = day?.CalculateExtensionMinutes() ?? 0;
        var computed = WorkRules.ComputeDay(planned, worked, extension);

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
        ExtensionMinutes: computed.CreditedMinutes,
        EffectiveMinutes: computed.EffectiveMinutes,
        DeltaMinutes: computed.DeltaMinutes,
        FlexCandidate: computed.FlexCandidate,
        Punches: punches.Select(p => new PunchDto(p.Id, p.At, p.Note)).ToList(),
        AttendanceRequests: day?.AttendanceRequests
            .OrderBy(a => a.Start)
            .Select(a => new AttendanceRequestDto(a.Id, a.Category, a.Start.ToString("HH:mm"), a.End.ToString("HH:mm"), a.Note))
            .ToList() ?? []
        );
    }

    public static DayDto ToDayDto(WorkDay day)
        => ToDayDto(day.Date, day);
}
