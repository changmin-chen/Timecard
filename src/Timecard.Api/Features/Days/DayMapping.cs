using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Features.Calendar;

namespace Timecard.Api.Features.Days;

public static class DayMapping
{
    public static DayDto ToDayDto(DateOnly date, WorkDay? day, ResolvedCalendarDay calendarDay)
    {
        var exists = day is not null;
        var isNonWorking = !calendarDay.IsWorking;
        var note = calendarDay.Note;

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, _) = day?.DerivePunchTimestamps() ?? (null, null, 0);
        var facts = DailySettlementFacts.From(day, isWorkingDay: !isNonWorking);
        var computed = WorkRules.ComputeDay(facts);

        return new DayDto(
        Date: date.ToString("yyyy-MM-dd"),
        Exists: exists,
        IsNonWorkingDay: isNonWorking,
        Note: note,
        CalendarKind: calendarDay.Kind,
        CalendarSource: calendarDay.Source,
        Start: start,
        End: end,
        PunchCount: punches.Count,
        PlannedMinutes: computed.PlannedMinutes,
        PunchedMinutes: computed.PunchedMinutes,
        ExtensionMinutes: computed.GrantedMinutes,
        RecognizedMinutes: computed.RecognizedMinutes,
        AttendanceDeltaMinutes: computed.AttendanceDeltaMinutes,
        FlexBankDeltaMinutes: computed.FlexBankDeltaMinutes,
        Punches: punches.Select(p => new PunchDto(p.Id, p.At, p.Note)).ToList(),
        AttendanceRequests: day?.AttendanceRequests
            .OrderBy(a => a.Start)
            .Select(a => new AttendanceRequestDto(a.Id, a.Category, a.Start.ToString("HH:mm"), a.End.ToString("HH:mm"), a.Note))
            .ToList() ?? []
        );
    }

    public static DayDto ToDayDto(WorkDay day, ResolvedCalendarDay calendarDay)
        => ToDayDto(day.Date, day, calendarDay);
}

