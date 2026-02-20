using Ardalis.GuardClauses;
using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Features.Calendar;

namespace Timecard.Api.Features.Days;

public static class DayMapping
{
    public static DayResponse ToDayResponse(DateOnly date, WorkDay? day, ResolvedCalendarDay calendarDay)
    {
        if (day is not null && day.Date != date)
            throw new ArgumentException("Day does not match date", nameof(day));
        
        var exists = day is not null;
        var isNonWorking = !calendarDay.IsWorking;
        var note = calendarDay.Note;

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end) = day?.GetPunchTimestamps() ?? (null, null);
        var facts = day is not null
            ? DailySettlementFacts.FromWorkday(day, isWorkingDay: !isNonWorking) 
            : DailySettlementFacts.ForAbsence(date, isWorkingDay: !isNonWorking);
        var computed = FlexTimePolicy.ComputeDay(facts);

        return new DayResponse(
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
        EligibleMinutes: computed.EligibleMinutes,
        AttendanceDeltaMinutes: computed.AttendanceDeltaMinutes,
        FlexDeltaMinutes: computed.FlexDeltaMinutes,
        Punches: punches.Select(p => new PunchDto(p.Id, p.At, p.Note)).ToList(),
        AttendanceRequests: day?.AttendanceRequests
            .OrderBy(a => a.Start)
            .Select(a => new AttendanceRequestDto(a.Id, a.Category, a.Start.ToString("HH:mm"), a.End.ToString("HH:mm"), a.Note))
            .ToList() ?? []
        );
    }
    
    public static DayResponse ToDayResponse(WorkDay day, ResolvedCalendarDay calendarDay)
        => ToDayResponse(day.Date, day, calendarDay);
}

