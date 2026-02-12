namespace Timecard.Api.Features.Shared;

public sealed record PunchDto(int Id, DateTimeOffset At, string Note);
public sealed record AttendanceRequestDto(int Id, string Category, string Start, string End, string Note);

public sealed record DayDto(
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
    int WorkedMinutes,
    int ExtensionMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate,

    IReadOnlyList<PunchDto> Punches,
    IReadOnlyList<AttendanceRequestDto> AttendanceRequests
);

public sealed record MonthDayDto(
    string Date,
    bool Exists,
    bool IsNonWorkingDay,
    string Note,
    string CalendarKind,
    string CalendarSource,

    int PunchCount,

    int PlannedMinutes,
    int WorkedMinutes,
    int ExtensionMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate,

    int FlexApplied,
    int FlexBankEnd,
    int DeficitMinutes
);

public sealed record MonthDto(
    int Year,
    int Month,
    int FlexBankEnd,
    IReadOnlyList<MonthDayDto> Days
);
