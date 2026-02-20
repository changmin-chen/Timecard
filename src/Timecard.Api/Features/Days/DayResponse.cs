namespace Timecard.Api.Features.Days;

public sealed record PunchDto(int Id, DateTimeOffset At, string Note);
public sealed record AttendanceRequestDto(int Id, string Category, string Start, string End, string Note);

public sealed record DayResponse(
    string Date, // yyyy-MM-dd
    bool Exists,
    bool IsNonWorkingDay,
    string Note,
    string CalendarKind,
    string CalendarSource,

    DateTimeOffset? Start, // derived: earliest punch
    DateTimeOffset? End,   // derived: latest punch (if >=2 punches)
    int PunchCount,

    int PlannedMinutes,
    int PunchedMinutes,
    int EligibleMinutes,
    int AttendanceDeltaMinutes,
    int FlexDeltaMinutes,

    IReadOnlyList<PunchDto> Punches,
    IReadOnlyList<AttendanceRequestDto> AttendanceRequests
);

